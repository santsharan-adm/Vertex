using IPCSoftware.App.Views;
using IPCSoftware.Core.Interfaces;
using IPCSoftware.Core.Interfaces.AppLoggerInterface;
using IPCSoftware.Services;
using IPCSoftware.Shared;
using IPCSoftware.Shared.Models.ConfigModels;
using System.Globalization;
using System.Windows.Input;



namespace IPCSoftware.App.ViewModels;
public class RibbonViewModel : BaseViewModel
{
    private readonly INavigationService _nav;
    private readonly IDialogService _dialog;
    private readonly IAppLogger _logger;

    public ICommand NavigateDashboardCommand { get; }

    public ICommand NavigateSettingsCommand { get; }
    public ICommand NavigateLogsCommand { get; }
    public ICommand NavigateUserMgmtCommand { get; }
    public ICommand NavigateLandingPageCommand { get; }

    
    public ICommand LogoutCommand { get; }
    public Action OnLogout { get; set; }
    public Action OnLandingPageRequested { get; set; }


    private (string Key, List<string> Items)? _currentMenu;

    public Action<(string Key, List<string> Items)> ShowSidebar { get; set; }   // NEW

    public RibbonViewModel(INavigationService nav, IAppLogger logger, IDialogService dialog)
    {
        _logger = logger;
        _nav = nav;

        NavigateDashboardCommand = new RelayCommand(OpenDashboardMenu);
        NavigateSettingsCommand = new RelayCommand(OpenSettingsMenu);
        NavigateLogsCommand = new RelayCommand(OpenLogsMenu);
        NavigateUserMgmtCommand = new RelayCommand(OpenUserMgtMenu);

        LogoutCommand = new RelayCommand(Logout);
        NavigateLandingPageCommand = new RelayCommand(OpenLandingPage);
        _dialog = dialog;
    }   

    public bool IsAdmin => UserSession.Role == "Admin";
    public string CurrentUserName => CultureInfo.CurrentCulture.TextInfo.ToTitleCase(UserSession.Username.ToLower()) ?? "Guest";


    private void OpenDashboardMenu()
    {
        LoadMenu(new List<string>
        {
            "OEE Dashboard",
            "Machine Summary",
            "KPI Monitoring"
        }, nameof(OpenDashboardMenu));
    }

    private void OpenSettingsMenu()
    {
        LoadMenu(new List<string>
        {
            "System Settings",
            "Manual Operation",
            "Mode Of Operation",
            "PLC IO",
            "Tag Control",
            "Alarm View"
        }, nameof(OpenSettingsMenu));

      /*  ShowSidebar?.Invoke(new Dictionary<string, List<string>> List<string>
        {
            "System Settings",
            "Manual Operation",
            "Mode Of Operation",
            "PLC IO"
        });*/
    }

    private void OpenLogsMenu()
    {
        LoadMenu(new List<string>
        {
            "Audit Logs",
            "Production Logs",
            "Error Logs",
            "Diagnostics Logs"
        }, nameof(OpenLogsMenu));
       /* ShowSidebar?.Invoke(new List<string>
        {
            "System Logs",
            "Production Logs"
        });*/
    }

    private void OpenUserMgtMenu()
    {

        if (IsAdmin)
        {
            LoadMenu(new List<string>
                {
                    "Log Config",
                        "Device Config",
                        "Alarm Config",
                        "User Config",
                        "PLC TAG Config",
                        "Report Config",
                        "External Interface"
                }, nameof(OpenUserMgtMenu));

            /*  ShowSidebar?.Invoke(new List<string>
              {
                  "Log Config",
                  "Device Config",
                  "Alarm Config",
                  "User Config",
                  "PLC TAG Config",
                  "Report Config",
                  "External Interface"

              });*/
        }
    }

    private void Logout()
    {
        bool confirm = _dialog.ShowYesNo("Are you sure you want to logout?", "Logout");

        if (confirm)
        {
            _logger.LogInfo($"Logout Sucess: {CurrentUserName}", LogType.Audit);
            // proceed delete
            OnLogout?.Invoke();
            _nav.ClearTop();
            _nav.NavigateMain<LoginView>();
            UserSession.Clear();
        }
       
    }


    private void OpenLandingPage()
    {
        OnLandingPageRequested?.Invoke();  // notify MainWindowViewModel
        _nav.NavigateMain<DashboardView>();
    }



    private void LoadMenu(List<string> items, string functionName)
    {
        string key = functionName.Replace("Open", "");  // "OpenDashboardMenu" → "DashboardMenu"

      /*  if (_currentMenu?.Key == key)
            return;*/ // Do nothing if user clicked same button again

       // _currentMenu = (key, items);

        // Send menu to sidebar
        ShowSidebar?.Invoke((key, items));
    }
}
