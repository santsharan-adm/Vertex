using IPCSoftware.Core.Interfaces;
using IPCSoftware.Core.Interfaces.AppLoggerInterface;
using IPCSoftware.Devices.PLC;
using IPCSoftware.Devices.UI;
using IPCSoftware.Engine;
using IPCSoftware.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IPCSoftware.CoreService.Bending.Service
{
    public class DashboardInitializerBending : DashboardInitializerBase
    {

        public DashboardInitializerBending(PLCClientManager manager,
            AlgorithmAnalysisService algo,
            OeeEngineBending oee,
            ShiftResetService shiftReset,
            SystemMonitorService systemMonitor,
            UiListener ui,
            AlarmService alarmService,
            ExternalParameters externalParameters,
            CCDTriggerServiceBending ccdTrigger,
           
            IAppLogger logger) : base(manager, algo, oee, shiftReset, systemMonitor, ui, alarmService, externalParameters, ccdTrigger,logger)
        {
        }
    }

}

