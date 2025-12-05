using IPCSoftware.CoreService.Services.Algorithm;
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
        private readonly UiListener _ui = new UiListener(5050);
        private readonly AlgorithmAnalysisService _algo;
        private readonly OeeEngine _oee = new OeeEngine();

        // latest packets per PLC (unitno)
        private readonly Dictionary<int, PlcPacket> _latestPackets = new();

        private Dictionary<uint, object>? _lastValues = null;

        public DashboardInitializer(PLCClientManager manager, List<PLCTagConfigurationModel> tags)
        {
            _manager = manager;
            _algo = new AlgorithmAnalysisService(tags);
        }

        //public async Task StartAsync()
        //{
        //    _ui.OnRequestReceived = HandleUiRequest;

        //    // Start PLC loops
        //    List<Task> tasks = new();

        //    foreach (var client in _manager.Clients)
        //    {
        //        client.OnPlcDataReceived += (plcNo, values) =>
        //        {
        //            var final = _algo.Apply(plcNo, values)
        //                             .ToDictionary(k => (uint)k.Key, v => v.Value);

        //            _latestPackets[plcNo] = new PlcPacket
        //            {
        //                PlcNo = plcNo,
        //                Values = final,
        //                Timestamp = DateTime.Now
        //            };
        //        };

        //        tasks.Add(client.StartAsync());
        //    }

        //    tasks.Add(_ui.StartAsync());

        //    await Task.WhenAll(tasks);
        //}

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
                    var final = _algo.Apply(plcNo, values)
                                     .ToDictionary(k => (uint)k.Key, v => v.Value);

                    var finalDict = final.ToDictionary(kv => (uint)kv.Key, kv => kv.Value);

                    _latestPackets[plcNo] = new PlcPacket
                    {
                        PlcNo = plcNo,
                        Values = final,
                        Timestamp = DateTime.Now
                    };

                    _lastValues = finalDict;
                };
                return client.StartAsync();
            });

            // Await everything
            await Task.WhenAll(plcTasks.Append(uiTask));
        }


        private void HandlePlcPacket(int plcNo, Dictionary<uint, object> values)
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

        private PLCTagConfigurationModel? GetTagConfig(uint tagId)
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
                        Parameters = new Dictionary<uint, object>()
                    };
                }

                return new ResponsePackage
                {
                    ResponseId = 4,
                    Parameters = packet.Values
                };
            }

            //---------------------------------------------------------
            // 3) UNKNOWN REQUEST
            //---------------------------------------------------------
            return new ResponsePackage
            {
                ResponseId = -1,
                Parameters = new Dictionary<uint, object>()
            };
        }

        private async Task<ResponsePackage> HandleUiWrite(RequestPackage request)
        {
            try
            {
                uint tagId = 0;
                bool value = false;

                if (request.Parameters is JsonElement json)
                {
                    foreach (var prop in json.EnumerateObject())
                    {
                        tagId = uint.Parse(prop.Name);
                        value = prop.Value.GetBoolean();
                    }
                }

                var cfg = GetTagConfig(tagId);
                if (cfg == null)
                    return Error($"Tag {tagId} not found");

                var plc = _manager.GetClient(cfg.PLCNo);
                if (plc == null)
                    return Error($"PLC {cfg.PLCNo} not connected");

                await plc.WriteAsync(cfg, value);

                return Ok();
            }
            catch (Exception ex)
            {
                return Error(ex.Message);
            }
        }


        private ResponsePackage Ok() =>
            new ResponsePackage { ResponseId = 6, Success = true };

        private ResponsePackage Error(string msg) =>
            new ResponsePackage { ResponseId = 6, Success = false, ErrorMessage = msg };














    }
}
