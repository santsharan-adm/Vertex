using IPCSoftware.CoreService.Services.Alarm;
using IPCSoftware.CoreService.Services.Algorithm;
using IPCSoftware.CoreService.Services.CCD;
using IPCSoftware.CoreService.Services.PLC;
using IPCSoftware.CoreService.Services.UI;
using IPCSoftware.Shared.Models.ConfigModels;
using IPCSoftware.Shared.Models.Messaging;
using System.Diagnostics;
using System.Text.Json;

namespace IPCSoftware.CoreService.Services.Dashboard
{
    public class DashboardInitializer
    {
        private readonly PLCClientManager _manager;

        // 🚨 FIX: Removed 'new UiListener(5050)'. We use the injected one.
        private readonly UiListener _ui;

        private readonly AlgorithmAnalysisService _algo;
        private readonly OeeEngine _oee = new OeeEngine();
        private readonly CCDTriggerService _ccdTrigger;
        private readonly AlarmService _alarmService;

        // Removed separate _publisher field as _ui acts as publisher now

        private readonly Dictionary<int, PlcPacket> _latestPackets = new();
        private Dictionary<uint, object>? _lastValues = null;

        // 🚨 FIX: Constructor now takes UiListener directly
        public DashboardInitializer(PLCClientManager manager,
                                    List<PLCTagConfigurationModel> tags,
                                    CCDTriggerService ccdTrigger,
                                    List<AlarmConfigurationModel> alarmDefinitions,
                                    UiListener uiListener)
        {
            _manager = manager;
            _algo = new AlgorithmAnalysisService(tags);
            _ccdTrigger = ccdTrigger;

            // Assign the injected listener
            _ui = uiListener;

            // Pass the listener (which implements IMessagePublisher) to AlarmService
            _alarmService = new AlarmService(alarmDefinitions, _ui);
        }


        public async Task StartAsync()
        {
            // 🚨 FIX: Hook up the request handler to the existing listener
            _ui.OnRequestReceived = HandleUiRequest;

            // 🚨 FIX: Removed '_ui.StartAsync()'. The Worker.cs already started it!

            // Start PLC read loops
            var plcTasks = _manager.Clients.Select(client =>
            {
                client.OnPlcDataReceived += (plcNo, values) =>
                {
                    var processedData = _algo.Apply(plcNo, values);
                    _ccdTrigger.ProcessTriggers(processedData, _manager);
                    _alarmService.ProcessTagData(processedData);

                    var final = processedData.ToDictionary(k => (uint)k.Key, v => v.Value);

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

            // Await PLC tasks only (UI is handled by Worker)
            await Task.WhenAll(plcTasks);
        }

        private void HandlePlcPacket(int plcNo, Dictionary<uint, object> values)
        {
            _latestPackets[plcNo] = new PlcPacket
            {
                PlcNo = plcNo,
                Values = values,
                Timestamp = DateTime.Now
            };
            Console.WriteLine($"Dashboard: Received {values.Count} tags from PLC {plcNo}");
        }

        private PLCTagConfigurationModel? GetTagConfig(uint tagId)
        {
            return _algo.Tags.FirstOrDefault(t => t.Id == tagId);
        }

        public async Task<ResponsePackage> HandleUiRequest(RequestPackage request)
        {
            Debug.WriteLine($"[Core] HandleUiRequest called → RequestId={request.RequestId}");

            if (request.RequestId == 6) return await HandleUiWrite(request);
            if (request.RequestId == 7) return await HandleAlarmRequest(request);

            if (request.RequestId == 5)
            {
                if (!_latestPackets.TryGetValue(1, out var packet))
                {
                    return new ResponsePackage { ResponseId = 5, Parameters = _lastValues };
                }
                return new ResponsePackage { ResponseId = 5, Parameters = packet.Values };
            }

            if (request.RequestId == 4)
            {
                if (!_latestPackets.TryGetValue(1, out var packet))
                {
                    return new ResponsePackage { ResponseId = 4, Parameters = new Dictionary<uint, object>() };
                }
                return new ResponsePackage { ResponseId = 4, Parameters = packet.Values };
            }

            return new ResponsePackage { ResponseId = -1, Parameters = new Dictionary<uint, object>() };
        }

        private async Task<ResponsePackage> HandleUiWrite(RequestPackage request)
        {
            try
            {
                uint tagId = 0;
                object value = null;

                if (request.Parameters is JsonElement json)
                {
                    foreach (var prop in json.EnumerateObject())
                    {
                        tagId = uint.Parse(prop.Name);
                        value = prop.Value.ValueKind switch
                        {
                            JsonValueKind.True => true,
                            JsonValueKind.False => false,
                            JsonValueKind.String => prop.Value.GetString(),
                            JsonValueKind.Number => prop.Value.TryGetInt32(out int intVal) ? intVal : prop.Value.GetDouble(),
                            _ => prop.Value.ToString()
                        };
                    }
                }

                var cfg = GetTagConfig(tagId);
                if (cfg == null) return Error($"Tag {tagId} not found");

                var plc = _manager.GetClient(cfg.PLCNo);
                if (plc == null) return Error($"PLC {cfg.PLCNo} not connected");

                await plc.WriteAsync(cfg, value);
                SetCachedValue(tagId, value);

                return Ok();
            }
            catch (Exception ex) { return Error(ex.Message); }
        }

        private async Task<ResponsePackage> HandleAlarmRequest(RequestPackage request)
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
                catch (Exception ex) { return ErrorAlarm($"Error processing alarm request: {ex.Message}"); }
            }
            return ErrorAlarm("Invalid alarm request parameters.");
        }

        private ResponsePackage OkAlarm(int alarmNo) =>
            new ResponsePackage
            {
                ResponseId = 7,
                Success = true,
                Parameters = new Dictionary<uint, object> { { 0, $"ACKNOWLEDGED:{alarmNo}" } }
            };

        private ResponsePackage ErrorAlarm(string msg) =>
            new ResponsePackage { ResponseId = 7, Success = false, ErrorMessage = msg, Parameters = null };

        private void SetCachedValue(uint tagId, object value)
        {
            if (_latestPackets.TryGetValue(1, out var packet))
            {
                packet.Values[tagId] = value;
            }
        }

        private ResponsePackage Ok() => new ResponsePackage { ResponseId = 6, Success = true };
        private ResponsePackage Error(string msg) => new ResponsePackage { ResponseId = 6, Success = false, ErrorMessage = msg };
    }
}