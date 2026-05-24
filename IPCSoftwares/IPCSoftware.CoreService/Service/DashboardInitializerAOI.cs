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

namespace IPCSoftware.CoreService.AOI.Service
{
    public class DashboardInitializerAOI : DashboardInitializerBase
    {
        public DashboardInitializerAOI(PLCClientManager manager,
            AlgorithmAnalysisService algo,
            OeeEngineAOI oee,
            ShiftResetService shiftReset,
            SystemMonitorService systemMonitor,
            UiListener ui,
            AlarmService alarmService,
            ExternalParameters externalParameters,
            CCDTriggerServiceAOI ccdTrigger,
          
            IAppLogger logger) : base(manager, algo, oee, shiftReset, systemMonitor, ui, alarmService,externalParameters ,ccdTrigger,logger)
        {
        }   
    }
}
