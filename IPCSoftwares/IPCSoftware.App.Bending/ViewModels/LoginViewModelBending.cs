using IPCSoftware.App.Bending.Views;
using IPCSoftware.Common.CommonFunctions;
using IPCSoftware.Core.Interfaces;
using IPCSoftware.Core.Interfaces.AppLoggerInterface;
using IPCSoftware.Services;
using IPCSoftware.Shared;
using IPCSoftware.Shared.Models.ConfigModels;
using IPCSoftware.UI.CommonViews;
using IPCSoftware.UI.CommonViews.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Window;

namespace IPCSoftware.App.Bending.ViewModels
{
    public class LoginViewModelBending : LoginViewModelBase
    {
        private readonly IDialogService _dialog;
        private readonly IAuthService _authService;
        private readonly INavigationService _navigation;
        private readonly RibbonViewModelBending _ribbonVM;
        public LoginViewModelBending(IAuthService authService,
            INavigationService navigation,
            IDialogService dialog,
            MainWindowViewModelBase? mainWindowViewModel,
            RibbonViewModelBending ribbonVM,
            IAppLogger logger) : base(authService, navigation, dialog, mainWindowViewModel, ribbonVM, logger)
        {
            _dialog = dialog;
            _authService = authService;
            _navigation = navigation;
            _ribbonVM = ribbonVM;

        }
        public override async Task ExecuteLoginAsync()
        {
            if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Password))
            {
                _dialog.ShowMessage("Please enter username and password.");
                return;
            }

            //   IsLoading = true;

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
                //var ribbonVM = ServiceLocator.GetService<RibbonViewModel>();
                var ribbonView = new RibbonViewBending { DataContext = _ribbonVM };

                // Load Ribbon
                _navigation.NavigateTop(ribbonView);

                // Load Dashboard
                // _navigation.NavigateMain<OEEDashboard>();
                _navigation.NavigateMain<WelcomePageView>();
            }
            catch (System.Exception ex)
            {
                _logger.LogError($"Login error: {ex.Message}", LogType.Error);
                _dialog.ShowMessage($"Login error: {ex.Message}");
            }

        }


    }
}
