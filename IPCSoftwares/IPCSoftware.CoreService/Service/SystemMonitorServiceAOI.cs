using IPCSoftware.Communication.External;
using IPCSoftware.Core.Interfaces;
using IPCSoftware.Core.Interfaces.AppLoggerInterface;
using IPCSoftware.Devices.PLC;
using IPCSoftware.Engine;
using IPCSoftware.Services;
using IPCSoftware.Shared.Models;
using IPCSoftware.Shared.Models.ConfigModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IPCSoftware.CoreService.AOI.Service
{
    public class SystemMonitorServiceAOI: SystemMonitorServiceBase
    {
        // --- HEARTBEAT STATE ---
        private bool? _lastPlcPulse = null;      // Nullable to detect first read
        private DateTime _lastPlcChangeTime;     // Last time PLC pulse changed
        private DateTime _lastSuccessfulRead;    // Last time we successfully read the tag
        private bool _ipcPulseState = false;
        private DateTime _lastIpcToggleTime;

        public SystemMonitorServiceAOI(
            PLCClientManager plcManager,
            IDeviceConfigurationService deviceService,
            ExternalInterfaceService extService,
            IAppLogger logger):base(plcManager, deviceService, extService, logger)
        {
            // Initialize any necessary resources or configurations for the AOI system monitor service
            // Initialize timestamps
            ResetHeartbeat();
        }

        public override List<bool> Process(Dictionary<int, object> tagValues)
        {
            try
            {
                bool isPlcConnected = false;

                // =========================================================
                // 1. MONITOR PLC PULSE (PLC -> IPC)
                // =========================================================
                if (tagValues != null && tagValues.Count > 0)
                {
                    _lastSuccessfulRead = DateTime.Now;

                    // Read current value from Tag Dictionary
                    bool currentPlcPulse = GetBool(tagValues, ConstantValues.TAG_Heartbeat_PLC1);

                    // First read initialization
                    if (_lastPlcPulse == null)
                    {
                        _lastPlcPulse = currentPlcPulse;
                        _lastPlcChangeTime = DateTime.Now;
                    }
                    // Detect Toggle (Change in value)
                    else if (currentPlcPulse != _lastPlcPulse.Value)
                    {
                        _lastPlcChangeTime = DateTime.Now;
                        _lastPlcPulse = currentPlcPulse;
                    }

                    // Check Logic: 
                    // 1. Data receiving (Read Timeout)
                    // 2. Value changing (Heartbeat Timeout)
                    double timeSinceLastChange = (DateTime.Now - _lastPlcChangeTime).TotalSeconds;
                    double timeSinceLastRead = (DateTime.Now - _lastSuccessfulRead).TotalSeconds;

                    isPlcConnected = (timeSinceLastRead < READ_TIMEOUT_SECONDS) &&
                                     (timeSinceLastChange < HEARTBEAT_TIMEOUT_SECONDS);
                }
                else
                {
                    // No data received
                    double timeSinceLastRead = (DateTime.Now - _lastSuccessfulRead).TotalSeconds;
                    isPlcConnected = timeSinceLastRead < READ_TIMEOUT_SECONDS;
                }

                // =========================================================
                // 2. SEND IPC PULSE (IPC -> PLC)
                // =========================================================
                double timeSinceLastToggle = (DateTime.Now - _lastIpcToggleTime).TotalSeconds;

                if (timeSinceLastToggle >= IPC_TOGGLE_INTERVAL_SECONDS)
                {
                    // Toggle state
                    _ipcPulseState = !_ipcPulseState;
                    _lastIpcToggleTime = DateTime.Now;

                    // Write to PLC (Fire and Forget)
                    // We write regardless of read status to try and wake up connection
                    _ = WriteTagAsync(ConstantValues.TAG_Heartbeat_IPC_PLC1, _ipcPulseState);

                    // Optional: Log toggle for debugging
                    // Console.WriteLine($"[Heartbeat] IPC Pulse: {_ipcPulseState}");
                }

                // =========================================================
                // 3. RETURN STATUS
                // =========================================================
                // Return a simple list: [PLC_Connected, Time_Synced(Dummy True)]
                // Maintaining structure for Dashboard compatibility
                bool isMacMiniConnected = _extService.IsConnected;
                var statusFlags = new List<bool> { isPlcConnected, true, isMacMiniConnected };
                /*  if (isPlcConnected)
                  {
                   _logger.LogInfo($"PLC Connected", LogType.Audit);
                  }
                  else
                  {
                   _logger.LogInfo($"PLC Not Connected", LogType.Audit);

                  }*/

                return statusFlags;
            }
            catch (Exception ex)
            {
                _logger.LogError($"[SystemMonitor] Error: {ex.Message}", LogType.Diagnostics);
                throw;
            }
        }
        protected override void ResetHeartbeat()
        {
            _lastPlcPulse = null;
            _lastPlcChangeTime = DateTime.Now;
            _lastSuccessfulRead = DateTime.Now;
            _ipcPulseState = false;
            _lastIpcToggleTime = DateTime.Now;
            _logger.LogInfo("[Heartbeat] State reset", LogType.Diagnostics);
        }
    }
}
