using IPCSoftware.Core.Interfaces;
using IPCSoftware.Core.Interfaces.AppLoggerInterface;
using IPCSoftware.Datalogger;
using IPCSoftware.Devices.PLC;
using IPCSoftware.Engine;
using IPCSoftware.Shared;
using IPCSoftware.Shared.Models;
using IPCSoftware.Shared.Models.ConfigModels;
using IPCSoftware.Shared.Models.Logging;
using Microsoft.Extensions.Configuration;

namespace IPCSoftware.CoreService.Bending.Service
{
    public class OeeEngineBending : OeeEngineBase
    {
        private bool _lastCycleTimeTriggerState = false;  // For A1 (Cycle Complete)
        public OeeEngineBending(
            IDeviceConfigurationService deviceService,
            PLCClientManager plcManager,
            IAppLogger logger,
            IProductionDataLogger prodLogger,
            IConfiguration configuration)
            : base(deviceService, plcManager, logger, prodLogger, configuration)
        {

        }

        // Override Calculate() so that Request ID 4 returns Bending OEE (Dashboard2Result)
        // matching the same contract as AOI — both use key 4 in the response dictionary.
        //public override Dictionary<int, object> Calculate(Dictionary<int, object> values)
        //{
        //    // CalculateDashboard2 now returns { 4, Dashboard2Result } directly.
        //    return CalculateDashboard2(values);
        //}

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
                // 3. Handle Cycle Complete (CtlCycleTimeA1 tag 55)
                // =========================================================
                bool currentA1State = GetBoolState(tagValues, ConstantValues.TAG_CTL_CYCLETIME_A1);

                // Rising edge Detection (0 -> 1) -> Normal Cycle End
                if (currentA1State && !_lastCycleTimeTriggerState)
                {
                    _logger.LogInfo($"[CycleTime] A1 Trigger Detected (Tag {ConstantValues.TAG_CTL_CYCLETIME_A1})", LogType.Diagnostics);

                    // Normal finish
                    FinalizeAndLogCycle(tagValues, isCycleComplete: true);

                    // Send Acknowledgement B1 (Tag 57)
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
            }
            catch (Exception exLog)
            {
                _logger.LogError($"[OEE] Finalize Log failed: {exLog.Message}", LogType.Diagnostics);
            }
            base.FinalizeAndLogCycle(tagValues, isCycleComplete);
        }
    }
}