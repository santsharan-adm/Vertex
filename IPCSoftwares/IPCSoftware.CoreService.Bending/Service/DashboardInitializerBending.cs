using IPCSoftware.Core.Interfaces;
using IPCSoftware.Core.Interfaces.AppLoggerInterface;
using IPCSoftware.Devices.PLC;
using IPCSoftware.Devices.UI;
using IPCSoftware.Engine;
using IPCSoftware.Shared.Models;
using IPCSoftware.Shared.Models.Messaging;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace IPCSoftware.CoreService.Bending.Service
{
    public class DashboardInitializerBending : DashboardInitializerBase
    {
        public DashboardInitializerBending(
            PLCClientManager manager,
            AlgorithmAnalysisService algo,
            OeeEngineBending oee,
            ShiftResetService shiftReset,
            SystemMonitorService systemMonitor,
            UiListener ui,
            AlarmService alarmService,
            CCDTriggerServiceBending ccdTrigger,
            IAppLogger logger)
            : base(manager, algo, oee, shiftReset, systemMonitor, ui, alarmService, ccdTrigger, logger)
        {
        }

        public override async Task<ResponsePackage> HandleUiRequest(RequestPackage request)
        {
            Debug.WriteLine($"[Bending] HandleUiRequest called → RequestId={request.RequestId}");

            // ----------------------------------------------------------------
            // DashboardInspectionModelBatch1
            // ----------------------------------------------------------------

            if (request.RequestId == 11)
            {
                return await DashboardInspectionModelBatch1(request);
            }
            // ----------------------------------------------------------------
            // DashboardInspectionModelBatch2
            // ----------------------------------------------------------------

            if (request.RequestId == 12)
            {
                return await DashboardInspectionModelBatch2(request);
            }
            // ----------------------------------------------------------------
            // BendingIndicators
            // ----------------------------------------------------------------

            if (request.RequestId == 13)
            {
                return await BendingIndicators(request);
            }
            // ----------------------------------------------------------------
            // Turntable1
            // ----------------------------------------------------------------

            if (request.RequestId == 14)
            {
                return await Turntable1(request);
            }
            // ----------------------------------------------------------------
            // Turntable2
            // ----------------------------------------------------------------

            if (request.RequestId == 15)
            {
                return await Turntable2(request);
            }
            // ----------------------------------------------------------------
            // TransferModule
            // ----------------------------------------------------------------

            if (request.RequestId == 16)
            {
                return await TransferModule(request);
            }
            // ----------------------------------------------------------------
            // InspectionData
            // ----------------------------------------------------------------

            if (request.RequestId == 17)
            {
                return await InspectionData(request);
            }
            // ----------------------------------------------------------------
            // RobotStatus
            // ----------------------------------------------------------------

            if (request.RequestId == 18)
            {
                return await RobotStatus(request);
            }
            // ----------------------------------------------------------------
            // InputTray
            // ----------------------------------------------------------------

            if (request.RequestId == 19)
            {
                return await InputTray(request);
            }
            // ----------------------------------------------------------------
            // OutputTray
            // ----------------------------------------------------------------

            if (request.RequestId == 20)
            {
                return await OutputTray(request);
            }
            // ----------------------------------------------------------------
            // NGBin1
            // ----------------------------------------------------------------

            if (request.RequestId == 21)
            {
                return await NGBin1(request);
            }

            // 
            // if (request.RequestId == 22)
            // {
            //     return await EfficiencyBreakdownPoller(request);
            // }

            // ----------------------------------------------------------------
            // DashboardInspectionModelBatch3
            // ----------------------------------------------------------------
            if (request.RequestId == 23)
            {
                return await DashboardInspectionModelBatch3(request);
            }
            // ----------------------------------------------------------------
            // DashboardInspectionModelBatch4
            // ----------------------------------------------------------------

            if (request.RequestId == 24)
            {
                return await DashboardInspectionModelBatch4(request);
            }
            // ----------------------------------------------------------------
            // NGBin2
            // ----------------------------------------------------------------

            if (request.RequestId == 25)
            {
                return await NGBin2(request);
            }
            // ----------------------------------------------------------------
            // Bending1Monitor
            // ----------------------------------------------------------------

            if (request.RequestId == 26)
            {
                return await Bending1Monitor(request);
            }
            // ----------------------------------------------------------------
            // Bending2Monitor
            // ----------------------------------------------------------------

            if (request.RequestId == 27)
            {
                return await Bending2Monitor(request);
            }
            // ----------------------------------------------------------------
            // Bending3Monitor
            // ----------------------------------------------------------------

            if (request.RequestId == 28)
            {
                return await Bending3Monitor(request);
            }

            // ----------------------------------------------------------------
            // Bending3Monitor
            // ----------------------------------------------------------------

            if (request.RequestId == 29)
            {
                return await PostBendingMonitor(request);
            }

            return await base.HandleUiRequest(request);
        }

        private Task<ResponsePackage> DashboardInspectionModelBatch1(RequestPackage request)
        {
            return Task.FromResult(GetPacketResponse(request, 1));
        }

        private Task<ResponsePackage> DashboardInspectionModelBatch2(RequestPackage request)
        {
            return Task.FromResult(GetPacketResponse(request, 1));
        }

        private Task<ResponsePackage> BendingIndicators(RequestPackage request)
        {
            return Task.FromResult(GetPacketResponse(request, 1));
        }

        private Task<ResponsePackage> Turntable1(RequestPackage request)
        {
            return Task.FromResult(GetPacketResponse(request, 1));
        }

        private Task<ResponsePackage> Turntable2(RequestPackage request)
        {
            return Task.FromResult(GetPacketResponse(request, 1));
        }

        private Task<ResponsePackage> TransferModule(RequestPackage request)
        {
            return Task.FromResult(GetPacketResponse(request, 1));
        }

        private Task<ResponsePackage> InspectionData(RequestPackage request)
        {
            return Task.FromResult(GetPacketResponse(request, 1));
        }

        private Task<ResponsePackage> RobotStatus(RequestPackage request)
        {
            return Task.FromResult(GetPacketResponse(request, 1));
        }

        private Task<ResponsePackage> InputTray(RequestPackage request)
        {
            return Task.FromResult(GetPacketResponse(request, 1));
        }

        private Task<ResponsePackage> OutputTray(RequestPackage request)
        {
            return Task.FromResult(GetPacketResponse(request, 1));
        }

        private Task<ResponsePackage> NGBin1(RequestPackage request)
        {
            return Task.FromResult(GetPacketResponse(request, 1));
        }

        private Task<ResponsePackage> DashboardInspectionModelBatch3(RequestPackage request)
        {
            return Task.FromResult(GetPacketResponse(request, 1));
        }

        private Task<ResponsePackage> DashboardInspectionModelBatch4(RequestPackage request)
        {
            return Task.FromResult(GetPacketResponse(request, 1));
        }

        private Task<ResponsePackage> NGBin2(RequestPackage request)
        {
            return Task.FromResult(GetPacketResponse(request, 1));
        }

        private Task<ResponsePackage> Bending1Monitor(RequestPackage request)
        {
            return Task.FromResult(GetPacketResponse(request, 1));
        }

        private Task<ResponsePackage> Bending2Monitor(RequestPackage request)
        {
            return Task.FromResult(GetPacketResponse(request, 1));
        }

        private Task<ResponsePackage> Bending3Monitor(RequestPackage request)
        {
            return Task.FromResult(GetPacketResponse(request, 1));
        }
        private Task<ResponsePackage> PostBendingMonitor(RequestPackage request)
        {
            return Task.FromResult(GetPacketResponse(request, 1));
        }

        private ResponsePackage GetPacketResponse(RequestPackage request, int plcNo)
        {
            if (!_latestPackets.TryGetValue(plcNo, out var packet))
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
    }
}