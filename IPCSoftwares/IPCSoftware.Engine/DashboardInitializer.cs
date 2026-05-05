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
        private readonly SystemMonitorService _systemMonitor;
        private readonly ShiftResetService _shiftReset;
        private readonly CCDTriggerServiceBase _ccdTrigger; // 1. Add field
        private readonly AlarmService _alarmService;
       // private readonly IPLCTagConfigurationService _tagService;         //Added by Rishabh - date - 26/04/2026//

        // latest packets per PLC (unitno)
        private readonly Dictionary<int, PlcPacket> _latestPackets = new();

        private Dictionary<int, object>? _lastValues = null;

        public DashboardInitializerBase(PLCClientManager manager,
            AlgorithmAnalysisService algo,
            OeeEngineBase oee,
            ShiftResetService shiftReset,
            SystemMonitorService systemMonitor,
          UiListener ui,
          AlarmService alarmService,
            CCDTriggerServiceBase ccdTrigger,          
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
           
        }

      

        public async Task StartAsync()
        {
            try
            {
                _ui.OnRequestReceived = HandleUiRequest;

                // Start UI
               // var uiTask = _ui.StartAsync();
                // Start PLC read loops
                var plcTasks = _manager.Clients.Select(client =>
                {
                    client.OnPlcDataReceived += async (plcNo, values) =>
                    {
                        
                      
                        
                        // A. Process Raw Data -> Typed Values (Int/Bool/String)
                        // processedData is Dictionary<int, object> where int is Tag ID
                        var processedData = _algo.Apply(plcNo, values);

                     
                        await _ccdTrigger.ProcessTriggers(processedData, _manager);
                        _oee.ProcessCycleTimeLogic(processedData);
                        _oee.Calculate(processedData);
                        _systemMonitor.Process(processedData);
                        _alarmService.ProcessTagData(processedData);
                        _shiftReset.Process(processedData);

                        // C. Prepare for UI (Convert int Key to uint Key for compatibility)
                        var final = processedData.ToDictionary(k => (int)k.Key, v => v.Value);

                        // D. Update Cache
                        _latestPackets[plcNo] = new PlcPacket
                        {
                            PlcNo = plcNo,
                            Values = final,
                            Timestamp = DateTime.Now
                        };

                        _lastValues = final;
                    };
                    return client.StartAsync();
                });

                // Await everything
                await Task.WhenAll(plcTasks);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message, LogType.Diagnostics);
            }
        }

        private void HandlePlcPacket(int plcNo, Dictionary<int, object> values)
        {
            // Store latest packet
            _latestPackets[plcNo] = new PlcPacket
            {
                PlcNo = plcNo,
                Values = values,
                Timestamp = DateTime.Now
            };

            Console.WriteLine($"Dashboard: Received {values.Count} tags from PLC {plcNo}");
        }

        private PLCTagConfigurationModel? GetTagConfig(int tagId)
        {
            return _algo.Tags.FirstOrDefault(t => t.Id == tagId);
        }


        public async Task<ResponsePackage> HandleUiRequest(RequestPackage request)
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
                    if (!_latestPackets.TryGetValue(1, out var packet))
                    {
                        return new ResponsePackage
                        {
                            ResponseId = 5,
                            Parameters = _lastValues
                        };
                    }

                    return new ResponsePackage
                    {
                        ResponseId = 5,
                        Parameters = packet.Values // Dictionary<uint, object>
                    };
                }

                //---------------------------------------------------------
                // 2) OEE REQUEST (RequestId = 4)
                //---------------------------------------------------------
                if (request.RequestId == 4)
                {
                    if (!_latestPackets.TryGetValue(1, out var packet))
                    {
                        return new ResponsePackage
                        {
                            ResponseId = 4,
                            Parameters = new Dictionary<int, object>()
                        };
                    }
                //    _oee.ProcessCycleTimeLogic(packet.Values);
                    return new ResponsePackage
                    {
                        ResponseId = 4,
                        Parameters =_oee.Calculate( packet.Values)
                    };
                }


                if (request.RequestId == 1)
                {
                    if (!_latestPackets.TryGetValue(1, out var packet))
                    {
                        return new ResponsePackage
                        {
                            ResponseId = 1,
                            Parameters = new Dictionary<int, object>()
                        };
                    }
                //    _oee.ProcessCycleTimeLogic(packet.Values);
                    return new ResponsePackage
                    {   
                        ResponseId = 1,
                        Parameters = _systemMonitor.Process(packet.Values)
                    };
                }




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
            // for PLC No = 1 (or use cfg.PLCNo)
            if (_latestPackets.TryGetValue(1, out var packet))
            {
                packet.Values[tagId] = value;
            }
        }

        private ResponsePackage Ok() =>
            new ResponsePackage { ResponseId = 6, Success = true };

        private ResponsePackage Error(string msg) =>
            new ResponsePackage { ResponseId = 6, Success = false, ErrorMessage = msg };


       


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
