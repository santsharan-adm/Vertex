using IPCSoftware.Communication.Common;
using IPCSoftware.Core.Interfaces;
using IPCSoftware.Core.Interfaces.AppLoggerInterface;
using IPCSoftware.CoreService.Services.Dashboard;
using IPCSoftware.Devices.Camera;
using IPCSoftware.Devices.PLC;
using IPCSoftware.Devices.UI;
using IPCSoftware.Services;
using IPCSoftware.Shared.Models;
using IPCSoftware.Shared.Models.ConfigModels;
using IPCSoftware.Shared.Models.Messaging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json.Linq;
using System.Diagnostics;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;

namespace IPCSoftware.Engine
{
    public class DashboardInitializerBase : BaseService
    {
        private readonly PLCClientManager _manager;
        private readonly UiListener _ui ;
        private readonly AlgorithmAnalysisService _algo;
        private readonly OeeEngineBase _oee ;
        private readonly SystemMonitorServiceBase _systemMonitor;
        private readonly ShiftResetService _shiftReset;
        private readonly CCDTriggerServiceBase _ccdTrigger; // 1. Add field
        private readonly AlarmService _alarmService;
       // private readonly IPLCTagConfigurationService _tagService;         //Added by Rishabh - date - 26/04/2026//

        // latest packets per PLC (unitno)
        //protected readonly Dictionary<int, PlcPacket> _latestPackets = new();
        protected Dictionary<int, object> latestValueNew = new Dictionary<int, object>();
        protected IProcessLogic processLogicEngine;

        //protected Dictionary<int, object>? _lastValues = null;

        public DashboardInitializerBase(PLCClientManager manager,
            AlgorithmAnalysisService algo,
            OeeEngineBase oee,
            ShiftResetService shiftReset,
            SystemMonitorServiceBase systemMonitor,
          UiListener ui,
          AlarmService alarmService,
            CCDTriggerServiceBase ccdTrigger,
             IProcessLogic processLogic,
            IAppLogger logger) : base(logger)
        {
            _ui = ui;
            _shiftReset = shiftReset;
            _alarmService = alarmService;   
            _systemMonitor = systemMonitor;
            _oee = oee;
            _manager = manager;
            _algo =algo;
            _ccdTrigger = ccdTrigger;

            _algo.OnPlcDataProcessed += (tagId, value) =>
            {
                latestValueNew[tagId] = value;
            };
            processLogicEngine = processLogic;


        }

        List<bool> sysMonData = new List<bool>();
        Dictionary<int, object> oeeCalculate = new Dictionary<int, object>();

        public async Task StartAsync()
        {
            try
            {
                _ui.OnRequestReceived = HandleUiRequest;

                // Start PLC read loops and collect tasks
                var plcTasks = _manager.Clients.Select(client =>
                {
                    client.OnPlcDataReceived += async (plcNo, values) =>
                    {
                        // A. Process Raw Data -> Typed Values (Int/Bool/String)
                        // processedData is Dictionary<int, object> where int is Tag ID
                        _algo.Apply(plcNo, values);
                        

                     
                        

                        // C. Prepare for UI (Convert int Key to uint Key for compatibility)
                        //var final = processedData.ToDictionary(k => (int)k.Key, v => v.Value);

                        // D. Update Cache
                        //_latestPackets[plcNo] = new PlcPacket
                        //{
                        //    PlcNo = plcNo,
                        //    Values = final,
                        //    Timestamp = DateTime.Now
                        //};

                       // _lastValues = final;
                    };
                    return client.StartAsync();
                }).ToList();

                // Wait loop: exit when any plc task have completed (RanToCompletion, Faulted or Canceled)
                while (plcTasks.All(t => !t.IsCompleted))
                {
                    var processedData = latestValueNew;
                    processLogicEngine.Process(processedData);
                    await _ccdTrigger.ProcessTriggers(processedData, _manager);
                    _oee.ProcessCycleTimeLogic(processedData);
                   // oeeCalculate=_oee.Calculate(processedData);
                    sysMonData=_systemMonitor.Process(processedData);
                    _alarmService.ProcessTagData(processedData);
                    _shiftReset.Process(processedData);
                    // short delay to avoid tight loop; adjust interval as needed
                    await Task.Delay(500);
                }

                _logger.LogError("Terminated process logic as one or more PLC task terminated.", LogType.Diagnostics);
                // -- BMK-23-05-2026
                //Todo : Implement graceful shutdown logic if needed (e.g., cancel remaining tasks, dispose resources, etc.) 
                //Todo : Raise alert/notification to UI to notify operator.

                // Ensure exceptions (if any) are observed/propagated
                await Task.WhenAll(plcTasks);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message, LogType.Diagnostics);
            }
        }

        //private void HandlePlcPacket(int plcNo, Dictionary<int, object> values)
        //{
        //    // Store latest packet
        //    _latestPackets[plcNo] = new PlcPacket
        //    {
        //        PlcNo = plcNo,
        //        Values = values,
        //        Timestamp = DateTime.Now
        //    };

        //    Console.WriteLine($"Dashboard: Received {values.Count} tags from PLC {plcNo}");
        //}

        private PLCTagConfigurationModel? GetTagConfig(int tagId)
        {
            return _algo.Tags.FirstOrDefault(t => t.Id == tagId);
        }


        public virtual async Task<ResponsePackage> HandleUiRequest(RequestPackage request)
        {
            try
            {
                Debug.WriteLine($"[Core] HandleUiRequest called → RequestId={request.RequestId}");
               // _logger.LogInfo($"[Core] HandleUiRequest called → RequestId={request.RequestId}",LogType.Diagnostics);

                //---------------------------------------------------------
                // 6) WRITE REQUEST (RequestId = 6)
                //---------------------------------------------------------
                if (request.RequestId == 6)
                {
                    return await HandleUiWrite(request);
                }
                //-----------------------------------------------------------
                //7) ALARM REQUEST (RequestId = 7)
                //-----------------------------------------------------------

                if (request.RequestId == 7)
                {
                    return await HandleAlarmRequest(request);
                }

                //----------------------------------------------------------
                //8) LOG REQUEST (RequestId = 8)       //Added on 30-04-2026
                //----------------------------------------------------------

                if (request.RequestId == 8) 
                {
                    return await HandleUiLogRequest (request);
                }



                //---------------------------------------------------------
                // 1) IO REQUEST (RequestId = 5)
                //---------------------------------------------------------
                if (request.RequestId == 5)
                {
                    return new ResponsePackage
                    {
                        ResponseId = 5,
                        Parameters = latestValueNew
                    };

                   // return new ResponsePackage;
                    //{
                    //    ResponseId = 5,
                    //    Parameters = packet // Dictionary<uint, object>
                    //};
                }

                //---------------------------------------------------------
                // 2) OEE REQUEST (RequestId = 4)
                //---------------------------------------------------------
                if (request.RequestId == 4)
                {
                    return new ResponsePackage
                    {
                        ResponseId = 4,
                        // Parameters = oeeCalculate
                        Parameters =  _oee.Calculate(latestValueNew)
                        // Parameters = new Dictionary<int, object> { { 4, _oee.OeeResult } }
                    };            
                }

                //-----------------------

                if (request.RequestId == 1)
                {
                    TaskbarItems taskbarItems = new TaskbarItems();
                    if (sysMonData.Count >= 0)
                    {
                        taskbarItems.IsPLC1Connected = sysMonData[0];
                    }
                    if (sysMonData.Count >= 1)
                    {
                        taskbarItems.IsPLC2Connected = sysMonData[1];
                    }
                    if (sysMonData.Count >= 2)
                    {
                        taskbarItems.IsMacMiniConnected = sysMonData[2];
                    }

                    if (GetBool( ConstantValues.Mode_Auto.Read)) taskbarItems.CurrentMachineMode = "AUTO RUN";
                    else if (GetBool( ConstantValues.Mode_DryRun.Read)) taskbarItems.CurrentMachineMode = "DRY RUN";
                    else if (GetBool( ConstantValues.Mode_CycleStop.Read)) taskbarItems.CurrentMachineMode = "CYCLE STOP";
                    else if (GetBool(ConstantValues.Mode_MassRTO.Read)) taskbarItems.CurrentMachineMode = "MACHINE HOME";
                    else taskbarItems.CurrentMachineMode = "MANUAL / IDLE"; // Default if no specific mode active
                   


                        return new ResponsePackage
                        {
                            ResponseId = 1,
                            Parameters = new Dictionary<int, object>()
                            {
                                {
                                    0,
                                   taskbarItems
                                }
                            }
                        };
                    }
                //if (GetBool(liveData, ConstantValues.Mode_Auto.Read)) CurrentMachineMode = "AUTO RUN";
                //            else if (GetBool(liveData, ConstantValues.Mode_DryRun.Read)) CurrentMachineMode = "DRY RUN";
                //            else if (GetBool(liveData, ConstantValues.Mode_CycleStop.Read)) CurrentMachineMode = "CYCLE STOP";
                //            else if (GetBool(liveData, ConstantValues.Mode_MassRTO.Read)) CurrentMachineMode = "MACHINE HOME";
                //            else CurrentMachineMode = "MANUAL / IDLE"; // Default if no specific mode active

                //    _oee.ProcessCycleTimeLogic(packet.Values);
                //return new ResponsePackage
                //{   
                //    ResponseId = 1,
                //    Parameters = _systemMonitor.Process(packet.Values)
                //};





                //---------------------------------------------------------
                // 3) UNKNOWN REQUEST
                //---------------------------------------------------------
                return new ResponsePackage
                {
                    ResponseId = -1,
                    Parameters = new Dictionary<int, object>()
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message, LogType.Diagnostics);
                return new ResponsePackage
                {
                    ResponseId = -1,
                    Parameters = new Dictionary<int, object>()
                };
            }
        }

        private async Task<ResponsePackage> HandleUiWrite(RequestPackage request)
        {
            try
            {
                int tagId = 0;
                object value = null;

                if (request.Parameters is JsonElement json)
                {
                    foreach (var prop in json.EnumerateObject())
                    {
                        tagId = int.Parse(prop.Name);

                        // Handle different value types
                        value = prop.Value.ValueKind switch
                        {
                            JsonValueKind.True => true,
                            JsonValueKind.False => false,
                            JsonValueKind.String => prop.Value.GetString(),
                            JsonValueKind.Number => prop.Value.TryGetInt32(out int intVal)
                                ? intVal
                                : prop.Value.GetDouble(),
                            _ => prop.Value.ToString()
                        };
                    }
                }

                var cfg = GetTagConfig(tagId);
                if (cfg == null)
                    return Error($"Tag {tagId} not found");

                var plc = _manager.GetClient(cfg.PLCNo);
                if (plc == null)
                    return Error($"PLC {cfg.PLCNo} not connected");


                //_logger.LogInfo($"Tag id  = {tagId} Tag Name = {cfg.Name} Value is = {value}  time is = {DateTime.Now.Millisecond}", LogType.Error);
              //  Debug.WriteLine($"Tag id  = {tagId} Tag Name = {cfg.Name} Value is = {value}  time is = {DateTime.Now.Millisecond}");
                await plc.WriteAsync(cfg, value);

                SetCachedValue(tagId, value);

                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message, LogType.Diagnostics);
                return Error(ex.Message);
            }
        }


        private async Task<ResponsePackage> HandleAlarmRequest(RequestPackage request)
        {
            try
            {
                if (request.Parameters is JsonElement json)
                {
                    try
                    {
                        if (json.TryGetProperty("Action", out var actionElement) && actionElement.GetString() == "Acknowledge")
                        {
                            int alarmNo = json.GetProperty("AlarmNo").GetInt32();
                            string userName = json.GetProperty("UserName").GetString() ?? "WebClient";

                            bool success = await _alarmService.AcknowledgeAlarm(alarmNo, userName);

                            if (success) return OkAlarm(alarmNo);
                            else return ErrorAlarm($"Failed to acknowledge Alarm {alarmNo}. Not active or already ack'd.");
                        }
                        return ErrorAlarm("Unknown alarm action.");
                    }
                    catch (Exception ex) { _logger.LogError(ex.Message, LogType.Diagnostics); return ErrorAlarm($"Error processing alarm request: {ex.Message}"); }
                }
                return ErrorAlarm("Invalid alarm request parameters.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message, LogType.Diagnostics);
                return ErrorAlarm($"Error processing alarm request: {ex.Message}.");
            }
        }

        //Added by Rishabh - 30/04/2026 -
        //This method will handle the log request coming from UI and log it using AppLogger
        private async Task<ResponsePackage> HandleUiLogRequest(RequestPackage request)
        {
           // throw new Exception("Testing via CoreService");
            try
            {
                if (request.Parameters is JsonElement json)
                {
                    var logRequest = JsonSerializer.Deserialize<LogRequest>(json.GetRawText());

                    if (logRequest != null)
                    {
                        switch (logRequest.Level)
                        {
                            case "INFO":
                                _logger.LogInfo(logRequest.Message, logRequest.LogType);
                                break;
                            case "WARN":
                                _logger.LogWarning(logRequest.Message, logRequest.LogType);
                                break;
                            case "ERROR":
                                _logger.LogError(logRequest.Message, logRequest.LogType,
                                    logRequest.MemberName, logRequest.FilePath, logRequest.LineNumber);
                                break;
                            case "TRACE":
                                _logger.LogTrace(logRequest.Message);
                                break;
                        }

                        return new ResponsePackage { ResponseId = 8, Success = true };
                    }
                }

                return new ResponsePackage
                {
                    ResponseId = 8,
                    Success = false,
                    ErrorMessage = "Invalid log request"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError($"HandleUiLogRequest error: {ex.Message}", LogType.Diagnostics);
                return new ResponsePackage
                {
                    ResponseId = 8,
                    Success = false,
                    ErrorMessage = ex.Message
                };
            }
        }


        private ResponsePackage OkAlarm(int alarmNo) =>
                                new ResponsePackage
                                {
                                    ResponseId = 7,
                                    Success = true,
                                    Parameters = new Dictionary<int, object> { { 0, $"ACKNOWLEDGED:{alarmNo}" } }
                                };

        private ResponsePackage ErrorAlarm(string msg) =>
            new ResponsePackage { ResponseId = 7, Success = false, ErrorMessage = msg, Parameters = null };

        private void SetCachedValue(int tagId, object value)
        {
            // Now the dictionary will be updated directly using tagId as the key
            latestValueNew[tagId] = value;
        }

        private ResponsePackage Ok() =>
            new ResponsePackage { ResponseId = 6, Success = true };

        private ResponsePackage Error(string msg) =>
            new ResponsePackage { ResponseId = 6, Success = false, ErrorMessage = msg };


        // Helper to read a float from the latest PLC packet by tag ID
        protected float GetFloat(int tagId)
        {
            if (latestValueNew.TryGetValue(tagId, out var val) &&
                float.TryParse(val.ToString(), out var result))
                return result;
            return 0f;  // NaN is not valid JSON — use 0 as safe default
        }

        protected int GetInt(int tagId)
        {

            if (latestValueNew.TryGetValue(tagId, out var val) &&
                int.TryParse(val.ToString(), out var result))
                return result;
            return 0;
        }

        protected bool GetBool(int tagId)
        {
            if (latestValueNew.TryGetValue(tagId, out var val) &&
                bool.TryParse(val.ToString(), out var result))
                return result;
            return false;
        }

        protected string GetString(int tagId)
        {
            if (latestValueNew.TryGetValue(tagId, out var val))
                return val?.ToString() ?? "NA";
            return "NA";
        }


    }

    // ✅ Moved here FROM Worker.cs — lives in IPCSoftware.Engine assembly
    // so TagChangeWatcherService (also in IPCSoftware.Engine) can access it directly.
    // Worker.cs calls SharedServiceHost.Initialize() via "using IPCSoftware.Engine".
    public static class SharedServiceHost
    {
        public static PLCClientManager? PlcManager { get; private set; }
        public static AlgorithmAnalysisService? AlgorithmService { get; private set; }

        public static void Initialize(PLCClientManager manager, AlgorithmAnalysisService algo)
        {
            PlcManager = manager;
            AlgorithmService = algo;
        }


    }
}
