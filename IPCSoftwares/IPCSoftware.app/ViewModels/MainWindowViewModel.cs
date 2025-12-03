using IPCSoftware.App;
using IPCSoftware.App.ViewModels;
using IPCSoftware.App.Views;

using IPCSoftware.Core.Interfaces;
using IPCSoftware.Shared;
using IPCSoftware.Shared.Models.ConfigModels;
using System.Collections.ObjectModel;
using System.Printing;
using System.Reflection;
using System.Windows;

using System.Windows.Input;
using System.Windows.Threading;

public class MainWindowViewModel : BaseViewModel
{
    private readonly INavigationService _nav;

    public ICommand SidebarItemClickCommand { get; }
    public RibbonViewModel RibbonVM { get; }
    public ICommand CloseSidebarCommand { get; }
    public ICommand MinimizeAppCommand { get; }
    public ICommand CloseAppCommand { get; }

    private readonly DispatcherTimer _timer;

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


    //public string AppVersion => $"Version {Assembly.GetExecutingAssembly().GetName().Version}";
    public string AppVersion => "AOI System v1.0.3";

    public MainWindowViewModel(INavigationService nav, RibbonViewModel ribbonVM)
    {
        _timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1)
        };
        _timer.Tick += (s, e) => SystemTime = DateTime.Now.ToString("dd-MMM-yyyy HH:mm:ss");
        _timer.Start();

        // Set initial time immediately
        SystemTime = DateTime.Now.ToString("dd-MMM-yyyy HH:mm:ss");

        _nav = nav;
        RibbonVM = ribbonVM;
        RibbonVM.ShowSidebar = LoadSidebarMenu;
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

    private void ResetLandingState()
    {
        existingUserControl = string.Empty;   // clear selected page
        IsSidebarOpen = false;                // close sidebar if open
    }


    // ==============================
    // RIBBON VISIBILITY PROPERTIES
    // ==============================
    public bool IsRibbonVisible => UserSession.IsLoggedIn;
    public string CurrentUserName => UserSession.Username ?? "Guest";
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
        Application.Current.Shutdown();
    }


    private void OnSidebarItemClick(string itemName)
    {
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
            case "OEE Dashboard":
                //_nav.NavigateMain<LiveOeeView>();
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

            // Config Menu
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

            //case "PLC IO":
            //    _nav.NavigateMain<PLCIOMonitor>(); 
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

        }





    }

}