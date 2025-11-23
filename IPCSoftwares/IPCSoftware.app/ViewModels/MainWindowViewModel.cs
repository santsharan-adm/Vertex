using IPCSoftware.App.ViewModels;
using IPCSoftware.Core.Interfaces;
using IPCSoftware.Shared;
using System.Collections.ObjectModel;
using System.Windows.Input;

public class MainWindowViewModel : BaseViewModel
{
    private readonly INavigationService _nav;

   // public ICommand SidebarItemClickCommand { get; }
    public RibbonViewModel RibbonVM { get; }

    public MainWindowViewModel(INavigationService nav, RibbonViewModel ribbonVM)
    {
        _nav = nav;
        RibbonVM = ribbonVM;
        RibbonVM.ShowSidebar = LoadSidebarMenu;
        // CONNECT RIBBON → SIDEBAR
       // ConnectSidebar(RibbonVM);
        //SidebarItemClickCommand = new RelayCommand(() => ConnectSidebar(RibbonVM));
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
}
