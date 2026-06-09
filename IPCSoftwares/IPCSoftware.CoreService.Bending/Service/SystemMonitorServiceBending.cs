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

namespace IPCSoftware.CoreService.Bending.Service
{
    public class SystemMonitorServiceBending: SystemMonitorServiceBase
    {
        // --- HEARTBEAT STATE PLC1---
        private bool? _lastPlc1Pulse = null;      // Nullable to detect first read
        private DateTime _lastPlc1ChangeTime;     // Last time PLC pulse changed
        private DateTime _lastPlc1SuccessfulRead;    // Last time we successfully read the tag
        private bool _ipcPlc1PulseState = false;
        private DateTime _lastIpcPlc1ToggleTime;

        // --- HEARTBEAT STATE PLC2---
        private bool? _lastPlc2Pulse = null;      // Nullable to detect first read
        private DateTime _lastPlc2ChangeTime;     // Last time PLC pulse changed
        private DateTime _lastPlc2SuccessfulRead;    // Last time we successfully read the tag
        private bool _ipcPlc2PulseState = false;
        private DateTime _lastIpcPlc2ToggleTime;

        public SystemMonitorServiceBending(
            PLCClientManager plcManager,
            IDeviceConfigurationService deviceService,
            ExternalInterfaceService extService,
            IAppLogger logger) : base(plcManager, deviceService, extService, logger)
        {
            // Initialize any necessary resources or configurations for the AOI system monitor service

            ResetHeartbeat();
        }

        override public List<bool> Process(Dictionary<int, object> tagValues)
        {
            try
            {
                bool isPlc1Connected = false;
                bool isPlc2Connected = false;

                // =========================================================
                // 1. MONITOR PLC PULSE (PLC -> IPC)
                // =========================================================
                if (tagValues != null && tagValues.Count > 0)
                {
                    _lastPlc2SuccessfulRead = _lastPlc1SuccessfulRead = DateTime.Now;

                    // Read current value from Tag Dictionary PLC1
                    bool currentPlc1Pulse = GetBool(tagValues, ConstantValues.TAG_Heartbeat_PLC1);

                    // First read initialization
                    if (_lastPlc1Pulse == null)
                    {
                        _lastPlc1Pulse = currentPlc1Pulse;
                        _lastPlc1ChangeTime = DateTime.Now;
                    }
                    // Detect Toggle (Change in value)
                    else if (currentPlc1Pulse != _lastPlc1Pulse.Value)
                    {
                        _lastPlc1ChangeTime = DateTime.Now;
                        _lastPlc1Pulse = currentPlc1Pulse;
                    }

                    // Read current value from Tag Dictionary PLC2
                    bool currentPlc2Pulse = GetBool(tagValues, ConstantValues.TAG_Heartbeat_PLC2);

                    // First read initialization
                    if (_lastPlc2Pulse == null)
                    {
                        _lastPlc2Pulse = currentPlc2Pulse;
                        _lastPlc2ChangeTime = DateTime.Now;
                    }
                    // Detect Toggle (Change in value)
                    else if (currentPlc2Pulse != _lastPlc2Pulse.Value)
                    {
                        _lastPlc2ChangeTime = DateTime.Now;
                        _lastPlc2Pulse = currentPlc2Pulse;
                    }

                    // Check Logic: 
                    // 1. Data receiving (Read Timeout)
                    // 2. Value changing (Heartbeat Timeout)
                    double timeSinceLastChange = (DateTime.Now - _lastPlc1ChangeTime).TotalSeconds;
                    double timeSinceLastRead = (DateTime.Now - _lastPlc1SuccessfulRead).TotalSeconds;

                    isPlc1Connected = (timeSinceLastRead < READ_TIMEOUT_SECONDS) &&
                                     (timeSinceLastChange < HEARTBEAT_TIMEOUT_SECONDS);

                    timeSinceLastChange = (DateTime.Now - _lastPlc2ChangeTime).TotalSeconds;
                    timeSinceLastRead = (DateTime.Now - _lastPlc2SuccessfulRead).TotalSeconds;

                    isPlc2Connected = (timeSinceLastRead < READ_TIMEOUT_SECONDS) &&
                                     (timeSinceLastChange < HEARTBEAT_TIMEOUT_SECONDS);
                }
                else
                {
                    // No data received
                    double timeSinceLastRead = (DateTime.Now - _lastPlc1SuccessfulRead).TotalSeconds;
                    isPlc1Connected = timeSinceLastRead < READ_TIMEOUT_SECONDS;

                    timeSinceLastRead = (DateTime.Now - _lastPlc2SuccessfulRead).TotalSeconds;
                    isPlc2Connected = timeSinceLastRead < READ_TIMEOUT_SECONDS;
                }

                // =========================================================
                // 2. SEND IPC PULSE (IPC -> PLC1)
                // =========================================================
                double timeSinceLastToggle = (DateTime.Now - _lastIpcPlc1ToggleTime).TotalSeconds;

                if (timeSinceLastToggle >= IPC_TOGGLE_INTERVAL_SECONDS)
                {
                    // Toggle state
                    _ipcPlc1PulseState = !_ipcPlc1PulseState;
                    _lastIpcPlc1ToggleTime = DateTime.Now;

                    // Write to PLC (Fire and Forget)
                    // We write regardless of read status to try and wake up connection
                    _ = WriteTagAsync(ConstantValues.TAG_Heartbeat_IPC_PLC1, _ipcPlc1PulseState);

                    // Optional: Log toggle for debugging
                    // Console.WriteLine($"[Heartbeat] IPC Pulse: {_ipcPulseState}");
                }

                // =========================================================
                // 2. SEND IPC PULSE (IPC -> PLC2)
                // =========================================================
                timeSinceLastToggle = (DateTime.Now - _lastIpcPlc2ToggleTime).TotalSeconds;

                if (timeSinceLastToggle >= IPC_TOGGLE_INTERVAL_SECONDS)
                {
                    // Toggle state
                    _ipcPlc2PulseState = !_ipcPlc2PulseState;
                    _lastIpcPlc2ToggleTime = DateTime.Now;

                    // Write to PLC (Fire and Forget)
                    // We write regardless of read status to try and wake up connection
                    _ = WriteTagAsync(ConstantValues.TAG_Heartbeat_IPC_PLC2, _ipcPlc2PulseState);

                    // Optional: Log toggle for debugging
                    // Console.WriteLine($"[Heartbeat] IPC Pulse: {_ipcPulseState}");
                }

                // =========================================================
                // 3. RETURN STATUS
                // =========================================================
                // Return a simple list: [PLC_Connected, Time_Synced(Dummy True)]
                // Maintaining structure for Dashboard compatibility
                bool isMacMiniConnected = _extService.IsConnected;
                var statusFlags = new List<bool> { isPlc1Connected, isPlc2Connected, isMacMiniConnected };
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
            _lastPlc1Pulse = null;
            _lastPlc1ChangeTime = DateTime.Now;
            _lastPlc1SuccessfulRead = DateTime.Now;
            _ipcPlc1PulseState = false;
            _lastIpcPlc1ToggleTime = DateTime.Now;

            _lastPlc2Pulse = null;
            _lastPlc2ChangeTime = DateTime.Now;
            _lastPlc2SuccessfulRead = DateTime.Now;
            _ipcPlc2PulseState = false;
            _lastIpcPlc2ToggleTime = DateTime.Now;
            _logger.LogInfo("[Heartbeat] State reset", LogType.Diagnostics);
        }

    }
}
