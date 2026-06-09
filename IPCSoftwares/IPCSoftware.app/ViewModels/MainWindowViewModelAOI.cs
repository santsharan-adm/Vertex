using IPCSoftware.Common.UIClientComm;
using IPCSoftware.Core.Interfaces;
using IPCSoftware.Core.Interfaces.AppLoggerInterface;
using IPCSoftware.Shared;
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
        public bool _plcConnected;
        public bool PLCConnected
        {
            get => _plcConnected;
            set => SetProperty(ref _plcConnected, value);
        }
        

        


        public MainWindowViewModelAOI(  
        INavigationService nav,
        CoreClient coreClient,
        IDialogService dialog,
        RibbonViewModelAOI ribbonVM,
        AlarmViewModel alarmVM,
        IOptionsMonitor<AboutSettings> aboutMonitor,
        IAppLogger logger) : base(nav, coreClient,dialog,ribbonVM,alarmVM,aboutMonitor, logger)
         { }


        protected override  Task UpdateTaskbarItemsFromService(Dictionary<int, object> data)
        {
            //Update the taskbar items based on the data received from the service
            if (data.TryGetValue(1, out object item))
            {
                // Update the first taskbar item
                var model = DeSerealiiseObjectHelper.Deserialize<TaskbarItems>(item);
                PLCConnected = model.IsPLC1Connected;
                MacMiniConnected = model.IsMacMiniConnected;
                CurrentMachineMode= model.CurrentMachineMode;
            }
            else
            {
                PLCConnected = false;
                MacMiniConnected = false;
                CurrentMachineMode = "NA";
            }
            return base.UpdateTaskbarItemsFromService(data);
        }
    }
}
