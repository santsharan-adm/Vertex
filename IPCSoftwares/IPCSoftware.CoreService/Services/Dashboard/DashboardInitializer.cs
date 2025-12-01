using IPCSoftware.Shared.Models.Messaging;
using IPCSoftware.CoreService.Services.PLC;
using IPCSoftware.CoreService.Services.UI;

namespace IPCSoftware.CoreService.Services.Dashboard
{




    public class DashboardInitializer
    {
        private readonly PlcClient _plc;
        private readonly UiListener _ui;

        private readonly OeeEngine _oee = new OeeEngine();
        private PlcPacket _lastPacket = new PlcPacket();

        // Store latest PLC data
        private Dictionary<uint, object> _lastPlcData = new();

        public DashboardInitializer()
        {
            _plc = new PlcClient("127.0.0.1", 502);
            _ui = new UiListener(5050);

            // Subscribe to PLC events
            _plc.OnPlcDataReceived = data =>
            {
                Console.WriteLine("--- PLC DATA RECEIVED ---");
                foreach (var kv in data)
                {
                    Console.WriteLine($"Register {kv.Key}: {kv.Value}");
                }

                // 2) OEE packet update 
                _lastPacket = ConvertToPacket(data);
            };
        }

        public void Start()
        {
            // Start PLC persistent connection
            _ = Task.Run(() => _plc.StartAsync());

            // Start UI listener
            _ui.OnRequestReceived = HandleUiRequest;
            _ = Task.Run(() => _ui.StartAsync());

        }

       

        private ResponsePackage HandleUiRequest(RequestPackage request)
        {
            if (request.RequestId != 4)
                return new ResponsePackage { ResponseId = -1 };

            var r = _oee.Calculate(_lastPacket);

            return new ResponsePackage
            {
                ResponseId = 4,
                Parameters = new Dictionary<uint, object>
        {
            { 1, Math.Round(r.Availability * 100, 2) },  // %
            { 2, Math.Round(r.Performance * 100, 2) },  // %
            { 3, Math.Round(r.Quality * 100, 2) },      // %
            { 4, Math.Round(r.OverallOEE * 100, 2) },   // %

            { 5, r.OperatingTime },
            { 6, r.Downtime },

            { 7, r.OKParts },
            { 8, r.NGParts },

            { 9, "Machine Running" }
        }
            };
        }


        private PlcPacket ConvertToPacket(Dictionary<uint, object> plc)
        {
            PlcPacket p = new PlcPacket();

            if (plc.TryGetValue(40001, out var op)) p.OperatingMin = Convert.ToInt32(op);
            if (plc.TryGetValue(40002, out var dt)) p.DownTimeMin = Convert.ToInt32(dt);
            if (plc.TryGetValue(40003, out var ac)) p.ActualCycle = Convert.ToInt32(ac);
            if (plc.TryGetValue(40004, out var ok)) p.OKParts = Convert.ToInt32(ok);
            if (plc.TryGetValue(40005, out var ng)) p.NGParts = Convert.ToInt32(ng);
            if (plc.TryGetValue(40006, out var tp)) p.TotalParts = Convert.ToInt32(tp);
            if (plc.TryGetValue(40007, out var ic)) p.IdealCycle = Convert.ToInt32(ic);

            return p;
        }

    }
}
