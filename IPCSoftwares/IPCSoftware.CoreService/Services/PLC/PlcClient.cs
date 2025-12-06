using IPCSoftware.Shared.Models.ConfigModels;
using NModbus;
using System.Linq;
using System.Net; // Added for IPAddress
using System.Net.Sockets;
using System.Text;
using System.Threading; // Added for Interlocked
using System.Threading.Tasks;

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

                // Use cfg.DataType to determine how to write
                switch (cfg.DataType)
                {
                    case 1: // Int16 (-32768 to 32767)
                        short int16Value = ConvertToInt16(value);
                        await _master!.WriteSingleRegisterAsync(1, start, (ushort)int16Value);
                        Console.WriteLine($"PLC[{_device.DeviceName}] [WRITE] Addr={cfg.ModbusAddress} → {int16Value} (Int16) SUCCESS");
                        break;

                    case 2: // UInt16/Integer (0 to 65535) or DWord/UInt32
                        if (cfg.Length == 1)
                        {
                            // Single register - UInt16
                            ushort uint16Value = ConvertToUInt16(value);
                            await _master!.WriteSingleRegisterAsync(1, start, uint16Value);
                            Console.WriteLine($"PLC[{_device.DeviceName}] [WRITE] Addr={cfg.ModbusAddress} → {uint16Value} (UInt16) SUCCESS");
                        }
                        else if (cfg.Length == 2)
                        {
                            // Double register - UInt32 or Int32
                            uint uint32Value = ConvertToUInt32(value);
                            ushort[] dwordRegs = UInt32ToRegisters(uint32Value);
                            await _master!.WriteMultipleRegistersAsync(1, start, dwordRegs);
                            Console.WriteLine($"PLC[{_device.DeviceName}] [WRITE] Addr={cfg.ModbusAddress} → {uint32Value} (UInt32) SUCCESS");
                        }
                        break;

                    case 3: // Boolean (bit manipulation)
                        bool boolValue = ConvertToBool(value);
                        ushort[] regs = await _master!.ReadHoldingRegistersAsync(1, start, 1);
                        ushort regValue = regs[0];
                        ushort mask = (ushort)(1 << cfg.BitNo);

                        if (boolValue)
                            regValue = (ushort)(regValue | mask);
                        else
                            regValue = (ushort)(regValue & ~mask);

                        await _master.WriteSingleRegisterAsync(1, start, regValue);
                        Console.WriteLine($"PLC[{_device.DeviceName}] [WRITE] Addr={cfg.ModbusAddress} Bit={cfg.BitNo} → {boolValue} SUCCESS");
                        break;

                    case 4: // Float (Real - IEEE 754)
                        float floatValue = ConvertToFloat(value);
                        ushort[] floatRegs = FloatToRegisters(floatValue);
                        await _master!.WriteMultipleRegistersAsync(1, start, floatRegs);
                        Console.WriteLine($"PLC[{_device.DeviceName}] [WRITE] Addr={cfg.ModbusAddress} → {floatValue} (Float) SUCCESS");
                        break;

                    case 5: // String
                        string stringValue = value?.ToString() ?? "";
                        var stringRegs = StringToRegisters(stringValue, cfg.Length);
                        await _master!.WriteMultipleRegistersAsync(1, start, stringRegs);
                        Console.WriteLine($"PLC[{_device.DeviceName}] [WRITE] Addr={cfg.ModbusAddress} → \"{stringValue}\" (String) SUCCESS");
                        break;

                    default:
                        throw new ArgumentException($"Unsupported DataType: {cfg.DataType}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"PLC[{_device.DeviceName}] [WRITE ERROR] Failed to write Tag {cfg.Name}. Message: {ex.Message}");
                throw;
            }
        }

        // Conversion helpers
        private short ConvertToInt16(object value)
        {
            return value switch
            {
                short s => s,
                int i => (short)i,
                long l => (short)l,
                ushort us => (short)us,
                uint ui => (short)ui,
                float f => (short)f,
                double d => (short)d,
                string str => short.Parse(str),
                _ => Convert.ToInt16(value)
            };
        }

        private ushort ConvertToUInt16(object value)
        {
            return value switch
            {
                ushort us => us,
                short s => (ushort)s,
                int i => (ushort)i,
                long l => (ushort)l,
                uint ui => (ushort)ui,
                float f => (ushort)f,
                double d => (ushort)d,
                string str => ushort.Parse(str),
                _ => Convert.ToUInt16(value)
            };
        }

        private uint ConvertToUInt32(object value)
        {
            return value switch
            {
                uint ui => ui,
                int i => (uint)i,
                long l => (uint)l,
                ushort us => us,
                short s => (uint)s,
                float f => (uint)f,
                double d => (uint)d,
                string str => uint.Parse(str),
                _ => Convert.ToUInt32(value)
            };
        }

        private bool ConvertToBool(object value)
        {
            return value switch
            {
                bool b => b,
                int i => i != 0,
                long l => l != 0,
                string str => bool.Parse(str),
                _ => Convert.ToBoolean(value)
            };
        }

        private float ConvertToFloat(object value)
        {
            return value switch
            {
                float f => f,
                double d => (float)d,
                int i => (float)i,
                long l => (float)l,
                string str => float.Parse(str),
                _ => Convert.ToSingle(value)
            };
        }

        // Register conversion methods
        private ushort[] UInt32ToRegisters(uint value)
        {
            // Split 32-bit into two 16-bit registers
            // High word first, then low word (Big Endian)
            return new ushort[]
            {
        (ushort)((value >> 16) & 0xFFFF), // High word
        (ushort)(value & 0xFFFF)           // Low word
            };
        }

        private ushort[] FloatToRegisters(float value)
        {
            // Convert float to IEEE 754 format (4 bytes = 2 registers)
            byte[] bytes = BitConverter.GetBytes(value);

            // Arrange bytes into registers (check your PLC's byte order)
            return new ushort[]
            {
        (ushort)((bytes[1] << 8) | bytes[0]), // First register
        (ushort)((bytes[3] << 8) | bytes[2])  // Second register
            };
        }

        private ushort[] StringToRegisters(string text, int maxLength)
        {
            // Convert string to bytes
            byte[] stringBytes = Encoding.UTF8.GetBytes(text);

            // Pad or truncate to exact length needed (2 bytes per register)
            byte[] paddedBytes = new byte[maxLength * 2];
            Array.Copy(stringBytes, 0, paddedBytes, 0, Math.Min(stringBytes.Length, paddedBytes.Length));

            var registers = new ushort[maxLength];

            for (int i = 0; i < maxLength; i++)
            {
                int byteIndex = i * 2;
                byte byte1 = paddedBytes[byteIndex];
                byte byte2 = paddedBytes[byteIndex + 1];

                // FIXED: Correct byte order to match reading
                // Try LOW-HIGH order first (most common for strings)
                registers[i] = (ushort)((byte2 << 8) | byte1);

                // If still scrambled, swap to:
                // registers[i] = (ushort)((byte1 << 8) | byte2);
            }

            return registers;
        }

    }
}