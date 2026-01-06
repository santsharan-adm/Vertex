using IPCSoftware.Core.Interfaces;
using IPCSoftware.Core.Interfaces.AppLoggerInterface;
using IPCSoftware.CoreService.Services.PLC;
using IPCSoftware.Services;
using IPCSoftware.Shared.Models;
using IPCSoftware.Shared.Models.ConfigModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IPCSoftware.CoreService.Services.Dashboard
{
    public class SystemMonitorService : BaseService
    {
        private readonly PLCClientManager _plcManager;
        private readonly IPLCTagConfigurationService _tagService;

        // --- HEARTBEAT STATE ---
        private bool? _lastPlcPulse = null;      // Nullable to detect first read
        private DateTime _lastPlcChangeTime;     // Last time PLC pulse changed
        private DateTime _lastSuccessfulRead;    // Last time we successfully read the tag
        private bool _ipcPulseState = false;
        private DateTime _lastIpcToggleTime;

        // --- CONFIGURATION ---
        // Description says: "abnormal if no change for 3 s"
        private const double HEARTBEAT_TIMEOUT_SECONDS = 3.0;
        private const double IPC_TOGGLE_INTERVAL_SECONDS = 1.0;
        private const double READ_TIMEOUT_SECONDS = 3.0;

        public SystemMonitorService(
            PLCClientManager plcManager,
            IPLCTagConfigurationService tagService,
            IAppLogger logger) : base(logger)
        {
            _plcManager = plcManager;
            _tagService = tagService;

            // Initialize timestamps
            _lastPlcChangeTime = DateTime.Now;
            _lastSuccessfulRead = DateTime.Now;
            _lastIpcToggleTime = DateTime.Now;
        }

        /// <summary>
        /// Processes Heartbeat Logic Only.
        /// 1. Monitors PLC Pulse (PLC -> IPC)
        /// 2. Sends IPC Pulse (IPC -> PLC)
        /// </summary>
        public Dictionary<int, object> Process(Dictionary<int, object> tagValues)
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
                    bool currentPlcPulse = GetBool(tagValues, ConstantValues.TAG_Heartbeat_PLC);

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
                    _ = WriteTagAsync(ConstantValues.TAG_Heartbeat_IPC, _ipcPulseState);

                    // Optional: Log toggle for debugging
                    // Console.WriteLine($"[Heartbeat] IPC Pulse: {_ipcPulseState}");
                }

                // =========================================================
                // 3. RETURN STATUS
                // =========================================================
                // Return a simple list: [PLC_Connected, Time_Synced(Dummy True)]
                // Maintaining structure for Dashboard compatibility
                var statusFlags = new List<bool> { isPlcConnected, true };
              /*  if (isPlcConnected)
                {
                 _logger.LogInfo($"PLC Connected", LogType.Audit);
                }
                else
                {
                 _logger.LogInfo($"PLC Not Connected", LogType.Audit);

                }*/

                    return new Dictionary<int, object> { { 1, statusFlags } };
            }
            catch (Exception ex)
            {
                _logger.LogError($"[SystemMonitor] Error: {ex.Message}", LogType.Diagnostics);
                throw;
            }
        }

        private bool GetBool(Dictionary<int, object> values, int tagId)
        {
            if (values != null && values.TryGetValue(tagId, out object val))
            {
                if (val is bool b) return b;
                if (val is int i) return i > 0;
                if (val is short s) return s > 0;
                if (val is string str)
                {
                    if (bool.TryParse(str, out bool result)) return result;
                    if (int.TryParse(str, out int intResult)) return intResult > 0;
                }
            }
            return false;
        }

        private async Task WriteTagAsync(int tagNo, object value)
        {
            try
            {
                // Retrieve Tag Info
                var allTags = await _tagService.GetAllTagsAsync();
                var tag = allTags.FirstOrDefault(t => t.TagNo == tagNo);

                if (tag == null) return;

                // Get Client
                var client = _plcManager.GetClient(tag.PLCNo);
                if (client != null && client.IsConnected)
                {
                    await client.WriteAsync(tag, value);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"[SystemMonitor] Write Error Tag {tagNo}: {ex.Message}", LogType.Diagnostics);
            }
        }

        public void ResetHeartbeat()
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