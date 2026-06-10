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
    }
}