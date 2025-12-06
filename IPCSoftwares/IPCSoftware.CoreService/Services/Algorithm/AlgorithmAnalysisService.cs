using System;
using System.Collections.Generic;
using System.Linq;
using IPCSoftware.Shared.Models.ConfigModels;
using System.Text;
using System.Threading.Tasks;

namespace IPCSoftware.CoreService.Services.Algorithm
{
    public class AlgorithmAnalysisService
    {
        // Removed 'readonly' keyword for dynamic update support
        private List<PLCTagConfigurationModel> _tags;

        public List<PLCTagConfigurationModel> Tags => _tags;

        // Constants matching definitions in TagConfigLoader (after necessary mapping)
        private const int AlgoNo_Raw = 0;
        private const int AlgoNo_LinearScale = 1;
        private const int DataType_Int16 = 1;
        private const int DataType_Word32 = 2; // DWord, Int32, Word
        private const int DataType_Bit = 3;
        private const int DataType_FP = 4; // Real, Float
        private const int DataType_String = 5;

        public AlgorithmAnalysisService(List<PLCTagConfigurationModel> tags)
        {
            _tags = tags;
        }

        // Method used by the Watcher Service for runtime update
        public void UpdateTags(List<PLCTagConfigurationModel> newTags)
        {
            // Thread-safe replacement of the internal list
            Interlocked.Exchange(ref _tags, newTags);
        }

        /// <summary>
        /// Applies data conversion (Word/FP/String) and configured algorithm (Raw/Scale).
        /// </summary>
        /// <param name="rawModbusData">Dictionary keyed by Modbus start address, containing raw ushort[] registers.</param>
        public Dictionary<int, object> Apply(int plcNo, Dictionary<uint, object> rawModbusData)
        {
            var result = new Dictionary<int, object>();

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

                    // 2. Algorithm Application (Scaling or Raw Pass-through)
                    object finalValue = ApplyScaling(rawTypedValue, tag);

                    // --- DEBUG OUTPUT ---
                    string algoName = tag.AlgNo == 1 ? "Scaled" : "Raw";
                    Console.WriteLine($"[ALGO_DEBUG] Tag: {tag.Name} (ID:{tag.Id}) | Value: {finalValue} | Type: {tag.DataType} | Algo: {algoName}");
                    // --- END DEBUG OUTPUT ---

                    // Add using Tag Id (for Dashboard cache)
                    result[tag.Id] = finalValue;
                }
            }

            return result;
        }

        // --- Data Type Conversion and Extraction ---
        private object ConvertData(object rawModbusObj, PLCTagConfigurationModel tag)
        {
            ushort[] registers;

            // 1. Normalize input to ushort[]
            if (rawModbusObj is ushort singleReg)
                registers = new ushort[] { singleReg };
            else if (rawModbusObj is ushort[] regArray)
                registers = regArray.Take(tag.Length).ToArray();
            else
                return null;

            if (registers.Length == 0) return null;

            // --- CRITICAL FIX: WORD SWAPPING ---
            // This is required for Big Endian Modbus slaves transmitting 32-bit values.
            bool requiresSwap = (tag.DataType == DataType_Word32 || tag.DataType == DataType_FP) && registers.Length >= 2;

            if (requiresSwap)
            {
                // Swap the order of the 16-bit registers (Word 1 <-> Word 2)
                // 
                ushort reg1 = registers[0];
                ushort reg2 = registers[1];
                registers[0] = reg2;
                registers[1] = reg1;
            }
            // ------------------------------------

            // 2. Combine registers (now in the correct order) into a byte array
            var bytes = new List<byte>();
            foreach (var reg in registers)
            {
                // BitConverter assumes standard (C# default) Little Endian byte order within the word
                byte[] wordBytes = BitConverter.GetBytes(reg);
                bytes.AddRange(wordBytes);
            }
            byte[] byteArray = bytes.ToArray();

            try
            {
                switch (tag.DataType)
                {
                    case DataType_Bit:
                        if (tag.BitNo >= 0 && tag.BitNo <= 15)
                            return ((registers[0] >> tag.BitNo) & 0x01) == 1;
                        return false;

                    case DataType_String:
                        return Encoding.ASCII.GetString(byteArray, 0, byteArray.Length).TrimEnd('\0');

                    case DataType_Int16:
                        return BitConverter.ToInt16(byteArray, 0);

                    case DataType_Word32:
                        // This now receives the correctly ordered byte array
                        if (byteArray.Length < 4) throw new InvalidOperationException("Insufficient bytes for 32-bit Word.");
                        return BitConverter.ToInt32(byteArray, 0);

                    case DataType_FP:
                        // This now receives the correctly ordered byte array
                        if (byteArray.Length < 4) throw new InvalidOperationException("Insufficient bytes for Floating Point.");
                        return BitConverter.ToSingle(byteArray, 0);

                    default:
                        return registers[0];
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ALGO_ERROR] Failed to convert data for Tag {tag.Name} (Type {tag.DataType}): {ex.Message}");
                return null;
            }
        }
        // --- Algorithm Application (Scaling) ---
        private object ApplyScaling(object rawTypedValue, PLCTagConfigurationModel tag)
        {
            if (tag.AlgNo == AlgoNo_Raw)
            {
                return rawTypedValue;
            }

            if (tag.AlgNo == AlgoNo_LinearScale)
            {
                // Rule: Linear Scale only applies to Int16 (1) and Word32 (2)
                if (tag.DataType == DataType_Int16 || tag.DataType == DataType_Word32)
                {
                    double rawNumericValue = Convert.ToDouble(rawTypedValue);
                    return LinearScale(rawNumericValue, tag);
                }
            }

            // Fallback for types that shouldn't be scaled (Bit, String, FP)
            return rawTypedValue;
        }

        // --- Correct Linear Scaling Formula ---
        private double LinearScale(double rawValue, PLCTagConfigurationModel tag)
        {
            // Assuming unsigned 16-bit range for Int16 scaling unless Offset/Span dictates otherwise
            double plcRawMax = (tag.DataType == DataType_Int16) ? 65535.0 :
                               (tag.DataType == DataType_Word32) ? 2147483647.0 : 0.0;
            double plcRawMin = 0.0;

            // Engineering range based on Offset and Span
            double engMin = tag.Offset;
            double engMax = tag.Offset + tag.Span;

            double rawRange = plcRawMax - plcRawMin;

            // Avoid division by zero
            if (Math.Abs(rawRange) < double.Epsilon)
            {
                return engMin;
            }

            // Scaling Formula: Scaled = (Raw - R_min) * (E_max - E_min) / (R_max - R_min) + E_min
            double scaledValue = (rawValue - plcRawMin) * (engMax - engMin) / rawRange + engMin;

            return scaledValue;
        }
    }
}