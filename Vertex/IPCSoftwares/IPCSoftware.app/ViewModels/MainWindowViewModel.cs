using IPCSoftware.App;
using IPCSoftware.App.ViewModels;
using IPCSoftware.App.Views;

using IPCSoftware.Core.Interfaces;
using IPCSoftware.Core.Interfaces.AppLoggerInterface;
using IPCSoftware.Shared;
using IPCSoftware.Shared.Models.ConfigModels;
using System.Collections.ObjectModel;
using System.Printing;
using System.Reflection;
using System.Windows;

using System.Windows.Input;
using System.Windows.Threading;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.StartPanel;


/// ViewModel for the Main Window of the IPC Software application.
/// 
/// Responsibilities:
/// - Handles sidebar and ribbon visibility and interaction
/// - Manages navigation across all major views
/// - Updates live system time
/// - Handles application-level actions (minimize/close)
/// - Responds to user session changes (login/logout)
public class MainWindowViewModel : BaseViewModel
{

    //------------------- Dependencies-------------------//


    private readonly INavigationService _nav;                    // Handles view navigation         
    private readonly IDialogService _dialog;                     // Shows dialogs and confirmation popups
    private readonly IAppLogger  _logger;                       // Logs system-level or user-level events


    // -------------Commands(Bound to UI Actions)-----------------//

    public ICommand SidebarItemClickCommand { get; }                             // Handles clicks on sidebar menu items
    public RibbonViewModel RibbonVM { get; }
    public ICommand CloseSidebarCommand { get; }                                 // Closes sidebar panel
    public ICommand MinimizeAppCommand { get; }                                   // Minimizes main window
    public ICommand CloseAppCommand { get; }                                      // Closes application safely


    // ----------------Timer (System Clock)----------------//

    private readonly DispatcherTimer _timer;                               // Updates the displayed system time every second

    // Live System Time Property
    private string _systemTime;

    /// Displays current system time (auto-updated every second).
    
    public string SystemTime
    {
        get => _systemTime;
        set => SetProperty(ref _systemTime, value);
    }


    // ------------------Sidebar Docking / Visibility Management------------------//

    private bool _isSidebarDocked;

    /// Indicates whether sidebar is docked (always visible).
    /// Toggling this also affects IsSidebarOpen property.
    
    public bool IsSidebarDocked
    {
        get => _isSidebarDocked;
        set
        {
            _isSidebarDocked = value;
            OnPropertyChanged();
            // When docked → always open
            // When undocked → can be closed
            if (value)
                IsSidebarOpen = true;
            else
                IsSidebarOpen = false;
        }
    }


    /// Application version shown in footer or title bar.
    public string AppVersion => "AOI System v1.0.3";


    // ---------------Constructor-------------------//
    public MainWindowViewModel(INavigationService nav, IAppLogger logger, IDialogService dialog,RibbonViewModel ribbonVM)
    {

        // Initialize real-time clock
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
        _dialog = dialog;
    }

    // ------------Ribbon Event Handlers------------//

    private void ResetLandingState()
    {
        existingUserControl = string.Empty;   // clear selected page
        IsSidebarOpen = false;                // close sidebar if open
        IsSidebarDocked = false;

      
    }
/*
    private void CloseSideBar()
    {
        IsSidebarOpen = false;
        IsSidebarDocked = false;

    }
*/

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

    /// Controls visibility of the sidebar.
    /// True → sidebar visible, False → hidden.
    public bool IsSidebarOpen
    {
        get => _isSidebarOpen;
        set { _isSidebarOpen = value; OnPropertyChanged(); }
    }


    /// Collection of sidebar menu items currently loaded (depends on ribbon selection).
    public ObservableCollection<string> SidebarItems { get; }
        = new ObservableCollection<string>();

    private string existingUserControl = string.Empty;               // Tracks currently active page/view
    private string currentRibbonKey = string.Empty;                    // Tracks which ribbon category is active



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


    // ---------------System Controls (Minimize / Close)---------------//

    /// Minimizes the main application window.
    private void ExecuteMinimizeApp()
    {
        // This finds the active main window and minimizes it
        if (Application.Current.MainWindow != null)
        {
            Application.Current.MainWindow.WindowState = WindowState.Minimized;
        }
    }

    /// Closes the application after user confirmation.
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

        // Navigate based on menu item

        switch (itemName)
        {

            // -----------Dashboard Menu-----------//


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

            // ------------Configuration Menu------------//


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


            // -----------Operations Menu-----------//
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


            //------------ Log Viewer Menu------------//
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

}