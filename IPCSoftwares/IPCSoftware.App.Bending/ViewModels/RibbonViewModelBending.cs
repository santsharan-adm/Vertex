using IPCSoftware.Core.Interfaces;
using IPCSoftware.Core.Interfaces.AppLoggerInterface;
using IPCSoftware.Shared;
using IPCSoftware.Shared.Models;
using IPCSoftware.Shared.Models.ConfigModels;
using IPCSoftware.UI.CommonViews;
using IPCSoftware.UI.CommonViews.ViewModels;
using IPCSoftware.App.Bending.ViewModels;
using IPCSoftware.App.Bending.Views;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Window;

namespace IPCSoftware.App.Bending.ViewModels
{
    
    public class RibbonViewModelBending : RibbonViewModelBase
    {
        private readonly INavigationService _nav;
        private readonly IDialogService _dialog;
        public RibbonViewModelBending(IOptions<ExternalSettings> extSetting, 
                                      INavigationService nav, IDialogService dialog, 
                                      Func<ProcessSequenceWindow> sequenceWindowFactory, 
                                      IAppLogger logger) : base(extSetting, nav, dialog, sequenceWindowFactory, logger)

        {
            _nav = nav;
            _dialog = dialog;
        }

        public override void OpenDashboardMenu()
        {
            base.OpenDashboardMenu();
            try
            {
                base.LoadMenu(new List<string>
            {
                "Bending1Monitor",
                "Bending2Monitor",
                "Bending3Monitor",              
                "UserControl1",
                "UserControl2",
                "PostBendingMonitor",
                "PLC IO",
                "Alarm View",
                "Startup Condition",
                "About"
                


            }, nameof(OpenDashboardMenu));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message, LogType.Diagnostics);
            }
        }


        public override void OpenLandingPage()
        {
            
            OnLandingPageRequested?.Invoke();  // notify MainWindowViewModel
            _nav.NavigateToBendingLandingPage();
        }

        public override void Logout()
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
                    _nav.NavigateMain<LoginViewBending>();
                    UserSession.Clear();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message, LogType.Diagnostics);
            }
        }
    }
}
