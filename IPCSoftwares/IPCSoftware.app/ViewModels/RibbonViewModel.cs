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

   // public ICommand NavigateSettingsCommand { get; }
    public ICommand NavigateLogsCommand { get; }
    public ICommand NavigateUserMgmtCommand { get; }
    public ICommand NavigateLandingPageCommand { get; }
   // public ICommand NavigateReportConfigCommand { get; }
    public ICommand NavigateReportsCommand { get; }

    public ICommand LogoutCommand { get; }
    public Action OnLogout { get; set; }
    public Action OnLandingPageRequested { get; set; }

    public Action<(string Key, List<string> Items)> ShowSidebar { get; set; }   // NEW

    public RibbonViewModel(
        INavigationService nav,
        IDialogService dialog,
        IAppLogger logger) : base(logger)
    {
        _nav = nav;
        _dialog = dialog;

        NavigateDashboardCommand = new RelayCommand(OpenDashboardMenu);
       // NavigateSettingsCommand = new RelayCommand(OpenSettingsMenu);
        NavigateLogsCommand = new RelayCommand(OpenLogsMenu);
        NavigateUserMgmtCommand = new RelayCommand(OpenUserMgtMenu);
        NavigateReportsCommand = new RelayCommand(OpenReportsView);
        LogoutCommand = new RelayCommand(Logout);
        NavigateLandingPageCommand = new RelayCommand(OpenLandingPage);
    }

    //public bool IsAdmin => UserSession.Role == "Admin";

    public bool IsAdmin => string.Equals(UserSession.Role, "Admin", StringComparison.OrdinalIgnoreCase);
    public bool IsSupervisor => string.Equals(UserSession.Role, "Supervisor", StringComparison.OrdinalIgnoreCase);
    public bool IsOperator => string.Equals(UserSession.Role, "Operator", StringComparison.OrdinalIgnoreCase);

    public string CurrentUserName => CultureInfo.CurrentCulture.TextInfo.ToTitleCase(UserSession.Username.ToLower()) ?? "Guest";
    public string CurrentUserRole=> CultureInfo.CurrentCulture.TextInfo.ToTitleCase(UserSession.Role.ToLower()) ?? "Guest";
    public bool IsConfigRibbonVisible => IsAdmin || IsSupervisor;

    private void OpenDashboardMenu()
    {
        try
        {
            LoadMenu(new List<string>
            {
                "Dashboard",
                "Control",
               
                "PLC IO",
                "Alarm View",
                "Startup Condition"
              

            }, nameof(OpenDashboardMenu));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.Message, LogType.Diagnostics);
        }
    }

    //private void OpenSettingsMenu()
    //{
    //    try
    //    {
    //        LoadMenu(new List<string>
    //        {
               
             
                
    //        }, nameof(OpenSettingsMenu));
    //    }
    //    catch (Exception ex)
    //    {
    //        _logger.LogError(ex.Message, LogType.Diagnostics);
    //    }
    //}

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
            // 1. Double check: If Operator somehow clicked this, do nothing
            if (IsOperator) return;

            // 2. Base list for Supervisor and Admin
            var configItems = new List<string>
            {
                "Log Config",
                "Device Config",
                "Alarm Config",
                // "User Config" is removed from here intentionally
                "PLC TAG Config",
                "Shift Config",

                "Report Config",
                 "Servo Parameters",
                   "Time Sync",
                "Diagnostic",
                "External Interface"
            };

            // 3. Logic: Only Admin can see "User Config"
            // Insert it at a specific index or add it
            if (IsAdmin)
            {
                // Inserting after Alarm Config (index 3) to match your original order
                configItems.Insert(3, "User Config");
            }

            LoadMenu(configItems, nameof(OpenUserMgtMenu));
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
        _nav.NavigateMain<ModeOfOperation>();
    }

    private void LoadMenu(List<string> items, string functionName)
    {
        string key = functionName.Replace("Open", "");  // "OpenDashboardMenu" → "DashboardMenu"
        ShowSidebar?.Invoke((key, items));
    }
}
