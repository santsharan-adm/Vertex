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

    /// ViewModel responsible for handling the login process of the application.
    /// Handles authentication, session initialization, and navigation to main views.
    
    public class LoginViewModel : BaseViewModel
    {
        // ------------------------Dependencies------------------------//


        private readonly IAuthService _authService;                                   // Handles user authentication logic
        private readonly INavigationService _navigation;                              // Handles navigation between application views
        private readonly IDialogService _dialog;                                         // Displays messages and dialogs to the user
        private readonly IAppLogger _logger;                                              // Logs audit and error messages


        // ----------------------Commands------------------------//


        /// Command bound to the "Login" button in the UI.
        /// Executes the asynchronous login operation.
        
        public ICommand LoginCommand { get; }


        // ------------------------Bindable Properties------------------//


        /// Username entered by the user in the login form.
        public string Username { get => _username; set => SetProperty(ref _username, value); }
        private string _username;


        /// Password entered by the user in the login form.
        public string Password { get => _password; set => SetProperty(ref _password, value); }
        private string _password;


        private bool _isLoading;

        //public bool IsLoading
        //{
        //    get => _isLoading;
        //    set => SetProperty(ref _isLoading, value);
        //}


        private bool _isUsernameFocused;

        /// Determines whether the username textbox should be focused on page load.
        /// Helps improve user experience.

        public bool IsUsernameFocused
        {
            get => _isUsernameFocused;
            set => SetProperty(ref _isUsernameFocused, value);

        }


        // ---------------------Constructor--------------------//


        /// Initializes the LoginViewModel with all required dependencies.
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

            // Automatically focus on the username field when the view loads
            IsUsernameFocused = true;

            // Bind the LoginCommand to the ExecuteLoginAsync method

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


        // -------------------------Core Logic ------------------------//


        /// Asynchronous method that handles the login process.
        /// Validates input, authenticates user, initializes services,
        /// and navigates to the main dashboard if successful.

        private async Task ExecuteLoginAsync()
        {

            // Validate that both username and password are entered

            if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Password))
            {
                _dialog.ShowMessage("Please enter username and password.");
                return;
            }

         //   IsLoading = true;

            try
            {

                // Log audit info - login attempt

                _logger.LogInfo($"Login attempt: {Username}", LogType.Audit);

                // Authenticate user credentials (from CSV or another data source)
                var result = await _authService.LoginAsync(Username, Password);

                // If authentication fails, log and display error
                if (!result.Success)
                {
                    _logger.LogError($"Login failed: {Username}", LogType.Error);
                    _dialog.ShowMessage("Invalid username or password.");
                    return;
                }

                // Log audit info - successful login
                _logger.LogInfo($"Login successful: {Username}", LogType.Audit);

                // Initialize all core services (loads configuration and CSV files)   
                await AppInitializationService.InitializeAllServicesAsync();

                // Set global user session data
                UserSession.Username = Username;
                UserSession.Role = result.Role;

                // Create and bind the RibbonView (top navigation bar)
                var ribbonVM = App.ServiceProvider.GetService<RibbonViewModel>();
                var ribbonView = new RibbonView { DataContext = ribbonVM };

                // Load ribbon view in the top navigation area
                _navigation.NavigateTop(ribbonView);

                // Load main dashboard after successful login
                _navigation.NavigateMain<OEEDashboard>();
                _navigation.NavigateMain<DashboardView>();
            }
            catch (System.Exception ex)
            {

                // Handle unexpected errors gracefully

                _logger.LogError($"Login error: {ex.Message}", LogType.Error);
                _dialog.ShowMessage($"Login error: {ex.Message}");
            }
          
        }


    }



}
