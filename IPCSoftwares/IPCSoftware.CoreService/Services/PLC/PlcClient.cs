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
                    ushort startOffset = (ushort)(baseAddress - 40000);
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

        public async Task WriteAsync(PLCTagConfigurationModel cfg, object value)
        {
            if (!IsConnected)
                throw new InvalidOperationException($"Cannot write to PLC {_device.DeviceName}: Connection is unavailable.");

            try
            {
                ushort start = (ushort)(cfg.ModbusAddress - 40000);

                // Convert value to registers based on DataType
                ushort[] registers = ConvertValueToRegisters(value, cfg);

                // Write to PLC
                if (cfg.DataType == DataType_Bit)
                {
                    // Special case: Bit manipulation
                    ushort[] existingRegs = await _master!.ReadHoldingRegistersAsync(1, start, 1);
                    ushort regValue = existingRegs[0];
                    ushort mask = (ushort)(1 << cfg.BitNo);

                    bool boolValue = Convert.ToBoolean(value);
                    if (boolValue)
                        regValue = (ushort)(regValue | mask);
                    else
                        regValue = (ushort)(regValue & ~mask);

                    await _master.WriteSingleRegisterAsync(1, start, regValue);
                    Console.WriteLine($"PLC[{_device.DeviceName}] [WRITE] Addr={cfg.ModbusAddress} Bit={cfg.BitNo} → {boolValue} SUCCESS");
                }
                else if (registers.Length == 1)
                {
                    // Single register write
                    await _master!.WriteSingleRegisterAsync(1, start, registers[0]);
                    Console.WriteLine($"PLC[{_device.DeviceName}] [WRITE] Addr={cfg.ModbusAddress} → {value} SUCCESS");
                }
                else
                {
                    // Multiple register write
                    await _master!.WriteMultipleRegistersAsync(1, start, registers);
                    Console.WriteLine($"PLC[{_device.DeviceName}] [WRITE] Addr={cfg.ModbusAddress} → {value} SUCCESS");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"PLC[{_device.DeviceName}] [WRITE ERROR] Failed to write Tag {cfg.Name}. Message: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Converts a value to Modbus registers based on the tag's DataType.
        /// This is the REVERSE of ConvertData() - it prepares data for writing to PLC.
        /// </summary>
        private ushort[] ConvertValueToRegisters(object value, PLCTagConfigurationModel tag)
        {
            try
            {
                byte[] byteArray;
                ushort[] registers;

                switch (tag.DataType)
                {
                    case DataType_Bit:
                        // Bit writes are handled separately in WriteAsync
                        // Return dummy register - actual bit manipulation happens in WriteAsync
                        bool boolVal = Convert.ToBoolean(value);
                        return new ushort[] { (ushort)(boolVal ? 1 : 0) };

                    case DataType_String:
                        // Convert string to ASCII bytes
                        string strValue = value?.ToString() ?? "";
                        byteArray = Encoding.ASCII.GetBytes(strValue);

                        // Pad to required length (2 bytes per register)
                        int requiredBytes = tag.Length * 2;
                        if (byteArray.Length < requiredBytes)
                        {
                            byte[] padded = new byte[requiredBytes];
                            Array.Copy(byteArray, padded, byteArray.Length);
                            byteArray = padded;
                        }
                        else if (byteArray.Length > requiredBytes)
                        {
                            Array.Resize(ref byteArray, requiredBytes);
                        }

                        // Convert bytes to registers
                        registers = new ushort[tag.Length];
                        for (int i = 0; i < tag.Length; i++)
                        {
                            // BitConverter.ToUInt16 reads Little Endian from byte array
                            registers[i] = BitConverter.ToUInt16(byteArray, i * 2);
                        }
                        return registers;

                    case DataType_Int16:
                        // 16-bit signed integer
                        short int16Value = Convert.ToInt16(value);
                        byteArray = BitConverter.GetBytes(int16Value);
                        return new ushort[] { BitConverter.ToUInt16(byteArray, 0) };
                    case DataType_UInt16:
                        // 16-bit signed integer
                        ushort uint16Value = Convert.ToUInt16(value);
                        byteArray = BitConverter.GetBytes(uint16Value);
                        return new ushort[] { BitConverter.ToUInt16(byteArray, 0) };

                    case DataType_UInt16:
                        // 16-bit signed integer
                        ushort uint16Value = Convert.ToUInt16(value);
                        byteArray = BitConverter.GetBytes(uint16Value);
                        return new ushort[] { BitConverter.ToUInt16(byteArray, 0) };


                    case DataType_Word32:
                        // 32-bit integer (2 registers)
                        int int32Value = Convert.ToInt32(value);
                        byteArray = BitConverter.GetBytes(int32Value);

                        // Convert to 2 registers
                        registers = new ushort[2];
                        registers[0] = BitConverter.ToUInt16(byteArray, 0); // Low word
                        registers[1] = BitConverter.ToUInt16(byteArray, 2); // High word

                        // CRITICAL: Swap words for Big Endian Modbus
                        // This is the REVERSE of the swap in ConvertData()
                        //ushort temp = registers[0];
                        //registers[0] = registers[1];
                        //registers[1] = temp;

                        return registers;
                    case DataType_UInt32:
                        // 32-bit integer (2 registers)
                        uint uint32Value = Convert.ToUInt32(value);
                        byteArray = BitConverter.GetBytes(uint32Value);

                        // Convert to 2 registers
                        registers = new ushort[2];
                        registers[0] = BitConverter.ToUInt16(byteArray, 0); // Low word
                        registers[1] = BitConverter.ToUInt16(byteArray, 2); // High word

                        // CRITICAL: Swap words for Big Endian Modbus
                        // This is the REVERSE of the swap in ConvertData()
                        //ushort temp2 = registers[0];
                        //registers[0] = registers[1];
                        //registers[1] = temp2;

                        return registers;

                    case DataType_FP:
                        // 32-bit floating point (2 registers)
                        float floatValue = Convert.ToSingle(value);
                        byteArray = BitConverter.GetBytes(floatValue);

                        // Convert to 2 registers
                        registers = new ushort[2];
                        registers[0] = BitConverter.ToUInt16(byteArray, 0); // Low word
                        registers[1] = BitConverter.ToUInt16(byteArray, 2); // High word

                        // CRITICAL: Swap words for Big Endian Modbus
                        // This is the REVERSE of the swap in ConvertData()
                        //temp = registers[0];
                        //registers[0] = registers[1];
                        //registers[1] = temp;

                        return registers;

                    default:
                        // Default: treat as UInt16
                        ushort uint16Value2 = Convert.ToUInt16(value);
                        return new ushort[] { uint16Value2 };
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[WRITE_ERROR] Failed to convert value for Tag {tag.Name} (Type {tag.DataType}): {ex.Message}");
                throw;
            }
        }

        // DataType constants (add these to match your ConvertData logic)
        private const int DataType_Bit = 3;
        private const int DataType_String = 5;
        private const int DataType_Int16 = 1;
        private const int DataType_Word32 = 2;
        private const int DataType_FP = 4;
        private const int DataType_UInt16 = 6;
        private const int DataType_UInt32 = 7;
    }
}