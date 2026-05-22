using IPCSoftware.Core.Interfaces;
using IPCSoftware.Core.Interfaces.AppLoggerInterface;
using IPCSoftware.Devices.PLC;
using IPCSoftware.Devices.UI;
using IPCSoftware.Engine;
using IPCSoftware.Shared.Models;
using IPCSoftware.Shared.Models.Bending;
using IPCSoftware.Shared.Models.Bending.IPCSoftware.App.Bending.Models;
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
                return await TurnTable1DataPointModel(request);
            }
            // ----------------------------------------------------------------
            // Turntable2
            // ----------------------------------------------------------------

            if (request.RequestId == 15)
            {
                return await TurnTable2DataPointModel(request);
            }
            // ----------------------------------------------------------------
            // TransferModule
            // ----------------------------------------------------------------

            if (request.RequestId == 16)
            {
                return await TransferModuleModel(request);
            }
            // ----------------------------------------------------------------
            // InspectionData
            // ----------------------------------------------------------------

            if (request.RequestId == 17)
            {
                return await InspectionDataModel(request);
            }
            // ----------------------------------------------------------------
            // RobotStatus
            // ----------------------------------------------------------------

            if (request.RequestId == 18)
            {
                return await RobotProcessStatus(request);
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
            DashboardInspectionModel item = new DashboardInspectionModel
            {
                BatchNo = _latestPackets.TryGetValue(1, out var packetBatchNo) && packetBatchNo.Values.TryGetValue(11, out var batchNo) ? batchNo.ToString() : "NA",
                LineItem1 = new DashboardInspectionLineModel
                {
                    QRCode1 = _latestPackets.TryGetValue(1, out var packetQRCode) && packetQRCode.Values.TryGetValue(11, out var qrCode) ? qrCode.ToString() : "NA",
                    HeaterTemp_Bend1 = _latestPackets.TryGetValue(1, out var packetHeaterTemp1) && packetHeaterTemp1.Values.TryGetValue(11, out var heaterTemp1) ? (float.TryParse(heaterTemp1.ToString(), out var heaterTemp1Result) ? heaterTemp1Result : float.NaN) : float.NaN,
                    HeaterTemp_Bend2 = _latestPackets.TryGetValue(1, out var packetHeaterTemp2) && packetHeaterTemp2.Values.TryGetValue(11, out var heaterTemp2) ? (float.TryParse(heaterTemp2.ToString(), out var heaterTemp2Result) ? heaterTemp2Result : float.NaN) : float.NaN,
                    HeaterTemp_Bend3 = _latestPackets.TryGetValue(1, out var packetHeaterTemp3) && packetHeaterTemp3.Values.TryGetValue(11, out var heaterTemp3) ? (float.TryParse(heaterTemp3.ToString(), out var heaterTemp3Result) ? heaterTemp3Result : float.NaN) : float.NaN,
                    Load_Bend1 = _latestPackets.TryGetValue(1, out var packetLoadBend1) && packetLoadBend1.Values.TryGetValue(11, out var loadBend1) ? (int.TryParse(loadBend1.ToString(), out var loadBend1Result) ? loadBend1Result : 0) : 0,
                    Load_Bend2 = _latestPackets.TryGetValue(1, out var packetLoadBend2) && packetLoadBend2.Values.TryGetValue(11, out var loadBend2) ? (int.TryParse(loadBend2.ToString(), out var loadBend2Result) ? loadBend2Result : 0) : 0,
                    Load_Bend3 = _latestPackets.TryGetValue(1, out var packetLoadBend3) && packetLoadBend3.Values.TryGetValue(11, out var loadBend3) ? (int.TryParse(loadBend3.ToString(), out var loadBend3Result) ? loadBend3Result : 0) : 0,
                    XValue = _latestPackets.TryGetValue(1, out var packetXValue) && packetXValue.Values.TryGetValue(11, out var xValue) ? (float.TryParse(xValue.ToString(), out var xValueResult) ? xValueResult : float.NaN) : float.NaN,
                    YValue = _latestPackets.TryGetValue(1, out var packetYValue) && packetYValue.Values.TryGetValue(11, out var yValue) ? (float.TryParse(yValue.ToString(), out var yValueResult) ? yValueResult : float.NaN) : float.NaN,
                    ZValue = _latestPackets.TryGetValue(1, out var packetZValue) && packetZValue.Values.TryGetValue(11, out var zValue) ? (float.TryParse(zValue.ToString(), out var zValueResult) ? zValueResult : float.NaN) : float.NaN,
                    WValue = _latestPackets.TryGetValue(1, out var packetWValue) && packetWValue.Values.TryGetValue(11, out var wValue) ? (float.TryParse(wValue.ToString(), out var wValueResult) ? wValueResult : float.NaN) : float.NaN,
                    Result1 = _latestPackets.TryGetValue(1, out var packetResult1) && packetResult1.Values.TryGetValue(11, out var result1) ? (bool.TryParse(result1.ToString(), out var result1Parsed) ? result1Parsed : false) : false
                },
                LineItem2 = new DashboardInspectionLineModel
                {
                    QRCode1 = _latestPackets.TryGetValue(1, out var packetQRCode2) && packetQRCode2.Values.TryGetValue(11, out var qrCode2) ? qrCode2.ToString() : "NA",
                    HeaterTemp_Bend1 = _latestPackets.TryGetValue(1, out var packetHeaterTemp21) && packetHeaterTemp21.Values.TryGetValue(11, out var heaterTemp21) ? (float.TryParse(heaterTemp21.ToString(), out var heaterTemp21Result) ? heaterTemp21Result : float.NaN) : float.NaN,
                    HeaterTemp_Bend2 = _latestPackets.TryGetValue(1, out var packetHeaterTemp22) && packetHeaterTemp22.Values.TryGetValue(11, out var heaterTemp22) ? (float.TryParse(heaterTemp22.ToString(), out var heaterTemp22Result) ? heaterTemp22Result : float.NaN) : float.NaN,
                    HeaterTemp_Bend3 = _latestPackets.TryGetValue(1, out var packetHeaterTemp23) && packetHeaterTemp23.Values.TryGetValue(11, out var heaterTemp23) ? (float.TryParse(heaterTemp23.ToString(), out var heaterTemp23Result) ? heaterTemp23Result : float.NaN) : float.NaN,
                    Load_Bend1 = _latestPackets.TryGetValue(1, out var packetLoadBend21) && packetLoadBend21.Values.TryGetValue(11, out var loadBend21) ? (int.TryParse(loadBend21.ToString(), out var loadBend21Result) ? loadBend21Result : 0) : 0,
                    Load_Bend2 = _latestPackets.TryGetValue(1, out var packetLoadBend22) && packetLoadBend22.Values.TryGetValue(11, out var loadBend22) ? (int.TryParse(loadBend22.ToString(), out var loadBend22Result) ? loadBend22Result : 0) : 0,
                    Load_Bend3 = _latestPackets.TryGetValue(1, out var packetLoadBend23) && packetLoadBend23.Values.TryGetValue(11, out var loadBend23) ? (int.TryParse(loadBend23.ToString(), out var loadBend23Result) ? loadBend23Result : 0) : 0,
                    XValue = _latestPackets.TryGetValue(1, out var packetXValue2) && packetXValue2.Values.TryGetValue(11, out var xValue2) ? (float.TryParse(xValue2.ToString(), out var xValueResult2) ? xValueResult2 : float.NaN) : float.NaN,
                    YValue = _latestPackets.TryGetValue(1, out var packetYValue2) && packetYValue2.Values.TryGetValue(11, out var yValue2) ? (float.TryParse(yValue2.ToString(), out var yValueResult2) ? yValueResult2 : float.NaN) : float.NaN,
                    ZValue = _latestPackets.TryGetValue(1, out var packetZValue2) && packetZValue2.Values.TryGetValue(11, out var zValue2) ? (float.TryParse(zValue2.ToString(), out var zValueResult2) ? zValueResult2 : float.NaN) : float.NaN,
                    WValue = _latestPackets.TryGetValue(1, out var packetWValue2) && packetWValue2.Values.TryGetValue(11, out var wValue2) ? (float.TryParse(wValue2.ToString(), out var wValueResult2) ? wValueResult2 : float.NaN) : float.NaN,
                    Result1 = _latestPackets.TryGetValue(1, out var packetResult21) && packetResult21.Values.TryGetValue(11, out var result21) ? (bool.TryParse(result21.ToString(), out var result1Parsed2) ? result1Parsed2 : false) : false
                },
                LineItem3 = new DashboardInspectionLineModel
                {
                    QRCode1 = _latestPackets.TryGetValue(1, out var packetQRCode3) && packetQRCode3.Values.TryGetValue(11, out var qrCode3) ? qrCode3.ToString() : "NA",
                    HeaterTemp_Bend1 = _latestPackets.TryGetValue(1, out var packetHeaterTemp31) && packetHeaterTemp31.Values.TryGetValue(11, out var heaterTemp31) ? (float.TryParse(heaterTemp31.ToString(), out var heaterTemp31Result) ? heaterTemp31Result : float.NaN) : float.NaN,
                    HeaterTemp_Bend2 = _latestPackets.TryGetValue(1, out var packetHeaterTemp32) && packetHeaterTemp32.Values.TryGetValue(11, out var heaterTemp32) ? (float.TryParse(heaterTemp32.ToString(), out var heaterTemp32Result) ? heaterTemp32Result : float.NaN) : float.NaN,
                    HeaterTemp_Bend3 = _latestPackets.TryGetValue(1, out var packetHeaterTemp33) && packetHeaterTemp33.Values.TryGetValue(11, out var heaterTemp33) ? (float.TryParse(heaterTemp33.ToString(), out var heaterTemp33Result) ? heaterTemp33Result : float.NaN) : float.NaN,
                    Load_Bend1 = _latestPackets.TryGetValue(1, out var packetLoadBend31) && packetLoadBend31.Values.TryGetValue(11, out var loadBend31) ? (int.TryParse(loadBend31.ToString(), out var loadBend31Result) ? loadBend31Result : 0) : 0,
                    Load_Bend2 = _latestPackets.TryGetValue(1, out var packetLoadBend32) && packetLoadBend32.Values.TryGetValue(11, out var loadBend32) ? (int.TryParse(loadBend32.ToString(), out var loadBend32Result) ? loadBend32Result : 0) : 0,
                    Load_Bend3 = _latestPackets.TryGetValue(1, out var packetLoadBend33) && packetLoadBend33.Values.TryGetValue(11, out var loadBend33) ? (int.TryParse(loadBend33.ToString(), out var loadBend33Result) ? loadBend33Result : 0) : 0,
                    XValue = _latestPackets.TryGetValue(1, out var packetXValue3) && packetXValue3.Values.TryGetValue(11, out var xValue3) ? (float.TryParse(xValue3.ToString(), out var xValueResult3) ? xValueResult3 : float.NaN) : float.NaN,
                    YValue = _latestPackets.TryGetValue(1, out var packetYValue3) && packetYValue3.Values.TryGetValue(11, out var yValue3) ? (float.TryParse(yValue3.ToString(), out var yValueResult3) ? yValueResult3 : float.NaN) : float.NaN,
                    ZValue = _latestPackets.TryGetValue(1, out var packetZValue3) && packetZValue3.Values.TryGetValue(11, out var zValue3) ? (float.TryParse(zValue3.ToString(), out var zValueResult3) ? zValueResult3 : float.NaN) : float.NaN,
                    WValue = _latestPackets.TryGetValue(1, out var packetWValue3) && packetWValue3.Values.TryGetValue(11, out var wValue3) ? (float.TryParse(wValue3.ToString(), out var wValueResult3) ? wValueResult3 : float.NaN) : float.NaN,
                    Result1 = _latestPackets.TryGetValue(1, out var packetResult31) && packetResult31.Values.TryGetValue(11, out var result31) ? (bool.TryParse(result31.ToString(), out var result1Parsed3) ? result1Parsed3 : false) : false
                },
                LineItem4 = new DashboardInspectionLineModel
                {
                    QRCode1 = _latestPackets.TryGetValue(1, out var packetQRCode4) && packetQRCode4.Values.TryGetValue(11, out var qrCode4) ? qrCode4.ToString() : "NA",
                    HeaterTemp_Bend1 = _latestPackets.TryGetValue(1, out var packetHeaterTemp41) && packetHeaterTemp41.Values.TryGetValue(11, out var heaterTemp41) ? (float.TryParse(heaterTemp41.ToString(), out var heaterTemp41Result) ? heaterTemp41Result : float.NaN) : float.NaN,
                    HeaterTemp_Bend2 = _latestPackets.TryGetValue(1, out var packetHeaterTemp42) && packetHeaterTemp42.Values.TryGetValue(11, out var heaterTemp42) ? (float.TryParse(heaterTemp42.ToString(), out var heaterTemp42Result) ? heaterTemp42Result : float.NaN) : float.NaN,
                    HeaterTemp_Bend3 = _latestPackets.TryGetValue(1, out var packetHeaterTemp43) && packetHeaterTemp43.Values.TryGetValue(11, out var heaterTemp43) ? (float.TryParse(heaterTemp43.ToString(), out var heaterTemp43Result) ? heaterTemp43Result : float.NaN) : float.NaN,
                    Load_Bend1 = _latestPackets.TryGetValue(1, out var packetLoadBend41) && packetLoadBend41.Values.TryGetValue(11, out var loadBend41) ? (int.TryParse(loadBend41.ToString(), out var loadBend41Result) ? loadBend41Result : 0) : 0,
                    Load_Bend2 = _latestPackets.TryGetValue(1, out var packetLoadBend42) && packetLoadBend42.Values.TryGetValue(11, out var loadBend42) ? (int.TryParse(loadBend42.ToString(), out var loadBend42Result) ? loadBend42Result : 0) : 0,
                    Load_Bend3 = _latestPackets.TryGetValue(1, out var packetLoadBend43) && packetLoadBend43.Values.TryGetValue(11, out var loadBend43) ? (int.TryParse(loadBend43.ToString(), out var loadBend43Result) ? loadBend43Result : 0) : 0,
                    XValue = _latestPackets.TryGetValue(1, out var packetXValue4) && packetXValue4.Values.TryGetValue(11, out var xValue4) ? (float.TryParse(xValue4.ToString(), out var xValueResult4) ? xValueResult4 : float.NaN) : float.NaN,
                    YValue = _latestPackets.TryGetValue(1, out var packetYValue4) && packetYValue4.Values.TryGetValue(11, out var yValue4) ? (float.TryParse(yValue4.ToString(), out var yValueResult4) ? yValueResult4 : float.NaN) : float.NaN,
                    ZValue = _latestPackets.TryGetValue(1, out var packetZValue4) && packetZValue4.Values.TryGetValue(11, out var zValue4) ? (float.TryParse(zValue4.ToString(), out var zValueResult4) ? zValueResult4 : float.NaN) : float.NaN,
                    WValue = _latestPackets.TryGetValue(1, out var packetWValue4) && packetWValue4.Values.TryGetValue(11, out var wValue4) ? (float.TryParse(wValue4.ToString(), out var wValueResult4) ? wValueResult4 : float.NaN) : float.NaN,
                    Result1 = _latestPackets.TryGetValue(1, out var packetResult41) && packetResult41.Values.TryGetValue(11, out var result41) ? (bool.TryParse(result41.ToString(), out var result1Parsed4) ? result1Parsed4 : false) : false

                },
            };

            return Task.FromResult(new ResponsePackage
            {
                ResponseId = request.RequestId,
                Parameters = new Dictionary<int, object>()
                {
                    { 0,item }

                }

            });


            //return Task.FromResult(GetPacketResponse(request, 1));

        }
        private Task<ResponsePackage> DashboardInspectionModelBatch2(RequestPackage request)
        {
            DashboardInspectionModel item = new DashboardInspectionModel
            {
                BatchNo = _latestPackets.TryGetValue(1, out var packetBatchNo) && packetBatchNo.Values.TryGetValue(12, out var batchNo) ? batchNo.ToString() : "NA",
                LineItem1 = new DashboardInspectionLineModel
                {
                    QRCode1 = _latestPackets.TryGetValue(1, out var packetQRCode) && packetQRCode.Values.TryGetValue(12, out var qrCode) ? qrCode.ToString() : "NA",
                    HeaterTemp_Bend1 = _latestPackets.TryGetValue(1, out var packetHeaterTemp1) && packetHeaterTemp1.Values.TryGetValue(12, out var heaterTemp1) ? (float.TryParse(heaterTemp1.ToString(), out var heaterTemp1Result) ? heaterTemp1Result : float.NaN) : float.NaN,
                    HeaterTemp_Bend2 = _latestPackets.TryGetValue(1, out var packetHeaterTemp2) && packetHeaterTemp2.Values.TryGetValue(12, out var heaterTemp2) ? (float.TryParse(heaterTemp2.ToString(), out var heaterTemp2Result) ? heaterTemp2Result : float.NaN) : float.NaN,
                    HeaterTemp_Bend3 = _latestPackets.TryGetValue(1, out var packetHeaterTemp3) && packetHeaterTemp3.Values.TryGetValue(12, out var heaterTemp3) ? (float.TryParse(heaterTemp3.ToString(), out var heaterTemp3Result) ? heaterTemp3Result : float.NaN) : float.NaN,
                    Load_Bend1 = _latestPackets.TryGetValue(1, out var packetLoadBend1) && packetLoadBend1.Values.TryGetValue(12, out var loadBend1) ? (int.TryParse(loadBend1.ToString(), out var loadBend1Result) ? loadBend1Result : 0) : 0,
                    Load_Bend2 = _latestPackets.TryGetValue(1, out var packetLoadBend2) && packetLoadBend2.Values.TryGetValue(12, out var loadBend2) ? (int.TryParse(loadBend2.ToString(), out var loadBend2Result) ? loadBend2Result : 0) : 0,
                    Load_Bend3 = _latestPackets.TryGetValue(1, out var packetLoadBend3) && packetLoadBend3.Values.TryGetValue(12, out var loadBend3) ? (int.TryParse(loadBend3.ToString(), out var loadBend3Result) ? loadBend3Result : 0) : 0,
                    XValue = _latestPackets.TryGetValue(1, out var packetXValue) && packetXValue.Values.TryGetValue(12, out var xValue) ? (float.TryParse(xValue.ToString(), out var xValueResult) ? xValueResult : float.NaN) : float.NaN,
                    YValue = _latestPackets.TryGetValue(1, out var packetYValue) && packetYValue.Values.TryGetValue(12, out var yValue) ? (float.TryParse(yValue.ToString(), out var yValueResult) ? yValueResult : float.NaN) : float.NaN,
                    ZValue = _latestPackets.TryGetValue(1, out var packetZValue) && packetZValue.Values.TryGetValue(12, out var zValue) ? (float.TryParse(zValue.ToString(), out var zValueResult) ? zValueResult : float.NaN) : float.NaN,
                    WValue = _latestPackets.TryGetValue(1, out var packetWValue) && packetWValue.Values.TryGetValue(12, out var wValue) ? (float.TryParse(wValue.ToString(), out var wValueResult) ? wValueResult : float.NaN) : float.NaN,
                    Result1 = _latestPackets.TryGetValue(1, out var packetResult1) && packetResult1.Values.TryGetValue(12, out var result1) ? (bool.TryParse(result1.ToString(), out var result1Parsed) ? result1Parsed : false) : false
                },
                LineItem2 = new DashboardInspectionLineModel
                {
                    QRCode1 = _latestPackets.TryGetValue(1, out var packetQRCode2) && packetQRCode2.Values.TryGetValue(12, out var qrCode2) ? qrCode2.ToString() : "NA",
                    HeaterTemp_Bend1 = _latestPackets.TryGetValue(1, out var packetHeaterTemp21) && packetHeaterTemp21.Values.TryGetValue(12, out var heaterTemp21) ? (float.TryParse(heaterTemp21.ToString(), out var heaterTemp21Result) ? heaterTemp21Result : float.NaN) : float.NaN,
                    HeaterTemp_Bend2 = _latestPackets.TryGetValue(1, out var packetHeaterTemp22) && packetHeaterTemp22.Values.TryGetValue(12, out var heaterTemp22) ? (float.TryParse(heaterTemp22.ToString(), out var heaterTemp22Result) ? heaterTemp22Result : float.NaN) : float.NaN,
                    HeaterTemp_Bend3 = _latestPackets.TryGetValue(1, out var packetHeaterTemp23) && packetHeaterTemp23.Values.TryGetValue(12, out var heaterTemp23) ? (float.TryParse(heaterTemp23.ToString(), out var heaterTemp23Result) ? heaterTemp23Result : float.NaN) : float.NaN,
                    Load_Bend1 = _latestPackets.TryGetValue(1, out var packetLoadBend21) && packetLoadBend21.Values.TryGetValue(12, out var loadBend21) ? (int.TryParse(loadBend21.ToString(), out var loadBend21Result) ? loadBend21Result : 0) : 0,
                    Load_Bend2 = _latestPackets.TryGetValue(1, out var packetLoadBend22) && packetLoadBend22.Values.TryGetValue(12, out var loadBend22) ? (int.TryParse(loadBend22.ToString(), out var loadBend22Result) ? loadBend22Result : 0) : 0,
                    Load_Bend3 = _latestPackets.TryGetValue(1, out var packetLoadBend23) && packetLoadBend23.Values.TryGetValue(12, out var loadBend23) ? (int.TryParse(loadBend23.ToString(), out var loadBend23Result) ? loadBend23Result : 0) : 0,
                    XValue = _latestPackets.TryGetValue(1, out var packetXValue2) && packetXValue2.Values.TryGetValue(12, out var xValue2) ? (float.TryParse(xValue2.ToString(), out var xValueResult2) ? xValueResult2 : float.NaN) : float.NaN,
                    YValue = _latestPackets.TryGetValue(1, out var packetYValue2) && packetYValue2.Values.TryGetValue(12, out var yValue2) ? (float.TryParse(yValue2.ToString(), out var yValueResult2) ? yValueResult2 : float.NaN) : float.NaN,
                    ZValue = _latestPackets.TryGetValue(1, out var packetZValue2) && packetZValue2.Values.TryGetValue(12, out var zValue2) ? (float.TryParse(zValue2.ToString(), out var zValueResult2) ? zValueResult2 : float.NaN) : float.NaN,
                    WValue = _latestPackets.TryGetValue(1, out var packetWValue2) && packetWValue2.Values.TryGetValue(12, out var wValue2) ? (float.TryParse(wValue2.ToString(), out var wValueResult2) ? wValueResult2 : float.NaN) : float.NaN,
                    Result1 = _latestPackets.TryGetValue(1, out var packetResult21) && packetResult21.Values.TryGetValue(12, out var result21) ? (bool.TryParse(result21.ToString(), out var result1Parsed2) ? result1Parsed2 : false) : false
                },
                LineItem3 = new DashboardInspectionLineModel
                {
                    QRCode1 = _latestPackets.TryGetValue(1, out var packetQRCode3) && packetQRCode3.Values.TryGetValue(12, out var qrCode3) ? qrCode3.ToString() : "NA",
                    HeaterTemp_Bend1 = _latestPackets.TryGetValue(1, out var packetHeaterTemp31) && packetHeaterTemp31.Values.TryGetValue(12, out var heaterTemp31) ? (float.TryParse(heaterTemp31.ToString(), out var heaterTemp31Result) ? heaterTemp31Result : float.NaN) : float.NaN,
                    HeaterTemp_Bend2 = _latestPackets.TryGetValue(1, out var packetHeaterTemp32) && packetHeaterTemp32.Values.TryGetValue(12, out var heaterTemp32) ? (float.TryParse(heaterTemp32.ToString(), out var heaterTemp32Result) ? heaterTemp32Result : float.NaN) : float.NaN,
                    HeaterTemp_Bend3 = _latestPackets.TryGetValue(1, out var packetHeaterTemp33) && packetHeaterTemp33.Values.TryGetValue(12, out var heaterTemp33) ? (float.TryParse(heaterTemp33.ToString(), out var heaterTemp33Result) ? heaterTemp33Result : float.NaN) : float.NaN,
                    Load_Bend1 = _latestPackets.TryGetValue(1, out var packetLoadBend31) && packetLoadBend31.Values.TryGetValue(12, out var loadBend31) ? (int.TryParse(loadBend31.ToString(), out var loadBend31Result) ? loadBend31Result : 0) : 0,
                    Load_Bend2 = _latestPackets.TryGetValue(1, out var packetLoadBend32) && packetLoadBend32.Values.TryGetValue(12, out var loadBend32) ? (int.TryParse(loadBend32.ToString(), out var loadBend32Result) ? loadBend32Result : 0) : 0,
                    Load_Bend3 = _latestPackets.TryGetValue(1, out var packetLoadBend33) && packetLoadBend33.Values.TryGetValue(12, out var loadBend33) ? (int.TryParse(loadBend33.ToString(), out var loadBend33Result) ? loadBend33Result : 0) : 0,
                    XValue = _latestPackets.TryGetValue(1, out var packetXValue3) && packetXValue3.Values.TryGetValue(12, out var xValue3) ? (float.TryParse(xValue3.ToString(), out var xValueResult3) ? xValueResult3 : float.NaN) : float.NaN,
                    YValue = _latestPackets.TryGetValue(1, out var packetYValue3) && packetYValue3.Values.TryGetValue(12, out var yValue3) ? (float.TryParse(yValue3.ToString(), out var yValueResult3) ? yValueResult3 : float.NaN) : float.NaN,
                    ZValue = _latestPackets.TryGetValue(1, out var packetZValue3) && packetZValue3.Values.TryGetValue(12, out var zValue3) ? (float.TryParse(zValue3.ToString(), out var zValueResult3) ? zValueResult3 : float.NaN) : float.NaN,
                    WValue = _latestPackets.TryGetValue(1, out var packetWValue3) && packetWValue3.Values.TryGetValue(12, out var wValue3) ? (float.TryParse(wValue3.ToString(), out var wValueResult3) ? wValueResult3 : float.NaN) : float.NaN,
                    Result1 = _latestPackets.TryGetValue(1, out var packetResult31) && packetResult31.Values.TryGetValue(1, out var result31) ? (bool.TryParse(result31.ToString(), out var result1Parsed3) ? result1Parsed3 : false) : false
                },
                LineItem4 = new DashboardInspectionLineModel
                {
                    QRCode1 = _latestPackets.TryGetValue(1, out var packetQRCode4) && packetQRCode4.Values.TryGetValue(12, out var qrCode4) ? qrCode4.ToString() : "NA",
                    HeaterTemp_Bend1 = _latestPackets.TryGetValue(1, out var packetHeaterTemp41) && packetHeaterTemp41.Values.TryGetValue(12, out var heaterTemp41) ? (float.TryParse(heaterTemp41.ToString(), out var heaterTemp41Result) ? heaterTemp41Result : float.NaN) : float.NaN,
                    HeaterTemp_Bend2 = _latestPackets.TryGetValue(1, out var packetHeaterTemp42) && packetHeaterTemp42.Values.TryGetValue(12, out var heaterTemp42) ? (float.TryParse(heaterTemp42.ToString(), out var heaterTemp42Result) ? heaterTemp42Result : float.NaN) : float.NaN,
                    HeaterTemp_Bend3 = _latestPackets.TryGetValue(1, out var packetHeaterTemp43) && packetHeaterTemp43.Values.TryGetValue(12, out var heaterTemp43) ? (float.TryParse(heaterTemp43.ToString(), out var heaterTemp43Result) ? heaterTemp43Result : float.NaN) : float.NaN,
                    Load_Bend1 = _latestPackets.TryGetValue(1, out var packetLoadBend41) && packetLoadBend41.Values.TryGetValue(12, out var loadBend41) ? (int.TryParse(loadBend41.ToString(), out var loadBend41Result) ? loadBend41Result : 0) : 0,
                    Load_Bend2 = _latestPackets.TryGetValue(1, out var packetLoadBend42) && packetLoadBend42.Values.TryGetValue(12, out var loadBend42) ? (int.TryParse(loadBend42.ToString(), out var loadBend42Result) ? loadBend42Result : 0) : 0,
                    Load_Bend3 = _latestPackets.TryGetValue(1, out var packetLoadBend43) && packetLoadBend43.Values.TryGetValue(12, out var loadBend43) ? (int.TryParse(loadBend43.ToString(), out var loadBend43Result) ? loadBend43Result : 0) : 0,
                    XValue = _latestPackets.TryGetValue(1, out var packetXValue4) && packetXValue4.Values.TryGetValue(12, out var xValue4) ? (float.TryParse(xValue4.ToString(), out var xValueResult4) ? xValueResult4 : float.NaN) : float.NaN,
                    YValue = _latestPackets.TryGetValue(1, out var packetYValue4) && packetYValue4.Values.TryGetValue(12, out var yValue4) ? (float.TryParse(yValue4.ToString(), out var yValueResult4) ? yValueResult4 : float.NaN) : float.NaN,
                    ZValue = _latestPackets.TryGetValue(1, out var packetZValue4) && packetZValue4.Values.TryGetValue(12, out var zValue4) ? (float.TryParse(zValue4.ToString(), out var zValueResult4) ? zValueResult4 : float.NaN) : float.NaN,
                    WValue = _latestPackets.TryGetValue(1, out var packetWValue4) && packetWValue4.Values.TryGetValue(12, out var wValue4) ? (float.TryParse(wValue4.ToString(), out var wValueResult4) ? wValueResult4 : float.NaN) : float.NaN,
                    Result1 = _latestPackets.TryGetValue(1, out var packetResult41) && packetResult41.Values.TryGetValue(12, out var result41) ? (bool.TryParse(result41.ToString(), out var result1Parsed4) ? result1Parsed4 : false) : false
                }

            };
            return Task.FromResult(new ResponsePackage
            {
                ResponseId = request.RequestId,
                Parameters = new Dictionary<int, object>()
                {
                    { 0, item }
                }
            });
        }

        private Task<ResponsePackage> DashboardInspectionModelBatch3(RequestPackage request)
        {
            DashboardInspectionModel item = new DashboardInspectionModel
            {
                BatchNo = _latestPackets.TryGetValue(1, out var packetBatchNo) && packetBatchNo.Values.TryGetValue(23, out var batchNo) ? batchNo.ToString() : "NA",
                LineItem1 = new DashboardInspectionLineModel
                {
                    QRCode1 = _latestPackets.TryGetValue(1, out var packetQRCode1) && packetQRCode1.Values.TryGetValue(23, out var qrCode1) ? qrCode1.ToString() : "NA"
                },
                LineItem2 = new DashboardInspectionLineModel
                {
                    QRCode1 = _latestPackets.TryGetValue(1, out var packetQRCode2) && packetQRCode2.Values.TryGetValue(23, out var qrCode2) ? qrCode2.ToString() : "NA"
                },
                LineItem3 = new DashboardInspectionLineModel
                {
                    QRCode1 = _latestPackets.TryGetValue(1, out var packetQRCode3) && packetQRCode3.Values.TryGetValue(23, out var qrCode3) ? qrCode3.ToString() : "NA"
                },
                LineItem4 = new DashboardInspectionLineModel
                {
                    QRCode1 = _latestPackets.TryGetValue(1, out var packetQRCode4) && packetQRCode4.Values.TryGetValue(23, out var qrCode4) ? qrCode4.ToString() : "NA"
                }
            };
            return Task.FromResult(new ResponsePackage
            {
                ResponseId = request.RequestId,
                Parameters = new Dictionary<int, object>()
                {
                    { 0, item }
                }
            });
        }

        private Task<ResponsePackage> DashboardInspectionModelBatch4(RequestPackage request)
        {
            DashboardInspectionModel item = new DashboardInspectionModel
            {
                BatchNo = _latestPackets.TryGetValue(1, out var packetBatchNo) && packetBatchNo.Values.TryGetValue(24, out var batchNo) ? batchNo.ToString() : "NA",
                LineItem1 = new DashboardInspectionLineModel
                {
                    QRCode1 = _latestPackets.TryGetValue(1, out var packetQRCode1) && packetQRCode1.Values.TryGetValue(24, out var qrCode1) ? qrCode1.ToString() : "NA"
                },
                LineItem2 = new DashboardInspectionLineModel
                {
                    QRCode1 = _latestPackets.TryGetValue(1, out var packetQRCode2) && packetQRCode2.Values.TryGetValue(24, out var qrCode2) ? qrCode2.ToString() : "NA"
                },
                LineItem3 = new DashboardInspectionLineModel
                {
                    QRCode1 = _latestPackets.TryGetValue(1, out var packetQRCode3) && packetQRCode3.Values.TryGetValue(24, out var qrCode3) ? qrCode3.ToString() : "NA"
                },
                LineItem4 = new DashboardInspectionLineModel
                {
                    QRCode1 = _latestPackets.TryGetValue(1, out var packetQRCode4) && packetQRCode4.Values.TryGetValue(24, out var qrCode4) ? qrCode4.ToString() : "NA"
                }
            };
            return Task.FromResult(new ResponsePackage
            {
                ResponseId = request.RequestId,
                Parameters = new Dictionary<int, object>()
                {
                    { 0, item }
                }
            });
        }

        private Task<ResponsePackage> BendingIndicators(RequestPackage request)
        {

            BendingIndicators item = new BendingIndicators
            {
                Bending1 = new FlexBendingIndicator
                {
                    Flex1 = new BendingIndicator
                    {
                        Clamp = _latestPackets.TryGetValue(1, out var b1f1Cp) && b1f1Cp.Values.TryGetValue(13, out var b1f1Cv) ? (bool.TryParse(b1f1Cv.ToString(), out var b1f1Cr) ? b1f1Cr : false) : false,
                        Punch = _latestPackets.TryGetValue(1, out var b1f1Pp) && b1f1Pp.Values.TryGetValue(13, out var b1f1Pv) ? (bool.TryParse(b1f1Pv.ToString(), out var b1f1Pr) ? b1f1Pr : false) : false,
                        Heat = _latestPackets.TryGetValue(1, out var b1f1Hp) && b1f1Hp.Values.TryGetValue(13, out var b1f1Hv) ? (bool.TryParse(b1f1Hv.ToString(), out var b1f1Hr) ? b1f1Hr : false) : false,
                        Temperature = _latestPackets.TryGetValue(1, out var b1f1Tp) && b1f1Tp.Values.TryGetValue(13, out var b1f1Tv) ? (float.TryParse(b1f1Tv.ToString(), out var b1f1Tr) ? b1f1Tr : float.NaN) : float.NaN,
                        Force = _latestPackets.TryGetValue(1, out var b1f1Fp) && b1f1Fp.Values.TryGetValue(13, out var b1f1Fv) ? (float.TryParse(b1f1Fv.ToString(), out var b1f1Fr) ? b1f1Fr : float.NaN) : float.NaN
                    },
                    Flex2 = new BendingIndicator
                    {
                        Clamp = _latestPackets.TryGetValue(1, out var b1f2Cp) && b1f2Cp.Values.TryGetValue(13, out var b1f2Cv) ? (bool.TryParse(b1f2Cv.ToString(), out var b1f2Cr) ? b1f2Cr : false) : false,
                        Punch = _latestPackets.TryGetValue(1, out var b1f2Pp) && b1f2Pp.Values.TryGetValue(13, out var b1f2Pv) ? (bool.TryParse(b1f2Pv.ToString(), out var b1f2Pr) ? b1f2Pr : false) : false,
                        Heat = _latestPackets.TryGetValue(1, out var b1f2Hp) && b1f2Hp.Values.TryGetValue(13, out var b1f2Hv) ? (bool.TryParse(b1f2Hv.ToString(), out var b1f2Hr) ? b1f2Hr : false) : false,
                        Temperature = _latestPackets.TryGetValue(1, out var b1f2Tp) && b1f2Tp.Values.TryGetValue(13, out var b1f2Tv) ? (float.TryParse(b1f2Tv.ToString(), out var b1f2Tr) ? b1f2Tr : float.NaN) : float.NaN,
                        Force = _latestPackets.TryGetValue(1, out var b1f2Fp) && b1f2Fp.Values.TryGetValue(13, out var b1f2Fv) ? (float.TryParse(b1f2Fv.ToString(), out var b1f2Fr) ? b1f2Fr : float.NaN) : float.NaN
                    },
                    Flex3 = new BendingIndicator
                    {
                        Clamp = _latestPackets.TryGetValue(1, out var b1f3Cp) && b1f3Cp.Values.TryGetValue(13, out var b1f3Cv) ? (bool.TryParse(b1f3Cv.ToString(), out var b1f3Cr) ? b1f3Cr : false) : false,
                        Punch = _latestPackets.TryGetValue(1, out var b1f3Pp) && b1f3Pp.Values.TryGetValue(13, out var b1f3Pv) ? (bool.TryParse(b1f3Pv.ToString(), out var b1f3Pr) ? b1f3Pr : false) : false,
                        Heat = _latestPackets.TryGetValue(1, out var b1f3Hp) && b1f3Hp.Values.TryGetValue(13, out var b1f3Hv) ? (bool.TryParse(b1f3Hv.ToString(), out var b1f3Hr) ? b1f3Hr : false) : false,
                        Temperature = _latestPackets.TryGetValue(1, out var b1f3Tp) && b1f3Tp.Values.TryGetValue(13, out var b1f3Tv) ? (float.TryParse(b1f3Tv.ToString(), out var b1f3Tr) ? b1f3Tr : float.NaN) : float.NaN,
                        Force = _latestPackets.TryGetValue(1, out var b1f3Fp) && b1f3Fp.Values.TryGetValue(13, out var b1f3Fv) ? (float.TryParse(b1f3Fv.ToString(), out var b1f3Fr) ? b1f3Fr : float.NaN) : float.NaN
                    },
                    Flex4 = new BendingIndicator
                    {
                        Clamp = _latestPackets.TryGetValue(1, out var b1f4Cp) && b1f4Cp.Values.TryGetValue(13, out var b1f4Cv) ? (bool.TryParse(b1f4Cv.ToString(), out var b1f4Cr) ? b1f4Cr : false) : false,
                        Punch = _latestPackets.TryGetValue(1, out var b1f4Pp) && b1f4Pp.Values.TryGetValue(13, out var b1f4Pv) ? (bool.TryParse(b1f4Pv.ToString(), out var b1f4Pr) ? b1f4Pr : false) : false,
                        Heat = _latestPackets.TryGetValue(1, out var b1f4Hp) && b1f4Hp.Values.TryGetValue(13, out var b1f4Hv) ? (bool.TryParse(b1f4Hv.ToString(), out var b1f4Hr) ? b1f4Hr : false) : false,
                        Temperature = _latestPackets.TryGetValue(1, out var b1f4Tp) && b1f4Tp.Values.TryGetValue(13, out var b1f4Tv) ? (float.TryParse(b1f4Tv.ToString(), out var b1f4Tr) ? b1f4Tr : float.NaN) : float.NaN,
                        Force = _latestPackets.TryGetValue(1, out var b1f4Fp) && b1f4Fp.Values.TryGetValue(13, out var b1f4Fv) ? (float.TryParse(b1f4Fv.ToString(), out var b1f4Fr) ? b1f4Fr : float.NaN) : float.NaN
                    }
                },
                Bending2 = new FlexBendingIndicator
                {
                    Flex1 = new BendingIndicator
                    {
                        Clamp = _latestPackets.TryGetValue(1, out var b2f1Cp) && b2f1Cp.Values.TryGetValue(13, out var b2f1Cv) ? (bool.TryParse(b2f1Cv.ToString(), out var b2f1Cr) ? b2f1Cr : false) : false,
                        Punch = _latestPackets.TryGetValue(1, out var b2f1Pp) && b2f1Pp.Values.TryGetValue(13, out var b2f1Pv) ? (bool.TryParse(b2f1Pv.ToString(), out var b2f1Pr) ? b2f1Pr : false) : false,
                        Heat = _latestPackets.TryGetValue(1, out var b2f1Hp) && b2f1Hp.Values.TryGetValue(13, out var b2f1Hv) ? (bool.TryParse(b2f1Hv.ToString(), out var b2f1Hr) ? b2f1Hr : false) : false,
                        Temperature = _latestPackets.TryGetValue(1, out var b2f1Tp) && b2f1Tp.Values.TryGetValue(13, out var b2f1Tv) ? (float.TryParse(b2f1Tv.ToString(), out var b2f1Tr) ? b2f1Tr : float.NaN) : float.NaN,
                        Force = _latestPackets.TryGetValue(1, out var b2f1Fp) && b2f1Fp.Values.TryGetValue(13, out var b2f1Fv) ? (float.TryParse(b2f1Fv.ToString(), out var b2f1Fr) ? b2f1Fr : float.NaN) : float.NaN
                    },
                    Flex2 = new BendingIndicator
                    {
                        Clamp = _latestPackets.TryGetValue(1, out var b2f2Cp) && b2f2Cp.Values.TryGetValue(13, out var b2f2Cv) ? (bool.TryParse(b2f2Cv.ToString(), out var b2f2Cr) ? b2f2Cr : false) : false,
                        Punch = _latestPackets.TryGetValue(1, out var b2f2Pp) && b2f2Pp.Values.TryGetValue(13, out var b2f2Pv) ? (bool.TryParse(b2f2Pv.ToString(), out var b2f2Pr) ? b2f2Pr : false) : false,
                        Heat = _latestPackets.TryGetValue(1, out var b2f2Hp) && b2f2Hp.Values.TryGetValue(13, out var b2f2Hv) ? (bool.TryParse(b2f2Hv.ToString(), out var b2f2Hr) ? b2f2Hr : false) : false,
                        Temperature = _latestPackets.TryGetValue(1, out var b2f2Tp) && b2f2Tp.Values.TryGetValue(13, out var b2f2Tv) ? (float.TryParse(b2f2Tv.ToString(), out var b2f2Tr) ? b2f2Tr : float.NaN) : float.NaN,
                        Force = _latestPackets.TryGetValue(1, out var b2f2Fp) && b2f2Fp.Values.TryGetValue(13, out var b2f2Fv) ? (float.TryParse(b2f2Fv.ToString(), out var b2f2Fr) ? b2f2Fr : float.NaN) : float.NaN
                    },
                    Flex3 = new BendingIndicator
                    {
                        Clamp = _latestPackets.TryGetValue(1, out var b2f3Cp) && b2f3Cp.Values.TryGetValue(13, out var b2f3Cv) ? (bool.TryParse(b2f3Cv.ToString(), out var b2f3Cr) ? b2f3Cr : false) : false,
                        Punch = _latestPackets.TryGetValue(1, out var b2f3Pp) && b2f3Pp.Values.TryGetValue(13, out var b2f3Pv) ? (bool.TryParse(b2f3Pv.ToString(), out var b2f3Pr) ? b2f3Pr : false) : false,
                        Heat = _latestPackets.TryGetValue(1, out var b2f3Hp) && b2f3Hp.Values.TryGetValue(13, out var b2f3Hv) ? (bool.TryParse(b2f3Hv.ToString(), out var b2f3Hr) ? b2f3Hr : false) : false,
                        Temperature = _latestPackets.TryGetValue(1, out var b2f3Tp) && b2f3Tp.Values.TryGetValue(13, out var b2f3Tv) ? (float.TryParse(b2f3Tv.ToString(), out var b2f3Tr) ? b2f3Tr : float.NaN) : float.NaN,
                        Force = _latestPackets.TryGetValue(1, out var b2f3Fp) && b2f3Fp.Values.TryGetValue(13, out var b2f3Fv) ? (float.TryParse(b2f3Fv.ToString(), out var b2f3Fr) ? b2f3Fr : float.NaN) : float.NaN
                    },
                    Flex4 = new BendingIndicator
                    {
                        Clamp = _latestPackets.TryGetValue(1, out var b2f4Cp) && b2f4Cp.Values.TryGetValue(13, out var b2f4Cv) ? (bool.TryParse(b2f4Cv.ToString(), out var b2f4Cr) ? b2f4Cr : false) : false,
                        Punch = _latestPackets.TryGetValue(1, out var b2f4Pp) && b2f4Pp.Values.TryGetValue(13, out var b2f4Pv) ? (bool.TryParse(b2f4Pv.ToString(), out var b2f4Pr) ? b2f4Pr : false) : false,
                        Heat = _latestPackets.TryGetValue(1, out var b2f4Hp) && b2f4Hp.Values.TryGetValue(13, out var b2f4Hv) ? (bool.TryParse(b2f4Hv.ToString(), out var b2f4Hr) ? b2f4Hr : false) : false,
                        Temperature = _latestPackets.TryGetValue(1, out var b2f4Tp) && b2f4Tp.Values.TryGetValue(13, out var b2f4Tv) ? (float.TryParse(b2f4Tv.ToString(), out var b2f4Tr) ? b2f4Tr : float.NaN) : float.NaN,
                        Force = _latestPackets.TryGetValue(1, out var b2f4Fp) && b2f4Fp.Values.TryGetValue(13, out var b2f4Fv) ? (float.TryParse(b2f4Fv.ToString(), out var b2f4Fr) ? b2f4Fr : float.NaN) : float.NaN
                    }
                },
                Bending3 = new FlexBendingIndicator
                {
                    Flex1 = new BendingIndicator
                    {
                        Clamp = _latestPackets.TryGetValue(1, out var b3f1Cp) && b3f1Cp.Values.TryGetValue(13, out var b3f1Cv) ? (bool.TryParse(b3f1Cv.ToString(), out var b3f1Cr) ? b3f1Cr : false) : false,
                        Punch = _latestPackets.TryGetValue(1, out var b3f1Pp) && b3f1Pp.Values.TryGetValue(13, out var b3f1Pv) ? (bool.TryParse(b3f1Pv.ToString(), out var b3f1Pr) ? b3f1Pr : false) : false,
                        Heat = _latestPackets.TryGetValue(1, out var b3f1Hp) && b3f1Hp.Values.TryGetValue(13, out var b3f1Hv) ? (bool.TryParse(b3f1Hv.ToString(), out var b3f1Hr) ? b3f1Hr : false) : false,
                        Temperature = _latestPackets.TryGetValue(1, out var b3f1Tp) && b3f1Tp.Values.TryGetValue(13, out var b3f1Tv) ? (float.TryParse(b3f1Tv.ToString(), out var b3f1Tr) ? b3f1Tr : float.NaN) : float.NaN,
                        Force = _latestPackets.TryGetValue(1, out var b3f1Fp) && b3f1Fp.Values.TryGetValue(13, out var b3f1Fv) ? (float.TryParse(b3f1Fv.ToString(), out var b3f1Fr) ? b3f1Fr : float.NaN) : float.NaN
                    },
                    Flex2 = new BendingIndicator
                    {
                        Clamp = _latestPackets.TryGetValue(1, out var b3f2Cp) && b3f2Cp.Values.TryGetValue(13, out var b3f2Cv) ? (bool.TryParse(b3f2Cv.ToString(), out var b3f2Cr) ? b3f2Cr : false) : false,
                        Punch = _latestPackets.TryGetValue(1, out var b3f2Pp) && b3f2Pp.Values.TryGetValue(13, out var b3f2Pv) ? (bool.TryParse(b3f2Pv.ToString(), out var b3f2Pr) ? b3f2Pr : false) : false,
                        Heat = _latestPackets.TryGetValue(1, out var b3f2Hp) && b3f2Hp.Values.TryGetValue(13, out var b3f2Hv) ? (bool.TryParse(b3f2Hv.ToString(), out var b3f2Hr) ? b3f2Hr : false) : false,
                        Temperature = _latestPackets.TryGetValue(1, out var b3f2Tp) && b3f2Tp.Values.TryGetValue(13, out var b3f2Tv) ? (float.TryParse(b3f2Tv.ToString(), out var b3f2Tr) ? b3f2Tr : float.NaN) : float.NaN,
                        Force = _latestPackets.TryGetValue(1, out var b3f2Fp) && b3f2Fp.Values.TryGetValue(13, out var b3f2Fv) ? (float.TryParse(b3f2Fv.ToString(), out var b3f2Fr) ? b3f2Fr : float.NaN) : float.NaN
                    },
                    Flex3 = new BendingIndicator
                    {
                        Clamp = _latestPackets.TryGetValue(1, out var b3f3Cp) && b3f3Cp.Values.TryGetValue(13, out var b3f3Cv) ? (bool.TryParse(b3f3Cv.ToString(), out var b3f3Cr) ? b3f3Cr : false) : false,
                        Punch = _latestPackets.TryGetValue(1, out var b3f3Pp) && b3f3Pp.Values.TryGetValue(13, out var b3f3Pv) ? (bool.TryParse(b3f3Pv.ToString(), out var b3f3Pr) ? b3f3Pr : false) : false,
                        Heat = _latestPackets.TryGetValue(1, out var b3f3Hp) && b3f3Hp.Values.TryGetValue(13, out var b3f3Hv) ? (bool.TryParse(b3f3Hv.ToString(), out var b3f3Hr) ? b3f3Hr : false) : false,
                        Temperature = _latestPackets.TryGetValue(1, out var b3f3Tp) && b3f3Tp.Values.TryGetValue(13, out var b3f3Tv) ? (float.TryParse(b3f3Tv.ToString(), out var b3f3Tr) ? b3f3Tr : float.NaN) : float.NaN,
                        Force = _latestPackets.TryGetValue(1, out var b3f3Fp) && b3f3Fp.Values.TryGetValue(13, out var b3f3Fv) ? (float.TryParse(b3f3Fv.ToString(), out var b3f3Fr) ? b3f3Fr : float.NaN) : float.NaN
                    },
                    Flex4 = new BendingIndicator
                    {
                        Clamp = _latestPackets.TryGetValue(1, out var b3f4Cp) && b3f4Cp.Values.TryGetValue(13, out var b3f4Cv) ? (bool.TryParse(b3f4Cv.ToString(), out var b3f4Cr) ? b3f4Cr : false) : false,
                        Punch = _latestPackets.TryGetValue(1, out var b3f4Pp) && b3f4Pp.Values.TryGetValue(13, out var b3f4Pv) ? (bool.TryParse(b3f4Pv.ToString(), out var b3f4Pr) ? b3f4Pr : false) : false,
                        Heat = _latestPackets.TryGetValue(1, out var b3f4Hp) && b3f4Hp.Values.TryGetValue(13, out var b3f4Hv) ? (bool.TryParse(b3f4Hv.ToString(), out var b3f4Hr) ? b3f4Hr : false) : false,
                        Temperature = _latestPackets.TryGetValue(1, out var b3f4Tp) && b3f4Tp.Values.TryGetValue(13, out var b3f4Tv) ? (float.TryParse(b3f4Tv.ToString(), out var b3f4Tr) ? b3f4Tr : float.NaN) : float.NaN,
                        Force = _latestPackets.TryGetValue(1, out var b3f4Fp) && b3f4Fp.Values.TryGetValue(13, out var b3f4Fv) ? (float.TryParse(b3f4Fv.ToString(), out var b3f4Fr) ? b3f4Fr : float.NaN) : float.NaN
                    }


                },
                Tearing = new FlexBendingIndicator
                {
                    Flex1 = new BendingIndicator { Tearing = _latestPackets.TryGetValue(1, out var tf1p) && tf1p.Values.TryGetValue(13, out var tf1v) ? (bool.TryParse(tf1v.ToString(), out var tf1r) ? tf1r : false) : false },
                    Flex2 = new BendingIndicator { Tearing = _latestPackets.TryGetValue(1, out var tf2p) && tf2p.Values.TryGetValue(13, out var tf2v) ? (bool.TryParse(tf2v.ToString(), out var tf2r) ? tf2r : false) : false },
                    Flex3 = new BendingIndicator { Tearing = _latestPackets.TryGetValue(1, out var tf3p) && tf3p.Values.TryGetValue(13, out var tf3v) ? (bool.TryParse(tf3v.ToString(), out var tf3r) ? tf3r : false) : false },
                    Flex4 = new BendingIndicator { Tearing = _latestPackets.TryGetValue(1, out var tf4p) && tf4p.Values.TryGetValue(13, out var tf4v) ? (bool.TryParse(tf4v.ToString(), out var tf4r) ? tf4r : false) : false }
                },
                Flipping = new FlexBendingIndicator
                {
                    Flex1 = new BendingIndicator { Flipping = _latestPackets.TryGetValue(1, out var ff1p) && ff1p.Values.TryGetValue(13, out var ff1v) ? (bool.TryParse(ff1v.ToString(), out var ff1r) ? ff1r : false) : false },
                    Flex2 = new BendingIndicator { Flipping = _latestPackets.TryGetValue(1, out var ff2p) && ff2p.Values.TryGetValue(13, out var ff2v) ? (bool.TryParse(ff2v.ToString(), out var ff2r) ? ff2r : false) : false },
                    Flex3 = new BendingIndicator { Flipping = _latestPackets.TryGetValue(1, out var ff3p) && ff3p.Values.TryGetValue(13, out var ff3v) ? (bool.TryParse(ff3v.ToString(), out var ff3r) ? ff3r : false) : false },
                    Flex4 = new BendingIndicator { Flipping = _latestPackets.TryGetValue(1, out var ff4p) && ff4p.Values.TryGetValue(13, out var ff4v) ? (bool.TryParse(ff4v.ToString(), out var ff4r) ? ff4r : false) : false }
                }
            };

            return Task.FromResult(new ResponsePackage
            {
                ResponseId = request.RequestId,
                Parameters = new Dictionary<int, object>()
                {
                    { 0, item }
                }
            });


        }

        private Task<ResponsePackage> TurnTable1DataPointModel(RequestPackage request)
        {
            TurnTable1DataPointModel item = new TurnTable1DataPointModel
            {
                Position1 = new TurnTablePositionItemsModel
                {
                    Flex1 = _latestPackets.TryGetValue(1, out var t1p1f1p) && t1p1f1p.Values.TryGetValue(14, out var t1p1f1v) ? (bool.TryParse(t1p1f1v.ToString(), out var t1p1f1r) ? t1p1f1r : false) : false,
                    Flex2 = _latestPackets.TryGetValue(1, out var t1p1f2p) && t1p1f2p.Values.TryGetValue(14, out var t1p1f2v) ? (bool.TryParse(t1p1f2v.ToString(), out var t1p1f2r) ? t1p1f2r : false) : false,
                    Flex3 = _latestPackets.TryGetValue(1, out var t1p1f3p) && t1p1f3p.Values.TryGetValue(14, out var t1p1f3v) ? (bool.TryParse(t1p1f3v.ToString(), out var t1p1f3r) ? t1p1f3r : false) : false,
                    Flex4 = _latestPackets.TryGetValue(1, out var t1p1f4p) && t1p1f4p.Values.TryGetValue(14, out var t1p1f4v) ? (bool.TryParse(t1p1f4v.ToString(), out var t1p1f4r) ? t1p1f4r : false) : false
                },
                Position2 = new TurnTablePositionItemsModel
                {
                    Flex1 = _latestPackets.TryGetValue(1, out var t1p2f1p) && t1p2f1p.Values.TryGetValue(14, out var t1p2f1v) ? (bool.TryParse(t1p2f1v.ToString(), out var t1p2f1r) ? t1p2f1r : false) : false,
                    Flex2 = _latestPackets.TryGetValue(1, out var t1p2f2p) && t1p2f2p.Values.TryGetValue(14, out var t1p2f2v) ? (bool.TryParse(t1p2f2v.ToString(), out var t1p2f2r) ? t1p2f2r : false) : false,
                    Flex3 = _latestPackets.TryGetValue(1, out var t1p2f3p) && t1p2f3p.Values.TryGetValue(14, out var t1p2f3v) ? (bool.TryParse(t1p2f3v.ToString(), out var t1p2f3r) ? t1p2f3r : false) : false,
                    Flex4 = _latestPackets.TryGetValue(1, out var t1p2f4p) && t1p2f4p.Values.TryGetValue(14, out var t1p2f4v) ? (bool.TryParse(t1p2f4v.ToString(), out var t1p2f4r) ? t1p2f4r : false) : false
                },
                Position3 = new TurnTablePositionItemsModel
                {
                    Flex1 = _latestPackets.TryGetValue(1, out var t1p3f1p) && t1p3f1p.Values.TryGetValue(14, out var t1p3f1v) ? (bool.TryParse(t1p3f1v.ToString(), out var t1p3f1r) ? t1p3f1r : false) : false,
                    Flex2 = _latestPackets.TryGetValue(1, out var t1p3f2p) && t1p3f2p.Values.TryGetValue(14, out var t1p3f2v) ? (bool.TryParse(t1p3f2v.ToString(), out var t1p3f2r) ? t1p3f2r : false) : false,
                    Flex3 = _latestPackets.TryGetValue(1, out var t1p3f3p) && t1p3f3p.Values.TryGetValue(14, out var t1p3f3v) ? (bool.TryParse(t1p3f3v.ToString(), out var t1p3f3r) ? t1p3f3r : false) : false,
                    Flex4 = _latestPackets.TryGetValue(1, out var t1p3f4p) && t1p3f4p.Values.TryGetValue(14, out var t1p3f4v) ? (bool.TryParse(t1p3f4v.ToString(), out var t1p3f4r) ? t1p3f4r : false) : false
                },
                Position4 = new TurnTablePositionItemsModel
                {
                    Flex1 = _latestPackets.TryGetValue(1, out var t1p4f1p) && t1p4f1p.Values.TryGetValue(14, out var t1p4f1v) ? (bool.TryParse(t1p4f1v.ToString(), out var t1p4f1r) ? t1p4f1r : false) : false,
                    Flex2 = _latestPackets.TryGetValue(1, out var t1p4f2p) && t1p4f2p.Values.TryGetValue(14, out var t1p4f2v) ? (bool.TryParse(t1p4f2v.ToString(), out var t1p4f2r) ? t1p4f2r : false) : false,
                    Flex3 = _latestPackets.TryGetValue(1, out var t1p4f3p) && t1p4f3p.Values.TryGetValue(14, out var t1p4f3v) ? (bool.TryParse(t1p4f3v.ToString(), out var t1p4f3r) ? t1p4f3r : false) : false,
                    Flex4 = _latestPackets.TryGetValue(1, out var t1p4f4p) && t1p4f4p.Values.TryGetValue(14, out var t1p4f4v) ? (bool.TryParse(t1p4f4v.ToString(), out var t1p4f4r) ? t1p4f4r : false) : false
                },
                Rotate = _latestPackets.TryGetValue(1, out var t1rp) && t1rp.Values.TryGetValue(14, out var t1rv) ? (bool.TryParse(t1rv.ToString(), out var t1rr) ? t1rr : false) : false
            };

            return Task.FromResult(new ResponsePackage
            {
                ResponseId = request.RequestId,
                Parameters = new Dictionary<int, object>()
                {
                    { 0, item }
                }
            });

        }

        private Task<ResponsePackage> TurnTable2DataPointModel(RequestPackage request)
        {
            TurnTable2DataPointModel item = new TurnTable2DataPointModel
            {
                Position1 = new TurnTable2PositionItemsModel
                {
                    Flex1 = _latestPackets.TryGetValue(1, out var t2p1f1p) && t2p1f1p.Values.TryGetValue(15, out var t2p1f1v) ? (bool.TryParse(t2p1f1v.ToString(), out var t2p1f1r) ? t2p1f1r : false) : false,
                    Flex2 = _latestPackets.TryGetValue(1, out var t2p1f2p) && t2p1f2p.Values.TryGetValue(15, out var t2p1f2v) ? (bool.TryParse(t2p1f2v.ToString(), out var t2p1f2r) ? t2p1f2r : false) : false,
                    Flex3 = _latestPackets.TryGetValue(1, out var t2p1f3p) && t2p1f3p.Values.TryGetValue(15, out var t2p1f3v) ? (bool.TryParse(t2p1f3v.ToString(), out var t2p1f3r) ? t2p1f3r : false) : false,
                    Flex4 = _latestPackets.TryGetValue(1, out var t2p1f4p) && t2p1f4p.Values.TryGetValue(15, out var t2p1f4v) ? (bool.TryParse(t2p1f4v.ToString(), out var t2p1f4r) ? t2p1f4r : false) : false
                },
                Position2 = new TurnTable2PositionItemsModel
                {
                    Flex1 = _latestPackets.TryGetValue(1, out var t2p2f1p) && t2p2f1p.Values.TryGetValue(15, out var t2p2f1v) ? (bool.TryParse(t2p2f1v.ToString(), out var t2p2f1r) ? t2p2f1r : false) : false,
                    Flex2 = _latestPackets.TryGetValue(1, out var t2p2f2p) && t2p2f2p.Values.TryGetValue(15, out var t2p2f2v) ? (bool.TryParse(t2p2f2v.ToString(), out var t2p2f2r) ? t2p2f2r : false) : false,
                    Flex3 = _latestPackets.TryGetValue(1, out var t2p2f3p) && t2p2f3p.Values.TryGetValue(15, out var t2p2f3v) ? (bool.TryParse(t2p2f3v.ToString(), out var t2p2f3r) ? t2p2f3r : false) : false,
                    Flex4 = _latestPackets.TryGetValue(1, out var t2p2f4p) && t2p2f4p.Values.TryGetValue(15, out var t2p2f4v) ? (bool.TryParse(t2p2f4v.ToString(), out var t2p2f4r) ? t2p2f4r : false) : false
                },
                Position3 = new TurnTable2PositionItemsModel
                {
                    Flex1 = _latestPackets.TryGetValue(1, out var t2p3f1p) && t2p3f1p.Values.TryGetValue(15, out var t2p3f1v) ? (bool.TryParse(t2p3f1v.ToString(), out var t2p3f1r) ? t2p3f1r : false) : false,
                    Flex2 = _latestPackets.TryGetValue(1, out var t2p3f2p) && t2p3f2p.Values.TryGetValue(15, out var t2p3f2v) ? (bool.TryParse(t2p3f2v.ToString(), out var t2p3f2r) ? t2p3f2r : false) : false,
                    Flex3 = _latestPackets.TryGetValue(1, out var t2p3f3p) && t2p3f3p.Values.TryGetValue(15, out var t2p3f3v) ? (bool.TryParse(t2p3f3v.ToString(), out var t2p3f3r) ? t2p3f3r : false) : false,
                    Flex4 = _latestPackets.TryGetValue(1, out var t2p3f4p) && t2p3f4p.Values.TryGetValue(15, out var t2p3f4v) ? (bool.TryParse(t2p3f4v.ToString(), out var t2p3f4r) ? t2p3f4r : false) : false
                },
                Rotate = _latestPackets.TryGetValue(1, out var t2rp) && t2rp.Values.TryGetValue(15, out var t2rv) ? (bool.TryParse(t2rv.ToString(), out var t2rr) ? t2rr : false) : false
            };

            return Task.FromResult(new ResponsePackage
            {
                ResponseId = request.RequestId,
                Parameters = new Dictionary<int, object>()
                {
                    { 0, item }
                }
            });


        }

        private Task<ResponsePackage> TransferModuleModel(RequestPackage request)
        {
            TransferModuleModel item = new TransferModuleModel
            {
                Flex1 = _latestPackets.TryGetValue(1, out var tfmf1p) && tfmf1p.Values.TryGetValue(16, out var tfmf1v) ? (bool.TryParse(tfmf1v.ToString(), out var tfmf1r) ? tfmf1r : false) : false,
                Flex2 = _latestPackets.TryGetValue(1, out var tfmf2p) && tfmf2p.Values.TryGetValue(16, out var tfmf2v) ? (bool.TryParse(tfmf2v.ToString(), out var tfmf2r) ? tfmf2r : false) : false,
                Flex3 = _latestPackets.TryGetValue(1, out var tfmf3p) && tfmf3p.Values.TryGetValue(16, out var tfmf3v) ? (bool.TryParse(tfmf3v.ToString(), out var tfmf3r) ? tfmf3r : false) : false,
                Flex4 = _latestPackets.TryGetValue(1, out var tfmf4p) && tfmf4p.Values.TryGetValue(16, out var tfmf4v) ? (bool.TryParse(tfmf4v.ToString(), out var tfmf4r) ? tfmf4r : false) : false,
                Rotate = _latestPackets.TryGetValue(1, out var tfmr) && tfmr.Values.TryGetValue(16, out var tfmrv) ? (bool.TryParse(tfmrv.ToString(), out var tfmrr) ? tfmrr : false) : false
            };

            return Task.FromResult(new ResponsePackage
            {
                ResponseId = request.RequestId,
                Parameters = new Dictionary<int, object>()
                {
                    { 0, item }
                }
            });


        }

        private Task<ResponsePackage> InspectionDataModel(RequestPackage request)
        {
            InspectionDataModel item = new InspectionDataModel
            {
                QRCode = _latestPackets.TryGetValue(1, out var packetQRCode) && packetQRCode.Values.TryGetValue(17, out var qrCode) ? qrCode.ToString() : "NA",
                Cam1Imagepath = _latestPackets.TryGetValue(1, out var packetCam1Imagepath) && packetCam1Imagepath.Values.TryGetValue(17, out var cam1Imagepath) ? cam1Imagepath.ToString() : "NA",
                Cam2Imagepath = _latestPackets.TryGetValue(1, out var packetCam2Imagepath) && packetCam2Imagepath.Values.TryGetValue(17, out var cam2Imagepath) ? cam2Imagepath.ToString() : "NA"
            };

            return Task.FromResult(new ResponsePackage
            {
                ResponseId = request.RequestId,
                Parameters = new Dictionary<int, object>()
                {
                    { 0, item }
                }
            });


        }

        private Task<ResponsePackage> RobotProcessStatus(RequestPackage request)
        {
            RobotProcessStatus item = new RobotProcessStatus
            {
                Movement = _latestPackets.TryGetValue(1, out var packetMovement) && packetMovement.Values.TryGetValue(18, out var movement) ? (int.TryParse(movement.ToString(), out var movementResult) ? movementResult : 0) : 0,
                Rotation = _latestPackets.TryGetValue(1, out var packetRotation) && packetRotation.Values.TryGetValue(18, out var rotation) ? (int.TryParse(rotation.ToString(), out var rotationResult) ? rotationResult : 0) : 0,
                Rotate = _latestPackets.TryGetValue(1, out var packetRotate) && packetRotate.Values.TryGetValue(18, out var rotate) ? (bool.TryParse(rotate.ToString(), out var rotateResult) ? rotateResult : false) : false
            };

            return Task.FromResult(new ResponsePackage
            {
                ResponseId = request.RequestId,
                Parameters = new Dictionary<int, object>()
                {
                    { 0, item }
                }
            });


        }

        private Task<ResponsePackage> InputTray(RequestPackage request)
        {
            InputTrayModel item = new InputTrayModel
            {
                IsLoaded = _latestPackets.TryGetValue(1, out var packetIsLoaded) && packetIsLoaded.Values.TryGetValue(19, out var isLoaded) ? (bool.TryParse(isLoaded.ToString(), out var isLoadedResult) ? isLoadedResult : false) : false,
                Numberofcomponent = _latestPackets.TryGetValue(1, out var packetNumberofcomponent) && packetNumberofcomponent.Values.TryGetValue(19, out var numberofcomponent) ? (int.TryParse(numberofcomponent.ToString(), out var numberofcomponentResult) ? numberofcomponentResult : 0) : 0,
                TraySize = _latestPackets.TryGetValue(1, out var packetTraySize) && packetTraySize.Values.TryGetValue(19, out var traySize) ? (int.TryParse(traySize.ToString(), out var traySizeResult) ? traySizeResult : 0) : 0,
                NumberOfTrays = _latestPackets.TryGetValue(1, out var packetNumberOfTrays) && packetNumberOfTrays.Values.TryGetValue(19, out var numberOfTrays) ? (int.TryParse(numberOfTrays.ToString(), out var numberOfTraysResult) ? numberOfTraysResult : 0) : 0
            };

            return Task.FromResult(new ResponsePackage
            {
                ResponseId = request.RequestId,
                Parameters = new Dictionary<int, object>()
                {
                    { 0, item }
                }
            });
        }

        private Task<ResponsePackage> OutputTray(RequestPackage request)
        {
            OutputTrayModel item = new OutputTrayModel
            {
                IsUnLoaded = _latestPackets.TryGetValue(1, out var packetIsUnLoaded) && packetIsUnLoaded.Values.TryGetValue(20, out var isUnLoaded) ? (bool.TryParse(isUnLoaded.ToString(), out var isUnLoadedResult) ? isUnLoadedResult : false) : false,
                Numberofcomponent = _latestPackets.TryGetValue(1, out var packetNumberofcomponent) && packetNumberofcomponent.Values.TryGetValue(20, out var numberofcomponent) ? (int.TryParse(numberofcomponent.ToString(), out var numberofcomponentResult) ? numberofcomponentResult : 0) : 0,
                TraySize = _latestPackets.TryGetValue(1, out var packetTraySize) && packetTraySize.Values.TryGetValue(20, out var traySize) ? (int.TryParse(traySize.ToString(), out var traySizeResult) ? traySizeResult : 0) : 0,
                NumberOfTrays = _latestPackets.TryGetValue(1, out var packetNumberOfTrays) && packetNumberOfTrays.Values.TryGetValue(20, out var numberOfTrays) ? (int.TryParse(numberOfTrays.ToString(), out var numberOfTraysResult) ? numberOfTraysResult : 0) : 0
            };

            return Task.FromResult(new ResponsePackage
            {
                ResponseId = request.RequestId,
                Parameters = new Dictionary<int, object>()
                {
                    { 0, item }
                }
            });
        }

        private Task<ResponsePackage> NGBin1(RequestPackage request)
        {
            NGBinGroupModel item = new NGBinGroupModel
            {
                NGBin1 = new NGBinModel
                {
                    RejectedCount = _latestPackets.TryGetValue(1, out var packetNgBin1RejectedCount) && packetNgBin1RejectedCount.Values.TryGetValue(21, out var ngBin1RejectedCount) ? (int.TryParse(ngBin1RejectedCount.ToString(), out var ngBin1RejectedCountResult) ? ngBin1RejectedCountResult : 0) : 0,
                    IsFull = _latestPackets.TryGetValue(1, out var packetNgBin1IsFull) && packetNgBin1IsFull.Values.TryGetValue(21, out var ngBin1IsFull) ? (bool.TryParse(ngBin1IsFull.ToString(), out var ngBin1IsFullResult) ? ngBin1IsFullResult : false) : false
                },

            };

            return Task.FromResult(new ResponsePackage
            {
                ResponseId = request.RequestId,
                Parameters = new Dictionary<int, object>()
                {
                    { 0, item }
                }
            });
        }

        private Task<ResponsePackage> NGBin2(RequestPackage request)
        {
            NGBinGroupModel item = new NGBinGroupModel
            {

                NGBin2 = new NGBinModel
                {
                    RejectedCount = _latestPackets.TryGetValue(1, out var packetNgBin2RejectedCount) && packetNgBin2RejectedCount.Values.TryGetValue(25, out var ngBin2RejectedCount) ? (int.TryParse(ngBin2RejectedCount.ToString(), out var ngBin2RejectedCountResult) ? ngBin2RejectedCountResult : 0) : 0,
                    IsFull = _latestPackets.TryGetValue(1, out var packetNgBin2IsFull) && packetNgBin2IsFull.Values.TryGetValue(25, out var ngBin2IsFull) ? (bool.TryParse(ngBin2IsFull.ToString(), out var ngBin2IsFullResult) ? ngBin2IsFullResult : false) : false
                }
            };

            return Task.FromResult(new ResponsePackage
            {
                ResponseId = request.RequestId,
                Parameters = new Dictionary<int, object>()
                {
                    { 0, item }
                }
            });
        }

        private Task<ResponsePackage> Bending1Monitor(RequestPackage request)
        {
            BendingMonitorModel item = new BendingMonitorModel
            {
                BatchNo = _latestPackets.TryGetValue(1, out var packetBatchNo) && packetBatchNo.Values.TryGetValue(26, out var batchNo) ? batchNo.ToString() : "NA",
                Product1 = new BendingMonitorProductModel
                {
                    QRCode = _latestPackets.TryGetValue(1, out var p1QRCode) && p1QRCode.Values.TryGetValue(26, out var p1QR) ? p1QR.ToString() : "NA",
                    Load = new ParameterLImitValues
                    {
                        UpperLimit = _latestPackets.TryGetValue(1, out var p1LoadUpper) && p1LoadUpper.Values.TryGetValue(26, out var p1LU) ? (double.TryParse(p1LU.ToString(), out var p1LUResult) ? p1LUResult : 0) : 0,
                        PresentValue = _latestPackets.TryGetValue(1, out var p1LoadPresent) && p1LoadPresent.Values.TryGetValue(26, out var p1LP) ? (double.TryParse(p1LP.ToString(), out var p1LPResult) ? p1LPResult : 0) : 0,
                        LowerLimit = _latestPackets.TryGetValue(1, out var p1LoadLower) && p1LoadLower.Values.TryGetValue(26, out var p1LL) ? (double.TryParse(p1LL.ToString(), out var p1LLResult) ? p1LLResult : 0) : 0
                    },
                    Temperature = new ParameterLImitValues
                    {
                        UpperLimit = _latestPackets.TryGetValue(1, out var p1TempUpper) && p1TempUpper.Values.TryGetValue(26, out var p1TU) ? (double.TryParse(p1TU.ToString(), out var p1TUResult) ? p1TUResult : 0) : 0,
                        PresentValue = _latestPackets.TryGetValue(1, out var p1TempPresent) && p1TempPresent.Values.TryGetValue(26, out var p1TP) ? (double.TryParse(p1TP.ToString(), out var p1TPResult) ? p1TPResult : 0) : 0,
                        LowerLimit = _latestPackets.TryGetValue(1, out var p1TempLower) && p1TempLower.Values.TryGetValue(26, out var p1TL) ? (double.TryParse(p1TL.ToString(), out var p1TLResult) ? p1TLResult : 0) : 0
                    },
                    BendingTime = _latestPackets.TryGetValue(1, out var p1BendTime) && p1BendTime.Values.TryGetValue(26, out var p1BT) ? (double.TryParse(p1BT.ToString(), out var p1BTResult) ? p1BTResult : 0) : 0,
                    Result = _latestPackets.TryGetValue(1, out var p1Result) && p1Result.Values.TryGetValue(26, out var p1R) ? (bool.TryParse(p1R.ToString(), out var p1RResult) ? p1RResult : false) : false
                },
                Product2 = new BendingMonitorProductModel
                {
                    QRCode = _latestPackets.TryGetValue(1, out var p2QRCode) && p2QRCode.Values.TryGetValue(26, out var p2QR) ? p2QR.ToString() : "NA",
                    Load = new ParameterLImitValues
                    {
                        UpperLimit = _latestPackets.TryGetValue(1, out var p2LoadUpper) && p2LoadUpper.Values.TryGetValue(26, out var p2LU) ? (double.TryParse(p2LU.ToString(), out var p2LUResult) ? p2LUResult : 0) : 0,
                        PresentValue = _latestPackets.TryGetValue(1, out var p2LoadPresent) && p2LoadPresent.Values.TryGetValue(26, out var p2LP) ? (double.TryParse(p2LP.ToString(), out var p2LPResult) ? p2LPResult : 0) : 0,
                        LowerLimit = _latestPackets.TryGetValue(1, out var p2LoadLower) && p2LoadLower.Values.TryGetValue(26, out var p2LL) ? (double.TryParse(p2LL.ToString(), out var p2LLResult) ? p2LLResult : 0) : 0
                    },
                    Temperature = new ParameterLImitValues
                    {
                        UpperLimit = _latestPackets.TryGetValue(1, out var p2TempUpper) && p2TempUpper.Values.TryGetValue(26, out var p2TU) ? (double.TryParse(p2TU.ToString(), out var p2TUResult) ? p2TUResult : 0) : 0,
                        PresentValue = _latestPackets.TryGetValue(1, out var p2TempPresent) && p2TempPresent.Values.TryGetValue(26, out var p2TP) ? (double.TryParse(p2TP.ToString(), out var p2TPResult) ? p2TPResult : 0) : 0,
                        LowerLimit = _latestPackets.TryGetValue(1, out var p2TempLower) && p2TempLower.Values.TryGetValue(26, out var p2TL) ? (double.TryParse(p2TL.ToString(), out var p2TLResult) ? p2TLResult : 0) : 0
                    },
                    BendingTime = _latestPackets.TryGetValue(1, out var p2BendTime) && p2BendTime.Values.TryGetValue(26, out var p2BT) ? (double.TryParse(p2BT.ToString(), out var p2BTResult) ? p2BTResult : 0) : 0,
                    Result = _latestPackets.TryGetValue(1, out var p2Result) && p2Result.Values.TryGetValue(26, out var p2R) ? (bool.TryParse(p2R.ToString(), out var p2RResult) ? p2RResult : false) : false
                },
                Product3 = new BendingMonitorProductModel
                {
                    QRCode = _latestPackets.TryGetValue(1, out var p3QRCode) && p3QRCode.Values.TryGetValue(26, out var p3QR) ? p3QR.ToString() : "NA",
                    Load = new ParameterLImitValues
                    {
                        UpperLimit = _latestPackets.TryGetValue(1, out var p3LoadUpper) && p3LoadUpper.Values.TryGetValue(26, out var p3LU) ? (double.TryParse(p3LU.ToString(), out var p3LUResult) ? p3LUResult : 0) : 0,
                        PresentValue = _latestPackets.TryGetValue(1, out var p3LoadPresent) && p3LoadPresent.Values.TryGetValue(26, out var p3LP) ? (double.TryParse(p3LP.ToString(), out var p3LPResult) ? p3LPResult : 0) : 0,
                        LowerLimit = _latestPackets.TryGetValue(1, out var p3LoadLower) && p3LoadLower.Values.TryGetValue(26, out var p3LL) ? (double.TryParse(p3LL.ToString(), out var p3LLResult) ? p3LLResult : 0) : 0
                    },
                    Temperature = new ParameterLImitValues
                    {
                        UpperLimit = _latestPackets.TryGetValue(1, out var p3TempUpper) && p3TempUpper.Values.TryGetValue(26, out var p3TU) ? (double.TryParse(p3TU.ToString(), out var p3TUResult) ? p3TUResult : 0) : 0,
                        PresentValue = _latestPackets.TryGetValue(1, out var p3TempPresent) && p3TempPresent.Values.TryGetValue(26, out var p3TP) ? (double.TryParse(p3TP.ToString(), out var p3TPResult) ? p3TPResult : 0) : 0,
                        LowerLimit = _latestPackets.TryGetValue(1, out var p3TempLower) && p3TempLower.Values.TryGetValue(26, out var p3TL) ? (double.TryParse(p3TL.ToString(), out var p3TLResult) ? p3TLResult : 0) : 0
                    },
                    BendingTime = _latestPackets.TryGetValue(1, out var p3BendTime) && p3BendTime.Values.TryGetValue(26, out var p3BT) ? (double.TryParse(p3BT.ToString(), out var p3BTResult) ? p3BTResult : 0) : 0,
                    Result = _latestPackets.TryGetValue(1, out var p3Result) && p3Result.Values.TryGetValue(26, out var p3R) ? (bool.TryParse(p3R.ToString(), out var p3RResult) ? p3RResult : false) : false
                },
                Product4 = new BendingMonitorProductModel
                {
                    QRCode = _latestPackets.TryGetValue(1, out var p4QRCode) && p4QRCode.Values.TryGetValue(26, out var p4QR) ? p4QR.ToString() : "NA",
                    Load = new ParameterLImitValues
                    {
                        UpperLimit = _latestPackets.TryGetValue(1, out var p4LoadUpper) && p4LoadUpper.Values.TryGetValue(26, out var p4LU) ? (double.TryParse(p4LU.ToString(), out var p4LUResult) ? p4LUResult : 0) : 0,
                        PresentValue = _latestPackets.TryGetValue(1, out var p4LoadPresent) && p4LoadPresent.Values.TryGetValue(26, out var p4LP) ? (double.TryParse(p4LP.ToString(), out var p4LPResult) ? p4LPResult : 0) : 0,
                        LowerLimit = _latestPackets.TryGetValue(1, out var p4LoadLower) && p4LoadLower.Values.TryGetValue(26, out var p4LL) ? (double.TryParse(p4LL.ToString(), out var p4LLResult) ? p4LLResult : 0) : 0
                    },
                    Temperature = new ParameterLImitValues
                    {
                        UpperLimit = _latestPackets.TryGetValue(1, out var p4TempUpper) && p4TempUpper.Values.TryGetValue(26, out var p4TU) ? (double.TryParse(p4TU.ToString(), out var p4TUResult) ? p4TUResult : 0) : 0,
                        PresentValue = _latestPackets.TryGetValue(1, out var p4TempPresent) && p4TempPresent.Values.TryGetValue(26, out var p4TP) ? (double.TryParse(p4TP.ToString(), out var p4TPResult) ? p4TPResult : 0) : 0,
                        LowerLimit = _latestPackets.TryGetValue(1, out var p4TempLower) && p4TempLower.Values.TryGetValue(26, out var p4TL) ? (double.TryParse(p4TL.ToString(), out var p4TLResult) ? p4TLResult : 0) : 0
                    },
                    BendingTime = _latestPackets.TryGetValue(1, out var p4BendTime) && p4BendTime.Values.TryGetValue(26, out var p4BT) ? (double.TryParse(p4BT.ToString(), out var p4BTResult) ? p4BTResult : 0) : 0,
                    Result = _latestPackets.TryGetValue(1, out var p4Result) && p4Result.Values.TryGetValue(26, out var p4R) ? (bool.TryParse(p4R.ToString(), out var p4RResult) ? p4RResult : false) : false
                }
            };

            return Task.FromResult(new ResponsePackage
            {
                ResponseId = request.RequestId,
                Parameters = new Dictionary<int, object>()
                {
                    { 0, item }
                }
            });
        }

        private Task<ResponsePackage> Bending2Monitor(RequestPackage request)
        {
            BendingMonitorModel item = new BendingMonitorModel
            {
                BatchNo = _latestPackets.TryGetValue(1, out var packetBatchNo) && packetBatchNo.Values.TryGetValue(27, out var batchNo) ? batchNo.ToString() : "NA",
                Product1 = new BendingMonitorProductModel
                {
                    QRCode = _latestPackets.TryGetValue(1, out var p1QRCode) && p1QRCode.Values.TryGetValue(27, out var p1QR) ? p1QR.ToString() : "NA",
                    Load = new ParameterLImitValues
                    {
                        UpperLimit = _latestPackets.TryGetValue(1, out var p1LoadUpper) && p1LoadUpper.Values.TryGetValue(27, out var p1LU) ? (double.TryParse(p1LU.ToString(), out var p1LUResult) ? p1LUResult : 0) : 0,
                        PresentValue = _latestPackets.TryGetValue(1, out var p1LoadPresent) && p1LoadPresent.Values.TryGetValue(27, out var p1LP) ? (double.TryParse(p1LP.ToString(), out var p1LPResult) ? p1LPResult : 0) : 0,
                        LowerLimit = _latestPackets.TryGetValue(1, out var p1LoadLower) && p1LoadLower.Values.TryGetValue(27, out var p1LL) ? (double.TryParse(p1LL.ToString(), out var p1LLResult) ? p1LLResult : 0) : 0
                    },
                    Temperature = new ParameterLImitValues
                    {
                        UpperLimit = _latestPackets.TryGetValue(1, out var p1TempUpper) && p1TempUpper.Values.TryGetValue(27, out var p1TU) ? (double.TryParse(p1TU.ToString(), out var p1TUResult) ? p1TUResult : 0) : 0,
                        PresentValue = _latestPackets.TryGetValue(1, out var p1TempPresent) && p1TempPresent.Values.TryGetValue(27, out var p1TP) ? (double.TryParse(p1TP.ToString(), out var p1TPResult) ? p1TPResult : 0) : 0,
                        LowerLimit = _latestPackets.TryGetValue(1, out var p1TempLower) && p1TempLower.Values.TryGetValue(27, out var p1TL) ? (double.TryParse(p1TL.ToString(), out var p1TLResult) ? p1TLResult : 0) : 0
                    },
                    BendingTime = _latestPackets.TryGetValue(1, out var p1BendTime) && p1BendTime.Values.TryGetValue(27, out var p1BT) ? (double.TryParse(p1BT.ToString(), out var p1BTResult) ? p1BTResult : 0) : 0,
                    Result = _latestPackets.TryGetValue(1, out var p1Result) && p1Result.Values.TryGetValue(27, out var p1R) ? (bool.TryParse(p1R.ToString(), out var p1RResult) ? p1RResult : false) : false
                },
                Product2 = new BendingMonitorProductModel
                {
                    QRCode = _latestPackets.TryGetValue(1, out var p2QRCode) && p2QRCode.Values.TryGetValue(27, out var p2QR) ? p2QR.ToString() : "NA",
                    Load = new ParameterLImitValues
                    {
                        UpperLimit = _latestPackets.TryGetValue(1, out var p2LoadUpper) && p2LoadUpper.Values.TryGetValue(27, out var p2LU) ? (double.TryParse(p2LU.ToString(), out var p2LUResult) ? p2LUResult : 0) : 0,
                        PresentValue = _latestPackets.TryGetValue(1, out var p2LoadPresent) && p2LoadPresent.Values.TryGetValue(27, out var p2LP) ? (double.TryParse(p2LP.ToString(), out var p2LPResult) ? p2LPResult : 0) : 0,
                        LowerLimit = _latestPackets.TryGetValue(1, out var p2LoadLower) && p2LoadLower.Values.TryGetValue(27, out var p2LL) ? (double.TryParse(p2LL.ToString(), out var p2LLResult) ? p2LLResult : 0) : 0
                    },
                    Temperature = new ParameterLImitValues
                    {
                        UpperLimit = _latestPackets.TryGetValue(1, out var p2TempUpper) && p2TempUpper.Values.TryGetValue(27, out var p2TU) ? (double.TryParse(p2TU.ToString(), out var p2TUResult) ? p2TUResult : 0) : 0,
                        PresentValue = _latestPackets.TryGetValue(1, out var p2TempPresent) && p2TempPresent.Values.TryGetValue(27, out var p2TP) ? (double.TryParse(p2TP.ToString(), out var p2TPResult) ? p2TPResult : 0) : 0,
                        LowerLimit = _latestPackets.TryGetValue(1, out var p2TempLower) && p2TempLower.Values.TryGetValue(27, out var p2TL) ? (double.TryParse(p2TL.ToString(), out var p2TLResult) ? p2TLResult : 0) : 0
                    },
                    BendingTime = _latestPackets.TryGetValue(1, out var p2BendTime) && p2BendTime.Values.TryGetValue(27, out var p2BT) ? (double.TryParse(p2BT.ToString(), out var p2BTResult) ? p2BTResult : 0) : 0,
                    Result = _latestPackets.TryGetValue(1, out var p2Result) && p2Result.Values.TryGetValue(27, out var p2R) ? (bool.TryParse(p2R.ToString(), out var p2RResult) ? p2RResult : false) : false
                },
                Product3 = new BendingMonitorProductModel
                {
                    QRCode = _latestPackets.TryGetValue(1, out var p3QRCode) && p3QRCode.Values.TryGetValue(27, out var p3QR) ? p3QR.ToString() : "NA",
                    Load = new ParameterLImitValues
                    {
                        UpperLimit = _latestPackets.TryGetValue(1, out var p3LoadUpper) && p3LoadUpper.Values.TryGetValue(27, out var p3LU) ? (double.TryParse(p3LU.ToString(), out var p3LUResult) ? p3LUResult : 0) : 0,
                        PresentValue = _latestPackets.TryGetValue(1, out var p3LoadPresent) && p3LoadPresent.Values.TryGetValue(27, out var p3LP) ? (double.TryParse(p3LP.ToString(), out var p3LPResult) ? p3LPResult : 0) : 0,
                        LowerLimit = _latestPackets.TryGetValue(1, out var p3LoadLower) && p3LoadLower.Values.TryGetValue(27, out var p3LL) ? (double.TryParse(p3LL.ToString(), out var p3LLResult) ? p3LLResult : 0) : 0
                    },
                    Temperature = new ParameterLImitValues
                    {
                        UpperLimit = _latestPackets.TryGetValue(1, out var p3TempUpper) && p3TempUpper.Values.TryGetValue(27, out var p3TU) ? (double.TryParse(p3TU.ToString(), out var p3TUResult) ? p3TUResult : 0) : 0,
                        PresentValue = _latestPackets.TryGetValue(1, out var p3TempPresent) && p3TempPresent.Values.TryGetValue(27, out var p3TP) ? (double.TryParse(p3TP.ToString(), out var p3TPResult) ? p3TPResult : 0) : 0,
                        LowerLimit = _latestPackets.TryGetValue(1, out var p3TempLower) && p3TempLower.Values.TryGetValue(27, out var p3TL) ? (double.TryParse(p3TL.ToString(), out var p3TLResult) ? p3TLResult : 0) : 0
                    },
                    BendingTime = _latestPackets.TryGetValue(1, out var p3BendTime) && p3BendTime.Values.TryGetValue(27, out var p3BT) ? (double.TryParse(p3BT.ToString(), out var p3BTResult) ? p3BTResult : 0) : 0,
                    Result = _latestPackets.TryGetValue(1, out var p3Result) && p3Result.Values.TryGetValue(27, out var p3R) ? (bool.TryParse(p3R.ToString(), out var p3RResult) ? p3RResult : false) : false
                },
                Product4 = new BendingMonitorProductModel
                {
                    QRCode = _latestPackets.TryGetValue(1, out var p4QRCode) && p4QRCode.Values.TryGetValue(27, out var p4QR) ? p4QR.ToString() : "NA",
                    Load = new ParameterLImitValues
                    {
                        UpperLimit = _latestPackets.TryGetValue(1, out var p4LoadUpper) && p4LoadUpper.Values.TryGetValue(27, out var p4LU) ? (double.TryParse(p4LU.ToString(), out var p4LUResult) ? p4LUResult : 0) : 0,
                        PresentValue = _latestPackets.TryGetValue(1, out var p4LoadPresent) && p4LoadPresent.Values.TryGetValue(27, out var p4LP) ? (double.TryParse(p4LP.ToString(), out var p4LPResult) ? p4LPResult : 0) : 0,
                        LowerLimit = _latestPackets.TryGetValue(1, out var p4LoadLower) && p4LoadLower.Values.TryGetValue(27, out var p4LL) ? (double.TryParse(p4LL.ToString(), out var p4LLResult) ? p4LLResult : 0) : 0
                    },
                    Temperature = new ParameterLImitValues
                    {
                        UpperLimit = _latestPackets.TryGetValue(1, out var p4TempUpper) && p4TempUpper.Values.TryGetValue(27, out var p4TU) ? (double.TryParse(p4TU.ToString(), out var p4TUResult) ? p4TUResult : 0) : 0,
                        PresentValue = _latestPackets.TryGetValue(1, out var p4TempPresent) && p4TempPresent.Values.TryGetValue(27, out var p4TP) ? (double.TryParse(p4TP.ToString(), out var p4TPResult) ? p4TPResult : 0) : 0,
                        LowerLimit = _latestPackets.TryGetValue(1, out var p4TempLower) && p4TempLower.Values.TryGetValue(27, out var p4TL) ? (double.TryParse(p4TL.ToString(), out var p4TLResult) ? p4TLResult : 0) : 0
                    },
                    BendingTime = _latestPackets.TryGetValue(1, out var p4BendTime) && p4BendTime.Values.TryGetValue(27, out var p4BT) ? (double.TryParse(p4BT.ToString(), out var p4BTResult) ? p4BTResult : 0) : 0,
                    Result = _latestPackets.TryGetValue(1, out var p4Result) && p4Result.Values.TryGetValue(27, out var p4R) ? (bool.TryParse(p4R.ToString(), out var p4RResult) ? p4RResult : false) : false
                }
            };

            return Task.FromResult(new ResponsePackage
            {
                ResponseId = request.RequestId,
                Parameters = new Dictionary<int, object>()
                {
                    { 0, item }
                }
            });
        }

        private Task<ResponsePackage> Bending3Monitor(RequestPackage request)
        {
            BendingMonitorModel item = new BendingMonitorModel
            {
                BatchNo = _latestPackets.TryGetValue(1, out var packetBatchNo) && packetBatchNo.Values.TryGetValue(28, out var batchNo) ? batchNo.ToString() : "NA",
                Product1 = new BendingMonitorProductModel
                {
                    QRCode = _latestPackets.TryGetValue(1, out var p1QRCode) && p1QRCode.Values.TryGetValue(28, out var p1QR) ? p1QR.ToString() : "NA",
                    Load = new ParameterLImitValues
                    {
                        UpperLimit = _latestPackets.TryGetValue(1, out var p1LoadUpper) && p1LoadUpper.Values.TryGetValue(28, out var p1LU) ? (double.TryParse(p1LU.ToString(), out var p1LUResult) ? p1LUResult : 0) : 0,
                        PresentValue = _latestPackets.TryGetValue(1, out var p1LoadPresent) && p1LoadPresent.Values.TryGetValue(28, out var p1LP) ? (double.TryParse(p1LP.ToString(), out var p1LPResult) ? p1LPResult : 0) : 0,
                        LowerLimit = _latestPackets.TryGetValue(1, out var p1LoadLower) && p1LoadLower.Values.TryGetValue(28, out var p1LL) ? (double.TryParse(p1LL.ToString(), out var p1LLResult) ? p1LLResult : 0) : 0
                    },
                    Temperature = new ParameterLImitValues
                    {
                        UpperLimit = _latestPackets.TryGetValue(1, out var p1TempUpper) && p1TempUpper.Values.TryGetValue(28, out var p1TU) ? (double.TryParse(p1TU.ToString(), out var p1TUResult) ? p1TUResult : 0) : 0,
                        PresentValue = _latestPackets.TryGetValue(1, out var p1TempPresent) && p1TempPresent.Values.TryGetValue(28, out var p1TP) ? (double.TryParse(p1TP.ToString(), out var p1TPResult) ? p1TPResult : 0) : 0,
                        LowerLimit = _latestPackets.TryGetValue(1, out var p1TempLower) && p1TempLower.Values.TryGetValue(28, out var p1TL) ? (double.TryParse(p1TL.ToString(), out var p1TLResult) ? p1TLResult : 0) : 0
                    },
                    BendingTime = _latestPackets.TryGetValue(1, out var p1BendTime) && p1BendTime.Values.TryGetValue(28, out var p1BT) ? (double.TryParse(p1BT.ToString(), out var p1BTResult) ? p1BTResult : 0) : 0,
                    Result = _latestPackets.TryGetValue(1, out var p1Result) && p1Result.Values.TryGetValue(28, out var p1R) ? (bool.TryParse(p1R.ToString(), out var p1RResult) ? p1RResult : false) : false
                },
                Product2 = new BendingMonitorProductModel
                {
                    QRCode = _latestPackets.TryGetValue(1, out var p2QRCode) && p2QRCode.Values.TryGetValue(28, out var p2QR) ? p2QR.ToString() : "NA",
                    Load = new ParameterLImitValues
                    {
                        UpperLimit = _latestPackets.TryGetValue(1, out var p2LoadUpper) && p2LoadUpper.Values.TryGetValue(28, out var p2LU) ? (double.TryParse(p2LU.ToString(), out var p2LUResult) ? p2LUResult : 0) : 0,
                        PresentValue = _latestPackets.TryGetValue(1, out var p2LoadPresent) && p2LoadPresent.Values.TryGetValue(28, out var p2LP) ? (double.TryParse(p2LP.ToString(), out var p2LPResult) ? p2LPResult : 0) : 0,
                        LowerLimit = _latestPackets.TryGetValue(1, out var p2LoadLower) && p2LoadLower.Values.TryGetValue(28, out var p2LL) ? (double.TryParse(p2LL.ToString(), out var p2LLResult) ? p2LLResult : 0) : 0
                    },
                    Temperature = new ParameterLImitValues
                    {
                        UpperLimit = _latestPackets.TryGetValue(1, out var p2TempUpper) && p2TempUpper.Values.TryGetValue(28, out var p2TU) ? (double.TryParse(p2TU.ToString(), out var p2TUResult) ? p2TUResult : 0) : 0,
                        PresentValue = _latestPackets.TryGetValue(1, out var p2TempPresent) && p2TempPresent.Values.TryGetValue(28, out var p2TP) ? (double.TryParse(p2TP.ToString(), out var p2TPResult) ? p2TPResult : 0) : 0,
                        LowerLimit = _latestPackets.TryGetValue(1, out var p2TempLower) && p2TempLower.Values.TryGetValue(28, out var p2TL) ? (double.TryParse(p2TL.ToString(), out var p2TLResult) ? p2TLResult : 0) : 0
                    },
                    BendingTime = _latestPackets.TryGetValue(1, out var p2BendTime) && p2BendTime.Values.TryGetValue(28, out var p2BT) ? (double.TryParse(p2BT.ToString(), out var p2BTResult) ? p2BTResult : 0) : 0,
                    Result = _latestPackets.TryGetValue(1, out var p2Result) && p2Result.Values.TryGetValue(28, out var p2R) ? (bool.TryParse(p2R.ToString(), out var p2RResult) ? p2RResult : false) : false
                },
                Product3 = new BendingMonitorProductModel
                {
                    QRCode = _latestPackets.TryGetValue(1, out var p3QRCode) && p3QRCode.Values.TryGetValue(28, out var p3QR) ? p3QR.ToString() : "NA",
                    Load = new ParameterLImitValues
                    {
                        UpperLimit = _latestPackets.TryGetValue(1, out var p3LoadUpper) && p3LoadUpper.Values.TryGetValue(28, out var p3LU) ? (double.TryParse(p3LU.ToString(), out var p3LUResult) ? p3LUResult : 0) : 0,
                        PresentValue = _latestPackets.TryGetValue(1, out var p3LoadPresent) && p3LoadPresent.Values.TryGetValue(28, out var p3LP) ? (double.TryParse(p3LP.ToString(), out var p3LPResult) ? p3LPResult : 0) : 0,
                        LowerLimit = _latestPackets.TryGetValue(1, out var p3LoadLower) && p3LoadLower.Values.TryGetValue(28, out var p3LL) ? (double.TryParse(p3LL.ToString(), out var p3LLResult) ? p3LLResult : 0) : 0
                    },
                    Temperature = new ParameterLImitValues
                    {
                        UpperLimit = _latestPackets.TryGetValue(1, out var p3TempUpper) && p3TempUpper.Values.TryGetValue(28, out var p3TU) ? (double.TryParse(p3TU.ToString(), out var p3TUResult) ? p3TUResult : 0) : 0,
                        PresentValue = _latestPackets.TryGetValue(1, out var p3TempPresent) && p3TempPresent.Values.TryGetValue(28, out var p3TP) ? (double.TryParse(p3TP.ToString(), out var p3TPResult) ? p3TPResult : 0) : 0,
                        LowerLimit = _latestPackets.TryGetValue(1, out var p3TempLower) && p3TempLower.Values.TryGetValue(28, out var p3TL) ? (double.TryParse(p3TL.ToString(), out var p3TLResult) ? p3TLResult : 0) : 0
                    },
                    BendingTime = _latestPackets.TryGetValue(1, out var p3BendTime) && p3BendTime.Values.TryGetValue(28, out var p3BT) ? (double.TryParse(p3BT.ToString(), out var p3BTResult) ? p3BTResult : 0) : 0,
                    Result = _latestPackets.TryGetValue(1, out var p3Result) && p3Result.Values.TryGetValue(28, out var p3R) ? (bool.TryParse(p3R.ToString(), out var p3RResult) ? p3RResult : false) : false
                },
                Product4 = new BendingMonitorProductModel
                {
                    QRCode = _latestPackets.TryGetValue(1, out var p4QRCode) && p4QRCode.Values.TryGetValue(28, out var p4QR) ? p4QR.ToString() : "NA",
                    Load = new ParameterLImitValues
                    {
                        UpperLimit = _latestPackets.TryGetValue(1, out var p4LoadUpper) && p4LoadUpper.Values.TryGetValue(28, out var p4LU) ? (double.TryParse(p4LU.ToString(), out var p4LUResult) ? p4LUResult : 0) : 0,
                        PresentValue = _latestPackets.TryGetValue(1, out var p4LoadPresent) && p4LoadPresent.Values.TryGetValue(28, out var p4LP) ? (double.TryParse(p4LP.ToString(), out var p4LPResult) ? p4LPResult : 0) : 0,
                        LowerLimit = _latestPackets.TryGetValue(1, out var p4LoadLower) && p4LoadLower.Values.TryGetValue(28, out var p4LL) ? (double.TryParse(p4LL.ToString(), out var p4LLResult) ? p4LLResult : 0) : 0
                    },
                    Temperature = new ParameterLImitValues
                    {
                        UpperLimit = _latestPackets.TryGetValue(1, out var p4TempUpper) && p4TempUpper.Values.TryGetValue(28, out var p4TU) ? (double.TryParse(p4TU.ToString(), out var p4TUResult) ? p4TUResult : 0) : 0,
                        PresentValue = _latestPackets.TryGetValue(1, out var p4TempPresent) && p4TempPresent.Values.TryGetValue(28, out var p4TP) ? (double.TryParse(p4TP.ToString(), out var p4TPResult) ? p4TPResult : 0) : 0,
                        LowerLimit = _latestPackets.TryGetValue(1, out var p4TempLower) && p4TempLower.Values.TryGetValue(28, out var p4TL) ? (double.TryParse(p4TL.ToString(), out var p4TLResult) ? p4TLResult : 0) : 0
                    },
                    BendingTime = _latestPackets.TryGetValue(1, out var p4BendTime) && p4BendTime.Values.TryGetValue(28, out var p4BT) ? (double.TryParse(p4BT.ToString(), out var p4BTResult) ? p4BTResult : 0) : 0,
                    Result = _latestPackets.TryGetValue(1, out var p4Result) && p4Result.Values.TryGetValue(28, out var p4R) ? (bool.TryParse(p4R.ToString(), out var p4RResult) ? p4RResult : false) : false
                }
            };
            return Task.FromResult(new ResponsePackage
            {
                ResponseId = request.RequestId,
                Parameters = new Dictionary<int, object>()
                {
                    { 0, item }
                }
            });
        }
        private Task<ResponsePackage> PostBendingMonitor(RequestPackage request)
        {
            PostBendingMonitorModel item = new PostBendingMonitorModel
            {
                BatchNo = _latestPackets.TryGetValue(1, out var packetBatchNo) && packetBatchNo.Values.TryGetValue(29, out var batchNo) ? batchNo.ToString() : "NA",
                Product1 = new PostBendingMonitorRow
                {
                    Product = _latestPackets.TryGetValue(1, out var p1Prodp) && p1Prodp.Values.TryGetValue(29, out var p1Prodv) ? p1Prodv.ToString() : "NA",
                    QRCode = _latestPackets.TryGetValue(1, out var p1QRp) && p1QRp.Values.TryGetValue(29, out var p1QRv) ? p1QRv.ToString() : "NA",
                    X = new PostParameterLImitValues
                    {
                        UpperLimit = _latestPackets.TryGetValue(1, out var p1XUp) && p1XUp.Values.TryGetValue(29, out var p1XUv) ? (double.TryParse(p1XUv.ToString(), out var p1XUr) ? p1XUr : 0) : 0,
                        PresentValue = _latestPackets.TryGetValue(1, out var p1XPp) && p1XPp.Values.TryGetValue(29, out var p1XPv) ? (double.TryParse(p1XPv.ToString(), out var p1XPr) ? p1XPr : 0) : 0,
                        LowerLimit = _latestPackets.TryGetValue(1, out var p1XLp) && p1XLp.Values.TryGetValue(29, out var p1XLv) ? (double.TryParse(p1XLv.ToString(), out var p1XLr) ? p1XLr : 0) : 0
                    },
                    Y = new PostParameterLImitValues
                    {
                        UpperLimit = _latestPackets.TryGetValue(1, out var p1YUp) && p1YUp.Values.TryGetValue(29, out var p1YUv) ? (double.TryParse(p1YUv.ToString(), out var p1YUr) ? p1YUr : 0) : 0,
                        PresentValue = _latestPackets.TryGetValue(1, out var p1YPp) && p1YPp.Values.TryGetValue(29, out var p1YPv) ? (double.TryParse(p1YPv.ToString(), out var p1YPr) ? p1YPr : 0) : 0,
                        LowerLimit = _latestPackets.TryGetValue(1, out var p1YLp) && p1YLp.Values.TryGetValue(29, out var p1YLv) ? (double.TryParse(p1YLv.ToString(), out var p1YLr) ? p1YLr : 0) : 0
                    },
                    Z = new PostParameterLImitValues
                    {
                        UpperLimit = _latestPackets.TryGetValue(1, out var p1ZUp) && p1ZUp.Values.TryGetValue(29, out var p1ZUv) ? (double.TryParse(p1ZUv.ToString(), out var p1ZUr) ? p1ZUr : 0) : 0,
                        PresentValue = _latestPackets.TryGetValue(1, out var p1ZPp) && p1ZPp.Values.TryGetValue(29, out var p1ZPv) ? (double.TryParse(p1ZPv.ToString(), out var p1ZPr) ? p1ZPr : 0) : 0,
                        LowerLimit = _latestPackets.TryGetValue(1, out var p1ZLp) && p1ZLp.Values.TryGetValue(29, out var p1ZLv) ? (double.TryParse(p1ZLv.ToString(), out var p1ZLr) ? p1ZLr : 0) : 0
                    },
                    W = new PostParameterLImitValues
                    {
                        UpperLimit = _latestPackets.TryGetValue(1, out var p1WUp) && p1WUp.Values.TryGetValue(29, out var p1WUv) ? (double.TryParse(p1WUv.ToString(), out var p1WUr) ? p1WUr : 0) : 0,
                        PresentValue = _latestPackets.TryGetValue(1, out var p1WPp) && p1WPp.Values.TryGetValue(29, out var p1WPv) ? (double.TryParse(p1WPv.ToString(), out var p1WPr) ? p1WPr : 0) : 0,
                        LowerLimit = _latestPackets.TryGetValue(1, out var p1WLp) && p1WLp.Values.TryGetValue(29, out var p1WLv) ? (double.TryParse(p1WLv.ToString(), out var p1WLr) ? p1WLr : 0) : 0
                    },
                    Result = _latestPackets.TryGetValue(1, out var p1Resp) && p1Resp.Values.TryGetValue(29, out var p1Resv) ? (bool.TryParse(p1Resv.ToString(), out var p1Resr) ? p1Resr : false) : false
                },
                Product2 = new PostBendingMonitorRow
                {
                    Product = _latestPackets.TryGetValue(1, out var p2Prodp) && p2Prodp.Values.TryGetValue(29, out var p2Prodv) ? p2Prodv.ToString() : "NA",
                    QRCode = _latestPackets.TryGetValue(1, out var p2QRp) && p2QRp.Values.TryGetValue(29, out var p2QRv) ? p2QRv.ToString() : "NA",
                    X = new PostParameterLImitValues
                    {
                        UpperLimit = _latestPackets.TryGetValue(1, out var p2XUp) && p2XUp.Values.TryGetValue(29, out var p2XUv) ? (double.TryParse(p2XUv.ToString(), out var p2XUr) ? p2XUr : 0) : 0,
                        PresentValue = _latestPackets.TryGetValue(1, out var p2XPp) && p2XPp.Values.TryGetValue(29, out var p2XPv) ? (double.TryParse(p2XPv.ToString(), out var p2XPr) ? p2XPr : 0) : 0,
                        LowerLimit = _latestPackets.TryGetValue(1, out var p2XLp) && p2XLp.Values.TryGetValue(29, out var p2XLv) ? (double.TryParse(p2XLv.ToString(), out var p2XLr) ? p2XLr : 0) : 0
                    },
                    Y = new PostParameterLImitValues
                    {
                        UpperLimit = _latestPackets.TryGetValue(1, out var p2YUp) && p2YUp.Values.TryGetValue(29, out var p2YUv) ? (double.TryParse(p2YUv.ToString(), out var p2YUr) ? p2YUr : 0) : 0,
                        PresentValue = _latestPackets.TryGetValue(1, out var p2YPp) && p2YPp.Values.TryGetValue(29, out var p2YPv) ? (double.TryParse(p2YPv.ToString(), out var p2YPr) ? p2YPr : 0) : 0,
                        LowerLimit = _latestPackets.TryGetValue(1, out var p2YLp) && p2YLp.Values.TryGetValue(29, out var p2YLv) ? (double.TryParse(p2YLv.ToString(), out var p2YLr) ? p2YLr : 0) : 0
                    },
                    Z = new PostParameterLImitValues
                    {
                        UpperLimit = _latestPackets.TryGetValue(1, out var p2ZUp) && p2ZUp.Values.TryGetValue(29, out var p2ZUv) ? (double.TryParse(p2ZUv.ToString(), out var p2ZUr) ? p2ZUr : 0) : 0,
                        PresentValue = _latestPackets.TryGetValue(1, out var p2ZPp) && p2ZPp.Values.TryGetValue(29, out var p2ZPv) ? (double.TryParse(p2ZPv.ToString(), out var p2ZPr) ? p2ZPr : 0) : 0,
                        LowerLimit = _latestPackets.TryGetValue(1, out var p2ZLp) && p2ZLp.Values.TryGetValue(29, out var p2ZLv) ? (double.TryParse(p2ZLv.ToString(), out var p2ZLr) ? p2ZLr : 0) : 0
                    },
                    W = new PostParameterLImitValues
                    {
                        UpperLimit = _latestPackets.TryGetValue(1, out var p2WUp) && p2WUp.Values.TryGetValue(29, out var p2WUv) ? (double.TryParse(p2WUv.ToString(), out var p2WUr) ? p2WUr : 0) : 0,
                        PresentValue = _latestPackets.TryGetValue(1, out var p2WPp) && p2WPp.Values.TryGetValue(29, out var p2WPv) ? (double.TryParse(p2WPv.ToString(), out var p2WPr) ? p2WPr : 0) : 0,
                        LowerLimit = _latestPackets.TryGetValue(1, out var p2WLp) && p2WLp.Values.TryGetValue(29, out var p2WLv) ? (double.TryParse(p2WLv.ToString(), out var p2WLr) ? p2WLr : 0) : 0
                    },
                    Result = _latestPackets.TryGetValue(1, out var p2Resp) && p2Resp.Values.TryGetValue(29, out var p2Resv) ? (bool.TryParse(p2Resv.ToString(), out var p2Resr) ? p2Resr : false) : false
                },
                Product3 = new PostBendingMonitorRow
                {
                    Product = _latestPackets.TryGetValue(1, out var p3Prodp) && p3Prodp.Values.TryGetValue(29, out var p3Prodv) ? p3Prodv.ToString() : "NA",
                    QRCode = _latestPackets.TryGetValue(1, out var p3QRp) && p3QRp.Values.TryGetValue(29, out var p3QRv) ? p3QRv.ToString() : "NA",
                    X = new PostParameterLImitValues
                    {
                        UpperLimit = _latestPackets.TryGetValue(1, out var p3XUp) && p3XUp.Values.TryGetValue(29, out var p3XUv) ? (double.TryParse(p3XUv.ToString(), out var p3XUr) ? p3XUr : 0) : 0,
                        PresentValue = _latestPackets.TryGetValue(1, out var p3XPp) && p3XPp.Values.TryGetValue(29, out var p3XPv) ? (double.TryParse(p3XPv.ToString(), out var p3XPr) ? p3XPr : 0) : 0,
                        LowerLimit = _latestPackets.TryGetValue(1, out var p3XLp) && p3XLp.Values.TryGetValue(29, out var p3XLv) ? (double.TryParse(p3XLv.ToString(), out var p3XLr) ? p3XLr : 0) : 0
                    },
                    Y = new PostParameterLImitValues
                    {
                        UpperLimit = _latestPackets.TryGetValue(1, out var p3YUp) && p3YUp.Values.TryGetValue(29, out var p3YUv) ? (double.TryParse(p3YUv.ToString(), out var p3YUr) ? p3YUr : 0) : 0,
                        PresentValue = _latestPackets.TryGetValue(1, out var p3YPp) && p3YPp.Values.TryGetValue(29, out var p3YPv) ? (double.TryParse(p3YPv.ToString(), out var p3YPr) ? p3YPr : 0) : 0,
                        LowerLimit = _latestPackets.TryGetValue(1, out var p3YLp) && p3YLp.Values.TryGetValue(29, out var p3YLv) ? (double.TryParse(p3YLv.ToString(), out var p3YLr) ? p3YLr : 0) : 0
                    },
                    Z = new PostParameterLImitValues
                    {
                        UpperLimit = _latestPackets.TryGetValue(1, out var p3ZUp) && p3ZUp.Values.TryGetValue(29, out var p3ZUv) ? (double.TryParse(p3ZUv.ToString(), out var p3ZUr) ? p3ZUr : 0) : 0,
                        PresentValue = _latestPackets.TryGetValue(1, out var p3ZPp) && p3ZPp.Values.TryGetValue(29, out var p3ZPv) ? (double.TryParse(p3ZPv.ToString(), out var p3ZPr) ? p3ZPr : 0) : 0,
                        LowerLimit = _latestPackets.TryGetValue(1, out var p3ZLp) && p3ZLp.Values.TryGetValue(29, out var p3ZLv) ? (double.TryParse(p3ZLv.ToString(), out var p3ZLr) ? p3ZLr : 0) : 0
                    },
                    W = new PostParameterLImitValues
                    {
                        UpperLimit = _latestPackets.TryGetValue(1, out var p3WUp) && p3WUp.Values.TryGetValue(29, out var p3WUv) ? (double.TryParse(p3WUv.ToString(), out var p3WUr) ? p3WUr : 0) : 0,
                        PresentValue = _latestPackets.TryGetValue(1, out var p3WPp) && p3WPp.Values.TryGetValue(29, out var p3WPv) ? (double.TryParse(p3WPv.ToString(), out var p3WPr) ? p3WPr : 0) : 0,
                        LowerLimit = _latestPackets.TryGetValue(1, out var p3WLp) && p3WLp.Values.TryGetValue(29, out var p3WLv) ? (double.TryParse(p3WLv.ToString(), out var p3WLr) ? p3WLr : 0) : 0
                    },
                    Result = _latestPackets.TryGetValue(1, out var p3Resp) && p3Resp.Values.TryGetValue(29, out var p3Resv) ? (bool.TryParse(p3Resv.ToString(), out var p3Resr) ? p3Resr : false) : false
                },
                Product4 = new PostBendingMonitorRow
                {
                    Product = _latestPackets.TryGetValue(1, out var p4Prodp) && p4Prodp.Values.TryGetValue(29, out var p4Prodv) ? p4Prodv.ToString() : "NA",
                    QRCode = _latestPackets.TryGetValue(1, out var p4QRp) && p4QRp.Values.TryGetValue(29, out var p4QRv) ? p4QRv.ToString() : "NA",
                    X = new PostParameterLImitValues
                    {
                        UpperLimit = _latestPackets.TryGetValue(1, out var p4XUp) && p4XUp.Values.TryGetValue(29, out var p4XUv) ? (double.TryParse(p4XUv.ToString(), out var p4XUr) ? p4XUr : 0) : 0,
                        PresentValue = _latestPackets.TryGetValue(1, out var p4XPp) && p4XPp.Values.TryGetValue(29, out var p4XPv) ? (double.TryParse(p4XPv.ToString(), out var p4XPr) ? p4XPr : 0) : 0,
                        LowerLimit = _latestPackets.TryGetValue(1, out var p4XLp) && p4XLp.Values.TryGetValue(29, out var p4XLv) ? (double.TryParse(p4XLv.ToString(), out var p4XLr) ? p4XLr : 0) : 0
                    },
                    Y = new PostParameterLImitValues
                    {
                        UpperLimit = _latestPackets.TryGetValue(1, out var p4YUp) && p4YUp.Values.TryGetValue(29, out var p4YUv) ? (double.TryParse(p4YUv.ToString(), out var p4YUr) ? p4YUr : 0) : 0,
                        PresentValue = _latestPackets.TryGetValue(1, out var p4YPp) && p4YPp.Values.TryGetValue(29, out var p4YPv) ? (double.TryParse(p4YPv.ToString(), out var p4YPr) ? p4YPr : 0) : 0,
                        LowerLimit = _latestPackets.TryGetValue(1, out var p4YLp) && p4YLp.Values.TryGetValue(29, out var p4YLv) ? (double.TryParse(p4YLv.ToString(), out var p4YLr) ? p4YLr : 0) : 0
                    },
                    Z = new PostParameterLImitValues
                    {
                        UpperLimit = _latestPackets.TryGetValue(1, out var p4ZUp) && p4ZUp.Values.TryGetValue(29, out var p4ZUv) ? (double.TryParse(p4ZUv.ToString(), out var p4ZUr) ? p4ZUr : 0) : 0,
                        PresentValue = _latestPackets.TryGetValue(1, out var p4ZPp) && p4ZPp.Values.TryGetValue(29, out var p4ZPv) ? (double.TryParse(p4ZPv.ToString(), out var p4ZPr) ? p4ZPr : 0) : 0,
                        LowerLimit = _latestPackets.TryGetValue(1, out var p4ZLp) && p4ZLp.Values.TryGetValue(29, out var p4ZLv) ? (double.TryParse(p4ZLv.ToString(), out var p4ZLr) ? p4ZLr : 0) : 0
                    },
                    W = new PostParameterLImitValues
                    {
                        UpperLimit = _latestPackets.TryGetValue(1, out var p4WUp) && p4WUp.Values.TryGetValue(29, out var p4WUv) ? (double.TryParse(p4WUv.ToString(), out var p4WUr) ? p4WUr : 0) : 0,
                        PresentValue = _latestPackets.TryGetValue(1, out var p4WPp) && p4WPp.Values.TryGetValue(29, out var p4WPv) ? (double.TryParse(p4WPv.ToString(), out var p4WPr) ? p4WPr : 0) : 0,
                        LowerLimit = _latestPackets.TryGetValue(1, out var p4WLp) && p4WLp.Values.TryGetValue(29, out var p4WLv) ? (double.TryParse(p4WLv.ToString(), out var p4WLr) ? p4WLr : 0) : 0
                    },
                    Result = _latestPackets.TryGetValue(1, out var p4Resp) && p4Resp.Values.TryGetValue(29, out var p4Resv) ? (bool.TryParse(p4Resv.ToString(), out var p4Resr) ? p4Resr : false) : false
                }
            };

            return Task.FromResult(new ResponsePackage
            {
                ResponseId = request.RequestId,
                Parameters = new Dictionary<int, object>()
                {
                    { 0, item }
                }
            });
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