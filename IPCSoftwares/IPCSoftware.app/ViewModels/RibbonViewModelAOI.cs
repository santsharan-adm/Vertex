using IPCSoftware.Core.Interfaces;
using IPCSoftware.Core.Interfaces.AppLoggerInterface;
using IPCSoftware.Shared.Models;
using IPCSoftware.Shared.Models.ConfigModels;
using IPCSoftware.UI.CommonViews;
using IPCSoftware.UI.CommonViews.ViewModels;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IPCSoftware.App.AOI.ViewModels
{
    public class RibbonViewModelAOI : RibbonViewModelBase
    {
        public RibbonViewModelAOI(IOptions<ExternalSettings> extSetting,
                                  INavigationService nav, IDialogService dialog,
                                  Func<ProcessSequenceWindow> sequenceWindowFactory,
                                  IAppLogger logger) : base(extSetting, nav, dialog, sequenceWindowFactory, logger)
        {
        }

        public override void OpenDashboardMenu()
        {
            base.OpenDashboardMenu();
            try
            {
                base.LoadMenu(new List<string>
            {
                "Dashboard",
                "Control",

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
    }
}
