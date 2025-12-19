using IPCSoftware.App.Services; // Ensure CoreClient is accessible
using IPCSoftware.App.Services.UI;
using IPCSoftware.Core.Interfaces.AppLoggerInterface;
using IPCSoftware.Shared;
using IPCSoftware.Shared.Models;
using IPCSoftware.Shared.Models.ConfigModels;
using IPCSoftware.Shared.Models.Messaging;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Threading;

namespace IPCSoftware.App.ViewModels
{
    public enum OperationMode
    {
        Auto,
        DryRun,
        Manual,
        CycleStop,
        MassRTO
    }

    public class ModeOfOperationViewModel : BaseViewModel, IDisposable
    {
        private readonly CoreClient _coreClient;
        private readonly DispatcherTimer _feedbackTimer;

        // --- TAG CONFIGURATION ---
        // Added 'Manual' with Tag ID 15
        private readonly Dictionary<OperationMode, int> _writeTagMap = new()
        {
            { OperationMode.Auto,       11 },
            { OperationMode.DryRun,     12 },
            { OperationMode.CycleStop,  13 },
            { OperationMode.MassRTO,    14 },
            { OperationMode.Manual,     15 }
        };

        // Status IDs (Reading) - Assumed to match Writing IDs based on your setup
        private readonly Dictionary<OperationMode, int> _readStatusMap = new()
        {
            { OperationMode.Auto,       11 },
            { OperationMode.DryRun,     12 },
            { OperationMode.CycleStop,  13 },
            { OperationMode.MassRTO,    14 },
            { OperationMode.Manual,     15 }
        };

        private OperationMode? _selectedButton;
        public OperationMode? SelectedButton
        {
            get => _selectedButton;
            set
            {
                if (_selectedButton != value)
                {
                    _selectedButton = value;
                    OnPropertyChanged();
                }
            }
        }

        public ObservableCollection<AuditLogModel> AuditLogs { get; set; }
        public ObservableCollection<OperationMode> Modes { get; }
        public ICommand ButtonClickCommand { get; }

        public ModeOfOperationViewModel(IAppLogger logger, CoreClient coreClient) : base(logger) // Removed 'base(logger)' if BaseViewModel uses Static Log
        {
            _coreClient = coreClient;
            // Note: If BaseViewModel uses Static Log.cs, we don't need to pass 'logger' to base.
            // If it uses constructor injection, pass it. Assuming Static based on previous context.

            Modes = new ObservableCollection<OperationMode>
            {
                OperationMode.Auto,
                OperationMode.DryRun,
                OperationMode.Manual,
                OperationMode.CycleStop,
                OperationMode.MassRTO
            };

            AuditLogs = new ObservableCollection<AuditLogModel>();
            ButtonClickCommand = new RelayCommand<object>(OnButtonClicked);

            // Start polling for feedback
            _feedbackTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
            _feedbackTimer.Tick += FeedbackLoop_Tick;
            _feedbackTimer.Start();
        }

        /// <summary>
        /// USER ACTION: Sends Toggle Command (1 or 0)
        /// </summary>
        private async void OnButtonClicked(object? param)
        {
            try
            {
                if (param is OperationMode mode)
                {
                    if (_writeTagMap.TryGetValue(mode, out int tagId))
                    {
                        // TOGGLE LOGIC:
                        // If the clicked mode IS the currently Selected (Green) mode -> Write 0 (Turn Off)
                        // If it is a different mode (Gray) -> Write 1 (Turn On)
                        bool isTurningOff = (SelectedButton == mode);
                        int valueToSend = isTurningOff ? 0 : 1;

                        _logger.LogInfo($"User Request: Set {mode} to {valueToSend} (Tag {tagId})", LogType.Audit);

                        await WriteTagToPlc(tagId, valueToSend);

                        string action = isTurningOff ? "stopped" : "started";
                        AddAudit($"Operator {action} {mode} mode.");
                    }
                    else
                    {
                       _logger.LogWarning($"No Tag ID configured for {mode}", LogType.Audit);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in button click: {ex.Message}", LogType.Diagnostics);
            }
        }

        /// <summary>
        /// FEEDBACK LOOP: Polling PLC Status to update UI Color
        /// </summary>
        private async void FeedbackLoop_Tick(object? sender, EventArgs e)
        {
            try
            {
                // 1. Request IO Values (Using ID 5 for General IOs)
                var liveData = await _coreClient.GetIoValuesAsync(5);

                if (liveData == null) return;

                // 2. Determine which mode is active on the PLC
                OperationMode? activeModeFromPlc = null;

                foreach (var kvp in _readStatusMap)
                {
                    OperationMode mode = kvp.Key;
                    int readTagId = kvp.Value;

                    if (liveData.TryGetValue(readTagId, out object? val))
                    {
                        // Check if bit is High (1 or True)
                        if (Convert.ToBoolean(val) == true)
                        {
                            activeModeFromPlc = mode;
                            // Assuming mutual exclusivity (only 1 can be true), we break on first find.
                            // If Manual (15) and Auto (11) can be on together, logic needs adjustment.
                            break;
                        }
                    }
                }

                // 3. Update UI only if state changed (Reduces flicker)
                if (SelectedButton != activeModeFromPlc)
                {
                    SelectedButton = activeModeFromPlc;

                    if (activeModeFromPlc.HasValue)
                    {
                        // Optional: Log confirmation from PLC
                        // AddAudit($"PLC Confirmed Mode: {activeModeFromPlc}"); 
                    }
                }
            }
            catch (Exception ex)
            {
                // Suppress excessive loop logs
            _logger.LogError($"Feedback Loop Error: {ex.Message}", LogType.Diagnostics);
            }
        }

        private async Task WriteTagToPlc(int tagId, object value)
        {
            try
            {
                await _coreClient.WriteTagAsync(tagId, value);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Write Error: {ex.Message}", LogType.Diagnostics);
            }
        }

        private void AddAudit(string message)
        {
            if (AuditLogs.Count > 100) AuditLogs.RemoveAt(0);

            AuditLogs.Add(new AuditLogModel
            {
                Time = DateTime.Now.ToString("HH:mm:ss"),
                Message = message
            });

            // Log to file as well
            _logger.LogInfo(message, LogType.Audit);
        }

        public void Dispose()
        {
            _feedbackTimer.Stop();
        }
    }
}