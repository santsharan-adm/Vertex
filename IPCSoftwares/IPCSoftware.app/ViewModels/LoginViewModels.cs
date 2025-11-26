using IPCSoftware.App.NavServices;
using IPCSoftware.App.Views;
using IPCSoftware.AppLogger.Interfaces;
using IPCSoftware.AppLogger.Models;
using IPCSoftware.Core.Interfaces;
using IPCSoftware.Shared;
using Microsoft.Extensions.DependencyInjection;
using System.Windows.Input;

namespace IPCSoftware.App.ViewModels
{
    public class LoginViewModel : BaseViewModel
    {
        private readonly IAuthService _authService;
        private readonly INavigationService _navigation;
        private readonly IDialogService _dialog;
        private readonly IAppLogger _logger;



        public ICommand LoginCommand { get; }

        public string Username { get => _username; set => SetProperty(ref _username, value); }
        private string _username;

        public string Password { get => _password; set => SetProperty(ref _password, value); }
        private string _password;


        private bool _isLoading;

        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        public LoginViewModel(
            IAuthService authService,
            INavigationService navigation,
            IDialogService dialog,
            IAppLogger logger,
            MainWindowViewModel? mainWindowViewModel)
        {
            _authService = authService;
            _navigation = navigation;
            _dialog = dialog;
            _logger = logger;

            LoginCommand = new RelayCommand(async () => await ExecuteLoginAsync());
        }

       /* private void ExecuteLogin()
        {


            _logger.LogInfo($"Login attempt: {Username}", LogType.Audit);

            var result = _authService.Login(Username, Password);
            if (!result.Success)
            {
                _logger.LogError($"Login failed: {Username}", LogType.Error);
                _dialog.ShowMessage("Invalid username or password.");
                return;
            }

            _logger.LogInfo($"Login successful: {Username}", LogType.Audit);

             AppInitializationService.InitializeAllServicesAsync();

            // Set session
            UserSession.Username = Username;
            UserSession.Role = result.Role;

            // Create Ribbon
            var ribbonVM = App.ServiceProvider.GetService<RibbonViewModel>();
            var ribbonView = new RibbonView { DataContext = ribbonVM };

            // Load Ribbon
            _navigation.NavigateTop(ribbonView);

            // Load Dashboard
            _navigation.NavigateMain<OEEDashboard>();
        }
*/

        private async Task ExecuteLoginAsync()
        {
            if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Password))
            {
                _dialog.ShowMessage("Please enter username and password.");
                return;
            }

            IsLoading = true;

            try
            {
                _logger.LogInfo($"Login attempt: {Username}", LogType.Audit);

                // Authenticate against CSV
                var result = await _authService.LoginAsync(Username, Password);

                if (!result.Success)
                {
                    _logger.LogError($"Login failed: {Username}", LogType.Error);
                    _dialog.ShowMessage("Invalid username or password.");
                    return;
                }

                _logger.LogInfo($"Login successful: {Username}", LogType.Audit);

                // Initialize all services (loads all CSV files)
                await AppInitializationService.InitializeAllServicesAsync();

                // Set session
                UserSession.Username = Username;
                UserSession.Role = result.Role;

                // Create Ribbon
                var ribbonVM = App.ServiceProvider.GetService<RibbonViewModel>();
                var ribbonView = new RibbonView { DataContext = ribbonVM };

                // Load Ribbon
                _navigation.NavigateTop(ribbonView);

                // Load Dashboard
                _navigation.NavigateMain<OEEDashboard>();
                _navigation.NavigateMain<DashboardView>();
            }
            catch (System.Exception ex)
            {
                _logger.LogError($"Login error: {ex.Message}", LogType.Error);
                _dialog.ShowMessage($"Login error: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }


    }
}
