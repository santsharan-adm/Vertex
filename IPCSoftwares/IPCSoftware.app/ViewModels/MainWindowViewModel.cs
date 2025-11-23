using IPCSoftware.App.ViewModels;
using IPCSoftware.App.Views;
using IPCSoftware.Core.Interfaces;
using IPCSoftware.Shared;
using IPCSoftware.Shared.IPCSoftware.Shared;
using System.Collections.ObjectModel;
using System.Windows.Input;

public class MainWindowViewModel : BaseViewModel
{
    private readonly INavigationService _nav;

    public ICommand SidebarItemClickCommand { get; }
    public RibbonViewModel RibbonVM { get; }

    public MainWindowViewModel(INavigationService nav, RibbonViewModel ribbonVM)
    {
        _nav = nav;
        RibbonVM = ribbonVM;
        RibbonVM.ShowSidebar = LoadSidebarMenu;

        SidebarItemClickCommand = new RelayCommand<string>(OnSidebarItemClick);

        UserSession.OnSessionChanged += () =>

        {
            OnPropertyChanged(nameof(IsRibbonVisible));
            OnPropertyChanged(nameof(CurrentUserName));
            OnPropertyChanged(nameof(IsAdmin));
        };
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

    // ==============================
    // RIBBON → SIDEBAR CONNECTION
    // ==============================
    /*  public void ConnectSidebar(RibbonViewModel ribbonVM)
      {
          ribbonVM.ShowSidebar = LoadSidebarMenu;
      }*/

    private void LoadSidebarMenu(List<string> items)
    {
        SidebarItems.Clear();
        foreach (var item in items)
            SidebarItems.Add(item);

        IsSidebarOpen = !IsSidebarOpen;   // MVVM triggers animation
    }


    private void OnSidebarItemClick(string itemName)
    {
        // Close sidebar
        IsSidebarOpen = false;

        // Navigate based on item name
        switch (itemName)
        {
            // Dashboard Menu
            case "Live Dashboard":
                _nav.NavigateMain<DashboardView>();
                break;
            case "Machine Summary":
                // Navigate to machine summary view
                break;
            case "Performance KPIs":
                // Navigate to performance view
                break;

            // Config Menu
            case "Log Config":
                _nav.NavigateMain<LogListView>();
                break;
            case "System Config":
                // Navigate to system config view
                break;
            case "Network Config":
                // Navigate to network config view
                break;

            // Settings Menu
            case "General Settings":
                // Navigate to general settings view
                break;
            case "User Preferences":
                // Navigate to user preferences view
                break;

            // Logs Menu
            case "System Logs":
                // Navigate to system logs view
                break;
            case "Application Logs":
                // Navigate to application logs view
                break;

            // User Management Menu (Admin only)
            case "Add User":
                // Navigate to add user view
                break;
            case "User Roles":
                // Navigate to user roles view
                break;
            case "User Audit":
                // Navigate to user audit view
                break;
        }


    }

}