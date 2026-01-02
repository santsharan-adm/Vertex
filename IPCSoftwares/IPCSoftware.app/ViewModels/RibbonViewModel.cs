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

    public ICommand NavigateDashboardCommand { get; }

    public ICommand NavigateSettingsCommand { get; }
    public ICommand NavigateLogsCommand { get; }
    public ICommand NavigateUserMgmtCommand { get; }
    public ICommand NavigateLandingPageCommand { get; }
   // public ICommand NavigateReportConfigCommand { get; }
    public ICommand NavigateReportsCommand { get; }

    public ICommand LogoutCommand { get; }
    public Action OnLogout { get; set; }
    public Action OnLandingPageRequested { get; set; }


    private (string Key, List<string> Items)? _currentMenu;

    public Action<(string Key, List<string> Items)> ShowSidebar { get; set; }   // NEW

    public RibbonViewModel(
        INavigationService nav,
        IDialogService dialog,
        IAppLogger logger) : base(logger)
    {
        _nav = nav;

        NavigateDashboardCommand = new RelayCommand(OpenDashboardMenu);
        NavigateSettingsCommand = new RelayCommand(OpenSettingsMenu);
        NavigateLogsCommand = new RelayCommand(OpenLogsMenu);
        NavigateUserMgmtCommand = new RelayCommand(OpenUserMgtMenu);
       // NavigateReportConfigCommand = new RelayCommand(OpenReportConfig);
        NavigateReportsCommand = new RelayCommand(OpenReportsView);

        LogoutCommand = new RelayCommand(Logout);
        NavigateLandingPageCommand = new RelayCommand(OpenLandingPage);
        _dialog = dialog;
    }

    public bool IsAdmin => UserSession.Role == "Admin";
    public string CurrentUserName => CultureInfo.CurrentCulture.TextInfo.ToTitleCase(UserSession.Username.ToLower()) ?? "Guest";


    private void OpenDashboardMenu()
    {
        try
        {
            LoadMenu(new List<string>
            {
                "OEE Dashboard",
                "Time Sync",
                "Alarm View"

            }, nameof(OpenDashboardMenu));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.Message, LogType.Diagnostics);
        }
    }

    private void OpenSettingsMenu()
    {
        try
        {
            LoadMenu(new List<string>
            {
                "Mode Of Operation",
                "Servo Parameters",
                "PLC IO",
                "Diagnostic",
                
            }, nameof(OpenSettingsMenu));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.Message, LogType.Diagnostics);
        }
    }

    private void OpenLogsMenu()
    {
        try
        {
            LoadMenu(new List<string>
            {
                "Audit Logs",
                "Error Logs",
                "Diagnostics Logs"
            }, nameof(OpenLogsMenu));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.Message, LogType.Diagnostics);
        }
     
    }

    private void OpenReportsView()
    {
        try
        {
            LoadMenu(new List<string>
            {
                "Production Data",
                "Production Images"

            }, nameof(OpenReportsView));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.Message, LogType.Diagnostics);
        }
       // _nav.NavigateMain<ReportViewerView>();
    }


    private void OpenUserMgtMenu()
    {
        try
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
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.Message, LogType.Diagnostics);
        }

    }

    private void Logout()
    {
        try
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
        catch (Exception ex)
        {
            _logger.LogError(ex.Message, LogType.Diagnostics);
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
        ShowSidebar?.Invoke((key, items));
    }
}
