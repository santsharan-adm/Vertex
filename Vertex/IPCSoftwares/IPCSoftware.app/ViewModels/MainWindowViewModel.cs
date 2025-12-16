using IPCSoftware.App;
using IPCSoftware.App.Services;
using IPCSoftware.App.ViewModels;
using IPCSoftware.App.Views;

using IPCSoftware.Core.Interfaces;
using IPCSoftware.Core.Interfaces.AppLoggerInterface;
using IPCSoftware.Shared;
using IPCSoftware.Shared.Models.ConfigModels;
using Newtonsoft.Json;
using System;
using System.Collections.ObjectModel;
using System.Reflection;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

public class MainWindowViewModel : BaseViewModel
{
    private readonly INavigationService _nav;
    private readonly IDialogService _dialog;
    private readonly IAppLogger _logger;
    private readonly CoreClient _coreClient;

    public ICommand SidebarItemClickCommand { get; }
    public RibbonViewModel RibbonVM { get; }
    public ICommand CloseSidebarCommand { get; }
    public ICommand MinimizeAppCommand { get; }
    public ICommand CloseAppCommand { get; }

    private readonly DispatcherTimer _timer;

    private string _systemTime;
    public string SystemTime
    {
        get => _systemTime;
        set => SetProperty(ref _systemTime, value);
    }

    private bool _isSidebarDocked;
    public bool IsSidebarDocked
    {
        get => _isSidebarDocked;
        set
        {
            _isSidebarDocked = value;
            OnPropertyChanged();
            if (value)
                IsSidebarOpen = true;
            else
                IsSidebarOpen = false;
        }
    }

    public bool _isConnected;
    public bool IsConnected
    {
        get => _isConnected;
        set => SetProperty(ref _isConnected, value);
    }

    public bool _timeSynched;
    public bool TimeSynched
    {
        get => _timeSynched;
        set => SetProperty(ref _timeSynched, value);
    }

    public string AppVersion => "AOI System v1.0.3";

    public MainWindowViewModel(
        INavigationService nav,
        CoreClient coreClient,
        IAppLogger logger,
        IDialogService dialog,
        RibbonViewModel ribbonVM)
    {
        try
        {
            _coreClient = coreClient;
            _nav = nav;
            _dialog = dialog;
            _logger = logger;

            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            _timer.Tick += LiveDataTimerTick;
            _timer.Start();

            SystemTime = DateTime.Now.ToString("dd-MMM-yyyy HH:mm:ss");

            RibbonVM = ribbonVM;
            RibbonVM.ShowSidebar = LoadSidebarMenu;
            RibbonVM.OnLogout = ResetLandingState;
            RibbonVM.OnLandingPageRequested = ResetLandingState;

            CloseAppCommand = new RelayCommand(ExecuteCloseApp);
            MinimizeAppCommand = new RelayCommand(ExecuteMinimizeApp);

            SidebarItemClickCommand = new RelayCommand<string>(OnSidebarItemClick);
            CloseSidebarCommand = new RelayCommand(() => IsSidebarOpen = false);

            UserSession.OnSessionChanged += () =>
            {
                OnPropertyChanged(nameof(IsRibbonVisible));
                OnPropertyChanged(nameof(CurrentUserName));
                OnPropertyChanged(nameof(IsAdmin));
            };
        }
        catch (Exception)
        {
            // Exception swallowed to prevent application crash
        }
    }

    private async void LiveDataTimerTick(object sender, EventArgs e)
    {
        try
        {
            SystemTime = DateTime.Now.ToString("dd-MMM-yyyy HH:mm:ss");

            var boolDict = await _coreClient.GetIoValuesAsync(1);
            if (boolDict != null && boolDict.TryGetValue(1, out object pulseObj))
            {
                var json = JsonConvert.SerializeObject(pulseObj);
                var pulseResult = JsonConvert.DeserializeObject<System.Collections.Generic.List<bool>>(json);
                IsConnected = pulseResult[0];
                TimeSynched = pulseResult[1];
            }
        }
        catch (Exception)
        {
            // Exception swallowed to prevent application crash
        }
    }

    private void ResetLandingState()
    {
        try
        {
            existingUserControl = string.Empty;
            IsSidebarOpen = false;
            IsSidebarDocked = false;
        }
        catch (Exception)
        {
            // Exception swallowed to prevent application crash
        }
    }

    public bool IsRibbonVisible => UserSession.IsLoggedIn;
    public string CurrentUserName => UserSession.Username ?? "Guest";
    public bool IsAdmin => UserSession.Role == "Admin";

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

    private void LoadSidebarMenu((string Key, System.Collections.Generic.List<string> Items) menu)
    {
        try
        {
            SidebarItems.Clear();

            foreach (var item in menu.Items)
                SidebarItems.Add(item);

            if (currentRibbonKey == menu.Key)
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
        catch (Exception)
        {
            // Exception swallowed to prevent application crash
        }
    }

    private void ExecuteMinimizeApp()
    {
        try
        {
            if (Application.Current.MainWindow != null)
            {
                Application.Current.MainWindow.WindowState = WindowState.Minimized;
            }
        }
        catch (Exception)
        {
            // Exception swallowed to prevent application crash
        }
    }

    private void ExecuteCloseApp()
    {
        try
        {
            bool confirm = _dialog.ShowYesNo("Close the application?", "Confirm Exit");
            if (confirm)
            {
                Application.Current.Shutdown();
            }
        }
        catch (Exception)
        {
            // Exception swallowed to prevent application crash
        }
    }

    private void OnSidebarItemClick(string itemName)
    {
        try
        {
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

            switch (itemName)
            {
                case "OEE Dashboard":
                    _nav.NavigateMain<OEEDashboard>();
                    break;

                case "Machine Summary":
                    _nav.NavigateMain<OeeDashboard2>();
                    break;

                case "KPI Monitoring":
                    _nav.NavigateMain<OeeDashboardNew>();
                    break;

                case "System Settings":
                    _nav.NavigateToSystemSettings();
                    break;

                case "Log Config":
                    _nav.NavigateMain<LogListView>();
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

                case "Manual Operation":
                    _nav.NavigateMain<ManualOperation>();
                    break;

                case "Mode Of Operation":
                    _nav.NavigateMain<ModeOfOperation>();
                    break;

                case "PLC IO":
                    _nav.NavigateMain<PLCIOView>();
                    break;

                case "Tag Control":
                    _nav.NavigateMain<TagControlView>();
                    break;

                case "PLC TAG Config":
                    _nav.NavigateMain<PLCTagListView>();
                    break;

                case "Audit Logs":
                    _nav.NavigateToLogs(LogType.Audit);
                    break;

                case "Error Logs":
                    _nav.NavigateToLogs(LogType.Error);
                    break;

                case "Production Logs":
                    _nav.NavigateToLogs(LogType.Production);
                    break;

                case "Diagnostics Logs":
                    _nav.NavigateToLogs(LogType.Diagnostics);
                    break;
            }
        }
        catch (Exception)
        {
            // Exception swallowed to prevent application crash
        }
    }
}
