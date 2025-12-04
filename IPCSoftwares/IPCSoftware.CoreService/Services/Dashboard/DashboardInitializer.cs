using IPCSoftware.Shared.Models.ConfigModels;
using IPCSoftware.CoreService.Services.Algorithm;
using IPCSoftware.Shared.Models.Messaging;
using IPCSoftware.CoreService.Services.PLC;
using IPCSoftware.CoreService.Services.UI;
using System.Diagnostics;

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

        public DashboardInitializer(PLCClientManager manager, List<PLCTagConfigurationModel> tags)
        {
            _manager = manager;
            _algo = new AlgorithmAnalysisService(tags);
        }

        public void Start()
        {
            // UI listener continues working
            _ui.OnRequestReceived = HandleUiRequest;
            _ = Task.Run(() => _ui.StartAsync());

            Console.WriteLine("DashboardInitializer: Waiting for PLC data (new Modbus engine pending).");

            foreach (var client in _manager.Clients)
            {
                client.OnPlcDataReceived += (plcNo, values) =>
                {
                    // 1) Run algorithm: get TagId → final value
                    var finalById = _algo.Apply(plcNo, values);

                    // 2) Convert int TagId → uint key because PlcPacket.Values is Dictionary<uint,object>
                    var finalDict = finalById.ToDictionary(
                        kv => (uint)kv.Key,
                        kv => kv.Value);

                    // 3) Store latest processed packet
                    _latestPackets[plcNo] = new PlcPacket
                    {
                        PlcNo = plcNo,
                        Values = finalDict,
                        Timestamp = DateTime.Now
                    };

                    // Debug log (now TagId-based)
                    Console.WriteLine($"Dashboard: Received {finalDict.Count} tags from PLC {plcNo}");
                    foreach (var kv in finalDict)
                        Console.WriteLine($"  TagId[{kv.Key}] = {kv.Value}");
                };

                _ = client.StartAsync();
            }

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


        public ResponsePackage HandleUiRequest(RequestPackage request)
        {
            Debug.WriteLine($"[Core] HandleUiRequest called → RequestId={request.RequestId}");

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
                        Parameters = new Dictionary<uint, object>()
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




    }
}
