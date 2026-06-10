using IPCSoftware.Core.Interfaces;
using IPCSoftware.Core.Interfaces.AppLoggerInterface;
using IPCSoftware.CoreService.Bending.Service;
using IPCSoftware.Devices.PLC;
using IPCSoftware.Devices.UI;
using IPCSoftware.Engine;
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
            SystemMonitorServiceAOI systemMonitor,
            UiListener ui,
            AlarmService alarmService,
            AOIProcessService aoiProcess,
            CCDTriggerServiceAOI ccdTrigger,
          
            IAppLogger logger) : base(manager, algo, oee, shiftReset, systemMonitor, ui, alarmService, ccdTrigger, aoiProcess , logger)
        {
        }   
    }
}
