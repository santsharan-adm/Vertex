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
using System.Windows.Input;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Window;

namespace IPCSoftware.App.AOI.ViewModels
{
    public class LoginViewModelAOI : LoginViewModelBase
    {
        public LoginViewModelAOI(IAuthService authService,
            INavigationService navigation,
            IDialogService dialog,
            MainWindowViewModelBase? mainWindowViewModel,
            RibbonViewModelAOI ribbonVM,
            IAppLogger logger) : base(authService, navigation, dialog, mainWindowViewModel, ribbonVM, logger)
        {
            
        }

        
    }
}
