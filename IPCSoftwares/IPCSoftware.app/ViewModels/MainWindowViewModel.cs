using IPCSoftware.App.Services; // Ensure this is using your CoreClient namespace
using IPCSoftware.App.ViewModels;
using IPCSoftware.App.Views;
using IPCSoftware.Core.Interfaces;
using IPCSoftware.Core.Interfaces.AppLoggerInterface;
using IPCSoftware.Shared;
using IPCSoftware.Shared.Models.ConfigModels;
using IPCSoftware.Shared.Models.Messaging;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reflection;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace IPCSoftware.App.ViewModels
{
    public class MainWindowViewModel : BaseViewModel
    {
        private readonly INavigationService _nav;
        private readonly IDialogService _dialog;
        private readonly IAppLogger _logger;
        private readonly CoreClient _coreClient; // <--- 1. Add CoreClient Field

        public ICommand SidebarItemClickCommand { get; }
        public RibbonViewModel RibbonVM { get; }
        public ICommand CloseSidebarCommand { get; }
        public ICommand MinimizeAppCommand { get; }
        public ICommand CloseAppCommand { get; }

        // --- ALARM BANNER COMMANDS & PROPERTIES ---
        public ICommand CloseAlarmBannerCommand { get; }

        private bool _isAlarmBannerVisible;
        public bool IsAlarmBannerVisible
        {
            get => _isAlarmBannerVisible;
            set => SetProperty(ref _isAlarmBannerVisible, value);
        }

        private string _alarmBannerMessage;
        public string AlarmBannerMessage
        {
            get => _alarmBannerMessage;
            set => SetProperty(ref _alarmBannerMessage, value);
        }

        private string _alarmBannerColor = "#D32F2F"; // Default Red
        public string AlarmBannerColor
        {
            get => _alarmBannerColor;
            set => SetProperty(ref _alarmBannerColor, value);
        }
        // ------------------------------------------

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
                if (value) IsSidebarOpen = true;
                else IsSidebarOpen = false;
            }
        }

        public string AppVersion => "AOI System v1.0.3";

        // 2. Add CoreClient to Constructor
        public MainWindowViewModel(INavigationService nav, IAppLogger logger, IDialogService dialog, RibbonViewModel ribbonVM, CoreClient coreClient)
        {
            _nav = nav;
            _logger = logger;
            _dialog = dialog;
            RibbonVM = ribbonVM;
            _coreClient = coreClient; // Store it

            // 3. Subscribe to Alarm Events
            _coreClient.OnAlarmMessageReceived += OnAlarmReceived;

            _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _timer.Tick += (s, e) => SystemTime = DateTime.Now.ToString("dd-MMM-yyyy HH:mm:ss");
            _timer.Start();
            SystemTime = DateTime.Now.ToString("dd-MMM-yyyy HH:mm:ss");

            RibbonVM.ShowSidebar = LoadSidebarMenu;
            RibbonVM.OnLogout = ResetLandingState;
            RibbonVM.OnLandingPageRequested = ResetLandingState;

            CloseAppCommand = new RelayCommand(ExecuteCloseApp);
            MinimizeAppCommand = new RelayCommand(ExecuteMinimizeApp);
            SidebarItemClickCommand = new RelayCommand<string>(OnSidebarItemClick);
            CloseSidebarCommand = new RelayCommand(() => IsSidebarOpen = false);

            // Command to close banner manually
            CloseAlarmBannerCommand = new RelayCommand(() => IsAlarmBannerVisible = false);

            UserSession.OnSessionChanged += () =>
            {
                OnPropertyChanged(nameof(IsRibbonVisible));
                OnPropertyChanged(nameof(CurrentUserName));
                OnPropertyChanged(nameof(IsAdmin));
            };
        }

        // 4. Handle the Alarm
        private void OnAlarmReceived(AlarmMessage msg)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                if (msg.MessageType == AlarmMessageType.Raised)
                {
                    // Format message
                    AlarmBannerMessage = $"⚠️ ALARM {msg.AlarmInstance.AlarmNo}: {msg.AlarmInstance.AlarmText}";

                    // Set color based on severity (Optional logic)
                    if (msg.AlarmInstance.Severity == "High") AlarmBannerColor = "#D32F2F"; // Red
                    else if (msg.AlarmInstance.Severity == "Warning") AlarmBannerColor = "#F57C00"; // Orange
                    else AlarmBannerColor = "#1976D2"; // Blue

                    IsAlarmBannerVisible = true; // SHOW BANNER
                }
                // Optional: Auto-hide on Clear?
                // else if (msg.MessageType == AlarmMessageType.Cleared) { IsAlarmBannerVisible = false; }
            });
        }

        private void ResetLandingState()
        {
            existingUserControl = string.Empty;
            IsSidebarOpen = false;
            IsSidebarDocked = false;
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

        public ObservableCollection<string> SidebarItems { get; } = new ObservableCollection<string>();

        private string existingUserControl = string.Empty;
        private string currentRibbonKey = string.Empty;

        private void LoadSidebarMenu((string Key, List<string> Items) menu)
        {
            SidebarItems.Clear();
            foreach (var item in menu.Items) SidebarItems.Add(item);

            if (currentRibbonKey == menu.Key)
            {
                if (!IsSidebarDocked) IsSidebarOpen = !IsSidebarOpen;
                return;
            }

            IsSidebarOpen = true;
            currentRibbonKey = menu.Key;
        }

        private void ExecuteMinimizeApp()
        {
            if (Application.Current.MainWindow != null)
                Application.Current.MainWindow.WindowState = WindowState.Minimized;
        }

        private void ExecuteCloseApp()
        {
            bool confirm = _dialog.ShowYesNo("Close the application?", "Confirm Exit");
            if (confirm) Application.Current.Shutdown();
        }

        private void OnSidebarItemClick(string itemName)
        {
            if (!IsSidebarDocked) IsSidebarOpen = false;

            if (string.IsNullOrWhiteSpace(existingUserControl)) existingUserControl = itemName;
            else if (existingUserControl == itemName) return;
            else existingUserControl = itemName;

            switch (itemName)
            {
                case "OEE Dashboard": _nav.NavigateMain<OEEDashboard>(); break;
                case "Machine Summary": _nav.NavigateMain<OeeDashboard2>(); break;
                case "KPI Monitoring": _nav.NavigateMain<OeeDashboardNew>(); break;
                case "System Settings": _nav.NavigateToSystemSettings(); break;
                case "Log Config": _nav.NavigateMain<LogListView>(); break;
                case "Device Config": _nav.NavigateMain<DeviceListView>(); break;
                case "Alarm Config": _nav.NavigateMain<AlarmListView>(); break;
                case "User Config": _nav.NavigateMain<UserListView>(); break;
                case "Manual Operation": _nav.NavigateMain<ManualOperation>(); break;
                case "Mode Of Operation": _nav.NavigateMain<ModeOfOperation>(); break;
                case "PLC IO": _nav.NavigateMain<PLCIOView>(); break;
                case "Alarm View": _nav.NavigateMain<AlarmView>(); break;
                case "Tag Control": _nav.NavigateMain<TagControlView>(); break;
                case "PLC TAG Config": _nav.NavigateMain<PLCTagListView>(); break;
                case "Audit Logs": _nav.NavigateToLogs(LogType.Audit); break;
                case "Error Logs": _nav.NavigateToLogs(LogType.Error); break;
                case "Production Logs": _nav.NavigateToLogs(LogType.Production); break;
                case "Diagnostics Logs": _nav.NavigateToLogs(LogType.Diagnostics); break;
            }
        }
    }
}