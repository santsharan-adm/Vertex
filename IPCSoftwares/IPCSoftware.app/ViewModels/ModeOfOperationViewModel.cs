using IPCSoftware.App.NavServices;
using IPCSoftware.App.Services;
using IPCSoftware.App.Services.UI;
using IPCSoftware.App.Views;
using IPCSoftware.Core.Interfaces;
using IPCSoftware.Core.Interfaces.AppLoggerInterface;
using IPCSoftware.Shared;
using IPCSoftware.Shared.Models;
using IPCSoftware.Shared.Models.ConfigModels;
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

    public class ModeButtonItem : ObservableObjectVM
    {
        public OperationMode Mode { get; set; }
        public string Name { get; set; }

        private bool _isEnabled = true;
        public bool IsEnabled
        {
            get => _isEnabled;
            // Only notify if value actually changes to prevent UI flickering
            set { if (_isEnabled != value) SetProperty(ref _isEnabled, value); }
        }

        private bool _isActive;
        public bool IsActive
        {
            get => _isActive;
            set => SetProperty(ref _isActive, value);
        }

        private bool _isBlinking;
        public bool IsBlinking
        {
            get => _isBlinking;
            set => SetProperty(ref _isBlinking, value);
        }

        public string BaseColor { get; set; }
        public string ActiveColor { get; set; }
    }


    public class ModeOfOperationViewModel : BaseViewModel, IDisposable
    {
        private readonly CoreClient _coreClient;
        private readonly INavigationService _navService;
        private readonly DispatcherTimer _feedbackTimer;

        // --- 1. WRITE TAGS (IPC -> PLC) ---
        private const int TAG_WRITE_AUTO = 11;
        private const int TAG_WRITE_DRY_RUN = 12;
        private const int TAG_WRITE_CYCLE_STOP = 13;
        private const int TAG_WRITE_MASS_RTO = 14;

        // --- 2. READ TAGS (STATUS/BLINKING) (PLC -> IPC) ---
        private const int TAG_READ_AUTO_LAUNCH = 481;
        private const int TAG_READ_DRY_LAUNCH = 482;
        private const int TAG_READ_STOP_ACTIVE = 483;
        private const int TAG_READ_HOME_LAMP = 484;

        // --- 3. READ TAGS (ENABLE/DISABLE VALIDATION) (PLC -> IPC) ---
        private const int TAG_ENABLE_AUTO = 477; // CD_DATA_A2
        private const int TAG_ENABLE_DRY_RUN = 478; // CD_DATA_A3
        private const int TAG_ENABLE_CYCLE_STOP = 479; // CD_DATA_A4
        private const int TAG_ENABLE_MASS_RTO = 480; // CD_DATA_A5

        private readonly Dictionary<OperationMode, int> _writeTags = new();
        private readonly Dictionary<OperationMode, int> _statusTags = new(); // For Blinking/Color
        private readonly Dictionary<OperationMode, int> _enableTags = new(); // For IsEnabled

        public ObservableCollection<ModeButtonItem> ModeButtons { get; } = new ObservableCollection<ModeButtonItem>();
        public ObservableCollection<AuditLogModel> AuditLogs { get; set; } = new();

        private bool _isMachineHome;
        public bool IsMachineHome { get => _isMachineHome; set => SetProperty(ref _isMachineHome, value); }

        public ICommand UnifiedOperationCommand { get; }

        public ModeOfOperationViewModel(IAppLogger logger, CoreClient coreClient, INavigationService navService) : base(logger)
        {
            _coreClient = coreClient;
            _navService = navService;

            InitializeTags();
            InitializeButtons();

            UnifiedOperationCommand = new RelayCommand<string>(async (args) => await ExecuteOperationAsync(args));

            _feedbackTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(200) };
            _feedbackTimer.Tick += FeedbackLoop_Tick;
            _feedbackTimer.Start();
        }

        private void InitializeTags()
        {
            // Write
            _writeTags[OperationMode.Auto] = TAG_WRITE_AUTO;
            _writeTags[OperationMode.DryRun] = TAG_WRITE_DRY_RUN;
            _writeTags[OperationMode.CycleStop] = TAG_WRITE_CYCLE_STOP;
            _writeTags[OperationMode.MassRTO] = TAG_WRITE_MASS_RTO;

            // Status (Blinking/Color)
            _statusTags[OperationMode.Auto] = TAG_READ_AUTO_LAUNCH;
            _statusTags[OperationMode.DryRun] = TAG_READ_DRY_LAUNCH;
            _statusTags[OperationMode.CycleStop] = TAG_READ_STOP_ACTIVE;
            // MassRTO might not have a blinking status tag in your list, usually it's just momentary

            // Enable (Validation from PLC)
            _enableTags[OperationMode.Auto] = TAG_ENABLE_AUTO;
            _enableTags[OperationMode.DryRun] = TAG_ENABLE_DRY_RUN;
            _enableTags[OperationMode.CycleStop] = TAG_ENABLE_CYCLE_STOP;
            _enableTags[OperationMode.MassRTO] = TAG_ENABLE_MASS_RTO;
        }

        private void InitializeButtons()
        {
            var list = new List<ModeButtonItem>
            {
                new ModeButtonItem { Mode = OperationMode.Auto,      Name = "Auto Run",     BaseColor = "#008B8B", ActiveColor = "#00FFFF" },
                new ModeButtonItem { Mode = OperationMode.DryRun,    Name = "Dry Run",      BaseColor = "#8B008B", ActiveColor = "#FF00FF" },
                // Manual is usually always enabled unless you have a specific tag for it
                new ModeButtonItem { Mode = OperationMode.Manual,    Name = "Manual",       BaseColor = "#607D8B", ActiveColor = "#4CAF50", IsEnabled = true },
                new ModeButtonItem { Mode = OperationMode.CycleStop, Name = "Cycle Stop",   BaseColor = "#5D4037", ActiveColor = "#D7CCC8" },
                new ModeButtonItem { Mode = OperationMode.MassRTO,   Name = "Machine Home", BaseColor = "#795548", ActiveColor = "#FF5722" }
            };
            foreach (var item in list) ModeButtons.Add(item);
        }

        private async Task ExecuteOperationAsync(string args)
        {
            if (string.IsNullOrEmpty(args)) return;
            var parts = args.Split('|');
            if (!Enum.TryParse(parts[0], out OperationMode mode)) return;
            bool isPressed = bool.Parse(parts[1]);

            try
            {
                // 1. Manual Navigation
                if (mode == OperationMode.Manual)
                {
                    // Logic: Only navigate if the button is Enabled
                    var btn = GetBtn(OperationMode.Manual);
                    if (isPressed && btn.IsEnabled)
                    {
                        _navService.NavigateMain<ManualOperationView>();
                    }
                    return;
                }

                // 2. PLC Write (Only if button is Enabled)
                var buttonItem = GetBtn(mode);
                if (buttonItem.IsEnabled && _writeTags.TryGetValue(mode, out int tagId))
                {
                    int value = isPressed ? 1 : 0;
                    await _coreClient.WriteTagAsync(tagId, value);

                    if (isPressed) AddAudit($"Operator Pressed: {mode}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Op Error: {ex.Message}", LogType.Diagnostics);
            }
        }

        private async void FeedbackLoop_Tick(object? sender, EventArgs e)
        {
            try
            {
                // Read range covering 11-14, 477-484 (Write, Enable, Status)
                // Assuming CoreClient can handle disjointed reads or you just read a large block.
                // If tags are far apart, you might need two calls or a block read.
                var liveData = await _coreClient.GetIoValuesAsync(5);
                if (liveData.Count <2) return;

                bool isAutoRunning = false;
                bool isDryRunning = false;
                bool isStopRunning = false;
                bool isRTORunning = false;

                foreach (var btn in ModeButtons)
                {
                    // A. Update BLINKING/ACTIVE Status (from 481-483)
                    if (_statusTags.TryGetValue(btn.Mode, out int statusTagId))
                    {
                        if (liveData.TryGetValue(statusTagId, out object? val))
                        {
                            bool signal = Convert.ToBoolean(val);
                            btn.IsBlinking = signal;
                            btn.IsActive = signal;
                            if (btn.Mode == OperationMode.Auto) isAutoRunning = signal;
                            if (btn.Mode == OperationMode.DryRun) isDryRunning = signal;
                            if (btn.Mode == OperationMode.MassRTO) isRTORunning = signal;
                            if (btn.Mode == OperationMode.CycleStop) isStopRunning = signal;
                        }
                    }

                    // B. Update ENABLE/DISABLE State (from 477-480)
                    if (_enableTags.TryGetValue(btn.Mode, out int enableTagId))
                    {
                        if (liveData.TryGetValue(enableTagId, out object? val))
                        {
                            // PLC Logic dictates Enabled State directly
                            btn.IsEnabled = Convert.ToBoolean(val);
                        }
                    }
                    // Note: 'Manual' mode IsEnabled is skipped here as it has no tag in _enableTags map. 
                    // It stays True (default) or you can add logic if needed.
                }

                var manualBtn = GetBtn(OperationMode.Manual);

                // If EITHER Auto OR Dry is running (True), Manual must be Disabled.
                // If BOTH are stopped (False), Manual is Enabled.
                bool isSystemBusy = isAutoRunning || isDryRunning ||  isStopRunning;

                manualBtn.IsEnabled = !isSystemBusy;

                // C. Update Home Lamp
                if (liveData.TryGetValue(TAG_READ_HOME_LAMP, out object? homeVal))
                {
                    IsMachineHome = Convert.ToBoolean(homeVal);
                }
            }
            catch { }
        }

        private ModeButtonItem GetBtn(OperationMode mode) => ModeButtons.FirstOrDefault(b => b.Mode == mode) ?? new ModeButtonItem();

        private void AddAudit(string message)
        {
            if (AuditLogs.Count > 0 && AuditLogs.Last().Message == message && (DateTime.Now - DateTime.Parse(AuditLogs.Last().Time)).TotalSeconds < 2) return;
            if (AuditLogs.Count > 50) AuditLogs.RemoveAt(0);
            AuditLogs.Add(new AuditLogModel { Time = DateTime.Now.ToString("HH:mm:ss"), Message = message });
        }

        public void Dispose() => _feedbackTimer.Stop();
    }


    /*   public class ModeOfOperationViewModel : BaseViewModel, IDisposable
       {
           private readonly CoreClient _coreClient;
           private readonly INavigationService _navService;
           private readonly DispatcherTimer _feedbackTimer;

           // --- TAG CONFIGURATION ---
           private const int TAG_WRITE_AUTO = 11;
           private const int TAG_WRITE_DRY_RUN = 12;
           private const int TAG_WRITE_CYCLE_STOP = 13;
           private const int TAG_WRITE_MASS_RTO = 14;

           // Note: Using 481, 482, etc based on your code snippet
           private const int TAG_READ_AUTO_LAUNCH = 481;
           private const int TAG_READ_DRY_LAUNCH = 482;
           private const int TAG_READ_STOP_ACTIVE = 483;
           private const int TAG_READ_HOME_LAMP = 484;


           private const int TAG_ENABLE_AUTO = 477; // CD_DATA_A2
           private const int TAG_ENABLE_DRY_RUN = 478; // CD_DATA_A3
           private const int TAG_ENABLE_CYCLE_STOP = 479; // CD_DATA_A4
           private const int TAG_ENABLE_MASS_RTO = 480; // CD_DATA_A5

           private readonly Dictionary<OperationMode, int> _writeTags = new();
           private readonly Dictionary<OperationMode, int> _readTags = new();

           public ObservableCollection<ModeButtonItem> ModeButtons { get; } = new ObservableCollection<ModeButtonItem>();
           public ObservableCollection<AuditLogModel> AuditLogs { get; set; } = new();

           private bool _isMachineHome;
           public bool IsMachineHome { get => _isMachineHome; set => SetProperty(ref _isMachineHome, value); }

           public ICommand UnifiedOperationCommand { get; }

           public ModeOfOperationViewModel(IAppLogger logger, CoreClient coreClient, INavigationService navService) : base(logger)
           {
               _coreClient = coreClient;
               _navService = navService;

               InitializeTags();
               InitializeButtons();

               UnifiedOperationCommand = new RelayCommand<string>(async (args) => await ExecuteOperationAsync(args));

               // Increased timer slightly to reduce network jitter impact
               _feedbackTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(100) };
               _feedbackTimer.Tick += FeedbackLoop_Tick;
               _feedbackTimer.Start();
           }

           private void InitializeTags()
           {
               _writeTags[OperationMode.Auto] = TAG_WRITE_AUTO;
               _writeTags[OperationMode.DryRun] = TAG_WRITE_DRY_RUN;
               _writeTags[OperationMode.CycleStop] = TAG_WRITE_CYCLE_STOP;
               _writeTags[OperationMode.MassRTO] = TAG_WRITE_MASS_RTO;

               _readTags[OperationMode.Auto] = TAG_READ_AUTO_LAUNCH;
               _readTags[OperationMode.DryRun] = TAG_READ_DRY_LAUNCH;
               _readTags[OperationMode.CycleStop] = TAG_READ_STOP_ACTIVE;
           }

           private void InitializeButtons()
           {
               var list = new List<ModeButtonItem>
               {
                   new ModeButtonItem { Mode = OperationMode.Auto,      Name = "Auto Run",     BaseColor = "#008B8B", ActiveColor = "#00FFFF" },
                   new ModeButtonItem { Mode = OperationMode.DryRun,    Name = "Dry Run",      BaseColor = "#8B008B", ActiveColor = "#FF00FF" },
                   new ModeButtonItem { Mode = OperationMode.Manual,    Name = "Manual",       BaseColor = "#607D8B", ActiveColor = "#4CAF50" },
                   new ModeButtonItem { Mode = OperationMode.CycleStop, Name = "Cycle Stop",   BaseColor = "#5D4037", ActiveColor = "#D7CCC8", IsEnabled = false },
                   new ModeButtonItem { Mode = OperationMode.MassRTO,   Name = "Machine Home", BaseColor = "#795548", ActiveColor = "#FF5722" }
               };
               foreach (var item in list) ModeButtons.Add(item);
           }

           private async Task ExecuteOperationAsync(string args)
           {
               if (string.IsNullOrEmpty(args)) return;
               var parts = args.Split('|');
               if (!Enum.TryParse(parts[0], out OperationMode mode)) return;
               bool isPressed = bool.Parse(parts[1]);

               try
               {
                   // 1. Manual Screen Navigation
                   if (mode == OperationMode.Manual)
                   {
                       if (isPressed)
                       {
                           // Double check logic: Only navigate if button is actually enabled
                           var btn = GetBtn(OperationMode.Manual);
                           if (btn.IsEnabled)
                           {
                               _navService.NavigateMain<ManualOperationView>();
                           }
                       }
                       return;
                   }

                   // 2. PLC Write Logic
                   if (_writeTags.TryGetValue(mode, out int tagId))
                   {
                       int value = isPressed ? 1 : 0;
                       await _coreClient.WriteTagAsync(tagId, value);

                       if (isPressed) AddAudit($"Operator Pressed: {mode}");
                   }
               }
               catch (Exception ex)
               {
                   _logger.LogError($"Op Error: {ex.Message}", LogType.Diagnostics);
               }
           }

           private async void FeedbackLoop_Tick(object? sender, EventArgs e)
           {
               try
               {
                   var liveData = await _coreClient.GetIoValuesAsync(5); // Ensure range covers 11-14 AND 481-484
                   if (liveData.Count<2) return;

                   bool isAutoRunning = false;
                   bool isDryRunning = false;
                   bool isCycleStopping = false;

                   // 1. Process Buttons State
                   foreach (var btn in ModeButtons)
                   {
                       if (_readTags.TryGetValue(btn.Mode, out int readTagId))
                       {
                           if (liveData.TryGetValue(readTagId, out object? val))
                           {
                               bool signal = Convert.ToBoolean(val);

                               // Visual Feedback
                               btn.IsBlinking = signal;
                               btn.IsActive = signal;

                               // Logic State Capture
                               if (btn.Mode == OperationMode.Auto) isAutoRunning = signal;
                               if (btn.Mode == OperationMode.DryRun) isDryRunning = signal;
                               if (btn.Mode == OperationMode.CycleStop) isCycleStopping = signal;
                           }
                       }
                   }

                   // 2. Home Lamp
                   if (liveData.TryGetValue(TAG_READ_HOME_LAMP, out object? homeVal))
                   {
                       IsMachineHome = Convert.ToBoolean(homeVal);
                   }
                   UpdateInterlocks(isAutoRunning, isDryRunning, isCycleStopping);

                   // 3. Strict Interlock Logic (No Flickering)
               }
               catch { }
           }

           private void UpdateInterlocks(bool autoOn, bool dryOn, bool stopOn)
           {
               var autoBtn = GetBtn(OperationMode.Auto);
               var dryBtn = GetBtn(OperationMode.DryRun);
               var manualBtn = GetBtn(OperationMode.Manual);
               var stopBtn = GetBtn(OperationMode.CycleStop);
               var rtoBtn = GetBtn(OperationMode.MassRTO);

               // STATE 1: CYCLE STOP ACTIVE
               // Priority: If Cycle Stop is blinking (Active), everything else is locked.
               if (stopOn)
               {
                   autoBtn.IsEnabled = false;
                   dryBtn.IsEnabled = false;
                   manualBtn.IsEnabled = false;
                   rtoBtn.IsEnabled = false;
                   stopBtn.IsEnabled = true; // Keep Stop enabled so we can see it blinking
                   return;
               }

               // STATE 2: RUNNING (Auto OR Dry)
               // If running, we DISABLE the other Start buttons, but we ENABLE Cycle Stop.
               if (autoOn || dryOn)
               {
                   // CRITICAL UI FIX: Even if Auto is ON, we keep IsEnabled=True so it can blink.
                   // If we set IsEnabled=False, WPF usually stops animations/colors.
                   // We just rely on the PLC to ignore the button press if logic dictates, 
                   // OR we disable specific conflicting buttons.

                   if (autoOn)
                   {
                       autoBtn.IsEnabled = true; // Keep enabled to show active state
                       dryBtn.IsEnabled = false; // Cannot switch directly to Dry
                   }
                   else if (dryOn)
                   {
                       autoBtn.IsEnabled = false; // Cannot switch directly to Auto
                       dryBtn.IsEnabled = true; // Keep enabled to show active state
                   }

                   manualBtn.IsEnabled = false; // Cannot go to Manual while running
                   rtoBtn.IsEnabled = false;    // Cannot Home while running

                   stopBtn.IsEnabled = true;    // ENABLE Cycle Stop
                   return;
               }

               // STATE 3: HOME / IDLE
               // "Auto Run Mode, Dry Run and Manual Mode should be enabled for selection in home condition"
               autoBtn.IsEnabled = true;
               dryBtn.IsEnabled = true;
               manualBtn.IsEnabled = true;
               rtoBtn.IsEnabled = true;

               // "If Auto/Dry selected THEN Cycle Stop enabled... else disabled"
               // Since we are in Home/Idle here (neither Auto nor Dry is active), Cycle Stop is DISABLED.
               stopBtn.IsEnabled = false;
           }



           private ModeButtonItem GetBtn(OperationMode mode) => ModeButtons.FirstOrDefault(b => b.Mode == mode) ?? new ModeButtonItem();

           private void AddAudit(string message)
           {
               // Simple rate limiter to prevent audit spam during flickering
               if (AuditLogs.Count > 0 && AuditLogs.Last().Message == message && (DateTime.Now - DateTime.Parse(AuditLogs.Last().Time)).TotalSeconds < 2) return;

               if (AuditLogs.Count > 50) AuditLogs.RemoveAt(0);
               AuditLogs.Add(new AuditLogModel { Time = DateTime.Now.ToString("HH:mm:ss"), Message = message });
           }

           public void Dispose() => _feedbackTimer.Stop();
       }

   */
    //public class ModeOfOperationViewModel : BaseViewModel, IDisposable
    //{
    //    private readonly CoreClient _coreClient;
    //    private readonly INavigationService _navService;
    //    private readonly DispatcherTimer _feedbackTimer;

    //    // --- TAG CONFIGURATION ---
    //    private readonly Dictionary<OperationMode, int> _tagMap = new()
    //    {
    //        { OperationMode.Auto,       11 },
    //        { OperationMode.DryRun,     12 },
    //        { OperationMode.CycleStop,  13 },
    //        { OperationMode.MassRTO,    14 }/*,
    //        { OperationMode.Manual,     15 }*/
    //    };

    //    // --- Properties ---
    //    public ObservableCollection<ModeButtonItem> ModeButtons { get; }
    //    public ObservableCollection<AuditLogModel> AuditLogs { get; set; } = new();

    //    private bool _isMachineHome;
    //    public bool IsMachineHome
    //    {
    //        get => _isMachineHome;
    //        set => SetProperty(ref _isMachineHome, value);
    //    }

    //    public ICommand ButtonClickCommand { get; }

    //    public ModeOfOperationViewModel(IAppLogger logger, CoreClient coreClient, INavigationService navService) : base(logger)
    //    {
    //        _coreClient = coreClient;
    //        _navService = navService;

    //        // Initialize Buttons with Client Specific Colors
    //        ModeButtons = new ObservableCollection<ModeButtonItem>
    //        {
    //            new ModeButtonItem
    //            {
    //                Mode = OperationMode.Auto,
    //                Name = "Auto Run",
    //                IsEnabled = true,
    //                BaseColor = "#008B8B",   // Dark Aqua Blue
    //                ActiveColor = "#00FFFF"  // Bright Aqua Blue
    //            },
    //            new ModeButtonItem
    //            {
    //                Mode = OperationMode.DryRun,
    //                Name = "Dry Run",
    //                IsEnabled = true,
    //                BaseColor = "#8B008B",   // Dark Magenta
    //                ActiveColor = "#FF00FF"  // Bright Magenta
    //            },
    //            new ModeButtonItem
    //            {
    //                Mode = OperationMode.Manual,
    //                Name = "Manual",
    //                IsEnabled = true,
    //                BaseColor = "#607D8B",   // Blue Grey
    //                ActiveColor = "#4CAF50"  // Green
    //            },
    //            new ModeButtonItem
    //            {
    //                Mode = OperationMode.CycleStop,
    //                Name = "Cycle Stop",
    //                IsEnabled = false,       // Initially Disabled
    //                BaseColor = "#5D4037",   // Dark Brown
    //                ActiveColor = "#D7CCC8"  // Bright Brown/Beige
    //            },
    //            new ModeButtonItem
    //            {
    //                Mode = OperationMode.MassRTO,
    //                Name = "Machine Home",
    //                IsEnabled = true,
    //                BaseColor = "#795548",
    //                ActiveColor = "#FF5722"
    //            }
    //        };

    //        ButtonClickCommand = new RelayCommand<ModeButtonItem>(OnButtonClicked);

    //        _feedbackTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
    //        _feedbackTimer.Tick += FeedbackLoop_Tick;
    //        _feedbackTimer.Start();
    //    }

    //    private async void OnButtonClicked(ModeButtonItem item)
    //    {
    //        try
    //        {
    //            // 1. Manual Screen Logic
    //            if (item.Mode == OperationMode.Manual)
    //            {
    //                // "When Manual mode is selected then Manual Screen should open else it should not open"
    //                // Only navigate if enabled (handled by UpdateUiLogic)
    //                _logger.LogInfo("Navigating to Manual Screen", LogType.Audit);
    //                //_navService.NavigateMain<ManualOperation>();
    //                _navService.NavigateMain<ManualOperationView>();
    //                return;
    //            }

    //            // 2. Cycle Stop Logic
    //            if (item.Mode == OperationMode.CycleStop)
    //            {
    //                _logger.LogInfo("Operator pressed Cycle Stop. Reseting Auto/Dry...", LogType.Audit);

    //                // Client Req: "When cycle stop is pressed auto and dry mode should be zero"
    //                // We send stop command (1) to CycleStop Tag, AND 0 to Auto/Dry tags to unlatch them.
    //                await WriteTagToPlc(_tagMap[OperationMode.CycleStop], 1);
    //                await WriteTagToPlc(_tagMap[OperationMode.Auto], 0);
    //                await WriteTagToPlc(_tagMap[OperationMode.DryRun], 0);

    //                //await WriteTagToPlc(_tagMap[OperationMode.CycleStop], 0);

    //                AddAudit("Cycle Stop Initiated. Waiting for Home...");
    //                return;
    //            }

    //            // 3. Auto / Dry Run / Mass RTO Toggle Logic
    //            if (_tagMap.TryGetValue(item.Mode, out int tagId))
    //            {
    //                // Toggle: If Active -> Send 0 (Stop). If Inactive -> Send 1 (Start).
    //                int valueToSend = item.IsActive ? 0 : 1;

    //                _logger.LogInfo($"Requesting {item.Mode}: {valueToSend}", LogType.Audit);
    //                await WriteTagToPlc(tagId, valueToSend);

    //                string action = valueToSend == 1 ? "started" : "stopped";
    //                AddAudit($"Operator {action} {item.Name}.");
    //            }
    //        }
    //        catch (Exception ex)
    //        {
    //            _logger.LogError($"Button Click Error: {ex.Message}", LogType.Diagnostics);
    //        }
    //    }

    //    private async void FeedbackLoop_Tick(object? sender, EventArgs e)
    //    {
    //        try
    //        {
    //            var liveData = await _coreClient.GetIoValuesAsync(5);
    //            if (liveData == null) return;

    //            bool anyActive = false;

    //            // 1. Update status of all buttons from PLC
    //            foreach (var btn in ModeButtons)
    //            {
    //                if (_tagMap.TryGetValue(btn.Mode, out int tagId))
    //                {
    //                    if (liveData.TryGetValue(tagId, out object? val))
    //                    {
    //                        bool state = Convert.ToBoolean(val);
    //                        btn.IsActive = state;
    //                        if (state) anyActive = true;
    //                    }
    //                }
    //            }

    //            // 2. Update Home Indication
    //            // "Home position means value of all tag should be zero"
    //            IsMachineHome = !anyActive;

    //            // 3. Enforce Interlocks (Enable/Disable logic)
    //            UpdateUiLogic();
    //        }
    //        catch (Exception ex)
    //        {
    //            // Suppress loop errors
    //            System.Diagnostics.Debug.WriteLine($"Feedback Error: {ex.Message}");
    //        }
    //    }

    //    private void UpdateUiLogic()
    //    {
    //        var autoBtn = GetBtn(OperationMode.Auto);
    //        var dryBtn = GetBtn(OperationMode.DryRun);
    //        var manualBtn = GetBtn(OperationMode.Manual);
    //        var stopBtn = GetBtn(OperationMode.CycleStop);
    //        var rtoBtn = GetBtn(OperationMode.MassRTO);

    //        // Client Req: "If Auto Mode or Dry Run is selected then Cycle Stop should be enabled 
    //        // and remaining buttons should be in disabled state"
    //        bool isRunning = autoBtn.IsActive || dryBtn.IsActive;
    //        bool isCycleStopping = stopBtn.IsActive;

    //        if (isCycleStopping)
    //        {
    //            // Cycle Stop is active -> Disable everything until it finishes (Home condition achieved)
    //            // "Cycle Stop button will return to home after completing the running cycle"
    //            // The PLC will turn off the CycleStop bit when done, triggering the 'else' block below.
    //            autoBtn.IsEnabled = false;
    //            dryBtn.IsEnabled = false;
    //            manualBtn.IsEnabled = false;
    //            rtoBtn.IsEnabled = false;
    //            stopBtn.IsEnabled = false;
    //        }
    //        else if (isRunning)
    //        {
    //            // Auto or Dry is ON
    //            autoBtn.IsEnabled = autoBtn.IsActive; // Can stop itself
    //            dryBtn.IsEnabled = dryBtn.IsActive;   // Can stop itself
    //            manualBtn.IsEnabled = false;          // Disabled during run
    //            rtoBtn.IsEnabled = false;             // Disabled during run

    //            stopBtn.IsEnabled = true;             // Cycle Stop ENABLED
    //        }
    //        else
    //        {
    //            // Home Condition (Nothing active)
    //            autoBtn.IsEnabled = true;
    //            dryBtn.IsEnabled = true;
    //            manualBtn.IsEnabled = true;
    //            rtoBtn.IsEnabled = true;

    //            stopBtn.IsEnabled = false;            // Disabled at Home
    //        }
    //    }

    //    private ModeButtonItem GetBtn(OperationMode mode) => ModeButtons.First(b => b.Mode == mode);

    //    private async Task WriteTagToPlc(int tagId, object value)
    //    {
    //        try { await _coreClient.WriteTagAsync(tagId, value); }
    //        catch (Exception ex) { _logger.LogError($"Write Error: {ex.Message}", LogType.Diagnostics); }
    //    }

    //    private void AddAudit(string message)
    //    {
    //        if (AuditLogs.Count > 100) AuditLogs.RemoveAt(0);
    //        AuditLogs.Add(new AuditLogModel { Time = DateTime.Now.ToString("HH:mm:ss"), Message = message });
    //        _logger.LogInfo(message, LogType.Audit);
    //    }

    //    public void Dispose()
    //    {
    //        _feedbackTimer.Stop();
    //    }
    //}
}