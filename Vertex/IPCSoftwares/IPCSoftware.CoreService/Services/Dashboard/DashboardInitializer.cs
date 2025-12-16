using IPCSoftware.CoreService.Services.Algorithm;
using IPCSoftware.CoreService.Services.CCD;
using IPCSoftware.CoreService.Services.PLC;
using IPCSoftware.CoreService.Services.UI;
using IPCSoftware.Shared.Models.ConfigModels;
using IPCSoftware.Shared.Models.Messaging;
using Microsoft.Extensions.FileSystemGlobbing.Internal.PatternContexts;
using System.Diagnostics;
using System.Net.Sockets;
using System.Text.Json;


namespace IPCSoftware.CoreService.Services.Dashboard
{
    public class DashboardInitializer
    {
        private readonly PLCClientManager _manager;
        private readonly UiListener _ui = new UiListener(5050);
        private readonly AlgorithmAnalysisService _algo;
        private readonly OeeEngine _oee ;
        private readonly SystemMonitorService _systemMonitor;
        private readonly CCDTriggerService _ccdTrigger; // 1. Add field

        // latest packets per PLC (unitno)
        private readonly Dictionary<int, PlcPacket> _latestPackets = new();

        private Dictionary<int, object>? _lastValues = null;

        public DashboardInitializer(PLCClientManager manager,
            AlgorithmAnalysisService algo,
            OeeEngine oee,
            SystemMonitorService systemMonitor,
          
            CCDTriggerService ccdTrigger)
        {
            _systemMonitor = systemMonitor;
            _oee = oee;
            _manager = manager;
            _algo =algo;
            _ccdTrigger = ccdTrigger;
        }

      

        public async Task StartAsync()
        {
            _ui.OnRequestReceived = HandleUiRequest;

            // Start UI
            var uiTask = _ui.StartAsync();
            // Start PLC read loops
            var plcTasks = _manager.Clients.Select(client =>
            {
                client.OnPlcDataReceived += (plcNo, values) =>
                {
                    // A. Process Raw Data -> Typed Values (Int/Bool/String)
                    // processedData is Dictionary<int, object> where int is Tag ID
                    var processedData = _algo.Apply(plcNo, values);

                    // B. CHECK FOR TRIGGERS (This is where the magic happens)
                    // We call this immediately after processing values, but before updating UI
                  _ccdTrigger.ProcessTriggers(processedData, _manager);
                    _oee.ProcessCycleTimeLogic(processedData);
                    _oee.Calculate(processedData);
                    _systemMonitor.Process(processedData);

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
            await Task.WhenAll(plcTasks.Append(uiTask));
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
            Debug.WriteLine($"[Core] HandleUiRequest called → RequestId={request.RequestId}");

            //---------------------------------------------------------
            // 6) WRITE REQUEST (RequestId = 6)
            //---------------------------------------------------------
            if (request.RequestId == 6)
            {
                return await HandleUiWrite(request);
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

                await plc.WriteAsync(cfg, value);

                SetCachedValue(tagId, value);

                return Ok();
            }
            catch (Exception ex)
            {
                return Error(ex.Message);
            }
        }

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
}
