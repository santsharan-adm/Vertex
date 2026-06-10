using IPCSoftware.Core.Interfaces;
using IPCSoftware.Core.Interfaces.AppLoggerInterface;
using IPCSoftware.Datalogger;
using IPCSoftware.Devices.PLC;
using IPCSoftware.Engine;
using IPCSoftware.Shared.Models;
using IPCSoftware.Shared.Models.ConfigModels;
using IPCSoftware.Shared.Models.Logging;
using Microsoft.Extensions.Configuration;
using System.IO;
using System.Text.Json;

namespace IPCSoftware.CoreService.AOI.Service
{
    public class OeeEngineAOI : OeeEngineBase
    {
        // Triggers 
        private bool _lastCycleTimeTriggerState = false;  // For A1 (Cycle Complete)
        private bool _lastCycleStartTriggerState = false; // For Cycle Start/Reset
        private bool _lastCcdTriggerState = false;        // For CCD Trigger

        // True only after the cycle start tag has gone HIGH at least once this session
        private bool _cycleEverStarted = false;

        
        

        

        // Tracks which station index we are currently at (0..12) for current part
        private int _currentStationIndex = -1;

        // SequenceIndex -> PositionId
        protected Dictionary<int, int> _sequenceToPositionId = new();
        private int _currentSequenceStep = 0;
        public OeeEngineAOI(
            IDeviceConfigurationService deviceService,
            PLCClientManager plcManager,
            IAppLogger logger,
            IProductionDataLogger prodLogger,
            IConfiguration configuration)
            : base(deviceService, plcManager, logger, prodLogger, configuration)
        {
        }

        protected override void LoadStationMap()
        {
            try
            {
                var jsonPath = _servoCalibrationPath;
                if (!File.Exists(jsonPath))
                {
                    _logger.LogError($"[OEE] Station positions JSON not found at: {jsonPath}", LogType.Diagnostics);
                    return;
                }
                string json = File.ReadAllText(jsonPath);
                var positions = JsonSerializer.Deserialize<List<ServoPositionModel>>(json);
                if (positions == null || positions.Count == 0)
                {
                    _logger.LogError("[OEE] Station positions JSON is empty or could not be deserialized.", LogType.Diagnostics);
                    return;
                }
                _sequenceToPositionId.Clear();
                foreach (var entry in positions.Where(p => p.SequenceIndex >= 0 && p.PositionId >= 0))
                {
                    _sequenceToPositionId[entry.SequenceIndex] = entry.PositionId;
                }

                _logger.LogInfo("[OEE] Loaded station map successfully.", LogType.Diagnostics);
            }
            catch (Exception ex)
            {
                _logger.LogError($"[OEE] Failed to load station positions JSON: {ex.Message}", LogType.Diagnostics);
            }
        }

        public override void ProcessCycleTimeLogic(Dictionary<int, object> tagValues)
        {
            base.ProcessCycleTimeLogic(tagValues);
            try
            {
                if (IsDryRunMode(tagValues))
                {
                    if (_currentCycleRecord != null)
                    {
                        _logger.LogInfo("[OEE] Dry Run active — clearing current production record.", LogType.Diagnostics);
                        ResetCycleTracking();
                    }
                    return;
                }

                // =========================================================
                // 1. Handle CCD Station Step (TriggerCCD tag 10)
                // =========================================================
                bool currentCcdState = GetBoolState(tagValues, ConstantValues.TRIGGER_TAG_ID); // Tag 10

                if (currentCcdState && !_lastCcdTriggerState)
                {
                    HandleCcdTrigger(tagValues);
                }
                _lastCcdTriggerState = currentCcdState;

                // =========================================================
                // 2. Handle Cycle Start / Reset (CycleStart Trigger - Tag 20)
                // =========================================================
                bool currentCycleStartState = GetBoolState(tagValues, ConstantValues.CYCLE_START_TRIGGER_TAG_ID);

                // DETECT FIRST START (Rising Edge: False -> True)
                if (currentCycleStartState && !_lastCycleStartTriggerState)
                {
                    _cycleEverStarted = true;
                }

                // DETECT RESET (Falling Edge: True -> False)
                if (!currentCycleStartState && _lastCycleStartTriggerState)
                {
                    _logger.LogInfo("[OEE] Cycle Reset Detected (Falling Edge). Finalizing current record as ABORTED.", LogType.Error);

                    // If we have an active record mid-cycle, log it now as 'Aborted'
                    if (_currentCycleRecord != null)
                    {
                        int step = _currentSequenceStep;
                        int maxStations = GetMaxStations();
                        if (step >= maxStations)
                        {
                            FinalizeAndLogCycle(tagValues, isCycleComplete: true);
                        }
                        else
                        {
                            FinalizeAndLogCycle(tagValues, isCycleComplete: false);
                        }
                    }
                }
                _lastCycleStartTriggerState = currentCycleStartState;

                // =========================================================
                // 3. Handle Cycle Complete (CtlCycleTimeA1 tag 21)
                // =========================================================
                bool currentA1State = GetBoolState(tagValues, ConstantValues.TAG_CTL_CYCLETIME_A1);

                // Rising edge Detection (0 -> 1) -> Normal Cycle End
                if (currentA1State && !_lastCycleTimeTriggerState)
                {
                    _logger.LogInfo($"[CycleTime] A1 Trigger Detected (Tag {ConstantValues.TAG_CTL_CYCLETIME_A1})", LogType.Diagnostics);

                    // Normal finish
                    FinalizeAndLogCycle(tagValues, isCycleComplete: true);

                    // Send Acknowledgement B1 (Tag 23)
                    _ = WriteTagAsync(ConstantValues.TAG_CTL_CYCLETIME_B1, true);
                }
                else if (!currentA1State && _lastCycleTimeTriggerState)
                {
                    // Falling edge of A1 -> Reset B1
                    _ = WriteTagAsync(ConstantValues.TAG_CTL_CYCLETIME_B1, false);
                }
                _lastCycleTimeTriggerState = currentA1State;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message, LogType.Diagnostics);
            }
        }

        protected override void HandleCcdTrigger(Dictionary<int, object> tagValues)
        {
            // 1) Read 2D code from QR tag (16)
            string twoDCode = GetString(tagValues, ConstantValues.TAG_QR_DATA);
            if (string.IsNullOrWhiteSpace(twoDCode)) twoDCode = "NA";

            // 2) If no current cycle record, or 2D code changed, start a new record
            if (_currentCycleRecord == null ||
                !string.Equals(_currentCycleRecord.TwoDCode, twoDCode, StringComparison.OrdinalIgnoreCase))
            {
                _currentCycleRecord = new ProductionDataRecord
                {
                    TwoDCode = twoDCode
                };
                _currentSequenceStep = 0; // start new sequence for this 2D
            }

            // 3) Determine which logical station this CCD hit
            int step = _currentSequenceStep;
            int maxStations = GetMaxStations();
            if (step >= maxStations)
            {
                _logger.LogWarning($"[OEE] CCD trigger ignored because step {step} exceeds configured station count ({maxStations}). Awaiting cycle reset (Tag 511).", LogType.Diagnostics);
                return;
            }
            _currentSequenceStep++;

            int logicalStationId = GetStationIdForStep(step);

            // Clamp to Stations array bounds
            if (logicalStationId < 0 || logicalStationId >= _currentCycleRecord.Stations.Length)
            {
                _logger.LogError($"[OEE] Logical station {logicalStationId} out of bounds. Forcing to 0.", LogType.Diagnostics);
                logicalStationId = 0;
            }

            // 4) Read CCD tags
            int statusRaw = GetInt(tagValues, ConstantValues.TAG_STATUS);
            string stResult = statusRaw switch
            {
                1 => "OK",
                2 => "NG",
                _ => statusRaw.ToString()
            };

            double? stX = GetDouble(tagValues, ConstantValues.TAG_X);
            double? stY = GetDouble(tagValues, ConstantValues.TAG_Y);
            double? stZ = GetDouble(tagValues, ConstantValues.TAG_Z);

            // 5) Store in the proper station
            var station = _currentCycleRecord.Stations[logicalStationId];
            station.Result = stResult;
            station.X = stX;
            station.Y = stY;
            station.Z = stZ;

            _logger.LogInfo(
                $"[CCD] Captured station {logicalStationId} (step {step}) for 2D={_currentCycleRecord.TwoDCode}, Status={stResult}",
                LogType.Diagnostics);
            base.HandleCcdTrigger(tagValues);
        }

        protected override void FinalizeAndLogCycle(Dictionary<int, object> tagValues, bool isCycleComplete)
        {
            try
            {
                // 1. Ensure record exists
                if (_currentCycleRecord == null)
                {
                    _logger.LogInfo("[OEE] Cycle finalized but no active record exists (Empty Cycle). Skipping CSV log.", LogType.Diagnostics);
                    return; // EXIT IMMEDIATELY. Do not log ghost rows.

                    /* string twoDCode = GetString(tagValues, ConstantValues.TAG_QR_DATA);
                     _currentCycleRecord = new ProductionDataRecord
                     {
                         TwoDCode = string.IsNullOrWhiteSpace(twoDCode) ? "NA" : twoDCode
                     };*/
                }

                // 2. Read Raw PLC Values
                int operatingMin = GetInt(tagValues, ConstantValues.TAG_UpTime);
                int downTimeMin = GetInt(tagValues, ConstantValues.TAG_DownTime);
                int totalParts = GetInt(tagValues, ConstantValues.TAG_InFlow);
                int okParts = GetInt(tagValues, ConstantValues.TAG_OK);
                int ngParts = GetInt(tagValues, ConstantValues.TAG_NG);
                double idealCycle = ConstantValues.IDEAL_CYCLE_TIME;

                // Get Actual Cycle time (only relevant if cycle completed normally)
                int actualCycleTime = isCycleComplete ? GetInt(tagValues, ConstantValues.TAG_CycleTime) : 0;
                if (isCycleComplete) _lastCycleTime = actualCycleTime;

                // 3. Calculate OEE Logic
                double availability = 0.0;
                double quality = 0.0;
                double performance = 0.0;
                double oee = 0.0;

                double totalTimeMin = operatingMin + downTimeMin;

                if (totalTimeMin > 0) availability = (double)operatingMin / totalTimeMin;
                if (totalParts > 0) quality = (double)okParts / totalParts;

                if (operatingMin > 0 && idealCycle > 0)
                {
                    double operatingSeconds = (double)operatingMin;
                    if (operatingSeconds > 0)
                        performance = ((double)idealCycle * totalParts) / operatingSeconds;
                }

                oee = availability * performance * quality;

                // 4. Fill Record
                _currentCycleRecord.OEE = oee;
                _currentCycleRecord.Availability = availability;
                _currentCycleRecord.Performance = performance;
                _currentCycleRecord.Quality = quality;

                _currentCycleRecord.Total_IN = totalParts;
                _currentCycleRecord.OK = okParts;
                _currentCycleRecord.NG = ngParts;

                _currentCycleRecord.Uptime = operatingMin;
                _currentCycleRecord.Downtime = downTimeMin;
                _currentCycleRecord.TotalTime = totalTimeMin;
                _currentCycleRecord.CT = actualCycleTime;

                // Mark as Aborted if reset
                if (!isCycleComplete)
                {
                    _currentCycleRecord.TwoDCode += " [RESET]";
                }

                // 5. Append Record
                _prodLogger.AppendRecord(_currentCycleRecord);

                // 6. Cleanup Memory
                _currentCycleRecord = null;
                _currentStationIndex = -1;
                _currentSequenceStep = 0;
            }
            catch (Exception exLog)
            {
                _logger.LogError($"[OEE] Finalize Log failed: {exLog.Message}", LogType.Diagnostics);
            }
            base.FinalizeAndLogCycle(tagValues, isCycleComplete);
        }

        private int GetMaxStations()
        {
            if (_sequenceToPositionId != null && _sequenceToPositionId.Count > 0)
            {
                return _sequenceToPositionId.Count;
            }
            // default: stations 0..12 => 13 steps
            return 13;
        }

        private int GetStationIdForStep(int sequenceStep)
        {
            if (_sequenceToPositionId.TryGetValue(sequenceStep, out var posId))
            {
                return posId;
            }
            // Fallback: if config missing, just use the step as station
            return sequenceStep;
        }

        protected override void ResetCycleTracking()
        {
            _currentCycleRecord = null;
            _currentStationIndex = -1;
            _currentSequenceStep = 0;
            base.ResetCycleTracking();
        }
    }
}
