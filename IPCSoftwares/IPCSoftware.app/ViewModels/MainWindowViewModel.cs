using IPCSoftware.App;
using IPCSoftware.App.Helpers;
using IPCSoftware.App.Services;
using IPCSoftware.App.ViewModels;
using IPCSoftware.App.Views;

using IPCSoftware.Core.Interfaces;
using IPCSoftware.Core.Interfaces.AppLoggerInterface;
using IPCSoftware.Shared;
using IPCSoftware.Shared.Models;
using IPCSoftware.Shared.Models.ConfigModels;
using IPCSoftware.Shared.Models.Messaging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System.Collections.ObjectModel;
using System.Printing;
using System.Reflection;
using System.Windows;

using System.Windows.Input;
using System.Windows.Threading;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.StartPanel;

public class MainWindowViewModel : BaseViewModel
{

    private readonly INavigationService _nav;
    private readonly IDialogService _dialog;
    private readonly CoreClient _coreClient;
    private readonly AlarmViewModel _alarmVM;

    public ICommand SidebarItemClickCommand { get; }
    public RibbonViewModel RibbonVM { get; }
    public ICommand CloseSidebarCommand { get; }
    public ICommand MinimizeAppCommand { get; }
    public ICommand CloseAppCommand { get; }

    // --- ALARM BANNER COMMANDS & PROPERTIES ---
    public ICommand CloseAlarmBannerCommand { get; }

    private string _currentMachineMode = "UNKNOWN";
    public string CurrentMachineMode
    {
        get => _currentMachineMode;
        set => SetProperty(ref _currentMachineMode, value);
    }


    public ICommand AcknowledgeBannerAlarmCommand { get; }

    private bool _isAlarmBannerVisible;
    public bool IsAlarmBannerVisible
    {
        get => _isAlarmBannerVisible;
        set => SetProperty(ref _isAlarmBannerVisible, value);
    }

    private string _alarmBannerMessage;
    public string AlarmBannerMessage
    {
        get => _alarmBannerMessage;
        set => SetProperty(ref _alarmBannerMessage, value);
    }

    private string _alarmBannerColor = "#D32F2F"; // Default Red
    public string AlarmBannerColor
    {
        get => _alarmBannerColor;
        set => SetProperty(ref _alarmBannerColor, value);
    }
    // ------------------------------------------


    private readonly SafePoller _timer;

    // Live System Time Property
    private string _systemTime;
    public string SystemTime
    {
        get => _systemTime;
        set => SetProperty(ref _systemTime, value);
    }


    // In MainViewModel.cs
    private bool _isSidebarDocked;
    public bool IsSidebarDocked
    {
        get => _isSidebarDocked;
        set
        {
            _isSidebarDocked = value;
            OnPropertyChanged();
            // If we dock, we must ensure it's open
            if (value)
                IsSidebarOpen = true;
            else
                IsSidebarOpen = false;
        }
    }


    public bool _macMiniConnected;
    public bool MacMiniConnected
    {
        get => _macMiniConnected;
        set => SetProperty(ref _macMiniConnected, value);
    }

    public bool _isConnected;
    public bool IsConnected
    {
        get => _isConnected;
        set => SetProperty(ref _isConnected, value);
    }

    public bool _plcConnected;
    public bool PLCConnected
    {
        get => _plcConnected;
        set => SetProperty(ref _plcConnected, value);
    }

    public bool _timeSynched;
    public bool TimeSynched
    {
        get => _timeSynched;
        set => SetProperty(ref _timeSynched, value);
    }

    private readonly IOptionsMonitor<AboutSettings> _aboutMonitor;

    //public string AppVersion => $"Version {Assembly.GetExecutingAssembly().GetName().Version}";
    public string AppVersion => _aboutMonitor.CurrentValue.ProductVersion;

    public MainWindowViewModel(
        INavigationService nav, 
        CoreClient coreClient,
        IDialogService dialog,
        RibbonViewModel ribbonVM, 
        AlarmViewModel alarmVM,
        IOptionsMonitor<AboutSettings> aboutMonitor,
        IAppLogger logger) : base(logger)
    {
        _coreClient = coreClient;
        _dialog = dialog;
        _aboutMonitor = aboutMonitor;
        _nav = nav;
        _alarmVM = alarmVM;
        _alarmVM.ActiveAlarms.CollectionChanged += (s, e) => RefreshAlarmBanner();
        _timer = new SafePoller
        (TimeSpan.FromSeconds(1), LiveDataTimerTick);
        _timer.Start(); 
        // 3. Subscribe to Alarm Events
        _coreClient.OnAlarmMessageReceived += OnAlarmReceived;

        // Set initial time immediately
        SystemTime = DateTime.Now.ToString("dd-MMM-yyyy HH:mm:ss");

        RibbonVM = ribbonVM;
        RibbonVM.ShowSidebar = LoadSidebarMenu;
        RibbonVM.OnLogout = ResetLandingState; 
        RibbonVM.OnLandingPageRequested = ResetLandingState;

        CloseAppCommand = new RelayCommand(ExecuteCloseApp);
        MinimizeAppCommand = new RelayCommand(ExecuteMinimizeApp);
        SidebarItemClickCommand = new RelayCommand<string>(OnSidebarItemClick);
        CloseSidebarCommand = new RelayCommand(() => IsSidebarOpen = false);

        CloseAlarmBannerCommand = new RelayCommand(() => IsAlarmBannerVisible = false);

        AcknowledgeBannerAlarmCommand = new RelayCommand(ExecuteAcknowledgeBannerAlarm, CanExecuteAcknowledgeBannerAlarm);

        UserSession.OnSessionChanged += () =>
        {
            OnPropertyChanged(nameof(IsRibbonVisible));
            OnPropertyChanged(nameof(CurrentUserName));
            OnPropertyChanged(nameof(CurrentUserRole));
            OnPropertyChanged(nameof(IsAdmin));
        };

        _aboutMonitor.OnChange(settings => {
            OnPropertyChanged(nameof(AppVersion));
         
        });
    }
    private bool CanExecuteAcknowledgeBannerAlarm()
    {
        // Enable the button only if there is at least one UNACKNOWLEDGED alarm
        // The command is only active if the banner is visible AND there are alarms to ack.
        return ActiveAlarmCount > 0 && IsAlarmBannerVisible;
    }

    private async void ExecuteAcknowledgeBannerAlarm()
    {
        try
        {
            // 1. Find the latest unacknowledged alarm (the one currently displayed)
            var latestUnackedAlarm = _alarmVM.ActiveAlarms
                .Where(a => a.AlarmAckTime == null)
                .OrderByDescending(a => a.AlarmTime)
                .FirstOrDefault();

            if (latestUnackedAlarm != null)
            {
                // 2. Call the Acknowledge logic in the AlarmViewModel
                // This relies on the core logic being available in AlarmViewModel (Action 2.2)
                await _alarmVM.AcknowledgeAlarmRequestAsync(latestUnackedAlarm);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.Message, LogType.Diagnostics);
        }
    }

    public string AlarmBannerTotalMessage => ActiveAlarmCount > 0
        ? ActiveAlarmCount == 1
        ? AlarmBannerMessage // Only one alarm, show the detailed message
       : $"Critical System Alarms ({ActiveAlarmCount}) | {AlarmBannerMessage}" // Multiple alarms, show count + latest message
       : "No Active Alarms";

    private int _activeAlarmCount;
    public int ActiveAlarmCount
    {
        get => _activeAlarmCount;
        set
        {
            SetProperty(ref _activeAlarmCount, value);
            // Recalculate the Banner Message when the count changes
            OnPropertyChanged(nameof(AlarmBannerTotalMessage));
        }
    }

    private void RefreshAlarmBanner()
    {
        var latestActiveAlarm = _alarmVM.ActiveAlarms
            .Where(a => a.AlarmResetTime == null)
            .OrderByDescending(a => a.AlarmTime)
            .FirstOrDefault();

        if (latestActiveAlarm != null)
        {
            AlarmBannerMessage = $"⚠️ ALARM {latestActiveAlarm.AlarmNo}: {latestActiveAlarm.AlarmText}";

            if (latestActiveAlarm.Severity == "High") AlarmBannerColor = "#D32F2F";
            else if (latestActiveAlarm.Severity == "Warning") AlarmBannerColor = "#F57C00";
            else AlarmBannerColor = "#1976D2";
        }
        else
        {
            AlarmBannerMessage = "No Critical Alarms";
            AlarmBannerColor = "#1976D2";
        }

        ActiveAlarmCount = _alarmVM.ActiveAlarms.Count(a => a.AlarmResetTime == null);
        IsAlarmBannerVisible = ActiveAlarmCount > 0;
    }

    private void OnAlarmReceived(AlarmMessage msg)
    {
        try
        {
            Application.Current.Dispatcher.Invoke(RefreshAlarmBanner);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.Message, LogType.Diagnostics);
        }
    }



    private async Task LiveDataTimerTick()
    {
        try 
        {
            IsConnected =  _coreClient.isConnected;

            var liveData = await _coreClient.GetIoValuesAsync(5);

            if (liveData != null && liveData.Count > 0)
            {
                // 1. Check Modes
                if (GetBool(liveData, ConstantValues.Mode_Auto.Read)) CurrentMachineMode = "AUTO RUN";
                else if (GetBool(liveData, ConstantValues.Mode_DryRun.Read)) CurrentMachineMode = "DRY RUN";
                else if (GetBool(liveData, ConstantValues.Mode_CycleStop.Read)) CurrentMachineMode = "CYCLE STOP";
                else if (GetBool(liveData, ConstantValues.Mode_MassRTO.Read)) CurrentMachineMode = "MACHINE HOME";
                else CurrentMachineMode = "MANUAL / IDLE"; // Default if no specific mode active
            }


            SystemTime = DateTime.Now.ToString("dd-MMM-yyyy HH:mm:ss");
            var boolDict = await _coreClient.GetIoValuesAsync(1);
            if (boolDict != null && boolDict.TryGetValue(1, out object pulseObj))
            {
                //var json = JsonConvert.SerializeObject(pulseObj);
                //var pulseResult = JsonConvert.DeserializeObject<List<bool>>(json);
                //PLCConnected = pulseResult[0];
                //TimeSynched = pulseResult[1];

                var json = JsonConvert.SerializeObject(pulseObj);
                var pulseResult = JsonConvert.DeserializeObject<List<bool>>(json);

                // Index 0: PLC
                if (pulseResult.Count > 0) PLCConnected = pulseResult[0];

                // Index 1: Time Sync
                if (pulseResult.Count > 1) TimeSynched = pulseResult[1];

                // Index 2: Mac Mini (NEW)
                if (pulseResult.Count > 2)
                {
                    MacMiniConnected = pulseResult[2];
                }
                else
                {
                    MacMiniConnected = false; // Fallback if backend is old version
                }

            }
            else
            {
                PLCConnected = false;
                TimeSynched = false;
                MacMiniConnected = false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.Message, LogType.Diagnostics);
            PLCConnected = false;
            TimeSynched = false;
            MacMiniConnected = false;
            CurrentMachineMode = "UNKNOWN";
        }

    }


    private bool GetBool(Dictionary<int, object> data, int tagId)
    {
        if (data.TryGetValue(tagId, out object val))
        {
            return Convert.ToBoolean(val);
        }
        return false;
    }



    private void ResetLandingState()
    {
        existingUserControl = string.Empty;   // clear selected page
        IsSidebarOpen = false;                // close sidebar if open
        IsSidebarDocked = false;
    }
    // ==============================
    // RIBBON VISIBILITY PROPERTIES
    // ==============================
    public bool IsRibbonVisible => UserSession.IsLoggedIn;
    public string CurrentUserName => UserSession.Username ?? "Guest";
    public string CurrentUserRole=> UserSession.Role ?? "Guest";
    public bool IsAdmin => UserSession.Role == "Admin";

    // ==============================
    // SIDEBAR PROPERTIES
    // ==============================
    private bool _isSidebarOpen;
    public bool IsSidebarOpen
    {
        get => _isSidebarOpen;
        set { _isSidebarOpen = value; OnPropertyChanged(); }
    }

    public ObservableCollection<string> SidebarItems { get; }
        = new ObservableCollection<string>();

    private string existingUserControl = string.Empty;
    private string currentRibbonKey = string.Empty;



    // ==============================
    // RIBBON → SIDEBAR CONNECTION
    // ==============================
    /*  public void ConnectSidebar(RibbonViewModel ribbonVM)
      {
          ribbonVM.ShowSidebar = LoadSidebarMenu;
      }*/

    private void LoadSidebarMenu((string Key, List<string> Items) menu)
    {
        try
        {
            SidebarItems.Clear();

            foreach (var item in menu.Items)
                SidebarItems.Add(item);
            if (currentRibbonKey == menu.Key )
            {
                if (!IsSidebarDocked)
                {
                    IsSidebarOpen = !IsSidebarOpen;
                }

                return;
            }

            IsSidebarOpen = true;
            currentRibbonKey = menu.Key;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.Message, LogType.Diagnostics);
        }

        // MVVM triggers animation
    }

    private void ExecuteMinimizeApp()
    {
        // This finds the active main window and minimizes it
        if (Application.Current.MainWindow != null)
        {
            Application.Current.MainWindow.WindowState = WindowState.Minimized;
        }
    }

    private void ExecuteCloseApp()
    {
        bool confirm = _dialog.ShowYesNo("Close the application?", "Confirm Exit");

        if (confirm)
        {
            Application.Current.Shutdown();
        }
           

    }


    private void OnSidebarItemClick(string itemName)
    {
        try
        {
            if (!_nav.CanNavigateFromCurrent()) return;

            // Close sidebar
            if (!IsSidebarDocked)
            {
              IsSidebarOpen = false;
            }
            if (string.IsNullOrWhiteSpace(existingUserControl))
            {
                existingUserControl = itemName;
            }
            else if (existingUserControl == itemName)
            {
                return;
            }
            else
            {
                existingUserControl = itemName;
            }

            // Navigate based on item name
            switch (itemName)
            {
                // OEEDashboard Menu
                case "Dashboard":
                    //_nav.NavigateMain<LiveOeeView>();
                    _nav.NavigateMain<OEEDashboard>();
                    break;

                case "Machine Summary":
                   // _nav.NavigateMain<ServoCalibrationView>();
                    break;
                case "Servo Parameters":
                    _nav.NavigateMain<ServoCalibrationView>();
                    break;

                case "Time Sync":
                    _nav.NavigateToSystemSettings();
                    break;

                // Config Menu
                case "Log Config":
                    _nav.NavigateMain<LogListView>();
                    break;
                case "Startup Condition":
                    _nav.NavigateMain<StartupConditionView>();
                    break;
                case "Device Config":
                    _nav.NavigateMain<DeviceListView>();
                    break;
                case "Alarm Config":
                    _nav.NavigateMain<AlarmListView>();
                    break;

                case "User Config":
                    _nav.NavigateMain<UserListView>();
                    break;

                case "Product Settings":
                    _nav.NavigateMain<ProductSettingsView>();
                    break;

                case "Manual Operation":
                    //_nav.NavigateMain<ManualOperation>();
                    _nav.NavigateMain<ManualOperationView>();
                    break;

                case "Control":
                    _nav.NavigateMain<ModeOfOperation>();
                    break;

                case "PLC IO":
                    _nav.NavigateMain<PLCIOView>();
                    break;

                case "Diagnostic":
                    _nav.NavigateMain<TagControlView>();
                    break;

                case "Shift Config":
                    _nav.NavigateMain<ShiftConfigurationView>();
                    break;

                case "PLC TAG Config":
                    _nav.NavigateMain<PLCTagListView>();
                    break;
                case "Report Config":
                    _nav.NavigateMain<ReportConfigView>();
                    break;

                case "Alarm View": 
                    _nav.NavigateMain<AlarmView>(); break;

                case "Production Data": 
                    _nav.NavigateMain<ReportViewerView>(); break;


                case "Production Images": 
                    _nav.NavigateMain<ProductionImageView>(); break;


                case "Audit Logs":
                    _nav.NavigateToLogs(LogType.Audit);
                    break;

                case "Error Logs":
                    _nav.NavigateToLogs(LogType.Error);
                    break;

                //case "Production Logs":
                //    _nav.NavigateToLogs(LogType.Production);
                //    break;
                case "Diagnostics Logs":
                    _nav.NavigateToLogs(LogType.Diagnostics);
                    break;

                case "Alarm Logs":
                    _nav.NavigateMain<AlarmLogView>();
                    break;
                
                case "External Interface":
                    _nav.NavigateMain<ApiTestView>();
                    break;

                case "AE Limit":
                    _nav.NavigateMain<AeLimitView>();
                    break;
                case "About":
                    _nav.NavigateMain<AboutView>();
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.Message, LogType.Diagnostics);
        }

    }

}