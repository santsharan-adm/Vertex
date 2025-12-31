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

        // --- TIME SYNC STATE ---
        private bool _lastTimeReqState = false;
        private bool _isTimeSynced = true;

        // --- CONFIGURATION ---
        private const double HEARTBEAT_TIMEOUT_SECONDS = 5.0;  // Increased from 3 to 5
        private const double IPC_TOGGLE_INTERVAL_SECONDS = 1.0;
        private const double READ_TIMEOUT_SECONDS = 3.0;       // NEW: Detect if we stop receiving data

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
        /// Processes Heartbeat and Time Sync logic.
        /// CRITICAL: This must be called continuously with live data
        /// </summary>
        public Dictionary<int, object> Process(Dictionary<int, object> tagValues)
        {
            try
            {

                bool isPlcConnected = false;

                if (tagValues != null && tagValues.Count > 0)
                {
                    // We're receiving data - update read timestamp
                    _lastSuccessfulRead = DateTime.Now;

                    // A. Monitor PLC Pulse (PLC -> IPC)
                    bool currentPlcPulse = GetBool(tagValues, ConstantValues.TAG_Heartbeat_PLC);

                    // First read initialization
                    if (_lastPlcPulse == null)
                    {
                        _lastPlcPulse = currentPlcPulse;
                        _lastPlcChangeTime = DateTime.Now;
                        Console.WriteLine($"[Heartbeat] Initial PLC pulse state: {currentPlcPulse}");
                        _logger.LogInfo($"[Heartbeat] Initial PLC pulse state: {currentPlcPulse}", LogType.Diagnostics);
                    }
                    // Detect change
                    else if (currentPlcPulse != _lastPlcPulse.Value)
                    {
                        _lastPlcChangeTime = DateTime.Now;
                        _lastPlcPulse = currentPlcPulse;
                        Console.WriteLine($"[Heartbeat] PLC pulse changed to: {currentPlcPulse}");
                        _logger.LogInfo($"[Heartbeat] PLC pulse changed to: {currentPlcPulse}", LogType.Diagnostics);
                    }

                    // Check connection status with multiple criteria
                    double timeSinceLastChange = (DateTime.Now - _lastPlcChangeTime).TotalSeconds;
                    double timeSinceLastRead = (DateTime.Now - _lastSuccessfulRead).TotalSeconds;

                    // Connected if:
                    // 1. We received data recently (within 3 seconds)
                    // AND
                    // 2. PLC pulse has changed recently (within 5 seconds)
                    isPlcConnected = (timeSinceLastRead < READ_TIMEOUT_SECONDS) &&
                                     (timeSinceLastChange < HEARTBEAT_TIMEOUT_SECONDS);

                    // Debug logging (remove in production)
                    if (!isPlcConnected)
                    {
                        Console.WriteLine($"[Heartbeat] DISCONNECTED - Last change: {timeSinceLastChange:F1}s ago, Last read: {timeSinceLastRead:F1}s ago");
                        _logger.LogInfo($"[Heartbeat] DISCONNECTED - Last change:" +
                            $" {timeSinceLastChange:F1}s ago, Last read: {timeSinceLastRead:F1}s ago", LogType.Diagnostics);
                    }
                }
                else
                {
                    // No data received at all
                    double timeSinceLastRead = (DateTime.Now - _lastSuccessfulRead).TotalSeconds;
                    isPlcConnected = timeSinceLastRead < READ_TIMEOUT_SECONDS;

                    if (!isPlcConnected)
                    {
                        Console.WriteLine($"[Heartbeat] NO DATA - Last read: {timeSinceLastRead:F1}s ago");
                        _logger.LogInfo($"[Heartbeat] NO DATA - Last read: {timeSinceLastRead:F1}s ago", LogType.Diagnostics);
                    }
                }

                // B. Generate IPC Pulse (IPC -> PLC) - Toggle every 1s
                double timeSinceLastToggle = (DateTime.Now - _lastIpcToggleTime).TotalSeconds;
                if (timeSinceLastToggle >= IPC_TOGGLE_INTERVAL_SECONDS)
                {
                    _ipcPulseState = !_ipcPulseState;
                    _lastIpcToggleTime = DateTime.Now;

                    // Only write if PLC is connected
                    if (isPlcConnected)
                    {
                        _ = WriteTagAsync(ConstantValues.TAG_Heartbeat_IPC, _ipcPulseState);
                        Console.WriteLine($"[Heartbeat] IPC pulse toggled to: {_ipcPulseState}");
                        _logger.LogInfo($"[Heartbeat] IPC pulse toggled to: {_ipcPulseState}", LogType.Diagnostics);
                    }
                }

                // ---------------------------------------------------------
                // 2. TIME SYNC LOGIC
                // ---------------------------------------------------------
                bool currentTimeReq = GetBool(tagValues, ConstantValues.TAG_TimeSync_Req);

                // Rising Edge Detection (0 -> 1)
                if (currentTimeReq && !_lastTimeReqState)
                {
                    Console.WriteLine("[TimeSync] Time Sync Requested by PLC.");
                    _logger.LogInfo("[TimeSync] Time Sync Requested by PLC.", LogType.Diagnostics);
                    _ = HandleTimeSyncAsync();
                }

                // Falling Edge Logic (Reset Ack)
                if (!currentTimeReq && _lastTimeReqState)
                {
                    _ = WriteTagAsync(ConstantValues.TAG_TimeSync_Ack, false);
                    Console.WriteLine("[TimeSync] Time request cleared, ACK reset.");
                    _logger.LogInfo("[TimeSync] Time request cleared, ACK reset.", LogType.Diagnostics);
                }

                _lastTimeReqState = currentTimeReq;

                // Return status flags
                var statusFlags = new List<bool> { isPlcConnected, _isTimeSynced };

                return new Dictionary<int, object> { { 1, statusFlags } };
            }
            catch (Exception ex)
            {
                // Log and continue running the service.
                _logger.LogError(ex.Message, LogType.Diagnostics);
                throw;
            }
        }

        private async Task HandleTimeSyncAsync()
        {
            try
            {
                DateTime now = DateTime.Now;

                // Write time data in sequence
                await WriteTagAsync(ConstantValues.TAG_Time_Year.Write, now.Year);
                await WriteTagAsync(ConstantValues.TAG_Time_Month.Write, now.Month);
                await WriteTagAsync(ConstantValues.TAG_Time_Day.Write, now.Day);
                await WriteTagAsync(ConstantValues.TAG_Time_Hour.Write, now.Hour);
                await WriteTagAsync(ConstantValues.TAG_Time_Minute.Write, now.Minute);
                await WriteTagAsync(ConstantValues.TAG_Time_Second.Write, now.Second);

                // Small delay to ensure all writes complete
                await Task.Delay(100);

                // Write acknowledgement
                await WriteTagAsync(ConstantValues.TAG_TimeSync_Ack, true);

                Console.WriteLine($"[TimeSync] Completed: {now:yyyy-MM-dd HH:mm:ss}");
                _logger.LogInfo($"[TimeSync] Completed: {now:yyyy-MM-dd HH:mm:ss}", LogType.Diagnostics);
                _isTimeSynced = true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[TimeSync] FAILED: {ex.Message}");
                _logger.LogError($"[TimeSync] FAILED: {ex.Message}", LogType.Diagnostics);
                _isTimeSynced = false;
            }
        }

        private bool GetBool(Dictionary<int, object> values, int tagId)
        {
            if (values != null && values.TryGetValue(tagId, out object val))
            {
                // Handle different data types
                if (val is bool b) return b;
                if (val is int i) return i > 0;
                if (val is short s) return s > 0;
                if (val is long l) return l > 0;

                // Try parsing string
                if (val is string str)
                {
                    if (bool.TryParse(str, out bool result))
                        return result;
                    if (int.TryParse(str, out int intResult))
                        return intResult > 0;
                }
            }
            return false;
        }

        private async Task WriteTagAsync(int tagNo, object value)
        {
            try
            {
                var allTags = await _tagService.GetAllTagsAsync();
                var tag = allTags.FirstOrDefault(t => t.TagNo == tagNo);

                if (tag == null)
                {
                    Console.WriteLine($"[SystemMonitor] WARNING: Tag {tagNo} not found in configuration");
                    _logger.LogWarning($"[SystemMonitor] WARNING: Tag {tagNo} not found in configuration", LogType.Diagnostics);
                    return;
                }

                var client = _plcManager.GetClient(tag.PLCNo);
                if (client == null || !client.IsConnected)
                {
                    Console.WriteLine($"[SystemMonitor] WARNING: PLC {tag.PLCNo} not connected");
                    _logger.LogWarning($"[SystemMonitor] WARNING: PLC {tag.PLCNo} not connected", LogType.Diagnostics);
                    return;
                }

                await client.WriteAsync(tag, value);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SystemMonitor] Write error for tag {tagNo}: {ex.Message}");
                _logger.LogError($"[SystemMonitor] Write error for tag {tagNo}: {ex.Message}", LogType.Diagnostics);
            }
        }

        /// <summary>
        /// Call this to manually reset the heartbeat state (e.g., after PLC reconnection)
        /// </summary>
        public void ResetHeartbeat()
        {
            _lastPlcPulse = null;
            _lastPlcChangeTime = DateTime.Now;
            _lastSuccessfulRead = DateTime.Now;
            _ipcPulseState = false;
            _lastIpcToggleTime = DateTime.Now;
            Console.WriteLine("[Heartbeat] State reset");
            _logger.LogInfo("[Heartbeat] State reset", LogType.Diagnostics);
        }
    }
}