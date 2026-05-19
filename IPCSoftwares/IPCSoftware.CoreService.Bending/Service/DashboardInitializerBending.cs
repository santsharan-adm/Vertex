using IPCSoftware.Core.Interfaces;
using IPCSoftware.Core.Interfaces.AppLoggerInterface;
using IPCSoftware.Devices.PLC;
using IPCSoftware.Devices.UI;
using IPCSoftware.Engine;
using IPCSoftware.Shared.Models;
using IPCSoftware.Shared.Models.ConfigModels;
using IPCSoftware.Shared.Models.Messaging;
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
            CCDTriggerServiceBending ccdTrigger,

            IAppLogger logger) : base(manager, algo, oee, shiftReset, systemMonitor, ui, alarmService, ccdTrigger, logger)
        {
        }

        // ----------------------------------------------------------------
        // Override: route Bending-specific IDs 11-22 and 1001/1021/1041/1061
        // to raw packet values.
        // All other IDs fall through to the base (1-8 standard handlers).
        // ----------------------------------------------------------------
        public override async Task<ResponsePackage> HandleUiRequest(RequestPackage request)
        {
            if (request.RequestId >= 11 && request.RequestId <= 22)
            {
                if (!_latestPackets.TryGetValue(1, out var packet))
                {
                    return new ResponsePackage
                    {
                        ResponseId = request.RequestId,
                        Parameters = _lastValues ?? new Dictionary<int, object>()
                    };
                }

                return new ResponsePackage
                {
                    ResponseId = request.RequestId,
                    Parameters = packet.Values
                };
            }

            // Route Bending screen IDs to their corresponding PLC unit packets
            int plcUnit = request.RequestId switch
            {
                1001 => 1,
                1021 => 2,
                1041 => 3,
                1061 => 4,
                _    => -1
            };

            if (plcUnit != -1)
            {
                if (!_latestPackets.TryGetValue(plcUnit, out var packet))
                {
                    return new ResponsePackage
                    {
                        ResponseId = request.RequestId,
                        Parameters = _lastValues ?? new Dictionary<int, object>()
                    };
                }

                return new ResponsePackage
                {
                    ResponseId = request.RequestId,
                    Parameters = packet.Values
                };
            }

            return await base.HandleUiRequest(request);
        }
    }

}

