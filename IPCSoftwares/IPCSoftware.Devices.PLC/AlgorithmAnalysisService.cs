using IPCSoftware.Core.Interfaces;
using IPCSoftware.Core.Interfaces.AppLoggerInterface;
using IPCSoftware.Services;
using IPCSoftware.Shared.Models;
using IPCSoftware.Shared.Models.ConfigModels;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IPCSoftware.Devices.PLC
{
    public class AlgorithmAnalysisService : BaseService
    {
        // Removed 'readonly' keyword for dynamic update support
        private List<PLCTagConfigurationModel> _tags;
        private readonly IDeviceConfigurationService _deviceService;
        public List<PLCTagConfigurationModel> Tags => _tags;
        private readonly bool _swapBytes;
        private readonly bool _swapStringBytes;

        // Constants matching definitions in TagConfigLoader (after necessary mapping)
        //private const int AlgoNo_Raw = 0;
        //private const int AlgoNo_LinearScale = 1;
        //private const int DataType_Int16 = 1;
        //private const int DataType_Word32 = 2; // DWord, Int32, Word
        //private const int DataType_Bit = 3;
        //private const int DataType_FP = 4; // Real, Float
        //private const int DataType_String = 5;

        //private const int DataType_UInt16 = 6;
        //private const int DataType_UInt32 = 7;



        public AlgorithmAnalysisService(
            IDeviceConfigurationService deviceService,
            IOptions<ConfigSettings> config,
            IAppLogger logger) : base(logger)
        {
            _deviceService = deviceService;
            _swapBytes = config.Value.SwapBytes;
            _swapStringBytes = config.Value.SwapStringBytes;
            _ = GetTags();
        }


        public async Task GetTags()
        {
            _tags = await _deviceService.GetAllTagsAsync();
        }

        // Method used by the Watcher Service for runtime update
        public void UpdateTags(List<PLCTagConfigurationModel> newTags)
        {
            // Thread-safe replacement of the internal list
            Interlocked.Exchange(ref _tags, newTags);
        }
        
        public event Action<int, object>? OnPlcDataProcessed;
        /// <summary>
        /// Applies data conversion (Word/FP/String) and configured algorithm (Raw/Scale).
        /// </summary>
        /// <param name="rawModbusData">Dictionary keyed by Modbus start address, containing raw ushort[] registers.</param>
        public void Apply(int plcNo, Dictionary<uint, object> rawModbusData)
        {
            

            var plcTags = _tags.Where(t => t.PLCNo == plcNo);

            foreach (var group in plcTags.GroupBy(t => t.ModbusAddress))
            {
                uint address = (uint)group.Key;

                if (!rawModbusData.TryGetValue(address, out var rawObj))
                    continue;

                foreach (var tag in group)
                {
                    // 1. Data Type Conversion and Extraction 
                    object rawTypedValue = ConvertData(rawObj, tag);

                    if (rawTypedValue == null) continue;
                    //Added new for TraceLog - 27-04-2026-
                    if (tag.EnableTraceLog)
                    {
                        string csvLine = $"{tag.Id},{rawTypedValue}";
                        _logger.LogTrace(csvLine);
                    }



                    // 2. Algorithm Application (Scaling or Raw Pass-through)
                    object finalValue = ApplyScaling(rawTypedValue, tag);

                    // --- TRACE LOGGING FOR SELECTED TAGS ---
                    //if (tag.EnableTraceLog)
                    //{
                    //    // Format: TagId,TagName,Value,PLCNo,ModbusAddress (Timestamp added by AppLoggerService)
                    //    _logger.LogTrace($"{tag.Id},{tag.Name},{finalValue},{tag.PLCNo},{tag.ModbusAddress}", LogType.TagTrace);
                    //}
                    // --- END TRACE LOGGING ---

                    // Add using Tag Id (for Dashboard cache)

                    // Raise event for real-time updates (e.g., Dashboard)
                    OnPlcDataProcessed?.Invoke(tag.Id, finalValue);
                }
            }

           // return result;
        }

        // --- Data Type Conversion and Extraction ---
        private object ConvertData(object rawModbusObj, PLCTagConfigurationModel tag)
        {
            try
            {
                ushort[] registers;

                if (rawModbusObj is ushort singleReg) registers = new ushort[] { singleReg };
                else if (rawModbusObj is ushort[] regArray) registers = regArray.Take(tag.Length).ToArray();
                else return null;

                if (registers.Length == 0) return null;

                // --- CRITICAL FIX: WORD SWAPPING ---
                // This is required for Big Endian Modbus slaves transmitting 32-bit values.
                //  bool requiresSwap = (tag.DataType == DataType_Word32 || tag.DataType == DataType_FP) && registers.Length >= 2;
                bool is32BitType = tag.DataType == PLCTagTypeExtensions.DataType_Int32 || tag.DataType == PLCTagTypeExtensions.DataType_UInt32 || tag.DataType == PLCTagTypeExtensions.DataType_FP;
                //bool requiresSwap = (tag.DataType == DataType_Word32) && registers.Length >= 2;

                if (_swapBytes && is32BitType && registers.Length >= 2)
                {
                    // Swap Word 1 <-> Word 2
                    ushort reg1 = registers[0];
                    ushort reg2 = registers[1];
                    registers[0] = reg2;
                    registers[1] = reg1;
                }

                /* if (requiresSwap)
                 {
                     // Swap the order of the 16-bit registers (Word 1 <-> Word 2)
                     // 
                     ushort reg1 = registers[0];
                     ushort reg2 = registers[1];
                     registers[0] = reg2;
                     registers[1] = reg1;
                 }*/
                // ------------------------------------

                // 2. Combine registers (now in the correct order) into a byte array
                var bytes = new List<byte>();
                foreach (var reg in registers)
                {
                    bytes.AddRange(BitConverter.GetBytes(reg));
                }
                byte[] byteArray = bytes.ToArray();


                switch (tag.DataType)
                {
                    case PLCTagTypeExtensions.DataType_Bit:
                        if (tag.BitNo >= 0 && tag.BitNo <= 15)
                            return ((registers[0] >> tag.BitNo) & 0x01) == 1;
                        return false;

                    case PLCTagTypeExtensions.DataType_String:
                        byte[] bytesToDecode = byteArray;
                        // --- CONFIGURABLE BYTE SWAP (String) ---
                        if (_swapStringBytes)
                        {
                            bytesToDecode = SwapEveryTwoBytes(byteArray);
                        }
                        // ---------------------------------------
                        // Replace ALL null characters (not just trailing) to handle embedded \0 from 2-byte Modbus string encoding
                        return Encoding.ASCII.GetString(bytesToDecode, 0, bytesToDecode.Length).Replace("\0", string.Empty).Trim();
                    case PLCTagTypeExtensions.DataType_Int16:
                        return BitConverter.ToInt16(byteArray, 0);
                    case PLCTagTypeExtensions.DataType_UInt16:
                        return BitConverter.ToUInt16(byteArray, 0);

                    case PLCTagTypeExtensions.DataType_Int32:
                        // This now receives the correctly ordered byte array
                        if (byteArray.Length < 4) return 0; // throw new InvalidOperationException("Insufficient bytes for 32-bit Word.");
                        return BitConverter.ToInt32(byteArray, 0);

                    case PLCTagTypeExtensions.DataType_UInt32:
                        if (byteArray.Length < 4) return 0u;
                        return BitConverter.ToUInt32(byteArray, 0);

                    case PLCTagTypeExtensions.DataType_FP:
                        if (byteArray.Length < 4) return 0.0f;
                        return BitConverter.ToSingle(byteArray, 0);

                    default:
                        return registers[0];
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ALGO_ERROR] Failed to convert data for Tag {tag.Name} (Type {tag.DataType}): {ex.Message}");
                _logger.LogError($"[ALGO_ERROR] Failed to convert data for Tag {tag.Name} (Type {tag.DataType}): {ex.Message}", LogType.Diagnostics);
                return null;
            }
        }




        // --- Algorithm Application (Scaling) ---
        private object ApplyScaling(object rawTypedValue, PLCTagConfigurationModel tag)
        {
            try
            {
                if (tag.AlgNo == PLCTagTypeExtensions.AlgoNo_Raw)
                {
                    return rawTypedValue;
                }

                if (tag.AlgNo == PLCTagTypeExtensions.AlgoNo_LinearScale)
                {
                    // Rule: Linear Scale only applies to Int16 (1) and Word32 (2)
                    if (tag.DataType == PLCTagTypeExtensions.DataType_Int16 || tag.DataType == PLCTagTypeExtensions.DataType_UInt32 || tag.DataType == PLCTagTypeExtensions.DataType_UInt16 || tag.DataType == PLCTagTypeExtensions.DataType_Int32)
                    {
                        double rawNumericValue = Convert.ToDouble(rawTypedValue);
                        if (tag.UseEngMinMax)
                        {
                            return LinearScale_EngMinMax(rawNumericValue, tag);
                        }
                        else
                        {
                            return LinearScale_GainOffset(rawNumericValue, tag);
                        }

                    }
                }

                // Fallback for types that shouldn't be scaled (Bit, String, FP)
                return rawTypedValue;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message, LogType.Diagnostics);
                return rawTypedValue;
            }
        }


        private double LinearScale_EngMinMax(double rawValue, PLCTagConfigurationModel tag)
        {
            /*      double plcRawMax = (tag.DataType == DataType_Int16) ? 65535.0 : 2147483647.0;
                  double plcRawMin = 0.0;*/

            double plcRawMax = tag.DataType.GetMaxValue(); //  (tag.DataType == DataType_Int16) ? 65535.0 : 2147483647.0;
            double plcRawMin = tag.DataType.GetMinValue();
            double engMin = tag.Offset;
            double engMax = tag.Offset + tag.Span;
            //double engMax = tag.Span;
            double rawRange = plcRawMax - plcRawMin;

            if (Math.Abs(rawRange) < double.Epsilon) return engMin;

            return (rawValue - plcRawMin) * (engMax - engMin) / rawRange + engMin;
            // return (rawValue - plcRawMin) * (engMax)  + engMin;
        }

        private double LinearScale_GainOffset(double rawValue, PLCTagConfigurationModel tag)
        {
            double plcRawMax = tag.DataType.GetMaxValue(); //  (tag.DataType == DataType_Int16) ? 65535.0 : 2147483647.0;
            double plcRawMin = tag.DataType.GetMinValue();
            double offset = tag.Offset;
            //double engMax = tag.Offset + tag.Span;
            double gain = tag.Span;
            double rawRange = plcRawMax - plcRawMin;

            if (Math.Abs(rawRange) < double.Epsilon) return offset;

            //return (rawValue - plcRawMin) * (engMax - engMin) / rawRange + engMin;
            return (rawValue - plcRawMin) / (gain) + offset;
        }

        byte[] SwapEveryTwoBytes(byte[] src)
        {
            if (src == null || (src.Length & 1) != 0) return src;
            var dst = new byte[src.Length];
            for (int i = 0; i < src.Length; i += 2)
            {
                dst[i] = src[i + 1];
                dst[i + 1] = src[i];
            }
            return dst;
        }
    }
    
}