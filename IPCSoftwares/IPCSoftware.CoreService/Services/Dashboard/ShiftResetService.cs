using IPCSoftware.Core.Interfaces;
using IPCSoftware.Core.Interfaces.AppLoggerInterface;
using IPCSoftware.CoreService.Services.PLC;
using IPCSoftware.Services;
using IPCSoftware.Shared;
using IPCSoftware.Shared.Models;
using IPCSoftware.Shared.Models.ConfigModels;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace IPCSoftware.CoreService.Services.Dashboard
{
    public class ShiftResetService : BaseService
    {
        private readonly PLCClientManager _plcManager;
        private readonly IPLCTagConfigurationService _tagService;
        private readonly string _shiftCsvPath;

        // --- SHIFT STATE ---
        private List<ShiftConfigurationModel> _shifts = new();
        private DateTime _lastShiftLoadTime = DateTime.MinValue;
        private DateTime _lastAutoResetTriggerTime = DateTime.MinValue;

        // --- RESET SEQUENCE STATE MACHINE ---
        private enum AutoResetState
        {
            Idle,
            Triggering,   // Step 1: Write Reset = 1
            WaitingForAck // Step 2: Wait for Ack = 1
        }

        private AutoResetState _resetState = AutoResetState.Idle;
        private DateTime _resetTimeoutStart;

        public ShiftResetService(
            PLCClientManager plcManager,
            IPLCTagConfigurationService tagService,
            IOptions<ConfigSettings> configSettings,
            IAppLogger logger) : base(logger)
        {
            _plcManager = plcManager;
            _tagService = tagService;

            // Define path to CSV
            var config = configSettings.Value;
            string dataFolder = config.DataFolder ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data");
            _shiftCsvPath = Path.Combine(dataFolder, "Shifts.csv");
        }

        /// <summary>
        /// Main Processing Loop. Call this from CoreService.
        /// </summary>
        public void Process(Dictionary<int, object> tagValues)
        {
            try
            {
                // 1. Reload Shifts every minute (to pick up config changes)
                if ((DateTime.Now - _lastShiftLoadTime).TotalMinutes > 1)
                {
                    LoadShiftsFromCsv();
                    _lastShiftLoadTime = DateTime.Now;
                }

                // 2. Execute Logic
                CheckShiftTimes();
                ExecuteResetSequence(tagValues);
            }
            catch (Exception ex)
            {
                _logger.LogError($"[ShiftResetService] Process Error: {ex.Message}", LogType.Diagnostics);
            }
        }

        private void CheckShiftTimes()
        {
            // Only check time if we are IDLE (not currently resetting)
            if (_resetState != AutoResetState.Idle) return;

            DateTime now = DateTime.Now;

            // Safety: Don't trigger multiple times in the same minute
            // If we triggered less than 65 seconds ago, skip logic.
            if ((now - _lastAutoResetTriggerTime).TotalSeconds < 65) return;

            foreach (var shift in _shifts)
            {
                if (!shift.IsActive) continue;

                // --- ROBUST TIME CHECK ---
                // We compare Hours and Minutes directly. 
                // We verify 'Second < 5' to catch the start of the minute.
                if (now.Hour == shift.StartTime.Hours &&
                    now.Minute == shift.StartTime.Minutes &&
                    now.Second < 5)
                {
                    _logger.LogInfo($"[ShiftReset] New Shift '{shift.ShiftName}' Started at {shift.StartTime}. Triggering Auto Reset.", LogType.Audit);

                    // Start the Sequence
                    _resetState = AutoResetState.Triggering;
                    _lastAutoResetTriggerTime = now;
                    break;
                }
            }
        }

        private void ExecuteResetSequence(Dictionary<int, object> tagValues)
        {
            if (_resetState == AutoResetState.Idle) return;

            switch (_resetState)
            {
                case AutoResetState.Triggering:
                    // Step A: Write Reset Tag (B26) -> TRUE
                    _ = WriteTagAsync(ConstantValues.RESET_TAG_ID, true);

                    _logger.LogInfo("[ShiftReset] Trigger sent (B26=1). Waiting for Ack.", LogType.Error);

                    _resetTimeoutStart = DateTime.Now;
                    _resetState = AutoResetState.WaitingForAck;
                    break;

                case AutoResetState.WaitingForAck:
                    // Step B: Check for Ack Tag (A27) in live data
                    bool ackReceived = GetBool(tagValues, ConstantValues.RESET_ACK_TAG_ID);

                    if (ackReceived)
                    {
                        _logger.LogInfo("[ShiftReset] Ack Received (A27=1). Resetting B26 to 0.", LogType.Audit);

                        // Step C: Write Reset Tag (B26) -> FALSE
                        _ = WriteTagAsync(ConstantValues.RESET_TAG_ID, false);

                        _resetState = AutoResetState.Idle; // Done
                    }
                    else
                    {
                        // Step D: Timeout Handler (e.g. 5 Seconds)
                        if ((DateTime.Now - _resetTimeoutStart).TotalSeconds > 5)
                        {
                            _logger.LogError("[ShiftReset] Timeout waiting for PLC Ack. Forcing Reset OFF.", LogType.Diagnostics);

                            // Force OFF for safety
                            _ = WriteTagAsync(ConstantValues.RESET_TAG_ID, false);

                            _resetState = AutoResetState.Idle;
                        }
                    }
                    break;
            }
        }

        // --- HELPERS ---

        private void LoadShiftsFromCsv()
        {
            try
            {
                if (!File.Exists(_shiftCsvPath)) return;

                var lines = File.ReadAllLines(_shiftCsvPath);
                _shifts.Clear();

                // Skip Header (Index 0)
                foreach (var line in lines.Skip(1))
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;
                    var parts = line.Split(','); // Simple CSV split

                    // Expected CSV format: Id,Name,Start,End,Active
                    if (parts.Length >= 5)
                    {
                        try
                        {
                            _shifts.Add(new ShiftConfigurationModel
                            {
                                // Remove quotes from Name if present
                                ShiftName = parts[1].Trim('"'),
                                StartTime = TimeSpan.Parse(parts[2]),
                                IsActive = bool.Parse(parts[4])
                            });
                        }
                        catch { /* Ignore bad lines */ }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"[ShiftReset] CSV Load Error: {ex.Message}", LogType.Diagnostics);
            }
        }

        private bool GetBool(Dictionary<int, object> values, int tagId)
        {
            if (values != null && values.TryGetValue(tagId, out object val))
            {
                if (val is bool b) return b;
                if (val is int i) return i > 0;
            }
            return false;
        }

        private async Task WriteTagAsync(int tagNo, object value)
        {
            try
            {
                var allTags = await _tagService.GetAllTagsAsync();
                var tag = allTags.FirstOrDefault(t => t.TagNo == tagNo);

                if (tag == null) return;

                var client = _plcManager.GetClient(tag.PLCNo);
                if (client != null && client.IsConnected)
                {
                    await client.WriteAsync(tag, value);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"[ShiftReset] Write Error Tag {tagNo}: {ex.Message}", LogType.Diagnostics);
            }
        }
    }
}