using IPCSoftware.App.ViewModels;
using IPCSoftware.App.Views;
using IPCSoftware.Core.Interfaces;
using IPCSoftware.Shared;
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
            case "OEE Dashboard":
                _nav.NavigateMain<OEEDashboard>();
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

            case "PLC IO":
                _nav.NavigateMain<PLCIOMonitor>();
                break;
            case "PLC TAG Config":
                _nav.NavigateMain<PLCTagListView>();
                break;

        }


    }

}