using IPCSoftware.Core.Interfaces;
using IPCSoftware.Core.Interfaces.AppLoggerInterface;
using IPCSoftware.UI.CommonViews.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IPCSoftware.App.Bending.ViewModels
{
    public class LoginViewModelBending : LoginViewModelBase
    {
        public LoginViewModelBending(IAuthService authService,
            INavigationService navigation,
            IDialogService dialog,
            MainWindowViewModelBase? mainWindowViewModel,
            RibbonViewModelBending ribbonVM,
            IAppLogger logger) : base(authService, navigation, dialog, mainWindowViewModel, ribbonVM, logger)
        {

        }


    }
}
