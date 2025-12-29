using IPCSoftware.Core.Interfaces.AppLoggerInterface;
using IPCSoftware.Services;
using IPCSoftware.Shared.Models;
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
        private readonly bool _swapBytes; // Configurable Flag
        private readonly int _modbusAddress; // Configurable 
        private readonly IAppLogger _logger;


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

        public PlcClient(
            DeviceInterfaceModel device,
            List<PLCTagConfigurationModel> tags,
            ConfigSettings config,
            IAppLogger logger)
        {
            _logger = logger;
            _device = device;
            _tags = tags;
            _swapBytes = config.SwapBytes;
            _modbusAddress = config.DefaultModBusAddress;
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
            _logger.LogWarning($"PLC[{_device.DeviceName}] [WARN] → DISCONNECTED. State Reset.", LogType.Diagnostics);
        }


        // ---------------------------------------------------------
        // START POLLING ENGINE (Robust Execution)
        // ---------------------------------------------------------
        public Task StartAsync()
        {
            return Task.Run(async () =>
            {
                Console.WriteLine($"PLC[{_device.DeviceName}] [INFO] Polling Task Starting."); // NEW DEBUG LOG
                _logger.LogInfo($"PLC[{_device.DeviceName}] [INFO] Polling Task Starting.", LogType.Diagnostics); // NEW DEBUG LOG

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
                            _logger.LogInfo($"PLC[{_device.DeviceName}] [DATA] Polling successful. " +
                                $"Dispatched {data.Count} raw register groups.", LogType.Diagnostics);
                        }
                    }
                    catch (Exception ex)
                    {
                        // CATCH-ALL for communication issues or polling exceptions
                        _logger.LogError($"PLC[{_device.DeviceName}] [ERROR] Polling Cycle FAILED. Retrying in 3s. Exception: {ex.Message}", LogType.Diagnostics);
                        Disconnect(); // Ensures clean retry in next loop

                        // NEW DEBUG LOG: Logging stack trace for silent failure investigation
                        // Console.WriteLine($"PLC[{_device.DeviceName}] [DEBUG] Stack Trace: {ex.StackTrace}"); 

                        await Task.Delay(3000); // Wait 3 seconds before next connection attempt
                    }

                    // Polling rate delay 
                    await Task.Delay(10);
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
                    _logger.LogInfo($"PLC[{_device.DeviceName}] [ATTEMPT] → Attempting connection to {_device.IPAddress}:{_device.PortNo}", LogType.Diagnostics);

                    _tcp = new TcpClient();

                    // Implementing connection timeout using WhenAny
                    var connectTask = _tcp.ConnectAsync(_device.IPAddress, _device.PortNo);

                    if (await Task.WhenAny(connectTask, Task.Delay(ConnectionTimeoutMs)) != connectTask)
                    {
                        _logger.LogWarning($"PLC[{_device.DeviceName}] [ATTEMPT] → Attempting connection to {_device.IPAddress}:{_device.PortNo}", LogType.Diagnostics);
                        throw new TimeoutException($"Connection attempt timed out after {ConnectionTimeoutMs}ms.");
                    }

                    await connectTask; // Await the connection task to propagate exceptions

                    var factory = new ModbusFactory();
                    _master = factory.CreateMaster(_tcp);
                    _logger.LogInfo($"PLC[{_device.DeviceName}] [SUCCESS] → CONNECTED.", LogType.Diagnostics);
                    Console.WriteLine($"PLC[{_device.DeviceName}] [SUCCESS] → CONNECTED.");
                    return;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"PLC[{_device.DeviceName}] [ERROR] → CONNECT ERROR. Retrying in 3s. Message: {ex.Message}");
                    _logger.LogError($"PLC[{_device.DeviceName}] [ERROR] → CONNECT ERROR. Retrying in 3s. Message: {ex.Message}", LogType.Diagnostics);

                    Disconnect();

                    await Task.Delay(3000);
                }
            }
        }


        private async Task<Dictionary<uint, object>> PollAllGroups()
        {
            // The final result will contain exactly 104 entries
            var result = new Dictionary<uint, object>();

            // 1. Get the optimized chunks (Maybe 3 or 4 chunks total)
            var chunks = OptimizeReads(_tags);

            foreach (var chunk in chunks)
            {
                try
                {
                    // 2. Perform ONE big read (e.g., 50 registers)
                    ushort[] bigBlockRaw = await _master.ReadHoldingRegistersAsync(
                        1, // Slave ID
                        chunk.StartOffset,
                        chunk.TotalCount
                    );

                    // 3. SLICE THE DATA
                    // We iterate through the specific 104 addresses we know are in this chunk
                    foreach (var addrDef in chunk.IncludedAddresses)
                    {
                        // Calculate where inside the big block this specific address starts
                        // Example: Chunk starts at 100. This address is 105. Index is 5.
                        int indexInBlock = addrDef.Offset - chunk.StartOffset;

                        // Safety check
                        if (indexInBlock < 0 || (indexInBlock + addrDef.Length) > bigBlockRaw.Length)
                        {
                           // _logger.LogError($"[SLICE ERROR] Index out of bounds for address {addrDef.ModbusAddress}");
                            continue;
                        }

                        // Extract specific registers for this address
                        // If it's a 32-bit float, we copy 2 registers. If 16-bit, 1 register.
                        ushort[] specificData = new ushort[addrDef.Length];
                        Array.Copy(bigBlockRaw, indexInBlock, specificData, 0, addrDef.Length);

                        // 4. ADD TO RESULT
                        // This restores the "104 Groups" structure you require
                        result[(uint)addrDef.ModbusAddress] = specificData;
                    }
                }
                catch (Exception ex)
                {
                   // _logger.LogError($"[CHUNK FAIL] Failed to read chunk start {chunk.StartOffset}: {ex.Message}");
                    // Optional: You could choose to throw here if you want to stop everything
                }
            }

            return result;
        }


        // ---------------------------------------------------------
        // POLL ALL TAG GROUPS (Fixed for Multi-Register Reading)
        // ---------------------------------------------------------
        /*  private async Task<Dictionary<uint, object>> PollAllGroups()
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
                      ushort startOffset = (ushort)(baseAddress - _modbusAddress);
                      //  ushort startOffset = (ushort)(baseAddress - 40000);
                      ushort maxLength = (ushort)g.Max(t => t.Length);

                      // Read the holding registers from the PLC.
                      ushort[] rawRegisters = await _master.ReadHoldingRegistersAsync(1, startOffset, maxLength);

                      // Pass the raw register block for the start address.
                      result[(uint)baseAddress] = rawRegisters;

                      // NEW DEBUG LOG: Confirming successful Modbus read
                      Console.WriteLine($"PLC[{_device.DeviceName}] [DEBUG] Read Addr {baseAddress}, Len {maxLength} successful.");
                      _logger.LogInfo($"PLC[{_device.DeviceName}] [DEBUG] Read Addr {baseAddress}, Len {maxLength} successful.", LogType.Diagnostics);
                  }
              }
              catch (Exception ex)
              {
                  // This exception will be caught by the outer StartAsync loop's CATCH-ALL.
                  Console.WriteLine($"PLC[{_device.DeviceName}] [CRITICAL] Modbus Read Failed! Message: {ex.Message}");
                  _logger.LogError($"PLC[{_device.DeviceName}] [CRITICAL] Modbus Read Failed! Message: {ex.Message}", LogType.Diagnostics);
                  // Rethrow to break the polling cycle and force a Disconnect/Reconnect sequence
                  throw;
              }

              return result;
          }*/



        public void UpdateTags(List<PLCTagConfigurationModel> allNewTags)
        {
            // Filter the new comprehensive list to only include tags relevant to this PLC
            var myNewTags = allNewTags
                .Where(t => t.PLCNo == _device.DeviceNo)
                .ToList();

            // Thread-safe replacement of the internal list
            Interlocked.Exchange(ref _tags, myNewTags);
            Console.WriteLine($"PLCClient[{_device.DeviceName}] [INFO] Tags updated to {myNewTags.Count} tags.");
            _logger.LogInfo($"PLCClient[{_device.DeviceName}] [INFO] Tags updated to {myNewTags.Count} tags.", LogType.Diagnostics);
        }


        public async Task WriteAsync(PLCTagConfigurationModel cfg, object value)
        {
            if (!IsConnected) throw new InvalidOperationException($"PLC {_device.DeviceName} not connected.");

            try
            {
                ushort start = (ushort)(cfg.ModbusAddress - _modbusAddress);
                ushort[] registers = ConvertValueToRegisters(value, cfg);

                if (cfg.DataType == 3) // Bit
                {
                    ushort[] existing = await _master!.ReadHoldingRegistersAsync(1, start, 1);
                    ushort regVal = existing[0];
                    ushort mask = (ushort)(1 << cfg.BitNo);
                    bool bVal = Convert.ToBoolean(value);
                    regVal = bVal ? (ushort)(regVal | mask) : (ushort)(regVal & ~mask);
                    await _master.WriteSingleRegisterAsync(1, start, regVal);
                }
                else if (registers.Length == 1)
                {
                    await _master!.WriteSingleRegisterAsync(1, start, registers[0]);
                }
                else
                {
                    await _master!.WriteMultipleRegistersAsync(1, start, registers);
                }
                Console.WriteLine($"PLC[{_device.DeviceName}] Written {cfg.Name}: {value}");
                _logger.LogInfo($"PLC[{_device.DeviceName}] Written {cfg.Name}: {value}", LogType.Diagnostics);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"PLC[{_device.DeviceName}] Write Error: {ex.Message}");
                _logger.LogError($"PLC[{_device.DeviceName}] Write Error: {ex.Message}", LogType.Diagnostics);
                throw;
            }
        }

        private ushort[] ConvertValueToRegisters(object value, PLCTagConfigurationModel tag)
        {
            byte[] bytes;
            ushort[] registers;

            switch (tag.DataType)
            {
                case DataType_Bit: // Bit
                    return new ushort[] { 0 }; // Placeholder, actual logic in WriteAsync

                case DataType_String: // String
                    string s = value?.ToString() ?? "";
                    bytes = Encoding.ASCII.GetBytes(s);
                    int reqBytes = tag.Length * 2;

                    // Resize to ensure correct register count
                    if (bytes.Length < reqBytes)
                    {
                        byte[] padded = new byte[reqBytes];
                        Array.Copy(bytes, padded, bytes.Length);
                        bytes = padded;
                    }
                    else if (bytes.Length > reqBytes)
                    {
                        Array.Resize(ref bytes, reqBytes);
                    }

                    if (_swapBytes)
                    {
                        for (int i = 0; i < bytes.Length; i += 2)
                        {
                            if (i + 1 < bytes.Length)
                            {
                                byte temp = bytes[i];
                                bytes[i] = bytes[i + 1];
                                bytes[i + 1] = temp;
                            }
                        }
                    }

                    registers = new ushort[tag.Length];
                    for (int i = 0; i < tag.Length; i++) registers[i] = BitConverter.ToUInt16(bytes, i * 2);
                    return registers;

                case DataType_Int16: // Int16
                    return new ushort[] { BitConverter.ToUInt16(BitConverter.GetBytes(Convert.ToInt16(value)), 0) };
                case DataType_UInt16: // UInt16
                    return new ushort[] { BitConverter.ToUInt16(BitConverter.GetBytes(Convert.ToUInt16(value)), 0) };

                case DataType_Word32: // Word32
                    bytes = BitConverter.GetBytes(Convert.ToInt32(value));
                    registers = new ushort[2];
                    registers[0] = BitConverter.ToUInt16(bytes, 0);
                    registers[1] = BitConverter.ToUInt16(bytes, 2);
                    if (_swapBytes) Swap(registers);
                    return registers;

                case DataType_UInt32: // UInt32
                    bytes = BitConverter.GetBytes(Convert.ToUInt32(value));
                    registers = new ushort[2];
                    registers[0] = BitConverter.ToUInt16(bytes, 0);
                    registers[1] = BitConverter.ToUInt16(bytes, 2);
                    if (_swapBytes) Swap(registers);
                    return registers;

                case DataType_FP: // Float
                    bytes = BitConverter.GetBytes(Convert.ToSingle(value));
                    registers = new ushort[2];
                    registers[0] = BitConverter.ToUInt16(bytes, 0);
                    registers[1] = BitConverter.ToUInt16(bytes, 2);
                    if (_swapBytes) Swap(registers);
                    return registers;

                default:
                    return new ushort[] { Convert.ToUInt16(value) };
            }
        }

        private void Swap(ushort[] regs)
        {
            if (regs.Length >= 2)
            {
                ushort temp = regs[0];
                regs[0] = regs[1];
                regs[1] = temp;
            }
        }



        #region Commented code

        //public async Task WriteAsync(PLCTagConfigurationModel cfg, object value)
        //{
        //    if (!IsConnected)
        //        throw new InvalidOperationException($"Cannot write to PLC {_device.DeviceName}: Connection is unavailable.");

        //    try
        //    {
        //        ushort start = (ushort)(cfg.ModbusAddress - _modbusAddress);
        //       // ushort start = (ushort)(cfg.ModbusAddress - 40000);

        //        // Convert value to registers based on DataType
        //        ushort[] registers = ConvertValueToRegisters(value, cfg);

        //        // Write to PLC
        //        if (cfg.DataType == DataType_Bit)
        //        {
        //            // Special case: Bit manipulation
        //            ushort[] existingRegs = await _master!.ReadHoldingRegistersAsync(1, start, 1);
        //            ushort regValue = existingRegs[0];
        //            ushort mask = (ushort)(1 << cfg.BitNo);

        //            bool boolValue = Convert.ToBoolean(value);
        //            if (boolValue)
        //                regValue = (ushort)(regValue | mask);
        //            else
        //                regValue = (ushort)(regValue & ~mask);

        //            await _master.WriteSingleRegisterAsync(1, start, regValue);
        //            Console.WriteLine($"PLC[{_device.DeviceName}] [WRITE] Addr={cfg.ModbusAddress} Bit={cfg.BitNo} → {boolValue} SUCCESS");
        //        }
        //        else if (registers.Length == 1)
        //        {
        //            // Single register write
        //            await _master!.WriteSingleRegisterAsync(1, start, registers[0]);
        //            Console.WriteLine($"PLC[{_device.DeviceName}] [WRITE] Addr={cfg.ModbusAddress} → {value} SUCCESS");
        //        }
        //        else
        //        {
        //            // Multiple register write
        //            await _master!.WriteMultipleRegistersAsync(1, start, registers);
        //            Console.WriteLine($"PLC[{_device.DeviceName}] [WRITE] Addr={cfg.ModbusAddress} → {value} SUCCESS");
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine($"PLC[{_device.DeviceName}] [WRITE ERROR] Failed to write Tag {cfg.Name}. Message: {ex.Message}");
        //        throw;
        //    }
        //}

        ///// <summary>
        ///// Converts a value to Modbus registers based on the tag's DataType.
        ///// This is the REVERSE of ConvertData() - it prepares data for writing to PLC.
        ///// </summary>
        //private ushort[] ConvertValueToRegisters(object value, PLCTagConfigurationModel tag)
        //{
        //    try
        //    {
        //        byte[] byteArray;
        //        ushort[] registers;

        //        switch (tag.DataType)
        //        {
        //            case DataType_Bit:
        //                // Bit writes are handled separately in WriteAsync
        //                // Return dummy register - actual bit manipulation happens in WriteAsync
        //                bool boolVal = Convert.ToBoolean(value);
        //                return new ushort[] { (ushort)(boolVal ? 1 : 0) };

        //            case DataType_String:
        //                // Convert string to ASCII bytes
        //                string strValue = value?.ToString() ?? "";
        //                byteArray = Encoding.ASCII.GetBytes(strValue);

        //                // Pad to required length (2 bytes per register)
        //                int requiredBytes = tag.Length * 2;
        //                if (byteArray.Length < requiredBytes)
        //                {
        //                    byte[] padded = new byte[requiredBytes];
        //                    Array.Copy(byteArray, padded, byteArray.Length);
        //                    byteArray = padded;
        //                }
        //                else if (byteArray.Length > requiredBytes)
        //                {
        //                    Array.Resize(ref byteArray, requiredBytes);
        //                }

        //                // Convert bytes to registers
        //                registers = new ushort[tag.Length];
        //                for (int i = 0; i < tag.Length; i++)
        //                {
        //                    // BitConverter.ToUInt16 reads Little Endian from byte array
        //                    registers[i] = BitConverter.ToUInt16(byteArray, i * 2);
        //                }
        //                return registers;

        //            case DataType_Int16:
        //                // 16-bit signed integer
        //                short int16Value = Convert.ToInt16(value);
        //                byteArray = BitConverter.GetBytes(int16Value);
        //                return new ushort[] { BitConverter.ToUInt16(byteArray, 0) };

        //            case DataType_UInt16:
        //                // 16-bit signed integer
        //                ushort uint16Value = Convert.ToUInt16(value);
        //                byteArray = BitConverter.GetBytes(uint16Value);
        //                return new ushort[] { BitConverter.ToUInt16(byteArray, 0) };


        //            case DataType_Word32:
        //                // 32-bit integer (2 registers)
        //                int int32Value = Convert.ToInt32(value);
        //                byteArray = BitConverter.GetBytes(int32Value);

        //                // Convert to 2 registers
        //                registers = new ushort[2];
        //                registers[0] = BitConverter.ToUInt16(byteArray, 0); // Low word
        //                registers[1] = BitConverter.ToUInt16(byteArray, 2); // High word

        //                // CRITICAL: Swap words for Big Endian Modbus
        //                // This is the REVERSE of the swap in ConvertData()
        //                //ushort temp = registers[0];
        //                //registers[0] = registers[1];
        //                //registers[1] = temp;

        //                return registers;

        //            case DataType_UInt32:
        //                // 32-bit integer (2 registers)
        //                uint uint32Value = Convert.ToUInt32(value);
        //                byteArray = BitConverter.GetBytes(uint32Value);

        //                // Convert to 2 registers
        //                registers = new ushort[2];
        //                registers[0] = BitConverter.ToUInt16(byteArray, 0); // Low word
        //                registers[1] = BitConverter.ToUInt16(byteArray, 2); // High word

        //                // CRITICAL: Swap words for Big Endian Modbus
        //                // This is the REVERSE of the swap in ConvertData()
        //                //ushort temp2 = registers[0];
        //                //registers[0] = registers[1];
        //                //registers[1] = temp2;

        //                return registers;

        //            case DataType_FP:
        //                // 32-bit floating point (2 registers)
        //                float floatValue = Convert.ToSingle(value);
        //                byteArray = BitConverter.GetBytes(floatValue);

        //                // Convert to 2 registers
        //                registers = new ushort[2];
        //                registers[0] = BitConverter.ToUInt16(byteArray, 0); // Low word
        //                registers[1] = BitConverter.ToUInt16(byteArray, 2); // High word

        //                // CRITICAL: Swap words for Big Endian Modbus
        //                // This is the REVERSE of the swap in ConvertData()
        //                //temp = registers[0];
        //                //registers[0] = registers[1];
        //                //registers[1] = temp;

        //                return registers;

        //            default:
        //                // Default: treat as UInt16
        //                ushort uint16Value2 = Convert.ToUInt16(value);
        //                return new ushort[] { uint16Value2 };
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine($"[WRITE_ERROR] Failed to convert value for Tag {tag.Name} (Type {tag.DataType}): {ex.Message}");
        //        throw;
        //    }
        //}


        #endregion


        // DataType constants (add these to match your ConvertData logic)


        private const int DataType_Int16 = 1;
        private const int DataType_Word32 = 2;
        private const int DataType_Bit = 3;
        private const int DataType_FP = 4;
        private const int DataType_String = 5;
        private const int DataType_UInt16 = 6;
        private const int DataType_UInt32 = 7;


        // Helper class to hold chunk info
        public class ModbusReadChunk
        {
            public ushort StartOffset { get; set; }
            public ushort TotalCount { get; set; }
            // We store WHICH addresses are inside this chunk so we can extract them later
            public List<AddressDef> IncludedAddresses { get; set; } = new List<AddressDef>();
        }

        public class AddressDef
        {
            public int ModbusAddress { get; set; } // e.g. 40001
            public ushort Offset { get; set; }     // e.g. 1 (The calculated 0-based offset)
            public ushort Length { get; set; }     // e.g. 1 or 2
        }

        private List<ModbusReadChunk> OptimizeReads(List<PLCTagConfigurationModel> tags)
        {
            var chunks = new List<ModbusReadChunk>();

            // 1. Filter to just the 104 Unique Addresses
            // We take Max(Length) because if Address 100 has a Bit (Len 1) and a Float (Len 2), we need 2 registers.
            var uniqueAddresses = tags
                .GroupBy(t => t.ModbusAddress)
                .Select(g => new AddressDef
                {
                    ModbusAddress = g.Key,
                    Offset = (ushort)(g.Key - _modbusAddress), // Ensure this calculation matches your logic
                    Length = (ushort)g.Max(t => t.Length)
                })
                .OrderBy(a => a.Offset)
                .ToList();

            if (!uniqueAddresses.Any()) return chunks;

            // 2. Algorithm to create chunks
            var currentChunk = new ModbusReadChunk();
            // Initialize with first address
            var first = uniqueAddresses[0];
            currentChunk.StartOffset = first.Offset;
            currentChunk.IncludedAddresses.Add(first);

            int currentEnd = first.Offset + first.Length;

            for (int i = 1; i < uniqueAddresses.Count; i++)
            {
                var addr = uniqueAddresses[i];
                int addrEnd = addr.Offset + addr.Length;

                // SETTINGS
                int MAX_GAP = 10;     // Don't read more than 10 empty registers just to bridge a gap
                int MAX_READ = 120;   // Max registers per Modbus request

                // Calculate gap from end of last data to start of this data
                int gap = addr.Offset - currentEnd;
                // Calculate total size if we add this address
                int newTotalSize = addrEnd - currentChunk.StartOffset;

                if (gap <= MAX_GAP && newTotalSize <= MAX_READ)
                {
                    // Add to current chunk
                    currentChunk.IncludedAddresses.Add(addr);
                    currentEnd = addrEnd;
                }
                else
                {
                    // Close current chunk
                    currentChunk.TotalCount = (ushort)(currentEnd - currentChunk.StartOffset);
                    chunks.Add(currentChunk);

                    // Start new chunk
                    currentChunk = new ModbusReadChunk();
                    currentChunk.StartOffset = addr.Offset;
                    currentChunk.IncludedAddresses.Add(addr);
                    currentEnd = addrEnd;
                }
            }

            // Add final chunk
            currentChunk.TotalCount = (ushort)(currentEnd - currentChunk.StartOffset);
            chunks.Add(currentChunk);

            return chunks;
        }

    }



}