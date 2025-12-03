using IPCSoftware.Shared.Models.ConfigModels;
using IPCSoftware.Shared.Models.Messaging;
using IPCSoftware.CoreService.Services.PLC;
using IPCSoftware.CoreService.Services.UI;

namespace IPCSoftware.CoreService.Services.Dashboard
{
    public class DashboardInitializer
    {
        private readonly PLCClientManager _manager;
        private readonly UiListener _ui = new UiListener(5050);

        private readonly OeeEngine _oee = new OeeEngine();

        // latest packets per PLC (unitno)
        private readonly Dictionary<int, PlcPacket> _latestPackets = new();

        public DashboardInitializer(PLCClientManager manager)
        {
            _manager = manager;
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
                    // Store latest values
                    _latestPackets[plcNo] = new PlcPacket
                    {
                        PlcNo = plcNo,
                        Values = values,
                        Timestamp = DateTime.Now
                    };

                    // Debug print to verify flow
                    Console.WriteLine($"Dashboard: Received {values.Count} tags from PLC {plcNo}");

                    foreach (var kv in values)
                        Console.WriteLine($"  Tag[{kv.Key}] = {kv.Value}");
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
            if (request.RequestId != 4)
                return new ResponsePackage
                {
                    ResponseId = -1,
                    Parameters = new Dictionary<uint, object>()
                };

            if (!_latestPackets.TryGetValue(1, out var packet))
            {
                return new ResponsePackage
                {
                    ResponseId = 4,
                    Parameters = new Dictionary<uint, object>()
                };
            }

            // Already uint → no conversion needed
            var numericDict = packet.Values;

            return new ResponsePackage
            {
                ResponseId = 4,
                Parameters = numericDict
            };
        }



    }
}
