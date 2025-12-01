using NModbus;
using System.Net.Sockets;

namespace IPCSoftware.CoreService.Services.PLC
{
    public class PlcClient
    {
        private readonly string _ip;
        private readonly int _port;

        private TcpClient? _tcp;
        private IModbusMaster? _master;

        // Callback: DashboardInitializer will subscribe to this
        public Action<Dictionary<uint, object>>? OnPlcDataReceived;

        public PlcClient(string ip, int port)
        {
            _ip = ip;
            _port = port;
        }

        public async Task StartAsync()
        {
            await ConnectAsync();

            // Start auto-polling (background)
            _ = Task.Run(async () =>
            {
                while (true)
                {
                    await PollRegistersAsync();
                    await Task.Delay(1000); //later we will make it dynamic
                }
            });
        }

        
        private async Task ConnectAsync()
        {
            while (true)
            {
                try
                {
                    Console.WriteLine($"PLC: Attempting to connect to {_ip}:{_port}...");

                    _tcp?.Dispose();
                    _tcp = new TcpClient();
                    await _tcp.ConnectAsync(_ip, _port);

                    var factory = new ModbusFactory();
                    _master = factory.CreateMaster(_tcp);

                    Console.WriteLine("PLC CONNECTED successfully.");
                    return;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"PLC CONNECTION ERROR: {ex.Message}. Retrying in 5 seconds...");
                    _tcp?.Dispose();
                    _tcp = null;
                    _master = null;

                    await Task.Delay(5000);
                }
            }
        }

       
        private async Task PollRegistersAsync()
        {
            try
            {
                if (_master == null || _tcp == null || !_tcp.Connected)
                {
                    await ConnectAsync();
                    return;
                }

                // MODBUS parameters
                byte slaveId = 1;
                ushort startAddress = 0;   // 40001
                ushort numRegisters = 12;  // read 12 holding registers--> later fetch from config

                // NModbus READ FC03
                ushort[] values = await Task.Run(() =>
                    _master.ReadHoldingRegisters(slaveId, startAddress, numRegisters)
                );

                // Convert array → dictionary
                var dict = new Dictionary<uint, object>();

                for (uint i = 0; i < values.Length; i++)
                    dict[40001 + i] = values[i];

                // Fire callback
                OnPlcDataReceived?.Invoke(dict);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"PLC Poll ERROR: {ex.Message}. Attempting reconnection...");
                _tcp?.Dispose();
                _tcp = null;
                _master = null;

                await Task.Delay(2000);
            }
        }
    }
}
