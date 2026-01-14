//using IPCSoftware.Core.Interfaces;
//using IPCSoftware.Core.Interfaces.AppLoggerInterface;
//using IPCSoftware.CoreService.Services.Logging;
//using IPCSoftware.CoreService.Services.PLC;
//using IPCSoftware.Services;
//using IPCSoftware.Shared;
//using IPCSoftware.Shared.Models;
//using IPCSoftware.Shared.Models.ConfigModels;
//using IPCSoftware.Shared.Models.Logging;
//using Newtonsoft.Json.Linq;
//using System.IO;
//using System.Text.Json;
//using Microsoft.Extensions.Configuration;

//namespace IPCSoftware.CoreService.Services.Dashboard
//{
//    public class OeeEngine : BaseService
//    {
//        private readonly IPLCTagConfigurationService _tagService;
//        private readonly PLCClientManager _plcManager;
//        private readonly IProductionDataLogger _prodLogger;

//        private bool _lastCycleTimeTriggerState = false;
//        private bool _lastCycleStartTriggerState = false; // For Cycle Start/Reset (New)
//        private bool _lastCcdTriggerState = false;

//        private int _lastCycleTime = 1;
//        private readonly string _servoCalibrationPath;

//        // Holds all data for the current 2D code / part
//        private ProductionDataRecord? _currentCycleRecord;

//        // Tracks which station index we are currently at (0..12) for current part
//        private int _currentStationIndex = -1;

//        // For rising-edge detection of TriggerCCD (tag 10)


//        // =======================
//        // Station sequence mapping (from APP/Data JSON)
//        // =======================

//        // SequenceIndex -> PositionId
//        private Dictionary<int, int> _sequenceToPositionId = new();

//        // Flag to ensure we only try loading once
//        private bool _stationMapLoaded = false;

//        // Step counter for CCD triggers within current part (0,1,2,...)
//        private int _currentSequenceStep = 0;


//        public OeeEngine(
//            IPLCTagConfigurationService tagService, 
//            PLCClientManager plcManager,
//            IAppLogger logger,
//            IProductionDataLogger prodLogger,
//            IConfiguration configuration) : base(logger)
//        {
//            _tagService = tagService;
//            _plcManager = plcManager;
//            _prodLogger = prodLogger;

//            var dataFolder = configuration["Config:DataFolder"];
//            var servoFileName = configuration["Config:ServoCalibrationFileName"] ?? "ServoCalibration.json";

//            // Fallbacks if something is missing
//            if (string.IsNullOrWhiteSpace(dataFolder))
//            {
//                dataFolder = AppContext.BaseDirectory;
//            }

//            _servoCalibrationPath = Path.Combine(dataFolder, servoFileName);
//        }

//        public void ProcessCycleTimeLogic(Dictionary<int, object> tagValues)
//        {
//            try
//            {
//                // =========================
//                // 0) Handle CCD Station Step (TriggerCCD tag 10)
//                // =========================
//                bool currentCcdState = GetBoolState(tagValues, ConstantValues.TRIGGER_TAG_ID); // -> 10

//                if (currentCcdState && !_lastCcdTriggerState)
//                {
//                    // Rising edge of TriggerCCD: capture one station's CCD snapshot

//                    // 1) Read 2D code from QR tag (16)
//                    string twoDCode = GetString(tagValues, ConstantValues.TAG_QR_DATA); // -> 16
//                    if (string.IsNullOrWhiteSpace(twoDCode))
//                    {
//                        twoDCode = "NA";
//                    }

//                    // 2) If no current cycle record, or 2D code changed, start a new record
//                    if (_currentCycleRecord == null ||
//                        !string.Equals(_currentCycleRecord.TwoDCode, twoDCode, StringComparison.OrdinalIgnoreCase))
//                    {
//                        _currentCycleRecord = new ProductionDataRecord
//                        {
//                            TwoDCode = twoDCode
//                        };
//                        _currentSequenceStep = 0; // start new sequence for this 2D
//                    }

//                    // 3) Determine which logical station this CCD hit, based on JSON mapping
//                    int step = _currentSequenceStep;      // 0,1,2,...
//                    _currentSequenceStep++;               // prepare for next trigger

//                    int logicalStationId = GetStationIdForStep(step);

//                    // Clamp to Stations array bounds
//                    if (logicalStationId < 0 || logicalStationId >= _currentCycleRecord.Stations.Length)
//                    {
//                        _logger.LogError($"[OEE] Logical station {logicalStationId} out of bounds. Forcing to 0.", LogType.Diagnostics);
//                        logicalStationId = 0;
//                    }

//                    // 4) Read CCD tags
//                    int statusRaw = GetInt(tagValues, ConstantValues.TAG_STATUS);      // 17
//                    string stResult = statusRaw switch
//                    {
//                        1 => "OK",
//                        2 => "NG",
//                        _ => statusRaw.ToString()
//                    };

//                    double? stX = GetDouble(tagValues, ConstantValues.TAG_X);         // 18
//                    double? stY = GetDouble(tagValues, ConstantValues.TAG_Y);         // 19
//                    double? stZ = GetDouble(tagValues, ConstantValues.TAG_Z);         // 20

//                    // 5) Store in the proper station (PositionId)
//                    var station = _currentCycleRecord.Stations[logicalStationId];
//                    station.Result = stResult;
//                    station.X = stX;
//                    station.Y = stY;
//                    station.Z = stZ;

//                    _logger.LogInfo(
//                        $"[CCD] Captured station {logicalStationId} (step {step}) for 2D={_currentCycleRecord.TwoDCode}, Status={stResult}, X={stX}, Y={stY}, Z={stZ}",
//                        LogType.Diagnostics);
//                }

//                // =========================
//                // 1) Handle Cycle Complete (CtlCycleTimeA1 tag 21)
//                // =========================
//                bool currentA1State = GetBoolState(tagValues, ConstantValues.TAG_CTL_CYCLETIME_A1);

//                // Rising edge Detection (0 -> 1)
//                if (currentA1State && !_lastCycleTimeTriggerState)
//                {
//                    _lastCycleTime = GetInt(tagValues, ConstantValues.TAG_CycleTime);
//                    Console.WriteLine($"[CycleTime] A1 Trigger Detected (Tag {ConstantValues.TAG_CTL_CYCLETIME_A1})");
//                    _logger.LogInfo($"[CycleTime] A1 Trigger Detected (Tag {ConstantValues.TAG_CTL_CYCLETIME_A1})", LogType.Diagnostics);

//                    try
//                    {
//                        // 1) Read raw OEE-related values from current tagValues
//                        int operatingMin = GetInt(tagValues, ConstantValues.TAG_UpTime);    // 24
//                        int downTimeMin = GetInt(tagValues, ConstantValues.TAG_DownTime);  // 26
//                        int totalParts = GetInt(tagValues, ConstantValues.TAG_InFlow);    // 27
//                        int okParts = GetInt(tagValues, ConstantValues.TAG_OK);        // 28
//                        int ngParts = GetInt(tagValues, ConstantValues.TAG_NG);        // 29
//                        int idealCycle = GetInt(tagValues, ConstantValues.TAG_CycleTime); // 22 (sec/part)
//                        int actualCycleTime = _lastCycleTime;

//                        // 2) Calculate OEE KPIs (same formulas as in Calculate)
//                        double availability = 0.0;
//                        double quality = 0.0;
//                        double performance = 0.0;
//                        double oee = 0.0;

//                        double totalTimeMin = operatingMin + downTimeMin;

//                        // Availability = Uptime / (Uptime + Downtime)
//                        if (totalTimeMin > 0)
//                        {
//                            availability = (double)operatingMin / totalTimeMin;
//                        }

//                        // Quality = Good / Total
//                        if (totalParts > 0)
//                        {
//                            quality = (double)okParts / totalParts;
//                        }

//                        // Performance = (IdealCycle * TotalProduction) / Uptime(sec)
//                        if (operatingMin > 0 && idealCycle > 0)
//                        {
//                            double operatingSeconds = (double)operatingMin * 60.0;
//                            if (operatingSeconds > 0)
//                            {
//                                performance = ((double)idealCycle * totalParts) / operatingSeconds;
//                            }
//                        }

//                        // OEE = A * P * Q
//                        oee = availability * performance * quality;

//                        // 3) Ensure we have a current cycle record (in case CCD never fired)
//                        if (_currentCycleRecord == null)
//                        {
//                            string twoDCode = GetString(tagValues, ConstantValues.TAG_QR_DATA);
//                            if (string.IsNullOrWhiteSpace(twoDCode))
//                                twoDCode = "NA";

//                            _currentCycleRecord = new ProductionDataRecord
//                            {
//                                TwoDCode = twoDCode
//                            };
//                        }

//                        // 4) Fill OEE + counters into current cycle record
//                        _currentCycleRecord.OEE = oee;
//                        _currentCycleRecord.Availability = availability;
//                        _currentCycleRecord.Performance = performance;
//                        _currentCycleRecord.Quality = quality;

//                        _currentCycleRecord.Total_IN = totalParts;
//                        _currentCycleRecord.OK = okParts;
//                        _currentCycleRecord.NG = ngParts;

//                        _currentCycleRecord.Uptime = operatingMin;
//                        _currentCycleRecord.Downtime = downTimeMin;
//                        _currentCycleRecord.TotalTime = totalTimeMin;
//                        _currentCycleRecord.CT = actualCycleTime;

//                        // 5) Append full record (all captured stations 0..n)
//                        _prodLogger.AppendRecord(_currentCycleRecord);

//                        // 6) Reset for next 2D / part
//                        _currentCycleRecord = null;
//                        _currentStationIndex = -1;
//                        _currentSequenceStep = 0;    // IMPORTANT: reset here, not on every scan
//                    }
//                    catch (Exception exLog)
//                    {
//                        _logger.LogError($"ProductionData CSV log failed: {exLog.Message}", LogType.Diagnostics);
//                    }

//                    // 7) Send Acknowledgement B1 (Tag 23) AFTER logging
//                    _ = WriteTagAsync(ConstantValues.TAG_CTL_CYCLETIME_B1, true);
//                }
//                else if (!currentA1State && _lastCycleTimeTriggerState)
//                {
//                    // Falling edge of A1 -> Reset B1 to ready for next
//                    _ = WriteTagAsync(ConstantValues.TAG_CTL_CYCLETIME_B1, false);
//                }

//                // Update edge states for next scan (only flags, no data reset)
//                _lastCcdTriggerState = currentCcdState;
//                _lastCycleTimeTriggerState = currentA1State;
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex.Message, LogType.Diagnostics);
//            }
//        }


//        private void EnsureStationMapLoaded()
//        {
//            if (_stationMapLoaded)
//                return;

//            try
//            {
//                // We already built the full path in the constructor from appsettings
//                var jsonPath = _servoCalibrationPath;

//                if (!File.Exists(jsonPath))
//                {
//                    _logger.LogError($"[OEE] Station positions JSON not found at: {jsonPath}", LogType.Diagnostics);
//                    _stationMapLoaded = true; // avoid repeated attempts
//                    return;
//                }

//                string json = File.ReadAllText(jsonPath);
//                var positions = JsonSerializer.Deserialize<List<PositionConfigJson>>(json);

//                if (positions == null || positions.Count == 0)
//                {
//                    _logger.LogError("[OEE] Station positions JSON is empty or could not be deserialized.", LogType.Diagnostics);
//                    _stationMapLoaded = true;
//                    return;
//                }

//                // Build SequenceIndex -> PositionId map
//                _sequenceToPositionId = positions
//                    .Where(p => p.SequenceIndex >= 0 && p.PositionId >= 0)
//                    .ToDictionary(p => p.SequenceIndex, p => p.PositionId);

//                _logger.LogInfo(
//                    "[OEE] Loaded station map (Seq->Pos): " +
//                    string.Join(", ", _sequenceToPositionId
//                        .OrderBy(kv => kv.Key)
//                        .Select(kv => $"{kv.Key}->{kv.Value}")),
//                    LogType.Diagnostics);
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError($"[OEE] Failed to load station positions JSON: {ex.Message}", LogType.Diagnostics);
//            }
//            finally
//            {
//                _stationMapLoaded = true;
//            }
//        }


//        private int GetStationIdForStep(int sequenceStep)
//        {
//            // Ensure mapping is loaded
//            EnsureStationMapLoaded();

//            if (_sequenceToPositionId != null &&
//                _sequenceToPositionId.TryGetValue(sequenceStep, out var posId))
//            {
//                return posId; // PositionId 0..12
//            }

//            // Fallback: if config missing, just use the step as station
//            return sequenceStep;
//        }


//        public Dictionary<int, object> Calculate(Dictionary<int, object> values)
//        {
//            try
//            {
//                OeeResult r = new OeeResult();

//                // 1. Extract Raw Values using ConstantValues IDs
//                int operatingMin = GetInt(values, ConstantValues.TAG_UpTime);
//                int downTimeMin = GetInt(values, ConstantValues.TAG_DownTime);
//                int totalParts = GetInt(values, ConstantValues.TAG_InFlow);
//                int okParts = GetInt(values, ConstantValues.TAG_OK);
//                int ngParts = GetInt(values, ConstantValues.TAG_NG);
//                int idealCycle = GetInt(values, ConstantValues.TAG_CycleTime); // Seconds per part
//                int actualCycleTime = GetInt(values, ConstantValues.TAG_CycleTime); // Seconds per part

//                // 2. Availability (A) Calculation
//                // Formula: Uptime / (Uptime + Alarm Stop + Downtime)
//                // Code assumes downTimeMin includes Alarm Stop
//                double totalTimeMin = operatingMin + downTimeMin;

//                r.Availability = 0.0;
//                if (totalTimeMin > 0)
//                {
//                    r.Availability = (double)operatingMin / totalTimeMin;
//                }

//                // 3. Quality (Q) Calculation
//                // Formula: Good Count / Total Count
//                // Note: Yield = Good / Total * 100 (Code returns decimal 0-1)
//                r.Quality = 0.0;
//                if (totalParts > 0)
//                {
//                    r.Quality = (double)okParts / (double)totalParts;
//                }

//                // 4. Performance (P) Calculation
//                // Formula: (Ideal Cycle Time * Total Production) / Uptime
//                // Unit Sync: CycleTime is Seconds, Uptime is Minutes -> Convert Uptime to Seconds
//                r.Performance = 0.0;
//                if (operatingMin > 0 && idealCycle > 0)
//                {
//                    double operatingSeconds = (double)operatingMin * 60.0;

//                    if (operatingSeconds > 0)
//                    {
//                        r.Performance = ((double)idealCycle * totalParts) / operatingSeconds;
//                    }
//                }

//                // 5. Overall OEE
//                // Formula: A * P * Q
//                r.OverallOEE = r.Availability * r.Performance * r.Quality;

//                // 6. Raw values pass-through for UI
//                r.OKParts = okParts;
//                r.NGParts = ngParts;
//                r.OperatingTime = operatingMin;
//                r.Downtime = downTimeMin;
//                r.TotalParts = totalParts;
//                r.CycleTime = _lastCycleTime;





//                // Return as dictionary with ID 4 (OEE_DATA)
//                return new Dictionary<int, object> { { 4, r } };
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex.Message, LogType.Diagnostics);
//                throw;
//            }
//        }

//        // Helper to safely extract int from dictionary using Tag ID
//        private int GetInt(Dictionary<int, object> values, int tagId)
//        {
//            if (values != null && values.TryGetValue((int)tagId, out object val))
//            {
//                try { return Convert.ToInt32(val); }
//                catch (Exception ex)
//                { _logger.LogError(ex.Message, LogType.Diagnostics); return 0; }
//            }
//            return 0;
//        }

//        private string GetString(Dictionary<int, object> values, int tagId)
//        {
//            if (values != null && values.TryGetValue(tagId, out object val) && val != null)
//            {
//                try
//                {
//                    return val.ToString() ?? string.Empty;
//                }
//                catch (Exception ex)
//                {
//                    _logger.LogError(ex.Message, LogType.Diagnostics);
//                    return string.Empty;
//                }
//            }
//            return string.Empty;
//        }


//        private double? GetDouble(Dictionary<int, object> values, int tagId)
//        {
//            if (values != null && values.TryGetValue(tagId, out object val) && val != null)
//            {
//                try
//                {
//                    return Convert.ToDouble(val);
//                }
//                catch (Exception ex)
//                {
//                    _logger.LogError(ex.Message, LogType.Diagnostics);
//                    return null;
//                }
//            }
//            return null;
//        }


//        private bool GetBoolState(Dictionary<int, object> tagValues, int tagId)
//        {
//            if (tagValues.TryGetValue(tagId, out object obj))
//            {
//                if (obj is bool bVal) return bVal;
//                if (obj is int iVal) return iVal > 0;
//            }
//            return false;
//        }

//        private async Task WriteTagAsync(int tagNo, object value)
//        {
//            try
//            {
//                var allTags = await _tagService.GetAllTagsAsync();
//                var tag = allTags.FirstOrDefault(t => t.TagNo == tagNo);
//                if (tag != null)
//                {
//                    var client = _plcManager.GetClient(tag.PLCNo);
//                    if (client != null)
//                    {
//                        await client.WriteAsync(tag, value);
//                        Console.WriteLine($"[CycleTime] Ack Tag {tagNo} set to {value}");
//                        _logger.LogInfo($"[CycleTime] Ack Tag {tagNo} set to {value}", LogType.Error);
//                    }
//                }
//            }
//            catch (Exception ex)
//            {
//                Console.WriteLine($"[Error] CycleTime Write Tag {tagNo}: {ex.Message}");
//                _logger.LogError($"[Error] CycleTime Write Tag {tagNo}: {ex.Message}", LogType.Error);
//            }
//        }

//        private class PositionConfigJson
//        {
//            public int PositionId { get; set; }
//            public string? Name { get; set; }
//            public int SequenceIndex { get; set; }
//            public int X { get; set; }
//            public int Y { get; set; }
//            public string? Description { get; set; }
//        }

//    }
//}

using IPCSoftware.Core.Interfaces;
using IPCSoftware.Core.Interfaces.AppLoggerInterface;
using IPCSoftware.CoreService.Services.Logging;
using IPCSoftware.CoreService.Services.PLC;
using IPCSoftware.Services;
using IPCSoftware.Shared;
using IPCSoftware.Shared.Models;
using IPCSoftware.Shared.Models.ConfigModels;
using IPCSoftware.Shared.Models.Logging;
using Newtonsoft.Json.Linq;
using System.IO;
using System.Text.Json;
using Microsoft.Extensions.Configuration;

namespace IPCSoftware.CoreService.Services.Dashboard
{
    public class OeeEngine : BaseService
    {
        private readonly IPLCTagConfigurationService _tagService;
        private readonly PLCClientManager _plcManager;
        private readonly IProductionDataLogger _prodLogger;

        // Triggers
        private bool _lastCycleTimeTriggerState = false;  // For A1 (Cycle Complete)
        private bool _lastCycleStartTriggerState = false; // For Cycle Start/Reset
        private bool _lastCcdTriggerState = false;        // For CCD Trigger

        private int _lastCycleTime = 1;
        private readonly string _servoCalibrationPath;

        // Holds all data for the current 2D code / part
        private ProductionDataRecord? _currentCycleRecord;

        // Tracks which station index we are currently at (0..12) for current part
        private int _currentStationIndex = -1;

        // SequenceIndex -> PositionId
        private Dictionary<int, int> _sequenceToPositionId = new();
        private bool _stationMapLoaded = false;
        private int _currentSequenceStep = 0;

        public OeeEngine(
            IPLCTagConfigurationService tagService,
            PLCClientManager plcManager,
            IAppLogger logger,
            IProductionDataLogger prodLogger,
            IConfiguration configuration) : base(logger)
        {
            _tagService = tagService;
            _plcManager = plcManager;
            _prodLogger = prodLogger;

            var dataFolder = configuration["Config:DataFolder"];
            var servoFileName = configuration["Config:ServoCalibrationFileName"] ?? "ServoCalibration.json";

            if (string.IsNullOrWhiteSpace(dataFolder))
            {
                dataFolder = AppContext.BaseDirectory;
            }

            _servoCalibrationPath = Path.Combine(dataFolder, servoFileName);
        }

        public void ProcessCycleTimeLogic(Dictionary<int, object> tagValues)
        {
            try
            {
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

                // DETECT RESET (Falling Edge: True -> False)
                if (!currentCycleStartState && _lastCycleStartTriggerState)
                {
                    _logger.LogInfo("[OEE] Cycle Reset Detected (Falling Edge). Finalizing current record as ABORTED.", LogType.Error);

                    // If we have an active record mid-cycle, log it now as 'Aborted'
                    if (_currentCycleRecord != null)
                    {
                        FinalizeAndLogCycle(tagValues, isCycleComplete: false);
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

        private void HandleCcdTrigger(Dictionary<int, object> tagValues)
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
        }

        /// <summary>
        /// Shared logic to calculate OEE, fill the record, save to CSV, and reset memory.
        /// </summary>
        private void FinalizeAndLogCycle(Dictionary<int, object> tagValues, bool isCycleComplete)
        {
            try
            {
                // 1. Ensure record exists
                if (_currentCycleRecord == null)
                {
                    string twoDCode = GetString(tagValues, ConstantValues.TAG_QR_DATA);
                    _currentCycleRecord = new ProductionDataRecord
                    {
                        TwoDCode = string.IsNullOrWhiteSpace(twoDCode) ? "NA" : twoDCode
                    };
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
                    double operatingSeconds = (double)operatingMin * 60.0;
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
        }

        // =========================================================
        // LIVE CALCULATION FOR UI (Called continuously)
        // =========================================================
        public Dictionary<int, object> Calculate(Dictionary<int, object> values)
        {
            try
            {
                OeeResult r = new OeeResult();

                // 1. Extract Raw Values
                int operatingMin = GetInt(values, ConstantValues.TAG_UpTime);
                int downTimeMin = GetInt(values, ConstantValues.TAG_DownTime);
                int totalParts = GetInt(values, ConstantValues.TAG_InFlow);
                int okParts = GetInt(values, ConstantValues.TAG_OK);
                int ngParts = GetInt(values, ConstantValues.TAG_NG);
                int idealCycle = GetInt(values, ConstantValues.TAG_CycleTime);

                double x = GetDouble(values, ConstantValues.TAG_X);
                double y = GetDouble(values, ConstantValues.TAG_Y);
                double z = GetDouble(values, ConstantValues.TAG_Z);

                
                // 2. Availability (A) Calculation
                double totalTimeMin = operatingMin + downTimeMin;
                r.Availability = 0.0;
                if (totalTimeMin > 0)
                {
                    r.Availability = (double)operatingMin / totalTimeMin;
                }

                // 3. Quality (Q) Calculation
                r.Quality = 0.0;
                if (totalParts > 0)
                {
                    r.Quality = (double)okParts / (double)totalParts;
                }

                // 4. Performance (P) Calculation
                r.Performance = 0.0;
                if (operatingMin > 0 && idealCycle > 0)
                {
                    double operatingSeconds = (double)operatingMin * 60.0;
                    if (operatingSeconds > 0)
                    {
                        r.Performance = ((double)idealCycle * totalParts) / operatingSeconds;
                    }
                }

                // 5. Overall OEE
                r.OverallOEE = r.Availability * r.Performance * r.Quality;

                // 6. Raw values pass-through for UI
                r.OKParts = okParts;
                r.NGParts = ngParts;
                r.OperatingTime = operatingMin;
                r.Downtime = downTimeMin;
                r.TotalParts = totalParts;
                r.CycleTime = _lastCycleTime;
                r.XValue = x;
                r.YValue = y;
                r.AngleValue = z;


                // Return as dictionary with ID 4 (OEE_DATA)
                return new Dictionary<int, object> { { 4, r } };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message, LogType.Diagnostics);
                throw;
            }
        }

        // =========================================================
        // HELPERS
        // =========================================================

        private void EnsureStationMapLoaded()
        {
            if (_stationMapLoaded) return;
            try
            {
                var jsonPath = _servoCalibrationPath;
                if (!File.Exists(jsonPath))
                {
                    _stationMapLoaded = true;
                    return;
                }
                string json = File.ReadAllText(jsonPath);
                var positions = JsonSerializer.Deserialize<List<PositionConfigJson>>(json);
                if (positions == null || positions.Count == 0)
                {
                    _stationMapLoaded = true;
                    return;
                }
                _sequenceToPositionId = positions
                    .Where(p => p.SequenceIndex >= 0 && p.PositionId >= 0)
                    .ToDictionary(p => p.SequenceIndex, p => p.PositionId);

                _logger.LogInfo("[OEE] Loaded station map.", LogType.Diagnostics);
            }
            catch (Exception ex)
            {
                _logger.LogError($"[OEE] Failed to load station positions JSON: {ex.Message}", LogType.Diagnostics);
            }
            finally
            {
                _stationMapLoaded = true;
            }
        }

        private int GetStationIdForStep(int sequenceStep)
        {
            EnsureStationMapLoaded();
            if (_sequenceToPositionId != null && _sequenceToPositionId.TryGetValue(sequenceStep, out var posId))
            {
                return posId;
            }
            return sequenceStep;
        }

        private int GetInt(Dictionary<int, object> values, int tagId)
        {
            if (values != null && values.TryGetValue(tagId, out object val))
            {
                try { return Convert.ToInt32(val); }
                catch (Exception ex) { _logger.LogError(ex.Message, LogType.Diagnostics); return 0; }
            }
            return 0;
        }


        private string GetString(Dictionary<int, object> values, int tagId)
        {
            if (values != null && values.TryGetValue(tagId, out object val) && val != null)
            {
                try { return val.ToString() ?? string.Empty; }
                catch (Exception ex) { _logger.LogError(ex.Message, LogType.Diagnostics); return string.Empty; }
            }
            return string.Empty;
        }

        private double GetDouble(Dictionary<int, object> values, int tagId)
        {
            if (values != null && values.TryGetValue(tagId, out object val) && val != null)
            {
                try { return Convert.ToDouble(val); }
                catch (Exception ex) { _logger.LogError(ex.Message, LogType.Diagnostics); return 0.0; }
            }
            return 0.0;
        }

        private bool GetBoolState(Dictionary<int, object> tagValues, int tagId)
        {
            if (tagValues.TryGetValue(tagId, out object obj))
            {
                if (obj is bool bVal) return bVal;
                if (obj is int iVal) return iVal > 0;
            }
            return false;
        }

        private async Task WriteTagAsync(int tagNo, object value)
        {
            try
            {
                var allTags = await _tagService.GetAllTagsAsync();
                var tag = allTags.FirstOrDefault(t => t.TagNo == tagNo);
                if (tag != null)
                {
                    var client = _plcManager.GetClient(tag.PLCNo);
                    if (client != null)
                    {
                        await client.WriteAsync(tag, value);
                        _logger.LogInfo($"[CycleTime] Ack Tag {tagNo} set to {value}", LogType.Diagnostics);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"[Error] CycleTime Write Tag {tagNo}: {ex.Message}", LogType.Diagnostics);
            }
        }

        private class PositionConfigJson
        {
            public int PositionId { get; set; }
            public string? Name { get; set; }
            public int SequenceIndex { get; set; }
            public int X { get; set; }
            public int Y { get; set; }
            public string? Description { get; set; }
        }
    }
}