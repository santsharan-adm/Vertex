using IPCSoftware.App.Views;
using IPCSoftware.Core.Interfaces;
using IPCSoftware.Shared;
using System.Windows.Input;



namespace IPCSoftware.App.ViewModels;
public class RibbonViewModel : BaseViewModel
{
    private readonly INavigationService _nav;

    public ICommand NavigateDashboardCommand { get; }

    public ICommand NavigateSettingsCommand { get; }
    public ICommand NavigateLogsCommand { get; }
    public ICommand NavigateUserMgmtCommand { get; }

    
    public ICommand LogoutCommand { get; }
    public Action OnLogout { get; set; }

    public Action<List<string>> ShowSidebar { get; set; }   // NEW

    public RibbonViewModel(INavigationService nav)
    {
        _nav = nav;

        NavigateDashboardCommand = new RelayCommand(OpenDashboardMenu);
        NavigateSettingsCommand = new RelayCommand(OpenSettingsMenu);
        NavigateLogsCommand = new RelayCommand(OpenLogsMenu);
        NavigateUserMgmtCommand = new RelayCommand(OpenUserMgtMenu);

        LogoutCommand = new RelayCommand(Logout);
    }

    public bool IsAdmin => UserSession.Role == "Admin";
    public string CurrentUserName => UserSession.Username ?? "Guest";
    private void OpenDashboardMenu()
    {
        ShowSidebar?.Invoke(new List<string>
        {
            "OEE Dashboard",
            "Machine Summary",
            "Performance KPIs"
        });
    }

    private void OpenSettingsMenu()
    {
        ShowSidebar?.Invoke(new List<string>
        {
            "System Settings",
            "Manual Operation",
            "Mode Of Operation"
        });
    }

    private void OpenLogsMenu()
    {
        ShowSidebar?.Invoke(new List<string>
        {
            "System Logs",
            "Production Logs"
        });
    }

    private void OpenUserMgtMenu()
    {
        if (IsAdmin)
        {
            ShowSidebar?.Invoke(new List<string>
            {
                "Log Config",
                "Device Config",
                "Alarm Config",
                "User Config",
                "PLC TAG Config",
                "Report Config",
                "External Interface"
            });
        }
    }

    private void Logout()
    {
        OnLogout?.Invoke();
        _nav.ClearTop();
        _nav.NavigateMain<LoginView>();
        UserSession.Clear();
    }
}
