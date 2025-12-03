using IPCSoftware.Shared.Models.ConfigModels;
using NModbus;
using System.Net.Sockets;

namespace IPCSoftware.CoreService.Services.PLC
{
    public class PlcClient
    {
        public event Action<int, Dictionary<uint, object>>? OnPlcDataReceived;

        private readonly DeviceInterfaceModel _device;
        private readonly List<PLCTagConfigurationModel> _tags;

        private TcpClient? _tcp;
        private IModbusMaster? _master;

        public DeviceInterfaceModel Device => _device;

        public PlcClient(DeviceInterfaceModel device, List<PLCTagConfigurationModel> tags)
        {
            _device = device;
            _tags = tags;
        }

        // ---------------------------------------------------------
        // START POLLING ENGINE
        // ---------------------------------------------------------
        public async Task StartAsync()
        {
            await ConnectAsync();

            _ = Task.Run(async () =>
            {
                while (true)
                {
                    try
                    {
                        var data = await PollAllGroups();

                        // DEBUG PRINT — EXACT FORMAT YOU WANT
                        foreach (var kv in data)
                        {
                            Console.WriteLine($"PLC[{_device.DeviceName}] Addr {kv.Key} = {kv.Value}");
                        }

                        // Fire event for Dashboard
                        OnPlcDataReceived?.Invoke(_device.DeviceNo, data);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"PLC[{_device.DeviceName}] Poll ERROR: {ex.Message}");
                    }

                    await Task.Delay(1000);
                }
            });
        }


        // ---------------------------------------------------------
        // AUTO-RECONNECT
        // ---------------------------------------------------------
        private async Task ConnectAsync()
        {
            while (true)
            {
                try
                {
                    Console.WriteLine($"PLC[{_device.DeviceName}] → Connecting to {_device.IPAddress}:{_device.PortNo}");

                    _tcp = new TcpClient();
                    await _tcp.ConnectAsync(_device.IPAddress, _device.PortNo);

                    var factory = new ModbusFactory();
                    _master = factory.CreateMaster(_tcp);

                    Console.WriteLine($"PLC[{_device.DeviceName}] → CONNECTED");
                    return;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"PLC[{_device.DeviceName}] → CONNECT ERROR: {ex.Message}");
                    await Task.Delay(3000);
                }
            }
        }


        // ---------------------------------------------------------
        // POLL ALL TAG GROUPS
        // ---------------------------------------------------------
        private async Task<Dictionary<uint, object>> PollAllGroups()
        {
            var result = new Dictionary<uint, object>();

            try
            {
                if (_master == null || _tcp == null || !_tcp.Connected)
                {
                    await ConnectAsync();
                    return result;
                }

                // Group tags by ModbusAddress (int)
                var groups = _tags
                    .GroupBy(t => t.ModbusAddress)
                    .ToList();

                foreach (var g in groups)
                {
                    int baseAddress = g.Key;   // Key is already int

                    // Example: Modbus Holding Register 40001 → offset starts from 0
                    ushort start = (ushort)(baseAddress - 40001);

                    // Max length in case multi-register tags come later
                    ushort len = (ushort)g.Max(t => t.Length);

                    // Read the holding registers from PLC
                    ushort[] values = _master.ReadHoldingRegisters(1, start, len);

                    foreach (var tag in g)
                    {
                        uint address = (uint)tag.ModbusAddress;  // No parsing needed

                        // For now, using only first value (1-register tags)
                        ushort rawValue = values[0];

                        result[address] = rawValue;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"PLC[{_device.DeviceName}] → POLL ERROR: {ex.Message}");
                await Task.Delay(1000);
                await ConnectAsync();
            }

            return result;
        }


    }
}
