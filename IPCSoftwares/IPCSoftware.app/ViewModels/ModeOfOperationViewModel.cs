using IPCSoftware.App.NavServices;
using IPCSoftware.App.Services;
using IPCSoftware.App.Services.UI;
using IPCSoftware.App.Views; // For ManualOperation View reference
using IPCSoftware.Core.Interfaces;
using IPCSoftware.Core.Interfaces.AppLoggerInterface;
using IPCSoftware.Shared;
using IPCSoftware.Shared.Models;
using IPCSoftware.Shared.Models.ConfigModels;
using IPCSoftware.Shared.Models.Messaging;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
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

    // Wrapper for Button UI State
    public class ModeButtonItem : ObservableObjectVM
    {
        public OperationMode Mode { get; set; }
        public string Name { get; set; }

        private bool _isEnabled;
        public bool IsEnabled
        {
            get => _isEnabled;
            set => SetProperty(ref _isEnabled, value);
        }

        private bool _isActive;
        public bool IsActive
        {
            get => _isActive;
            set => SetProperty(ref _isActive, value);
        }

        // Colors
        public string BaseColor { get; set; } = "#E0E0E0";    // Normal
        public string ActiveColor { get; set; } = "#00FF00";  // Blinking/Active
    }

    public class ModeOfOperationViewModel : BaseViewModel, IDisposable
    {
        private readonly CoreClient _coreClient;
        private readonly INavigationService _navService;
        private readonly DispatcherTimer _feedbackTimer;

        // --- TAG CONFIGURATION ---
        private readonly Dictionary<OperationMode, int> _tagMap = new()
        {
            { OperationMode.Auto,       11 },
            { OperationMode.DryRun,     12 },
            { OperationMode.CycleStop,  13 },
            { OperationMode.MassRTO,    14 }/*,
            { OperationMode.Manual,     15 }*/
        };

        // --- Properties ---
        public ObservableCollection<ModeButtonItem> ModeButtons { get; }
        public ObservableCollection<AuditLogModel> AuditLogs { get; set; } = new();

        private bool _isMachineHome;
        public bool IsMachineHome
        {
            get => _isMachineHome;
            set => SetProperty(ref _isMachineHome, value);
        }

        public ICommand ButtonClickCommand { get; }

        public ModeOfOperationViewModel(IAppLogger logger, CoreClient coreClient, INavigationService navService) : base(logger)
        {
            _coreClient = coreClient;
            _navService = navService;

            // Initialize Buttons with Client Specific Colors
            ModeButtons = new ObservableCollection<ModeButtonItem>
            {
                new ModeButtonItem
                {
                    Mode = OperationMode.Auto,
                    Name = "Auto Run",
                    IsEnabled = true,
                    BaseColor = "#008B8B",   // Dark Aqua Blue
                    ActiveColor = "#00FFFF"  // Bright Aqua Blue
                },
                new ModeButtonItem
                {
                    Mode = OperationMode.DryRun,
                    Name = "Dry Run",
                    IsEnabled = true,
                    BaseColor = "#8B008B",   // Dark Magenta
                    ActiveColor = "#FF00FF"  // Bright Magenta
                },
                new ModeButtonItem
                {
                    Mode = OperationMode.Manual,
                    Name = "Manual",
                    IsEnabled = true,
                    BaseColor = "#607D8B",   // Blue Grey
                    ActiveColor = "#4CAF50"  // Green
                },
                new ModeButtonItem
                {
                    Mode = OperationMode.CycleStop,
                    Name = "Cycle Stop",
                    IsEnabled = false,       // Initially Disabled
                    BaseColor = "#5D4037",   // Dark Brown
                    ActiveColor = "#D7CCC8"  // Bright Brown/Beige
                },
                new ModeButtonItem
                {
                    Mode = OperationMode.MassRTO,
                    Name = "Mass RTO",
                    IsEnabled = true,
                    BaseColor = "#795548",
                    ActiveColor = "#FF5722"
                }
            };

            ButtonClickCommand = new RelayCommand<ModeButtonItem>(OnButtonClicked);

            _feedbackTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
            _feedbackTimer.Tick += FeedbackLoop_Tick;
            _feedbackTimer.Start();
        }

        private async void OnButtonClicked(ModeButtonItem item)
        {
            try
            {
                // 1. Manual Screen Logic
                if (item.Mode == OperationMode.Manual)
                {
                    // "When Manual mode is selected then Manual Screen should open else it should not open"
                    // Only navigate if enabled (handled by UpdateUiLogic)
                    _logger.LogInfo("Navigating to Manual Screen", LogType.Audit);
                    _navService.NavigateMain<ManualOperation>();
                    return;
                }

                // 2. Cycle Stop Logic
                if (item.Mode == OperationMode.CycleStop)
                {
                    _logger.LogInfo("Operator pressed Cycle Stop. Reseting Auto/Dry...", LogType.Audit);

                    // Client Req: "When cycle stop is pressed auto and dry mode should be zero"
                    // We send stop command (1) to CycleStop Tag, AND 0 to Auto/Dry tags to unlatch them.
                    await WriteTagToPlc(_tagMap[OperationMode.CycleStop], 1);
                    await WriteTagToPlc(_tagMap[OperationMode.Auto], 0);
                    await WriteTagToPlc(_tagMap[OperationMode.DryRun], 0);

                    //await WriteTagToPlc(_tagMap[OperationMode.CycleStop], 0);

                    AddAudit("Cycle Stop Initiated. Waiting for Home...");
                    return;
                }

                // 3. Auto / Dry Run / Mass RTO Toggle Logic
                if (_tagMap.TryGetValue(item.Mode, out int tagId))
                {
                    // Toggle: If Active -> Send 0 (Stop). If Inactive -> Send 1 (Start).
                    int valueToSend = item.IsActive ? 0 : 1;

                    _logger.LogInfo($"Requesting {item.Mode}: {valueToSend}", LogType.Audit);
                    await WriteTagToPlc(tagId, valueToSend);

                    string action = valueToSend == 1 ? "started" : "stopped";
                    AddAudit($"Operator {action} {item.Name}.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Button Click Error: {ex.Message}", LogType.Diagnostics);
            }
        }

        private async void FeedbackLoop_Tick(object? sender, EventArgs e)
        {
            try
            {
                var liveData = await _coreClient.GetIoValuesAsync(5);
                if (liveData == null) return;

                bool anyActive = false;

                // 1. Update status of all buttons from PLC
                foreach (var btn in ModeButtons)
                {
                    if (_tagMap.TryGetValue(btn.Mode, out int tagId))
                    {
                        if (liveData.TryGetValue(tagId, out object? val))
                        {
                            bool state = Convert.ToBoolean(val);
                            btn.IsActive = state;
                            if (state) anyActive = true;
                        }
                    }
                }

                // 2. Update Home Indication
                // "Home position means value of all tag should be zero"
                IsMachineHome = !anyActive;

                // 3. Enforce Interlocks (Enable/Disable logic)
                UpdateUiLogic();
            }
            catch (Exception ex)
            {
                // Suppress loop errors
                System.Diagnostics.Debug.WriteLine($"Feedback Error: {ex.Message}");
            }
        }

        private void UpdateUiLogic()
        {
            var autoBtn = GetBtn(OperationMode.Auto);
            var dryBtn = GetBtn(OperationMode.DryRun);
            var manualBtn = GetBtn(OperationMode.Manual);
            var stopBtn = GetBtn(OperationMode.CycleStop);
            var rtoBtn = GetBtn(OperationMode.MassRTO);

            // Client Req: "If Auto Mode or Dry Run is selected then Cycle Stop should be enabled 
            // and remaining buttons should be in disabled state"
            bool isRunning = autoBtn.IsActive || dryBtn.IsActive;
            bool isCycleStopping = stopBtn.IsActive;

            if (isCycleStopping)
            {
                // Cycle Stop is active -> Disable everything until it finishes (Home condition achieved)
                // "Cycle Stop button will return to home after completing the running cycle"
                // The PLC will turn off the CycleStop bit when done, triggering the 'else' block below.
                autoBtn.IsEnabled = false;
                dryBtn.IsEnabled = false;
                manualBtn.IsEnabled = false;
                rtoBtn.IsEnabled = false;
                stopBtn.IsEnabled = false;
            }
            else if (isRunning)
            {
                // Auto or Dry is ON
                autoBtn.IsEnabled = autoBtn.IsActive; // Can stop itself
                dryBtn.IsEnabled = dryBtn.IsActive;   // Can stop itself
                manualBtn.IsEnabled = false;          // Disabled during run
                rtoBtn.IsEnabled = false;             // Disabled during run

                stopBtn.IsEnabled = true;             // Cycle Stop ENABLED
            }
            else
            {
                // Home Condition (Nothing active)
                autoBtn.IsEnabled = true;
                dryBtn.IsEnabled = true;
                manualBtn.IsEnabled = true;
                rtoBtn.IsEnabled = true;

                stopBtn.IsEnabled = false;            // Disabled at Home
            }
        }

        private ModeButtonItem GetBtn(OperationMode mode) => ModeButtons.First(b => b.Mode == mode);

        private async Task WriteTagToPlc(int tagId, object value)
        {
            try { await _coreClient.WriteTagAsync(tagId, value); }
            catch (Exception ex) { _logger.LogError($"Write Error: {ex.Message}", LogType.Diagnostics); }
        }

        private void AddAudit(string message)
        {
            if (AuditLogs.Count > 100) AuditLogs.RemoveAt(0);
            AuditLogs.Add(new AuditLogModel { Time = DateTime.Now.ToString("HH:mm:ss"), Message = message });
            _logger.LogInfo(message, LogType.Audit);
        }

        public void Dispose()
        {
            _feedbackTimer.Stop();
        }
    }
}