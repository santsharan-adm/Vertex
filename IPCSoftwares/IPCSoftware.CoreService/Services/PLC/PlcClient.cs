using IPCSoftware.Shared.Models.ConfigModels;
using NModbus;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Linq;
using System.Threading; // Added for Interlocked
using System.Net; // Added for IPAddress

namespace IPCSoftware.CoreService.Services.PLC
{
    public class PlcClient
    {
        // Event to notify the DashboardInitializer when new, raw data registers are ready.
        public event Action<int, Dictionary<uint, object>>? OnPlcDataReceived;

        private readonly DeviceInterfaceModel _device;
        private List<PLCTagConfigurationModel> _tags; // 'readonly' removed for dynamic update

        private TcpClient? _tcp;
        private IModbusMaster? _master;

        // Flag to track if the connection is currently considered active and stable.
        public bool IsConnected => _tcp != null && _tcp.Connected;
        public DeviceInterfaceModel Device => _device;

        // Define a standard timeout value for use in ConnectAsync
        private const int ConnectionTimeoutMs = 5000;

        public PlcClient(DeviceInterfaceModel device, List<PLCTagConfigurationModel> tags)
        {
            _device = device;
            _tags = tags;
        }

        // --- Graceful Disconnection Logic ---
        private void Disconnect()
        {
            if (_master != null) { _master.Dispose(); _master = null; }
            if (_tcp != null)
            {
                // Ensure proper socket shutdown
                _tcp.Close();
                _tcp.Dispose();
                _tcp = null;
            }
            Console.WriteLine($"PLC[{_device.DeviceName}] [WARN] → DISCONNECTED. State Reset.");
        }


        // ---------------------------------------------------------
        // START POLLING ENGINE (Robust Execution)
        // ---------------------------------------------------------
        public Task StartAsync()
        {
            return Task.Run(async () =>
            {
                Console.WriteLine($"PLC[{_device.DeviceName}] [INFO] Polling Task Starting."); // NEW DEBUG LOG

                while (true) // This loop runs indefinitely
                {
                    try
                    {
                        if (!IsConnected)
                        {
                            await ConnectAsync();
                            await Task.Delay(500); // Brief stabilization delay
                            continue;
                        }

                        // Poll only if tags are configured
                        if (_tags.Any())
                        {
                            var data = await PollAllGroups();
                            OnPlcDataReceived?.Invoke(_device.DeviceNo, data);
                            // NEW DEBUG LOG: Confirming data dispatched
                            Console.WriteLine($"PLC[{_device.DeviceName}] [DATA] Polling successful. Dispatched {data.Count} raw register groups.");
                        }
                    }
                    catch (Exception ex)
                    {
                        // CATCH-ALL for communication issues or polling exceptions
                        Console.WriteLine($"PLC[{_device.DeviceName}] [ERROR] Polling Cycle FAILED. Retrying in 3s. Exception: {ex.Message}");
                        Disconnect(); // Ensures clean retry in next loop

                        // NEW DEBUG LOG: Logging stack trace for silent failure investigation
                        // Console.WriteLine($"PLC[{_device.DeviceName}] [DEBUG] Stack Trace: {ex.StackTrace}"); 

                        await Task.Delay(3000); // Wait 3 seconds before next connection attempt
                    }

                    // Polling rate delay 
                    await Task.Delay(1000);
                }
            });
        }


        // ---------------------------------------------------------
        // AUTO-RECONNECT (Optimized and Robust)
        // ---------------------------------------------------------
        private async Task ConnectAsync()
        {
            if (IsConnected) return;

            while (true)
            {
                try
                {
                    Console.WriteLine($"PLC[{_device.DeviceName}] [ATTEMPT] → Attempting connection to {_device.IPAddress}:{_device.PortNo}");

                    _tcp = new TcpClient();

                    // Implementing connection timeout using WhenAny
                    var connectTask = _tcp.ConnectAsync(_device.IPAddress, _device.PortNo);

                    if (await Task.WhenAny(connectTask, Task.Delay(ConnectionTimeoutMs)) != connectTask)
                    {
                        throw new TimeoutException($"Connection attempt timed out after {ConnectionTimeoutMs}ms.");
                    }

                    await connectTask; // Await the connection task to propagate exceptions

                    var factory = new ModbusFactory();
                    _master = factory.CreateMaster(_tcp);

                    Console.WriteLine($"PLC[{_device.DeviceName}] [SUCCESS] → CONNECTED.");
                    return;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"PLC[{_device.DeviceName}] [ERROR] → CONNECT ERROR. Retrying in 3s. Message: {ex.Message}");

                    Disconnect();

                    await Task.Delay(3000);
                }
            }
        }


        // ---------------------------------------------------------
        // POLL ALL TAG GROUPS (Fixed for Multi-Register Reading)
        // ---------------------------------------------------------
        private async Task<Dictionary<uint, object>> PollAllGroups()
        {
            var result = new Dictionary<uint, object>();

            try
            {
                var groups = _tags
                    .GroupBy(t => t.ModbusAddress)
                    .ToList();

                foreach (var g in groups)
                {
                    int baseAddress = g.Key;
                    ushort startOffset = (ushort)(baseAddress - 40001);
                    ushort maxLength = (ushort)g.Max(t => t.Length);

                    // Read the holding registers from the PLC.
                    ushort[] rawRegisters = await _master.ReadHoldingRegistersAsync(1, startOffset, maxLength);

                    // Pass the raw register block for the start address.
                    result[(uint)baseAddress] = rawRegisters;

                    // NEW DEBUG LOG: Confirming successful Modbus read
                    Console.WriteLine($"PLC[{_device.DeviceName}] [DEBUG] Read Addr {baseAddress}, Len {maxLength} successful.");
                }
            }
            catch (Exception ex)
            {
                // This exception will be caught by the outer StartAsync loop's CATCH-ALL.
                Console.WriteLine($"PLC[{_device.DeviceName}] [CRITICAL] Modbus Read Failed! Message: {ex.Message}");
                // Rethrow to break the polling cycle and force a Disconnect/Reconnect sequence
                throw;
            }

            return result;
        }

        public void UpdateTags(List<PLCTagConfigurationModel> allNewTags)
        {
            // Filter the new comprehensive list to only include tags relevant to this PLC
            var myNewTags = allNewTags
                .Where(t => t.PLCNo == _device.DeviceNo)
                .ToList();

            // Thread-safe replacement of the internal list
            Interlocked.Exchange(ref _tags, myNewTags);
            Console.WriteLine($"PLCClient[{_device.DeviceName}] [INFO] Tags updated to {myNewTags.Count} tags.");
        }

        // ---------------------------------------------------------
        // WRITE LOGIC (Required for Bit/Control operations)
        // ---------------------------------------------------------
        public async Task WriteAsync(PLCTagConfigurationModel cfg, object value)
        {
            if (!IsConnected)
                throw new InvalidOperationException($"Cannot write to PLC {_device.DeviceName}: Connection is unavailable.");

            try
            {
                ushort start = (ushort)(cfg.ModbusAddress - 40001);

                switch (value)
                {
                    case bool boolVal:
                        // Bit manipulation for boolean
                        ushort[] regs = await _master!.ReadHoldingRegistersAsync(1, start, 1);
                        ushort regValue = regs[0];
                        ushort mask = (ushort)(1 << cfg.BitNo);

                        if (boolVal)
                            regValue = (ushort)(regValue | mask);
                        else
                            regValue = (ushort)(regValue & ~mask);

                        await _master.WriteSingleRegisterAsync(1, start, regValue);
                        Console.WriteLine($"PLC[{_device.DeviceName}] [WRITE] Addr={cfg.ModbusAddress} Bit={cfg.BitNo} → {boolVal} SUCCESS");
                        break;

                    case int intVal:
                    case ushort ushortVal:
                        // Direct register write for numbers
                        ushort numValue = value is int i ? (ushort)i : (ushort)value;
                        await _master!.WriteSingleRegisterAsync(1, start, numValue);
                        Console.WriteLine($"PLC[{_device.DeviceName}] [WRITE] Addr={cfg.ModbusAddress} → {numValue} SUCCESS");
                        break;

                    case string strVal:
                        // Multiple register write for strings
                        var registers = StringToRegisters(strVal, cfg.Length);
                        await _master!.WriteMultipleRegistersAsync(1, start, registers);
                        Console.WriteLine($"PLC[{_device.DeviceName}] [WRITE] Addr={cfg.ModbusAddress} → \"{strVal}\" SUCCESS");
                        break;

                    case double doubleVal:
                        // Convert double to ushort (you might want more sophisticated conversion)
                        await _master!.WriteSingleRegisterAsync(1, start, (ushort)doubleVal);
                        Console.WriteLine($"PLC[{_device.DeviceName}] [WRITE] Addr={cfg.ModbusAddress} → {doubleVal} SUCCESS");
                        break;

                    default:
                        throw new ArgumentException($"Unsupported value type: {value.GetType().Name}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"PLC[{_device.DeviceName}] [WRITE ERROR] Failed to write Tag {cfg.Name}. Message: {ex.Message}");
                throw;
            }
        }

        private ushort[] StringToRegisters(string text, int maxLength)
        {
            text = text.PadRight(maxLength * 2, '\0').Substring(0, maxLength * 2);
            var registers = new ushort[maxLength];

            for (int i = 0; i < maxLength; i++)
            {
                byte highByte = (byte)text[i * 2];
                byte lowByte = (byte)text[i * 2 + 1];
                registers[i] = (ushort)((highByte << 8) | lowByte);
            }

            return registers;
        }
    }
}