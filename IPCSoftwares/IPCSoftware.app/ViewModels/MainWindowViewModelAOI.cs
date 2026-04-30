using IPCSoftware.Common.UIClientComm;
using IPCSoftware.Core.Interfaces;
using IPCSoftware.Core.Interfaces.AppLoggerInterface;
using IPCSoftware.Shared.Models;
using IPCSoftware.UI.CommonViews.ViewModels;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IPCSoftware.App.AOI.ViewModels
{
    public class MainWindowViewModelAOI : MainWindowViewModelBase
    {
        public MainWindowViewModelAOI(  
        INavigationService nav,
        CoreClient coreClient,
        IDialogService dialog,
        RibbonViewModelAOI ribbonVM,
        AlarmViewModel alarmVM,
        IOptionsMonitor<AboutSettings> aboutMonitor,
        IAppLogger logger) : base(nav, coreClient,dialog,ribbonVM,alarmVM,aboutMonitor, logger)
         { }

    }
}
