using IPCSoftware.Core.Interfaces;
using IPCSoftware.Core.Interfaces.AppLoggerInterface;
using IPCSoftware.Devices.PLC;
using IPCSoftware.Devices.UI;
using IPCSoftware.Engine;
using IPCSoftware.Shared.Models;
using IPCSoftware.Shared.Models.Bending;
using IPCSoftware.Shared.Models.Bending.IPCSoftware.App.Bending.Models;
using IPCSoftware.Shared.Models.ConfigModels;
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
            SystemMonitorServiceBending systemMonitor,
            UiListener ui,
            AlarmService alarmService,
            CCDTriggerServiceBending ccdTrigger,
            BendingProcessService bendingProcess,
            IAppLogger logger)
            : base(manager, algo, oee, shiftReset, systemMonitor, ui, alarmService, ccdTrigger, bendingProcess, logger)
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
                return await GetActiveBatches(request);
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
            // PostBendingMonitor
            // ----------------------------------------------------------------

            if (request.RequestId == 29)
            {
                return await PostBendingMonitor(request);
            }

            return await base.HandleUiRequest(request);
        }

        private Task<ResponsePackage> GetActiveBatches(RequestPackage request)
        {
            ResponsePackage response = new ResponsePackage
            {
                ResponseId = request.RequestId,
                Parameters = new Dictionary<int, object>()
            };
            var batches = processLogicEngine.GetActiveBatches();
            int i = 0;
            foreach (var batch in batches)
            {
                Debug.WriteLine($"[Bending] Active Batch → BatchNumber={batch.BatchNumber}, Stage={batch.Stage}");
                if (batch != null)
                {
                    var item = new DashboardInspectionModel
                    {
                        BatchNo = batch.BatchNumber,
                        StationIndex = batch.Stage,
                        LineItem1 = new DashboardInspectionLineModel
                        {
                            QRCode = batch.QrCode1,
                            HeaterTemp_Bend1 = batch.Bending1Temperatures[0],
                            HeaterTemp_Bend2 = batch.Bending2Temperatures[0],
                            HeaterTemp_Bend3 = batch.Bending3Temperatures[0],
                            Load_Bend1 = batch.Bending1Loads[0],
                            Load_Bend2 = batch.Bending2Loads[0],
                            Load_Bend3 = batch.Bending3Loads[0],
                            XValue = batch.Bending3X[0],
                            YValue = batch.Bending3Y[0],
                            ZValue = batch.Bending3Z[0],
                            WValue = batch.Bending3W[0],
                            Result1 = batch.InspectionResults[0],
                        },
                        LineItem2 = new DashboardInspectionLineModel
                        {
                            QRCode = batch.QrCode2,
                            HeaterTemp_Bend1 = batch.Bending1Temperatures[1],
                            HeaterTemp_Bend2 = batch.Bending2Temperatures[1],
                            HeaterTemp_Bend3 = batch.Bending3Temperatures[1],
                            Load_Bend1 = batch.Bending1Loads[1],
                            Load_Bend2 = batch.Bending2Loads[1],
                            Load_Bend3 = batch.Bending3Loads[1],
                            XValue = batch.Bending3X[1],
                            YValue = batch.Bending3Y[1],
                            ZValue = batch.Bending3Z[1],
                            WValue = batch.Bending3W[1],
                            Result1 = batch.InspectionResults[1],
                        },
                        LineItem3 = new DashboardInspectionLineModel
                        {
                            QRCode = batch.QrCode3,
                            HeaterTemp_Bend1 = batch.Bending1Temperatures[2],
                            HeaterTemp_Bend2 = batch.Bending2Temperatures[2],
                            HeaterTemp_Bend3 = batch.Bending3Temperatures[2],
                            Load_Bend1 = batch.Bending1Loads[2],
                            Load_Bend2 = batch.Bending2Loads[2],
                            Load_Bend3 = batch.Bending3Loads[2],
                            XValue = batch.Bending3X[2],
                            YValue = batch.Bending3Y[2],
                            ZValue = batch.Bending3Z[2],
                            WValue = batch.Bending3W[2],
                            Result1 = batch.InspectionResults[2],
                        },
                        LineItem4 = new DashboardInspectionLineModel
                        {
                            QRCode = batch.QrCode4,
                            HeaterTemp_Bend1 = batch.Bending1Temperatures[3],
                            HeaterTemp_Bend2 = batch.Bending2Temperatures[3],
                            HeaterTemp_Bend3 = batch.Bending3Temperatures[3],
                            Load_Bend1 = batch.Bending1Loads[3],
                            Load_Bend2 = batch.Bending2Loads[3],
                            Load_Bend3 = batch.Bending3Loads[3],
                            XValue = batch.Bending3X[3],
                            YValue = batch.Bending3Y[3],
                            ZValue = batch.Bending3Z[3],
                            WValue = batch.Bending3W[3],
                            Result1 = batch.InspectionResults[3],
                        },
                    };
                    response.Parameters.Add(i, item);

                }
                i++;

            }
            return Task.FromResult(response);
        }

        private Task<ResponsePackage> DashboardInspectionModelBatch1(RequestPackage request)
        {
  
            // Build a LineItem using the correct ConstantValues tag IDs (from appsettings Dashboard2 section)
            DashboardInspectionLineModel BuildLineItem1() => new DashboardInspectionLineModel
            {
                QRCode = GetString(ConstantValues.L1_QRCode),    //GetString(ConstantValues.L1_QRCode != null ? int.TryParse(ConstantValues.L1_QRCode, out var qrId) ? qrId : 1187 : 1187),
                HeaterTemp_Bend1 = GetFloat(ConstantValues.L1_HeaterTemp_Bend1),  
                HeaterTemp_Bend2 = GetFloat(ConstantValues.L2_HeaterTemp_Bend2),  
                HeaterTemp_Bend3 = GetFloat(ConstantValues.L3_HeaterTemp_Bend3),  
                Load_Bend1 = GetFloat(ConstantValues.L1_Load_Bend1),          
                Load_Bend2 = GetFloat(ConstantValues.L1_Load_Bend2),          
                Load_Bend3 = GetFloat(ConstantValues.L1_Load_Bend3),          
                XValue = GetFloat(ConstantValues.L1_XValue),            
                YValue = GetFloat(ConstantValues.L1_YValue),            
                ZValue = GetFloat(ConstantValues.L1_ZValue),            
                WValue = GetFloat(ConstantValues.L1_WValue),            
                Result1 = GetBool(ConstantValues.L1_Result),            
            }; 

            DashboardInspectionLineModel BuildLineItem2() => new DashboardInspectionLineModel
            {
                QRCode = GetString(ConstantValues.L2_QRCode),                //GetString(ConstantValues.L2_QRCode != null ? int.TryParse(ConstantValues.L2_QRCode, out var qrId) ? qrId : 1187 : 1187),
                HeaterTemp_Bend1 = GetFloat(ConstantValues.L2_HeaterTemp_Bend1),  
                HeaterTemp_Bend2 = GetFloat(ConstantValues.L2_HeaterTemp_Bend2),  
                HeaterTemp_Bend3 = GetFloat(ConstantValues.L2_HeaterTemp_Bend3),  
                Load_Bend1 = GetFloat(ConstantValues.L2_Load_Bend1),          
                Load_Bend2 = GetFloat(ConstantValues.L2_Load_Bend2),          
                Load_Bend3 = GetFloat(ConstantValues.L2_Load_Bend3),          
                XValue = GetFloat(ConstantValues.L2_XValue),            
                YValue = GetFloat(ConstantValues.L2_YValue),            
                ZValue = GetFloat(ConstantValues.L2_ZValue),            
                WValue = GetFloat(ConstantValues.L2_WValue),            
                Result1 = GetBool(ConstantValues.L2_Result),            
            };

            DashboardInspectionLineModel BuildLineItem3() => new DashboardInspectionLineModel
            {
                QRCode = GetString(ConstantValues.L3_QRCode),                    //GetString(ConstantValues.L3_QRCode != null ? int.TryParse(ConstantValues.L3_QRCode, out var qrId) ? qrId : 1187 : 1187),
                HeaterTemp_Bend1 = GetFloat(ConstantValues.L3_HeaterTemp_Bend1),  
                HeaterTemp_Bend2 = GetFloat(ConstantValues.L3_HeaterTemp_Bend2),  
                HeaterTemp_Bend3 = GetFloat(ConstantValues.L3_HeaterTemp_Bend3),  
                Load_Bend1 = GetFloat(ConstantValues.L3_Load_Bend1),          
                Load_Bend2 = GetFloat(ConstantValues.L3_Load_Bend2),          
                Load_Bend3 = GetFloat(ConstantValues.L3_Load_Bend3),          
                XValue = GetFloat(ConstantValues.L3_XValue),            
                YValue = GetFloat(ConstantValues.L3_YValue),            
                ZValue = GetFloat(ConstantValues.L3_ZValue),            
                WValue = GetFloat(ConstantValues.L3_WValue),            
                Result1 = GetBool(ConstantValues.L3_Result),            
            };

            DashboardInspectionLineModel BuildLineItem4() => new DashboardInspectionLineModel
            {
                QRCode = GetString(ConstantValues.L4_QRCode),             //GetString(ConstantValues.L4_QRCode != null ? int.TryParse(ConstantValues.L4_QRCode, out var qrId) ? qrId : 1187 : 1187),
                HeaterTemp_Bend1 = GetFloat(ConstantValues.L4_HeaterTemp_Bend1),  
                HeaterTemp_Bend2 = GetFloat(ConstantValues.L4_HeaterTemp_Bend2),  
                HeaterTemp_Bend3 = GetFloat(ConstantValues.L4_HeaterTemp_Bend3),  
                Load_Bend1 = GetFloat(ConstantValues.L4_Load_Bend1),          
                Load_Bend2 = GetFloat(ConstantValues.L4_Load_Bend2),          
                Load_Bend3 = GetFloat(ConstantValues.L4_Load_Bend3),          
                XValue = GetFloat(ConstantValues.L4_XValue),           
                YValue = GetFloat(ConstantValues.L4_YValue),           
                ZValue = GetFloat(ConstantValues.L4_ZValue),           
                WValue = GetFloat(ConstantValues.L4_WValue),           
                Result1 = GetBool(ConstantValues.L4_Result),           
            };


            DashboardInspectionModel item = new DashboardInspectionModel
            {
                BatchNo =   1,/* GetString(ConstantValues.QRCode1 != null ? int.TryParse(ConstantValues.QRCode1, out var batchQrId) ? batchQrId : 1187 : 1187),*/
                StationIndex=1,
                LineItem1 = BuildLineItem1(),
                LineItem2 = BuildLineItem2(),
                LineItem3 = BuildLineItem3(),
                LineItem4 = BuildLineItem4(),

            };


            // ResponseId must match the RequestId (11) so the ViewModel's TryGetValue(11) succeeds
            return Task.FromResult(new ResponsePackage
            {
                ResponseId = request.RequestId,
                Parameters = new Dictionary<int, object>()
                {
                    { request.RequestId, item }  // key = 11, matching ViewModel's TryGetValue(11)
                }
            });
        }

        private Task<ResponsePackage> DashboardInspectionModelBatch2(RequestPackage request)
        {           

            // Build a LineItem using the correct ConstantValues tag IDs (from appsettings Dashboard2 section)
            DashboardInspectionLineModel BuildLineItem1() => new DashboardInspectionLineModel
            {
                QRCode = GetString(ConstantValues.L1_QRCode),//GetString(ConstantValues.L1_QRCode != null ? int.TryParse(ConstantValues.L1_QRCode, out var qrId) ? qrId : 1187 : 1187),
                HeaterTemp_Bend1 = GetFloat(ConstantValues.L1_HeaterTemp_Bend1)+10.0f,
                HeaterTemp_Bend2 = GetFloat(ConstantValues.L2_HeaterTemp_Bend2),
                HeaterTemp_Bend3 = GetFloat(ConstantValues.L3_HeaterTemp_Bend3),
                Load_Bend1 = GetFloat(ConstantValues.L1_Load_Bend1),
                Load_Bend2 = GetFloat(ConstantValues.L1_Load_Bend2),
                Load_Bend3 = GetFloat(ConstantValues.L1_Load_Bend3),
                XValue = GetFloat(ConstantValues.L1_XValue),
                YValue = GetFloat(ConstantValues.L1_YValue),
                ZValue = GetFloat(ConstantValues.L1_ZValue),
                WValue = GetFloat(ConstantValues.L1_WValue),
                Result1 = GetBool(ConstantValues.L1_Result),
            };

            DashboardInspectionLineModel BuildLineItem2() => new DashboardInspectionLineModel
            {
                QRCode = GetString(ConstantValues.L2_QRCode),//GetString(ConstantValues.L2_QRCode != null ? int.TryParse(ConstantValues.L2_QRCode, out var qrId) ? qrId : 1187 : 1187),
                HeaterTemp_Bend1 = GetFloat(ConstantValues.L2_HeaterTemp_Bend1),
                HeaterTemp_Bend2 = GetFloat(ConstantValues.L2_HeaterTemp_Bend2),
                HeaterTemp_Bend3 = GetFloat(ConstantValues.L2_HeaterTemp_Bend3),
                Load_Bend1 = GetFloat(ConstantValues.L2_Load_Bend1),
                Load_Bend2 = GetFloat(ConstantValues.L2_Load_Bend2),
                Load_Bend3 = GetFloat(ConstantValues.L2_Load_Bend3),
                XValue = GetFloat(ConstantValues.L2_XValue),
                YValue = GetFloat(ConstantValues.L2_YValue),
                ZValue = GetFloat(ConstantValues.L2_ZValue),
                WValue = GetFloat(ConstantValues.L2_WValue),
                Result1 = GetBool(ConstantValues.L2_Result),
            };

            DashboardInspectionLineModel BuildLineItem3() => new DashboardInspectionLineModel
            {
                QRCode = GetString(ConstantValues.L3_QRCode),//GetString(ConstantValues.L3_QRCode != null ? int.TryParse(ConstantValues.L3_QRCode, out var qrId) ? qrId : 1187 : 1187),
                HeaterTemp_Bend1 = GetFloat(ConstantValues.L3_HeaterTemp_Bend1),
                HeaterTemp_Bend2 = GetFloat(ConstantValues.L3_HeaterTemp_Bend2),
                HeaterTemp_Bend3 = GetFloat(ConstantValues.L3_HeaterTemp_Bend3),
                Load_Bend1 = GetFloat(ConstantValues.L3_Load_Bend1),
                Load_Bend2 = GetFloat(ConstantValues.L3_Load_Bend2),
                Load_Bend3 = GetFloat(ConstantValues.L3_Load_Bend3),
                XValue = GetFloat(ConstantValues.L3_XValue),
                YValue = GetFloat(ConstantValues.L3_YValue),
                ZValue = GetFloat(ConstantValues.L3_ZValue),
                WValue = GetFloat(ConstantValues.L3_WValue),
                Result1 = GetBool(ConstantValues.L3_Result),
            };

            DashboardInspectionLineModel BuildLineItem4() => new DashboardInspectionLineModel
            {
                QRCode = GetString(ConstantValues.L4_QRCode),//GetString(ConstantValues.L4_QRCode != null ? int.TryParse(ConstantValues.L4_QRCode, out var qrId) ? qrId : 1187 : 1187),
                HeaterTemp_Bend1 = GetFloat(ConstantValues.L4_HeaterTemp_Bend1),
                HeaterTemp_Bend2 = GetFloat(ConstantValues.L4_HeaterTemp_Bend2),
                HeaterTemp_Bend3 = GetFloat(ConstantValues.L4_HeaterTemp_Bend3),
                Load_Bend1 = GetFloat(ConstantValues.L4_Load_Bend1),
                Load_Bend2 = GetFloat(ConstantValues.L4_Load_Bend2),
                Load_Bend3 = GetFloat(ConstantValues.L4_Load_Bend3),
                XValue = GetFloat(ConstantValues.L4_XValue),
                YValue = GetFloat(ConstantValues.L4_YValue),
                ZValue = GetFloat(ConstantValues.L4_ZValue),
                WValue = GetFloat(ConstantValues.L4_WValue),
                Result1 = GetBool(ConstantValues.L4_Result),
            };


            DashboardInspectionModel item = new DashboardInspectionModel
            {
                BatchNo = 2,/* GetString(ConstantValues.QRCode1 != null ? int.TryParse(ConstantValues.QRCode1, out var batchQrId) ? batchQrId : 1187 : 1187),*/
                LineItem1 = BuildLineItem1(),
                LineItem2 = BuildLineItem2(),
                LineItem3 = BuildLineItem3(),
                LineItem4 = BuildLineItem4(),

            };


            // ResponseId must match the RequestId (11) so the ViewModel's TryGetValue(11) succeeds
            return Task.FromResult(new ResponsePackage
            {
                ResponseId = request.RequestId,
                Parameters = new Dictionary<int, object>()
                {
                    { request.RequestId, item }  // key = 11, matching ViewModel's TryGetValue(11)
                }
            });
        }

        private Task<ResponsePackage> DashboardInspectionModelBatch3(RequestPackage request)
        {

            DashboardInspectionModel BuiltedBatch = new DashboardInspectionModel
            {
                BatchNo = 3,
                LineItem1 = new DashboardInspectionLineModel { QRCode = GetString(ConstantValues.L1_QRCode) },
                LineItem2 = new DashboardInspectionLineModel { QRCode = GetString(ConstantValues.L2_QRCode) },
                LineItem3 = new DashboardInspectionLineModel { QRCode = GetString(ConstantValues.L3_QRCode) },
                LineItem4 = new DashboardInspectionLineModel { QRCode = GetString(ConstantValues.L4_QRCode) }
            };



 

            return Task.FromResult(new ResponsePackage
            {
                ResponseId = request.RequestId,
                Parameters = new Dictionary<int, object>()
                {
                    { request.RequestId, BuiltedBatch }
                }
            });
        }

        private Task<ResponsePackage> DashboardInspectionModelBatch4(RequestPackage request)
        {
           

            DashboardInspectionModel BuiltedBatch = new DashboardInspectionModel
            {
                BatchNo = 4,
                LineItem1 = new DashboardInspectionLineModel { QRCode = GetString(ConstantValues.L1_QRCode) },
                LineItem2 = new DashboardInspectionLineModel { QRCode = GetString(ConstantValues.L2_QRCode) },
                LineItem3 = new DashboardInspectionLineModel { QRCode = GetString(ConstantValues.L3_QRCode) },
                LineItem4 = new DashboardInspectionLineModel { QRCode = GetString(ConstantValues.L4_QRCode) }
            };


            return Task.FromResult(new ResponsePackage
            {
                ResponseId = request.RequestId,
                Parameters = new Dictionary<int, object>()
                {
                    { request.RequestId, BuiltedBatch }
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
                        Clamp =  latestValueNew.TryGetValue(ConstantValues.Bending1Flex1Clamp, out var b1f1Cv) ? (bool.TryParse(b1f1Cv.ToString(), out var b1f1Cr) ? b1f1Cr : false) : false,
                        Punch = latestValueNew.TryGetValue(ConstantValues.Bending1Flex1Punch, out var b1f1Pv) ? (bool.TryParse(b1f1Pv.ToString(), out var b1f1Pr) ? b1f1Pr : false) : false,
                        Heat = latestValueNew.TryGetValue(ConstantValues.Bending1Flex1Heat, out var b1f1Hv) ? (bool.TryParse(b1f1Hv.ToString(), out var b1f1Hr) ? b1f1Hr : false) : false,
                        Temperature = latestValueNew.TryGetValue(ConstantValues.L1Bending1Temperature, out var b1f1Tv) ? (float.TryParse(b1f1Tv.ToString(), out var b1f1Tr) ? b1f1Tr : 0f) : 0f,
                        Force = latestValueNew.TryGetValue(ConstantValues.L1Bending1Force, out var b1f1Fv) ? (float.TryParse(b1f1Fv.ToString(), out var b1f1Fr) ? b1f1Fr : 0f) : 0f
                    },
                    Flex2 = new BendingIndicator
                    {
                        Clamp = latestValueNew.TryGetValue(ConstantValues.Bending1Flex2Clamp, out var b1f2Cv) ? (bool.TryParse(b1f2Cv.ToString(), out var b1f2Cr) ? b1f2Cr : false) : false,
                        Punch = latestValueNew.TryGetValue(ConstantValues.Bending1Flex2Punch, out var b1f2Pv) ? (bool.TryParse(b1f2Pv.ToString(), out var b1f2Pr) ? b1f2Pr : false) : false,
                        Heat = latestValueNew.TryGetValue(ConstantValues.Bending1Flex2Heat, out var b1f2Hv) ? (bool.TryParse(b1f2Hv.ToString(), out var b1f2Hr) ? b1f2Hr : false) : false,
                        Temperature = latestValueNew.TryGetValue(ConstantValues.L2Bending1Temperature, out var b1f2Tv) ? (float.TryParse(b1f2Tv.ToString(), out var b1f2Tr) ? b1f2Tr : 0f) : 0f,
                        Force = latestValueNew.TryGetValue(ConstantValues.L2Bending1Force, out var b1f2Fv) ? (float.TryParse(b1f2Fv.ToString(), out var b1f2Fr) ? b1f2Fr : 0f) : 0f
                    },
                    Flex3 = new BendingIndicator
                    {
                        Clamp = latestValueNew.TryGetValue(ConstantValues.Bending1Flex3Clamp, out var b1f3Cv) ? (bool.TryParse(b1f3Cv.ToString(), out var b1f3Cr) ? b1f3Cr : false) : false,
                        Punch = latestValueNew.TryGetValue(ConstantValues.Bending1Flex3Punch, out var b1f3Pv) ? (bool.TryParse(b1f3Pv.ToString(), out var b1f3Pr) ? b1f3Pr : false) : false,
                        Heat = latestValueNew.TryGetValue(ConstantValues.Bending1Flex3Heat, out var b1f3Hv) ? (bool.TryParse(b1f3Hv.ToString(), out var b1f3Hr) ? b1f3Hr : false) : false,
                        Temperature = latestValueNew.TryGetValue(ConstantValues.L3Bending1Temperature, out var b1f3Tv) ? (float.TryParse(b1f3Tv.ToString(), out var b1f3Tr) ? b1f3Tr : 0f) : 0f,
                        Force = latestValueNew.TryGetValue(ConstantValues.L3Bending1Force, out var b1f3Fv) ? (float.TryParse(b1f3Fv.ToString(), out var b1f3Fr) ? b1f3Fr : 0f) : 0f
                    },
                    Flex4 = new BendingIndicator
                    {
                        Clamp = latestValueNew.TryGetValue(ConstantValues.Bending1Flex4Clamp, out var b1f4Cv) ? (bool.TryParse(b1f4Cv.ToString(), out var b1f4Cr) ? b1f4Cr : false) : false,
                        Punch = latestValueNew.TryGetValue(ConstantValues.Bending1Flex4Clamp, out var b1f4Pv) ? (bool.TryParse(b1f4Pv.ToString(), out var b1f4Pr) ? b1f4Pr : false) : false,
                        Heat = latestValueNew.TryGetValue(ConstantValues.Bending1Flex4Heat, out var b1f4Hv) ? (bool.TryParse(b1f4Hv.ToString(), out var b1f4Hr) ? b1f4Hr : false) : false,
                        Temperature = latestValueNew.TryGetValue(ConstantValues.L4Bending1Temperature, out var b1f4Tv) ? (float.TryParse(b1f4Tv.ToString(), out var b1f4Tr) ? b1f4Tr : 0f) : 0f,
                        Force = latestValueNew.TryGetValue(ConstantValues.L4Bending1Force, out var b1f4Fv) ? (float.TryParse(b1f4Fv.ToString(), out var b1f4Fr) ? b1f4Fr : 0f) : 0f
                    }
                },
                Bending2 = new FlexBendingIndicator
                {
                    Flex1 = new BendingIndicator
                    {
                        Clamp = latestValueNew.TryGetValue(ConstantValues.Bending2Flex1Clamp, out var b2f1Cv) ? (bool.TryParse(b2f1Cv.ToString(), out var b2f1Cr) ? b2f1Cr : false) : false,
                        Punch = latestValueNew.TryGetValue(ConstantValues.Bending2Flex1Punch, out var b2f1Pv) ? (bool.TryParse(b2f1Pv.ToString(), out var b2f1Pr) ? b2f1Pr : false) : false,
                        Heat = latestValueNew.TryGetValue(ConstantValues.Bending2Flex1Heat, out var b2f1Hv) ? (bool.TryParse(b2f1Hv.ToString(), out var b2f1Hr) ? b2f1Hr : false) : false,
                        Temperature = latestValueNew.TryGetValue(ConstantValues.L1Bending2Temperature, out var b2f1Tv) ? (float.TryParse(b2f1Tv.ToString(), out var b2f1Tr) ? b2f1Tr : 0f) : 0f,
                        Force = latestValueNew.TryGetValue(ConstantValues.L1Bending2Force, out var b2f1Fv) ? (float.TryParse(b2f1Fv.ToString(), out var b2f1Fr) ? b2f1Fr : 0f) : 0f
                    },
                    Flex2 = new BendingIndicator
                    {
                        Clamp = latestValueNew.TryGetValue(ConstantValues.Bending2Flex1Clamp, out var b2f2Cv) ? (bool.TryParse(b2f2Cv.ToString(), out var b2f2Cr) ? b2f2Cr : false) : false,
                        Punch = latestValueNew.TryGetValue(ConstantValues.Bending2Flex1Punch, out var b2f2Pp) ? (bool.TryParse(b2f1Pv.ToString(), out var b2f2Pr) ? b2f2Pr : false) : false,
                        Heat = latestValueNew.TryGetValue(ConstantValues.Bending2Flex1Heat, out var b2f2Hv) ? (bool.TryParse(b2f2Hv.ToString(), out var b2f2Hr) ? b2f2Hr : false) : false,
                        Temperature = latestValueNew.TryGetValue(ConstantValues.L2Bending2Temperature, out var b2f2Tv) ? (float.TryParse(b2f2Tv.ToString(), out var b2f2Tr) ? b2f2Tr : 0f) : 0f,
                        Force = latestValueNew.TryGetValue(ConstantValues.L2Bending2Force, out var b2f2Fv) ? (float.TryParse(b2f2Fv.ToString(), out var b2f2Fr) ? b2f2Fr : 0f) : 0f
                    },
                    Flex3 = new BendingIndicator
                    {
                        Clamp = latestValueNew.TryGetValue(ConstantValues.Bending2Flex3Clamp, out var b2f3Cv) ? (bool.TryParse(b2f3Cv.ToString(), out var b2f3Cr) ? b2f3Cr : false) : false,
                        Punch = latestValueNew.TryGetValue(ConstantValues.Bending2Flex3Punch, out var b2f3Pv) ? (bool.TryParse(b2f3Pv.ToString(), out var b2f3Pr) ? b2f3Pr : false) : false,
                        Heat = latestValueNew.TryGetValue(ConstantValues.Bending2Flex3Heat, out var b2f3Hv) ? (bool.TryParse(b2f3Hv.ToString(), out var b2f3Hr) ? b2f3Hr : false) : false,
                        Temperature = latestValueNew.TryGetValue(ConstantValues.L3Bending2Temperature, out var b2f3Tv) ? (float.TryParse(b2f3Tv.ToString(), out var b2f3Tr) ? b2f3Tr : 0f) : 0f,
                        Force = latestValueNew.TryGetValue(ConstantValues.L3Bending2Force, out var b2f3Fv) ? (float.TryParse(b2f3Fv.ToString(), out var b2f3Fr) ? b2f3Fr : 0f) : 0f
                    },
                    Flex4 = new BendingIndicator
                    {
                        Clamp = latestValueNew.TryGetValue(ConstantValues.Bending2Flex4Clamp, out var b2f4Cv) ? (bool.TryParse(b2f4Cv.ToString(), out var b2f4Cr) ? b2f4Cr : false) : false,
                        Punch = latestValueNew.TryGetValue(ConstantValues.Bending2Flex4Punch, out var b2f4Pv) ? (bool.TryParse(b2f4Pv.ToString(), out var b2f4Pr) ? b2f4Pr : false) : false,
                        Heat = latestValueNew.TryGetValue(ConstantValues.Bending2Flex4Heat, out var b2f4Hv) ? (bool.TryParse(b2f4Hv.ToString(), out var b2f4Hr) ? b2f4Hr : false) : false,
                        Temperature = latestValueNew.TryGetValue(ConstantValues.L4Bending2Temperature, out var b2f4Tv) ? (float.TryParse(b2f4Tv.ToString(), out var b2f4Tr) ? b2f4Tr : 0f) : 0f,
                        Force = latestValueNew.TryGetValue(ConstantValues.L4Bending2Force, out var b2f4Fv) ? (float.TryParse(b2f4Fv.ToString(), out var b2f4Fr) ? b2f4Fr : 0f) : 0f
                    }
                },
                Bending3 = new FlexBendingIndicator
                {
                    Flex1 = new BendingIndicator
                    {
                        Clamp = latestValueNew.TryGetValue(ConstantValues.Bending3Flex1Clamp, out var b3f1Cv) ? (bool.TryParse(b3f1Cv.ToString(), out var b3f1Cr) ? b3f1Cr : false) : false,
                        Punch = latestValueNew.TryGetValue(ConstantValues.Bending3Flex1Punch, out var b3f1Pv) ? (bool.TryParse(b3f1Pv.ToString(), out var b3f1Pr) ? b3f1Pr : false) : false,
                        Heat = latestValueNew.TryGetValue(ConstantValues.Bending3Flex1Heat, out var b3f1Hv) ? (bool.TryParse(b3f1Hv.ToString(), out var b3f1Hr) ? b3f1Hr : false) : false,
                        Temperature = latestValueNew.TryGetValue(ConstantValues.L1Bending3Temperature, out var b3f1Tv) ? (float.TryParse(b3f1Tv.ToString(), out var b3f1Tr) ? b3f1Tr : 0f) : 0f,
                        Force = latestValueNew.TryGetValue(ConstantValues.L1Bending3Force, out var b3f1Fv) ? (float.TryParse(b3f1Fv.ToString(), out var b3f1Fr) ? b3f1Fr : 0f) : 0f
                    },
                    Flex2 = new BendingIndicator
                    {
                        Clamp = latestValueNew.TryGetValue(ConstantValues.Bending3Flex2Clamp, out var b3f2Cv) ? (bool.TryParse(b3f2Cv.ToString(), out var b3f2Cr) ? b3f2Cr : false) : false,
                        Punch = latestValueNew.TryGetValue(ConstantValues.Bending3Flex2Punch, out var b3f2Pv) ? (bool.TryParse(b3f2Pv.ToString(), out var b3f2Pr) ? b3f2Pr : false) : false,
                        Heat = latestValueNew.TryGetValue(ConstantValues.Bending3Flex2Heat, out var b3f2Hv) ? (bool.TryParse(b3f2Hv.ToString(), out var b3f2Hr) ? b3f2Hr : false) : false,
                        Temperature = latestValueNew.TryGetValue(ConstantValues.L2Bending3Temperature, out var b3f2Tv) ? (float.TryParse(b3f2Tv.ToString(), out var b3f2Tr) ? b3f2Tr : 0f) : 0f,
                        Force = latestValueNew.TryGetValue(ConstantValues.L2Bending3Force, out var b3f2Fv) ? (float.TryParse(b3f2Fv.ToString(), out var b3f2Fr) ? b3f2Fr : 0f) : 0f
                    },
                    Flex3 = new BendingIndicator
                    {
                        Clamp = latestValueNew.TryGetValue(ConstantValues.Bending3Flex3Clamp, out var b3f3Cv) ? (bool.TryParse(b3f3Cv.ToString(), out var b3f3Cr) ? b3f3Cr : false) : false,
                        Punch = latestValueNew.TryGetValue(ConstantValues.Bending3Flex3Punch, out var b3f3Pv) ? (bool.TryParse(b3f3Pv.ToString(), out var b3f3Pr) ? b3f3Pr : false) : false,
                        Heat = latestValueNew.TryGetValue(ConstantValues.Bending3Flex3Heat, out var b3f3Hv) ? (bool.TryParse(b3f3Hv.ToString(), out var b3f3Hr) ? b3f3Hr : false) : false,
                        Temperature = latestValueNew.TryGetValue(ConstantValues.L3Bending3Temperature, out var b3f3Tv) ? (float.TryParse(b3f3Tv.ToString(), out var b3f3Tr) ? b3f3Tr : 0f) : 0f,
                        Force = latestValueNew.TryGetValue(ConstantValues.L3Bending3Force, out var b3f3Fv) ? (float.TryParse(b3f3Fv.ToString(), out var b3f3Fr) ? b3f3Fr : 0f) : 0f
                    },

                    Flex4 = new BendingIndicator
                    {
                        Clamp = latestValueNew.TryGetValue(ConstantValues.Bending3Flex4Clamp, out var b3f4Cv) ? (bool.TryParse(b3f4Cv.ToString(), out var b3f4Cr) ? b3f4Cr : false) : false,
                        Punch = latestValueNew.TryGetValue(ConstantValues.Bending3Flex4Punch, out var b3f4Pv) ? (bool.TryParse(b3f4Pv.ToString(), out var b3f4Pr) ? b3f4Pr : false) : false,
                        Heat = latestValueNew.TryGetValue(ConstantValues.Bending3Flex4Heat, out var b3f4Hv) ? (bool.TryParse(b3f4Hv.ToString(), out var b3f4Hr) ? b3f4Hr : false) : false,
                        Temperature = latestValueNew.TryGetValue(ConstantValues.L4Bending3Temperature, out var b3f4Tv) ? (float.TryParse(b3f4Tv.ToString(), out var b3f4Tr) ? b3f4Tr : 0f) : 0f,
                        Force = latestValueNew.TryGetValue(ConstantValues.L4Bending3Force, out var b3f4Fv) ? (float.TryParse(b3f4Fv.ToString(), out var b3f4Fr) ? b3f4Fr : 0f) : 0f
                    }


                },
                Tearing = new FlexBendingIndicator
                {
                    Flex1 = new BendingIndicator { Tearing = latestValueNew.TryGetValue(ConstantValues.TearingFlex1, out var tf1p) ? (bool.TryParse(tf1p.ToString(), out var tf1r) ? tf1r : false) : false },
                    Flex2 = new BendingIndicator { Tearing = latestValueNew.TryGetValue(ConstantValues.TearingFlex2, out var tf2v) ? (bool.TryParse(tf2v.ToString(), out var tf2r) ? tf2r : false) : false },
                    Flex3 = new BendingIndicator { Tearing = latestValueNew.TryGetValue(ConstantValues.TearingFlex3, out var tf3v) ? (bool.TryParse(tf3v.ToString(), out var tf3r) ? tf3r : false) : false },
                    Flex4 = new BendingIndicator { Tearing = latestValueNew.TryGetValue(ConstantValues.TearingFlex4, out var tf4v) ? (bool.TryParse(tf4v.ToString(), out var tf4r) ? tf4r : false) : false }
                },
                Flipping = new FlexBendingIndicator
                {
                    Flex1 = new BendingIndicator { Flipping = latestValueNew.TryGetValue(ConstantValues.FlippingFlex1, out var ff1v) ? (bool.TryParse(ff1v.ToString(), out var ff1r) ? ff1r : false) : false },
                    Flex2 = new BendingIndicator { Flipping = latestValueNew.TryGetValue(ConstantValues.FlippingFlex2, out var ff2v) ? (bool.TryParse(ff2v.ToString(), out var ff2r) ? ff2r : false) : false },
                    Flex3 = new BendingIndicator { Flipping = latestValueNew.TryGetValue(ConstantValues.FlippingFlex3, out var ff3v) ? (bool.TryParse(ff3v.ToString(), out var ff3r) ? ff3r : false) : false },
                    Flex4 = new BendingIndicator { Flipping = latestValueNew.TryGetValue(ConstantValues.FlippingFlex4, out var ff4v) ? (bool.TryParse(ff4v.ToString(), out var ff4r) ? ff4r : false) : false }
                }
            };

            return Task.FromResult(new ResponsePackage
            {
                ResponseId = request.RequestId,
                Parameters = new Dictionary<int, object>()
                {
                    { request.RequestId, item }
                }
            });


        }

        private Task<ResponsePackage> TurnTable1DataPointModel(RequestPackage request)
        {
            TurnTable1DataPointModel item = new TurnTable1DataPointModel
            {
                Position1 = new TurnTablePositionItemsModel
                {
                    Flex1 = latestValueNew.TryGetValue(14, out var t1p1f1v) ? (bool.TryParse(t1p1f1v.ToString(), out var t1p1f1r) ? t1p1f1r : false) : false,
                    Flex2 = latestValueNew.TryGetValue(14, out var t1p1f2v) ? (bool.TryParse(t1p1f2v.ToString(), out var t1p1f2r) ? t1p1f2r : false) : false,
                    Flex3 = latestValueNew.TryGetValue(14, out var t1p1f3v) ? (bool.TryParse(t1p1f3v.ToString(), out var t1p1f3r) ? t1p1f3r : false) : false,
                    Flex4 = latestValueNew.TryGetValue(14, out var t1p1f4v) ? (bool.TryParse(t1p1f4v.ToString(), out var t1p1f4r) ? t1p1f4r : false) : false
                },
                Position2 = new TurnTablePositionItemsModel
                {
                    Flex1 = latestValueNew.TryGetValue(14, out var t1p2f1v) ? (bool.TryParse(t1p2f1v.ToString(), out var t1p2f1r) ? t1p2f1r : false) : false,
                    Flex2 = latestValueNew.TryGetValue(14, out var t1p2f2v) ? (bool.TryParse(t1p2f2v.ToString(), out var t1p2f2r) ? t1p2f2r : false) : false,
                    Flex3 = latestValueNew.TryGetValue(14, out var t1p2f3v) ? (bool.TryParse(t1p2f3v.ToString(), out var t1p2f3r) ? t1p2f3r : false) : false,
                    Flex4 = latestValueNew.TryGetValue(14, out var t1p2f4v) ? (bool.TryParse(t1p2f4v.ToString(), out var t1p2f4r) ? t1p2f4r : false) : false
                },
                Position3 = new TurnTablePositionItemsModel
                {
                    Flex1 = latestValueNew.TryGetValue(14, out var t1p3f1v) ? (bool.TryParse(t1p3f1v.ToString(), out var t1p3f1r) ? t1p3f1r : false) : false,
                    Flex2 = latestValueNew.TryGetValue(14, out var t1p3f2v) ? (bool.TryParse(t1p3f2v.ToString(), out var t1p3f2r) ? t1p3f2r : false) : false,
                    Flex3 = latestValueNew.TryGetValue(14, out var t1p3f3v) ? (bool.TryParse(t1p3f3v.ToString(), out var t1p3f3r) ? t1p3f3r : false) : false,
                    Flex4 = latestValueNew.TryGetValue(14, out var t1p3f4v) ? (bool.TryParse(t1p3f4v.ToString(), out var t1p3f4r) ? t1p3f4r : false) : false
                },
                Position4 = new TurnTablePositionItemsModel
                {
                    Flex1 = latestValueNew.TryGetValue(14, out var t1p4f1v) ? (bool.TryParse(t1p4f1v.ToString(), out var t1p4f1r) ? t1p4f1r : false) : false,
                    Flex2 = latestValueNew.TryGetValue(14, out var t1p4f2v) ? (bool.TryParse(t1p4f2v.ToString(), out var t1p4f2r) ? t1p4f2r : false) : false,
                    Flex3 = latestValueNew.TryGetValue(14, out var t1p4f3v) ? (bool.TryParse(t1p4f3v.ToString(), out var t1p4f3r) ? t1p4f3r : false) : false,
                    Flex4 = latestValueNew.TryGetValue(14, out var t1p4f4v) ? (bool.TryParse(t1p4f4v.ToString(), out var t1p4f4r) ? t1p4f4r : false) : false
                },
                Rotate = latestValueNew.TryGetValue(14, out var t1rv) ? (bool.TryParse(t1rv.ToString(), out var t1rr) ? t1rr : false) : false
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
                    Flex1 = latestValueNew.TryGetValue(15, out var t2p1f1v) ? (bool.TryParse(t2p1f1v.ToString(), out var t2p1f1r) ? t2p1f1r : false) : false,
                    Flex2 = latestValueNew.TryGetValue(15, out var t2p1f2v) ? (bool.TryParse(t2p1f2v.ToString(), out var t2p1f2r) ? t2p1f2r : false) : false,
                    Flex3 = latestValueNew.TryGetValue(15, out var t2p1f3v) ? (bool.TryParse(t2p1f3v.ToString(), out var t2p1f3r) ? t2p1f3r : false) : false,
                    Flex4 = latestValueNew.TryGetValue(15, out var t2p1f4v) ? (bool.TryParse(t2p1f4v.ToString(), out var t2p1f4r) ? t2p1f4r : false) : false
                },
                Position2 = new TurnTable2PositionItemsModel
                {
                    Flex1 = latestValueNew.TryGetValue(15, out var t2p2f1v) ? (bool.TryParse(t2p2f1v.ToString(), out var t2p2f1r) ? t2p2f1r : false) : false,
                    Flex2 = latestValueNew.TryGetValue(15, out var t2p2f2v) ? (bool.TryParse(t2p2f2v.ToString(), out var t2p2f2r) ? t2p2f2r : false) : false,
                    Flex3 = latestValueNew.TryGetValue(15, out var t2p2f3v) ? (bool.TryParse(t2p2f3v.ToString(), out var t2p2f3r) ? t2p2f3r : false) : false,
                    Flex4 = latestValueNew.TryGetValue(15, out var t2p2f4v) ? (bool.TryParse(t2p2f4v.ToString(), out var t2p2f4r) ? t2p2f4r : false) : false
                },
                Position3 = new TurnTable2PositionItemsModel
                {
                    Flex1 = latestValueNew.TryGetValue(15, out var t2p3f1v) ? (bool.TryParse(t2p3f1v.ToString(), out var t2p3f1r) ? t2p3f1r : false) : false,
                    Flex2 = latestValueNew.TryGetValue(15, out var t2p3f2v) ? (bool.TryParse(t2p3f2v.ToString(), out var t2p3f2r) ? t2p3f2r : false) : false,
                    Flex3 = latestValueNew.TryGetValue(15, out var t2p3f3v) ? (bool.TryParse(t2p3f3v.ToString(), out var t2p3f3r) ? t2p3f3r : false) : false,
                    Flex4 = latestValueNew.TryGetValue(15, out var t2p3f4v) ? (bool.TryParse(t2p3f4v.ToString(), out var t2p3f4r) ? t2p3f4r : false) : false
                },
                Rotate = latestValueNew.TryGetValue(15, out var t2rv) ? (bool.TryParse(t2rv.ToString(), out var t2rr) ? t2rr : false) : false
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
                Flex1 = latestValueNew.TryGetValue(16, out var tfmf1v) ? (bool.TryParse(tfmf1v.ToString(), out var tfmf1r) ? tfmf1r : false) : false,
                Flex2 = latestValueNew.TryGetValue(16, out var tfmf2v) ? (bool.TryParse(tfmf2v.ToString(), out var tfmf2r) ? tfmf2r : false) : false,
                Flex3 = latestValueNew.TryGetValue(16, out var tfmf3v) ? (bool.TryParse(tfmf3v.ToString(), out var tfmf3r) ? tfmf3r : false) : false,
                Flex4 = latestValueNew.TryGetValue(16, out var tfmf4v) ? (bool.TryParse(tfmf4v.ToString(), out var tfmf4r) ? tfmf4r : false) : false,
                Rotate = latestValueNew.TryGetValue(16, out var tfmrv) ? (bool.TryParse(tfmrv.ToString(), out var tfmrr) ? tfmrr : false) : false
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
                QRCode = latestValueNew.TryGetValue(17, out var qrCode) ? qrCode.ToString() : "NA",
                Cam1Imagepath = latestValueNew.TryGetValue(17, out var cam1Imagepath) ? cam1Imagepath.ToString() : "NA",
                Cam2Imagepath = latestValueNew.TryGetValue(17, out var cam2Imagepath) ? cam2Imagepath.ToString() : "NA"
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
                Movement = latestValueNew.TryGetValue(18, out var movement) ? (int.TryParse(movement.ToString(), out var movementResult) ? movementResult : 0) : 0,
                Rotation = latestValueNew.TryGetValue(18, out var rotation) ? (int.TryParse(rotation.ToString(), out var rotationResult) ? rotationResult : 0) : 0,
                Rotate = latestValueNew.TryGetValue(18, out var rotate) ? (bool.TryParse(rotate.ToString(), out var rotateResult) ? rotateResult : false) : false
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
                IsLoaded = latestValueNew.TryGetValue(19, out var isLoaded) ? (bool.TryParse(isLoaded.ToString(), out var isLoadedResult) ? isLoadedResult : false) : false,
                Numberofcomponent = latestValueNew.TryGetValue(19, out var numberofcomponent) ? (int.TryParse(numberofcomponent.ToString(), out var numberofcomponentResult) ? numberofcomponentResult : 0) : 0,
                TraySize = latestValueNew.TryGetValue(19, out var traySize) ? (int.TryParse(traySize.ToString(), out var traySizeResult) ? traySizeResult : 0) : 0,
                NumberOfTrays = latestValueNew.TryGetValue(19, out var numberOfTrays) ? (int.TryParse(numberOfTrays.ToString(), out var numberOfTraysResult) ? numberOfTraysResult : 0) : 0
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
                IsUnLoaded = latestValueNew.TryGetValue(20, out var isUnLoaded) ? (bool.TryParse(isUnLoaded.ToString(), out var isUnLoadedResult) ? isUnLoadedResult : false) : false,
                Numberofcomponent = latestValueNew.TryGetValue(20, out var numberofcomponent) ? (int.TryParse(numberofcomponent.ToString(), out var numberofcomponentResult) ? numberofcomponentResult : 0) : 0,
                TraySize = latestValueNew.TryGetValue(20, out var traySize) ? (int.TryParse(traySize.ToString(), out var traySizeResult) ? traySizeResult : 0) : 0,
                NumberOfTrays = latestValueNew.TryGetValue(20, out var numberOfTrays) ? (int.TryParse(numberOfTrays.ToString(), out var numberOfTraysResult) ? numberOfTraysResult : 0) : 0
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
                    RejectedCount = latestValueNew.TryGetValue(21, out var ngBin1RejectedCount) ? (int.TryParse(ngBin1RejectedCount.ToString(), out var ngBin1RejectedCountResult) ? ngBin1RejectedCountResult : 0) : 0,
                    IsFull = latestValueNew.TryGetValue(21, out var ngBin1IsFull) ? (bool.TryParse(ngBin1IsFull.ToString(), out var ngBin1IsFullResult) ? ngBin1IsFullResult : false) : false
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
                    RejectedCount = latestValueNew.TryGetValue(25, out var ngBin2RejectedCount) ? (int.TryParse(ngBin2RejectedCount.ToString(), out var ngBin2RejectedCountResult) ? ngBin2RejectedCountResult : 0) : 0,
                    IsFull = latestValueNew.TryGetValue(25, out var ngBin2IsFull) ? (bool.TryParse(ngBin2IsFull.ToString(), out var ngBin2IsFullResult) ? ngBin2IsFullResult : false) : false
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
                BatchNo = latestValueNew.TryGetValue(26, out var batchNo) ? int.TryParse(batchNo.ToString(), out var batchNoResult) ? batchNoResult : 0 : int.MinValue,
                Product1 = new BendingMonitorProductModel
                {
                    QRCode = latestValueNew.TryGetValue(26, out var p1QR) ? p1QR.ToString() : "NA",
                    Load = new ParameterLImitValues
                    {
                        UpperLimit = latestValueNew.TryGetValue(26, out var p1LU) ? (double.TryParse(p1LU.ToString(), out var p1LUResult) ? p1LUResult : 0) : 0,
                        PresentValue = latestValueNew.TryGetValue(26, out var p1LP) ? (double.TryParse(p1LP.ToString(), out var p1LPResult) ? p1LPResult : 0) : 0,
                        LowerLimit = latestValueNew.TryGetValue(26, out var p1LL) ? (double.TryParse(p1LL.ToString(), out var p1LLResult) ? p1LLResult : 0) : 0
                    },
                    Temperature = new ParameterLImitValues
                    {
                        UpperLimit = latestValueNew.TryGetValue(26, out var p1TU) ? (double.TryParse(p1TU.ToString(), out var p1TUResult) ? p1TUResult : 0) : 0,
                        PresentValue = latestValueNew.TryGetValue(26, out var p1TP) ? (double.TryParse(p1TP.ToString(), out var p1TPResult) ? p1TPResult : 0) : 0,
                        LowerLimit = latestValueNew.TryGetValue(26, out var p1TL) ? (double.TryParse(p1TL.ToString(), out var p1TLResult) ? p1TLResult : 0) : 0
                    },
                    BendingTime = latestValueNew.TryGetValue(26, out var p1BT) ? (double.TryParse(p1BT.ToString(), out var p1BTResult) ? p1BTResult : 0) : 0,
                    Result = latestValueNew.TryGetValue(26, out var p1R) ? (bool.TryParse(p1R.ToString(), out var p1RResult) ? p1RResult : false) : false
                },
                Product2 = new BendingMonitorProductModel
                {
                    QRCode = latestValueNew.TryGetValue(26, out var p2QR) ? p2QR.ToString() : "NA",
                    Load = new ParameterLImitValues
                    {
                        UpperLimit = latestValueNew.TryGetValue(26, out var p2LU) ? (double.TryParse(p2LU.ToString(), out var p2LUResult) ? p2LUResult : 0) : 0,
                        PresentValue = latestValueNew.TryGetValue(26, out var p2LP) ? (double.TryParse(p2LP.ToString(), out var p2LPResult) ? p2LPResult : 0) : 0,
                        LowerLimit = latestValueNew.TryGetValue(26, out var p2LL) ? (double.TryParse(p2LL.ToString(), out var p2LLResult) ? p2LLResult : 0) : 0
                    },
                    Temperature = new ParameterLImitValues
                    {
                        UpperLimit = latestValueNew.TryGetValue(26, out var p2TempUpper) ? (double.TryParse(p2TempUpper.ToString(), out var p2TempUpperResult) ? p2TempUpperResult : 0) : 0,
                        PresentValue = latestValueNew.TryGetValue(26, out var p2TempPresent) ? (double.TryParse(p2TempPresent.ToString(), out var p2TempPresentResult) ? p2TempPresentResult : 0) : 0,
                        LowerLimit = latestValueNew.TryGetValue(26, out var p2TempLower) ? (double.TryParse(p2TempLower.ToString(), out var p2TempLowerResult) ? p2TempLowerResult : 0) : 0
                    },
                    BendingTime = latestValueNew.TryGetValue(26, out var p2BendTime) ? (double.TryParse(p2BendTime.ToString(), out var p2BTResult) ? p2BTResult : 0) : 0,
                    Result = latestValueNew.TryGetValue(26, out var p2R) ? (bool.TryParse(p2R.ToString(), out var p2RResult) ? p2RResult : false) : false
                },
                Product3 = new BendingMonitorProductModel
                {
                    QRCode = latestValueNew.TryGetValue(26, out var p3QR) ? p3QR.ToString() : "NA",
                    Load = new ParameterLImitValues
                    {
                        UpperLimit = latestValueNew.TryGetValue(26, out var p3LU) ? (double.TryParse(p3LU.ToString(), out var p3LUResult) ? p3LUResult : 0) : 0,
                        PresentValue = latestValueNew.TryGetValue(26, out var p3LP) ? (double.TryParse(p3LP.ToString(), out var p3LPResult) ? p3LPResult : 0) : 0,
                        LowerLimit = latestValueNew.TryGetValue(26, out var p3LL) ? (double.TryParse(p3LL.ToString(), out var p3LLResult) ? p3LLResult : 0) : 0
                    },
                    Temperature = new ParameterLImitValues
                    {
                        UpperLimit = latestValueNew.TryGetValue(26, out var p3TempUpper) ? (double.TryParse(p3TempUpper.ToString(), out var p3TempUpperResult) ? p3TempUpperResult : 0) : 0,
                        PresentValue = latestValueNew.TryGetValue(26, out var p3TempPresent) ? (double.TryParse(p3TempPresent.ToString(), out var p3TempPresentResult) ? p3TempPresentResult : 0) : 0,
                        LowerLimit = latestValueNew.TryGetValue(26, out var p3TempLower) ? (double.TryParse(p3TempLower.ToString(), out var p3TempLowerResult) ? p3TempLowerResult : 0) : 0
                    },
                    BendingTime = latestValueNew.TryGetValue(26, out var p3BT) ? (double.TryParse(p3BT.ToString(), out var p3BTResult) ? p3BTResult : 0) : 0,
                    Result = latestValueNew.TryGetValue(26, out var p3R) ? (bool.TryParse(p3R.ToString(), out var p3RResult) ? p3RResult : false) : false
                },
                Product4 = new BendingMonitorProductModel
                {
                    QRCode = latestValueNew.TryGetValue(26, out var p4QR) ? p4QR.ToString() : "NA",
                    Load = new ParameterLImitValues
                    {
                        UpperLimit = latestValueNew.TryGetValue(26, out var p4LU) ? (double.TryParse(p4LU.ToString(), out var p4LUResult) ? p4LUResult : 0) : 0,
                        PresentValue = latestValueNew.TryGetValue(26, out var p4LP) ? (double.TryParse(p4LP.ToString(), out var p4LPResult) ? p4LPResult : 0) : 0,
                        LowerLimit = latestValueNew.TryGetValue(26, out var p4LL) ? (double.TryParse(p4LL.ToString(), out var p4LLResult) ? p4LLResult : 0) : 0
                    },
                    Temperature = new ParameterLImitValues
                    {
                        UpperLimit = latestValueNew.TryGetValue(26, out var p4TempUpper) ? (double.TryParse(p4TempUpper.ToString(), out var p4TempUpperResult) ? p4TempUpperResult : 0) : 0,
                        PresentValue = latestValueNew.TryGetValue(26, out var p4TempPresent) ? (double.TryParse(p4TempPresent.ToString(), out var p4TempPresentResult) ? p4TempPresentResult : 0) : 0,
                        LowerLimit = latestValueNew.TryGetValue(26, out var p4TempLower) ? (double.TryParse(p4TempLower.ToString(), out var p4TempLowerResult) ? p4TempLowerResult : 0) : 0
                    },
                    BendingTime = latestValueNew.TryGetValue(26, out var p4BendTime) ? (double.TryParse(p4BendTime.ToString(), out var p4BTResult) ? p4BTResult : 0) : 0,
                    Result = latestValueNew.TryGetValue(26, out var p4R) ? (bool.TryParse(p4R.ToString(), out var p4RResult) ? p4RResult : false) : false
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
                BatchNo = latestValueNew.TryGetValue(27, out var batchNo) ? int.TryParse(batchNo.ToString(), out var batchNoResult) ? batchNoResult : 0 : int.MinValue,
                Product1 = new BendingMonitorProductModel
                {
                    QRCode = latestValueNew.TryGetValue(27, out var p1QR) ? p1QR.ToString() : "NA",
                    Load = new ParameterLImitValues
                    {
                        UpperLimit = latestValueNew.TryGetValue(27, out var p1LU) ? (double.TryParse(p1LU.ToString(), out var p1LUResult) ? p1LUResult : 0) : 0,
                        PresentValue = latestValueNew.TryGetValue(27, out var p1LP) ? (double.TryParse(p1LP.ToString(), out var p1LPResult) ? p1LPResult : 0) : 0,
                        LowerLimit = latestValueNew.TryGetValue(27, out var p1LL) ? (double.TryParse(p1LL.ToString(), out var p1LLResult) ? p1LLResult : 0) : 0
                    },
                    Temperature = new ParameterLImitValues
                    {
                        UpperLimit = latestValueNew.TryGetValue(27, out var p1TU) ? (double.TryParse(p1TU.ToString(), out var p1TUResult) ? p1TUResult : 0) : 0,
                        PresentValue = latestValueNew.TryGetValue(27, out var p1TP) ? (double.TryParse(p1TP.ToString(), out var p1TPResult) ? p1TPResult : 0) : 0,
                        LowerLimit = latestValueNew.TryGetValue(27, out var p1TL) ? (double.TryParse(p1TL.ToString(), out var p1TLResult) ? p1TLResult : 0) : 0
                    },
                    BendingTime = latestValueNew.TryGetValue(27, out var p1BendTime) ? (double.TryParse(p1BendTime.ToString(), out var p1BTResult) ? p1BTResult : 0) : 0,
                    Result = latestValueNew.TryGetValue(27, out var p1R) ? (bool.TryParse(p1R.ToString(), out var p1RResult) ? p1RResult : false) : false
                },
                Product2 = new BendingMonitorProductModel
                {
                    QRCode = latestValueNew.TryGetValue(27, out var p2QR) ? p2QR.ToString() : "NA",
                    Load = new ParameterLImitValues
                    {
                        UpperLimit = latestValueNew.TryGetValue(27, out var p2LU) ? (double.TryParse(p2LU.ToString(), out var p2LUResult) ? p2LUResult : 0) : 0,
                        PresentValue = latestValueNew.TryGetValue(27, out var p2LP) ? (double.TryParse(p2LP.ToString(), out var p2LPResult) ? p2LPResult : 0) : 0,
                        LowerLimit = latestValueNew.TryGetValue(27, out var p2LL) ? (double.TryParse(p2LL.ToString(), out var p2LLResult) ? p2LLResult : 0) : 0
                    },
                    Temperature = new ParameterLImitValues
                    {
                        UpperLimit = latestValueNew.TryGetValue(27, out var p2TempUpper) ? (double.TryParse(p2TempUpper.ToString(), out var p2TUResult) ? p2TUResult : 0) : 0,
                        PresentValue = latestValueNew.TryGetValue(27, out var p2TempPresent) ? (double.TryParse(p2TempPresent.ToString(), out var p2TPResult) ? p2TPResult : 0) : 0,
                        LowerLimit = latestValueNew.TryGetValue(27, out var p2TempLower) ? (double.TryParse(p2TempLower.ToString(), out var p2TLResult) ? p2TLResult : 0) : 0
                    },
                    BendingTime = latestValueNew.TryGetValue(27, out var p2BendTime) ? (double.TryParse(p2BendTime.ToString(), out var p2BTResult) ? p2BTResult : 0) : 0,
                    Result = latestValueNew.TryGetValue(27, out var p2Result) ? (bool.TryParse(p2Result.ToString(), out var p2RResult) ? p2RResult : false) : false
                },
                Product3 = new BendingMonitorProductModel
                {
                    QRCode = latestValueNew.TryGetValue(27, out var p3QR) ? p3QR.ToString() : "NA",
                    Load = new ParameterLImitValues
                    {
                        UpperLimit = latestValueNew.TryGetValue(27, out var p3LoadUpper) ? (double.TryParse(p3LoadUpper.ToString(), out var p3LUResult) ? p3LUResult : 0) : 0,
                        PresentValue = latestValueNew.TryGetValue(27, out var p3LoadPresent) ? (double.TryParse(p3LoadPresent.ToString(), out var p3LPResult) ? p3LPResult : 0) : 0,
                        LowerLimit = latestValueNew.TryGetValue(27, out var p3LoadLower) ? (double.TryParse(p3LoadLower.ToString(), out var p3LLResult) ? p3LLResult : 0) : 0
                    },
                    Temperature = new ParameterLImitValues
                    {
                        UpperLimit = latestValueNew.TryGetValue(27, out var p3TempUpper) ? (double.TryParse(p3TempUpper.ToString(), out var p3TUResult) ? p3TUResult : 0) : 0,
                        PresentValue = latestValueNew.TryGetValue(27, out var p3TempPresent) ? (double.TryParse(p3TempPresent.ToString(), out var p3TPResult) ? p3TPResult : 0) : 0,
                        LowerLimit = latestValueNew.TryGetValue(27, out var p3TempLower) ? (double.TryParse(p3TempLower.ToString(), out var p3TLResult) ? p3TLResult : 0) : 0
                    },
                    BendingTime = latestValueNew.TryGetValue(27, out var p3BendTime) ? (double.TryParse(p3BendTime.ToString(), out var p3BTResult) ? p3BTResult : 0) : 0,
                    Result = latestValueNew.TryGetValue(27, out var p3Result) ? (bool.TryParse(p3Result.ToString(), out var p3RResult) ? p3RResult : false) : false
                },
                Product4 = new BendingMonitorProductModel
                {
                    QRCode = latestValueNew.TryGetValue(27, out var p4QRCode) ? p4QRCode.ToString() : "NA",
                    Load = new ParameterLImitValues
                    {
                        UpperLimit = latestValueNew.TryGetValue(27, out var p4LoadUpper) ? (double.TryParse(p4LoadUpper.ToString(), out var p4LUResult) ? p4LUResult : 0) : 0,
                        PresentValue = latestValueNew.TryGetValue(27, out var p4LoadPresent) ? (double.TryParse(p4LoadPresent.ToString(), out var p4LPResult) ? p4LPResult : 0) : 0,
                        LowerLimit = latestValueNew.TryGetValue(27, out var p4LoadLower) ? (double.TryParse(p4LoadLower.ToString(), out var p4LLResult) ? p4LLResult : 0) : 0
                    },
                    Temperature = new ParameterLImitValues
                    {
                        UpperLimit = latestValueNew.TryGetValue(27, out var p4TempUpper) ? (double.TryParse(p4TempUpper.ToString(), out var p4TUResult) ? p4TUResult : 0) : 0,
                        PresentValue = latestValueNew.TryGetValue(27, out var p4TempPresent) ? (double.TryParse(p4TempPresent.ToString(), out var p4TPResult) ? p4TPResult : 0) : 0,
                        LowerLimit = latestValueNew.TryGetValue(27, out var p4TempLower) ? (double.TryParse(p4TempLower.ToString(), out var p4TLResult) ? p4TLResult : 0) : 0
                    },
                    BendingTime = latestValueNew.TryGetValue(27, out var p4BendTime) ? (double.TryParse(p4BendTime.ToString(), out var p4BTResult) ? p4BTResult : 0) : 0,
                    Result = latestValueNew.TryGetValue(27, out var p4Result) ? (bool.TryParse(p4Result.ToString(), out var p4RResult) ? p4RResult : false) : false
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
                BatchNo = latestValueNew.TryGetValue(28, out var batchNo) ? int.TryParse(batchNo.ToString(), out var batchNoResult) ? batchNoResult : 0 : int.MinValue,
                Product1 = new BendingMonitorProductModel
                {
                    QRCode = latestValueNew.TryGetValue(28, out var p1QR) ? p1QR.ToString() : "NA",
                    Load = new ParameterLImitValues
                    {
                        UpperLimit = latestValueNew.TryGetValue(28, out var p1LU) ? (double.TryParse(p1LU.ToString(), out var p1LUResult) ? p1LUResult : 0) : 0,
                        PresentValue = latestValueNew.TryGetValue(28, out var p1LP) ? (double.TryParse(p1LP.ToString(), out var p1LPResult) ? p1LPResult : 0) : 0,
                        LowerLimit = latestValueNew.TryGetValue(28, out var p1LL) ? (double.TryParse(p1LL.ToString(), out var p1LLResult) ? p1LLResult : 0) : 0
                    },
                    Temperature = new ParameterLImitValues
                    {
                        UpperLimit = latestValueNew.TryGetValue(28, out var p1TU) ? (double.TryParse(p1TU.ToString(), out var p1TUResult) ? p1TUResult : 0) : 0,
                        PresentValue = latestValueNew.TryGetValue(28, out var p1TP) ? (double.TryParse(p1TP.ToString(), out var p1TPResult) ? p1TPResult : 0) : 0,
                        LowerLimit = latestValueNew.TryGetValue(28, out var p1TL) ? (double.TryParse(p1TL.ToString(), out var p1TLResult) ? p1TLResult : 0) : 0
                    },
                    BendingTime = latestValueNew.TryGetValue(28, out var p1BT) ? (double.TryParse(p1BT.ToString(), out var p1BTResult) ? p1BTResult : 0) : 0,
                    Result = latestValueNew.TryGetValue(28, out var p1R) ? (bool.TryParse(p1R.ToString(), out var p1RResult) ? p1RResult : false) : false
                },
                Product2 = new BendingMonitorProductModel
                {
                    QRCode = latestValueNew.TryGetValue(28, out var p2QR) ? p2QR.ToString() : "NA",
                    Load = new ParameterLImitValues
                    {
                        UpperLimit = latestValueNew.TryGetValue(28, out var p2LU) ? (double.TryParse(p2LU.ToString(), out var p2LUResult) ? p2LUResult : 0) : 0,
                        PresentValue = latestValueNew.TryGetValue(28, out var p2LP) ? (double.TryParse(p2LP.ToString(), out var p2LPResult) ? p2LPResult : 0) : 0,
                        LowerLimit = latestValueNew.TryGetValue(28, out var p2LL) ? (double.TryParse(p2LL.ToString(), out var p2LLResult) ? p2LLResult : 0) : 0
                    },
                    Temperature = new ParameterLImitValues
                    {
                        UpperLimit = latestValueNew.TryGetValue(28, out var p2TempUpper) ? (double.TryParse(p2TempUpper.ToString(), out var p2TempUpperResult) ? p2TempUpperResult : 0) : 0,
                        PresentValue = latestValueNew.TryGetValue(28, out var p2TempPresent) ? (double.TryParse(p2TempPresent.ToString(), out var p2TempPresentResult) ? p2TempPresentResult : 0) : 0,
                        LowerLimit = latestValueNew.TryGetValue(28, out var p2TempLower) ? (double.TryParse(p2TempLower.ToString(), out var p2TempLowerResult) ? p2TempLowerResult : 0) : 0
                    },
                    BendingTime = latestValueNew.TryGetValue(28, out var p2BendTime) ? (double.TryParse(p2BendTime.ToString(), out var p2BTResult) ? p2BTResult : 0) : 0,
                    Result = latestValueNew.TryGetValue(28, out var p2Result) ? (bool.TryParse(p2Result.ToString(), out var p2RResult) ? p2RResult : false) : false
                },
                Product3 = new BendingMonitorProductModel
                {
                    QRCode = latestValueNew.TryGetValue(28, out var p3QR) ? p3QR.ToString() : "NA",
                    Load = new ParameterLImitValues
                    {
                        UpperLimit = latestValueNew.TryGetValue(28, out var p3LU) ? (double.TryParse(p3LU.ToString(), out var p3LUResult) ? p3LUResult : 0) : 0,
                        PresentValue = latestValueNew.TryGetValue(28, out var p3LP) ? (double.TryParse(p3LP.ToString(), out var p3LPResult) ? p3LPResult : 0) : 0,
                        LowerLimit = latestValueNew.TryGetValue(28, out var p3LL) ? (double.TryParse(p3LL.ToString(), out var p3LLResult) ? p3LLResult : 0) : 0
                    },
                    Temperature = new ParameterLImitValues
                    {
                        UpperLimit = latestValueNew.TryGetValue(28, out var p3TempUpper) ? (double.TryParse(p3TempUpper.ToString(), out var p3TempUpperResult) ? p3TempUpperResult : 0) : 0,
                        PresentValue = latestValueNew.TryGetValue(28, out var p3TempPresent) ? (double.TryParse(p3TempPresent.ToString(), out var p3TempPresentResult) ? p3TempPresentResult : 0) : 0,
                        LowerLimit = latestValueNew.TryGetValue(28, out var p3TempLower) ? (double.TryParse(p3TempLower.ToString(), out var p3TempLowerResult) ? p3TempLowerResult : 0) : 0
                    },
                    BendingTime = latestValueNew.TryGetValue(28, out var p3BendTime) ? (double.TryParse(p3BendTime.ToString(), out var p3BTResult) ? p3BTResult : 0) : 0,
                    Result = latestValueNew.TryGetValue(28, out var p3Result) ? (bool.TryParse(p3Result.ToString(), out var p3RResult) ? p3RResult : false) : false
                },
                Product4 = new BendingMonitorProductModel
                {
                    QRCode = latestValueNew.TryGetValue(28, out var p4QR) ? p4QR.ToString() : "NA",
                    Load = new ParameterLImitValues
                    {
                        UpperLimit = latestValueNew.TryGetValue(28, out var p4LU) ? (double.TryParse(p4LU.ToString(), out var p4LUResult) ? p4LUResult : 0) : 0,
                        PresentValue = latestValueNew.TryGetValue(28, out var p4LP) ? (double.TryParse(p4LP.ToString(), out var p4LPResult) ? p4LPResult : 0) : 0,
                        LowerLimit = latestValueNew.TryGetValue(28, out var p4LL) ? (double.TryParse(p4LL.ToString(), out var p4LLResult) ? p4LLResult : 0) : 0
                    },
                    Temperature = new ParameterLImitValues
                    {
                        UpperLimit = latestValueNew.TryGetValue(28, out var p4TempUpper) ? (double.TryParse(p4TempUpper.ToString(), out var p4TempUpperResult) ? p4TempUpperResult : 0) : 0,
                        PresentValue = latestValueNew.TryGetValue(28, out var p4TempPresent) ? (double.TryParse(p4TempPresent.ToString(), out var p4TempPresentResult) ? p4TempPresentResult : 0) : 0,
                        LowerLimit = latestValueNew.TryGetValue(28, out var p4TempLower) ? (double.TryParse(p4TempLower.ToString(), out var p4TempLowerResult) ? p4TempLowerResult : 0) : 0
                    },
                    BendingTime = latestValueNew.TryGetValue(28, out var p4BendTime) ? (double.TryParse(p4BendTime.ToString(), out var p4BTResult) ? p4BTResult : 0) : 0,
                    Result = latestValueNew.TryGetValue(28, out var p4Result) ? (bool.TryParse(p4Result.ToString(), out var p4RResult) ? p4RResult : false) : false
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
                BatchNo = latestValueNew.TryGetValue(29, out var batchNo) ? batchNo.ToString() : "NA",
                Product1 = new PostBendingMonitorRow
                {
                    Product = latestValueNew.TryGetValue(29, out var p1Prodv) ? p1Prodv.ToString() : "NA",
                    QRCode = latestValueNew.TryGetValue(29, out var p1QRv) ? p1QRv.ToString() : "NA",
                    X = new PostParameterLImitValues
                    {
                        UpperLimit = latestValueNew.TryGetValue(29, out var p1XUv) ? (double.TryParse(p1XUv.ToString(), out var p1XUr) ? p1XUr : 0) : 0,
                        PresentValue = latestValueNew.TryGetValue(29, out var p1XPv) ? (double.TryParse(p1XPv.ToString(), out var p1XPr) ? p1XPr : 0) : 0,
                        LowerLimit = latestValueNew.TryGetValue(29, out var p1XLv) ? (double.TryParse(p1XLv.ToString(), out var p1XLr) ? p1XLr : 0) : 0
                    },
                    Y = new PostParameterLImitValues
                    {
                        UpperLimit = latestValueNew.TryGetValue(29, out var p1YUv) ? (double.TryParse(p1YUv.ToString(), out var p1YUr) ? p1YUr : 0) : 0,
                        PresentValue = latestValueNew.TryGetValue(29, out var p1YPv) ? (double.TryParse(p1YPv.ToString(), out var p1YPr) ? p1YPr : 0) : 0,
                        LowerLimit = latestValueNew.TryGetValue(29, out var p1YLv) ? (double.TryParse(p1YLv.ToString(), out var p1YLr) ? p1YLr : 0) : 0
                    },
                    Z = new PostParameterLImitValues
                    {
                        UpperLimit = latestValueNew.TryGetValue(29, out var p1ZUv) ? (double.TryParse(p1ZUv.ToString(), out var p1ZUr) ? p1ZUr : 0) : 0,
                        PresentValue = latestValueNew.TryGetValue(29, out var p1ZPv) ? (double.TryParse(p1ZPv.ToString(), out var p1ZPr) ? p1ZPr : 0) : 0,
                        LowerLimit = latestValueNew.TryGetValue(29, out var p1ZLv) ? (double.TryParse(p1ZLv.ToString(), out var p1ZLr) ? p1ZLr : 0) : 0
                    },
                    W = new PostParameterLImitValues
                    {
                        UpperLimit = latestValueNew.TryGetValue(29, out var p1WUv) ? (double.TryParse(p1WUv.ToString(), out var p1WUr) ? p1WUr : 0) : 0,
                        PresentValue = latestValueNew.TryGetValue(29, out var p1WPv) ? (double.TryParse(p1WPv.ToString(), out var p1WPr) ? p1WPr : 0) : 0,
                        LowerLimit = latestValueNew.TryGetValue(29, out var p1WLv) ? (double.TryParse(p1WLv.ToString(), out var p1WLr) ? p1WLr : 0) : 0
                    },
                    Result = latestValueNew.TryGetValue(29, out var p1Resp) ? (bool.TryParse(p1Resp.ToString(), out var p1Resr) ? p1Resr : false) : false
                },
                Product2 = new PostBendingMonitorRow
                {
                    Product = latestValueNew.TryGetValue(29, out var p2Prodv) ? p2Prodv.ToString() : "NA",
                    QRCode = latestValueNew.TryGetValue(29, out var p2QRv) ? p2QRv.ToString() : "NA",
                    X = new PostParameterLImitValues
                    {
                        UpperLimit = latestValueNew.TryGetValue(29, out var p2XUv) ? (double.TryParse(p2XUv.ToString(), out var p2XUr) ? p2XUr : 0) : 0,
                        PresentValue = latestValueNew.TryGetValue(29, out var p2XPv) ? (double.TryParse(p2XPv.ToString(), out var p2XPr) ? p2XPr : 0) : 0,
                        LowerLimit = latestValueNew.TryGetValue(29, out var p2XLv) ? (double.TryParse(p2XLv.ToString(), out var p2XLr) ? p2XLr : 0) : 0
                    },
                    Y = new PostParameterLImitValues
                    {
                        UpperLimit = latestValueNew.TryGetValue(29, out var p2YUv) ? (double.TryParse(p2YUv.ToString(), out var p2YUr) ? p2YUr : 0) : 0,
                        PresentValue = latestValueNew.TryGetValue(29, out var p2YPv) ? (double.TryParse(p2YPv.ToString(), out var p2YPr) ? p2YPr : 0) : 0,
                        LowerLimit = latestValueNew.TryGetValue(29, out var p2YLv) ? (double.TryParse(p2YLv.ToString(), out var p2YLr) ? p2YLr : 0) : 0
                    },
                    Z = new PostParameterLImitValues
                    {
                        UpperLimit = latestValueNew.TryGetValue(29, out var p2ZUv) ? (double.TryParse(p2ZUv.ToString(), out var p2ZUr) ? p2ZUr : 0) : 0,
                        PresentValue = latestValueNew.TryGetValue(29, out var p2ZPv) ? (double.TryParse(p2ZPv.ToString(), out var p2ZPr) ? p2ZPr : 0) : 0,
                        LowerLimit = latestValueNew.TryGetValue(29, out var p2ZLv) ? (double.TryParse(p2ZLv.ToString(), out var p2ZLr) ? p2ZLr : 0) : 0
                    },
                    W = new PostParameterLImitValues
                    {
                        UpperLimit = latestValueNew.TryGetValue(29, out var p2WUv) ? (double.TryParse(p2WUv.ToString(), out var p2WUr) ? p2WUr : 0) : 0,
                        PresentValue = latestValueNew.TryGetValue(29, out var p2WPv) ? (double.TryParse(p2WPv.ToString(), out var p2WPr) ? p2WPr : 0) : 0,
                        LowerLimit = latestValueNew.TryGetValue(29, out var p2WLv) ? (double.TryParse(p2WLv.ToString(), out var p2WLr) ? p2WLr : 0) : 0
                    },
                    Result = latestValueNew.TryGetValue(29, out var p2Resv) ? (bool.TryParse(p2Resv.ToString(), out var p2Resr) ? p2Resr : false) : false
                },
                Product3 = new PostBendingMonitorRow
                {
                    Product = latestValueNew.TryGetValue(29, out var p3Prodv) ? p3Prodv.ToString() : "NA",
                    QRCode = latestValueNew.TryGetValue(29, out var p3QRv) ? p3QRv.ToString() : "NA",
                    X = new PostParameterLImitValues
                    {
                        UpperLimit = latestValueNew.TryGetValue(29, out var p3XUv) ? (double.TryParse(p3XUv.ToString(), out var p3XUr) ? p3XUr : 0) : 0,
                        PresentValue = latestValueNew.TryGetValue(29, out var p3XPv) ? (double.TryParse(p3XPv.ToString(), out var p3XPr) ? p3XPr : 0) : 0,
                        LowerLimit = latestValueNew.TryGetValue(29, out var p3XLv) ? (double.TryParse(p3XLv.ToString(), out var p3XLr) ? p3XLr : 0) : 0
                    },
                    Y = new PostParameterLImitValues
                    {
                        UpperLimit = latestValueNew.TryGetValue(29, out var p3YUv) ? (double.TryParse(p3YUv.ToString(), out var p3YUr) ? p3YUr : 0) : 0,
                        PresentValue = latestValueNew.TryGetValue(29, out var p3YPv) ? (double.TryParse(p3YPv.ToString(), out var p3YPr) ? p3YPr : 0) : 0,
                        LowerLimit = latestValueNew.TryGetValue(29, out var p3YLv) ? (double.TryParse(p3YLv.ToString(), out var p3YLr) ? p3YLr : 0) : 0
                    },
                    Z = new PostParameterLImitValues
                    {
                        UpperLimit = latestValueNew.TryGetValue(29, out var p3ZUv) ? (double.TryParse(p3ZUv.ToString(), out var p3ZUr) ? p3ZUr : 0) : 0,
                        PresentValue = latestValueNew.TryGetValue(29, out var p3ZPv) ? (double.TryParse(p3ZPv.ToString(), out var p3ZPr) ? p3ZPr : 0) : 0,
                        LowerLimit = latestValueNew.TryGetValue(29, out var p3ZLv) ? (double.TryParse(p3ZLv.ToString(), out var p3ZLr) ? p3ZLr : 0) : 0
                    },
                    W = new PostParameterLImitValues
                    {
                        UpperLimit = latestValueNew.TryGetValue(29, out var p3WUv) ? (double.TryParse(p3WUv.ToString(), out var p3WUr) ? p3WUr : 0) : 0,
                        PresentValue = latestValueNew.TryGetValue(29, out var p3WPv) ? (double.TryParse(p3WPv.ToString(), out var p3WPr) ? p3WPr : 0) : 0,
                        LowerLimit = latestValueNew.TryGetValue(29, out var p3WLv) ? (double.TryParse(p3WLv.ToString(), out var p3WLr) ? p3WLr : 0) : 0
                    },
                    Result = latestValueNew.TryGetValue(29, out var p3Resv) ? (bool.TryParse(p3Resv.ToString(), out var p3Resr) ? p3Resr : false) : false
                },
                Product4 = new PostBendingMonitorRow
                {
                    Product = latestValueNew.TryGetValue(29, out var p4Prodv) ? p4Prodv.ToString() : "NA",
                    QRCode = latestValueNew.TryGetValue(29, out var p4QRv) ? p4QRv.ToString() : "NA",
                    X = new PostParameterLImitValues
                    {
                        UpperLimit = latestValueNew.TryGetValue(29, out var p4XUv) ? (double.TryParse(p4XUv.ToString(), out var p4XUr) ? p4XUr : 0) : 0,
                        PresentValue = latestValueNew.TryGetValue(29, out var p4XPv) ? (double.TryParse(p4XPv.ToString(), out var p4XPr) ? p4XPr : 0) : 0,
                        LowerLimit = latestValueNew.TryGetValue(29, out var p4XLv) ? (double.TryParse(p4XLv.ToString(), out var p4XLr) ? p4XLr : 0) : 0
                    },
                    Y = new PostParameterLImitValues
                    {
                        UpperLimit = latestValueNew.TryGetValue(29, out var p4YUv) ? (double.TryParse(p4YUv.ToString(), out var p4YUr) ? p4YUr : 0) : 0,
                        PresentValue = latestValueNew.TryGetValue(29, out var p4YPv) ? (double.TryParse(p4YPv.ToString(), out var p4YPr) ? p4YPr : 0) : 0,
                        LowerLimit = latestValueNew.TryGetValue(29, out var p4YLv) ? (double.TryParse(p4YLv.ToString(), out var p4YLr) ? p4YLr : 0) : 0
                    },
                    Z = new PostParameterLImitValues
                    {
                        UpperLimit = latestValueNew.TryGetValue(29, out var p4ZUv) ? (double.TryParse(p4ZUv.ToString(), out var p4ZUr) ? p4ZUr : 0) : 0,
                        PresentValue = latestValueNew.TryGetValue(29, out var p4ZPv) ? (double.TryParse(p4ZPv.ToString(), out var p4ZPr) ? p4ZPr : 0) : 0,
                        LowerLimit = latestValueNew.TryGetValue(29, out var p4ZLv) ? (double.TryParse(p4ZLv.ToString(), out var p4ZLr) ? p4ZLr : 0) : 0
                    },
                    W = new PostParameterLImitValues
                    {
                        UpperLimit = latestValueNew.TryGetValue(29, out var p4WUv) ? (double.TryParse(p4WUv.ToString(), out var p4WUr) ? p4WUr : 0) : 0,
                        PresentValue = latestValueNew.TryGetValue(29, out var p4WPv) ? (double.TryParse(p4WPv.ToString(), out var p4WPr) ? p4WPr : 0) : 0,
                        LowerLimit = latestValueNew.TryGetValue(29, out var p4WLv) ? (double.TryParse(p4WLv.ToString(), out var p4WLr) ? p4WLr : 0) : 0
                    },
                    Result = latestValueNew.TryGetValue(29, out var p4Resv) ? (bool.TryParse(p4Resv.ToString(), out var p4Resr) ? p4Resr : false) : false
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
            //if (latestValueNew.TryGetValue(plcNo, out var packet))
            {
                return new ResponsePackage
                {
                    ResponseId = request.RequestId,
                    Parameters = latestValueNew ?? new Dictionary<int, object>()
                };
            }

            //return new ResponsePackage
            //{
            //    ResponseId = request.RequestId,
            //    Parameters = packet.Values
            //};
        }

        


    }
}
