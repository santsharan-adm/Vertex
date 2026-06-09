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

namespace IPCSoftware.App.Bending.ViewModels
{
    public class MainWindowViewModelBending : MainWindowViewModelBase
    {
        public bool _plc1Connected;
        public bool PLC1Connected
        {
            get => _plc1Connected;
            set => SetProperty(ref _plc1Connected, value);
        }

        public bool _plc2Connected;
        public bool PLC2Connected
        {
            get => _plc2Connected;
            set => SetProperty(ref _plc2Connected, value);
        }

        public MainWindowViewModelBending(
        INavigationService nav,
        CoreClient coreClient,
        IDialogService dialog,
        RibbonViewModelBending ribbonVM,
        AlarmViewModel alarmVM,
        IOptionsMonitor<AboutSettings> aboutMonitor,
        IAppLogger logger) : base(nav, coreClient, dialog, ribbonVM, alarmVM, aboutMonitor, logger)
        {

        }

        protected override  Task UpdateTaskbarItemsFromService(Dictionary<int, object> data)
        {
            
            //Update the taskbar items based on the data received from the service
            if (data.TryGetValue(1, out object item))
            {
                // Update the first taskbar item
                var model = DeSerealiiseObjectHelper.Deserialize<TaskbarItems>(item);
                PLC1Connected = model.IsPLC1Connected;
                PLC2Connected = model.IsPLC2Connected;
                
                MacMiniConnected = model.IsMacMiniConnected;
                CurrentMachineMode = model.CurrentMachineMode;
            }
            else
            {
                PLC1Connected = false;
                PLC2Connected = false;
                MacMiniConnected = false;
                CurrentMachineMode = "NA";
            }
            return base.UpdateTaskbarItemsFromService(data);
        }
    }
}
