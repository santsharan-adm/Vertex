using IPCSoftware.App.NavServices;
using IPCSoftware.App.Views;

using IPCSoftware.Core.Interfaces;
using IPCSoftware.Core.Interfaces.AppLoggerInterface;
using IPCSoftware.Shared;
using IPCSoftware.Shared.Models.ConfigModels;
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

        private bool _isUsernameFocused;
        public bool IsUsernameFocused
        {
            get => _isUsernameFocused;
            set => SetProperty(ref _isUsernameFocused, value);
        }

        public LoginViewModel(
            IAuthService authService,
            INavigationService navigation,
            IDialogService dialog,
            IAppLogger logger,
            MainWindowViewModel? mainWindowViewModel)
        {
            try
            {
                _authService = authService;
                _navigation = navigation;
                _dialog = dialog;
                _logger = logger;

                IsUsernameFocused = true;
                LoginCommand = new RelayCommand(async () => await ExecuteLoginAsync());
            }
            catch (System.Exception)
            {
                // Exception swallowed to prevent application crash
            }
        }

        private async Task ExecuteLoginAsync()
        {
            if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Password))
            {
                _dialog.ShowMessage("Please enter username and password.");
                return;
            }

            try
            {
                _logger.LogInfo($"Login attempt: {Username}", LogType.Audit);

                var result = await _authService.LoginAsync(Username, Password);

                if (!result.Success)
                {
                    _logger.LogError($"Login failed: {Username}", LogType.Error);
                    _dialog.ShowMessage("Invalid username or password.");
                    return;
                }

                _logger.LogInfo($"Login successful: {Username}", LogType.Audit);

                await AppInitializationService.InitializeAllServicesAsync();

                UserSession.Username = Username;
                UserSession.Role = result.Role;

                var ribbonVM = App.ServiceProvider.GetService<RibbonViewModel>();
                var ribbonView = new RibbonView { DataContext = ribbonVM };

                _navigation.NavigateTop(ribbonView);
                _navigation.NavigateMain<DashboardView>();
            }
            catch (System.Exception ex)
            {
                _logger.LogError($"Login error: {ex.Message}", LogType.Error);
                _dialog.ShowMessage($"Login error: {ex.Message}");
            }
        }
    }
}
