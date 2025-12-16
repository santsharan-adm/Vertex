using IPCSoftware.Shared.Models.ConfigModels;
using System.Collections.Generic;
using System.Linq;

namespace IPCSoftware.Services
{
    public class TagConfigLoader
    {
        // Constants matching definitions in AlgorithmAnalysisService/Requirements
        private const int DataType_Int16 = 1;
        private const int DataType_Word32 = 2;
        private const int DataType_Bit = 3;
        private const int DataType_FP = 4;
        private const int DataType_String = 5;
        private const int DataType_UInt16 = 6;
        private const int DataType_UInt32 = 7;

        public List<PLCTagConfigurationModel> Load(string filePath)
        {
            var rows = CsvReader.Read(filePath);
            var tags = new List<PLCTagConfigurationModel>();

            foreach (var r in rows)
            {
                // Ensure the row has enough columns (at least 14 columns, including CanWrite at [13])
                if (r.Length < 14) continue;

                try
                {
                    // 1. Parse Data Type and Bit No first, as they determine Length
                    int dataType = ParseDataType(r[7]);
                    int bitNo = ParseBitNo(r[7], r[8]);
                    int configuredLength = int.Parse(r[5]);

                    // 2. Apply Length Enforcement (Fix for requirements C, D, F, G)
                    int enforcedLength = EnforceDataLength(dataType, configuredLength);

                    // 3. Load the base model
                    var tag = new PLCTagConfigurationModel
                    {
                        Id = int.Parse(r[0]),
                        TagNo = int.Parse(r[1]),
                        Name = r[2],
                        PLCNo = int.Parse(r[3]),
                        ModbusAddress = int.Parse(r[4]),

                        // Use the enforced length
                        Length = enforcedLength,

                        AlgNo = int.Parse(r[6]),
                        DataType = dataType,
                        BitNo = bitNo,
                        Offset = int.Parse(r[9]),
                        Span = int.Parse(r[10]),
                        Description = r[11],
                        Remark = r[12],

                        // NEW: Read CanWrite (assuming column [13])
                        CanWrite = ParseBoolean(r[13])
                    };

                    tags.Add(tag);
                }
                catch
                {
                    // skip bad rows
                }
            }

            return tags;
        }

        // --- Existing Helper Methods (Simplified for context) ---

        // Inside TagConfigLoader.cs:ParseDataType(string s)

        private int ParseDataType(string s)
        {
            // ... (Your existing code to clean/trim the string 's' remains here) ...
            s = s.Trim().ToLowerInvariant();

            return s switch
            {
                // Existing Mappings (Int16)
                "int" => 1,
                "int16" => 1,

                // Existing Mappings (Word/Int32 - Code 2)
                "word" => 2,
                "dint" => 2,

                // NEW Mappings for 32-bit Word (Code 2)
                //"uint" => 2,     // New from your CSV (Unsigned Int)
                "dword" => 2,    // New from your CSV (Double Word / Int32)
                "int32" => 2,

                // Existing Mappings (Bit - Code 3)
                "bit" => 3,
                "bool" => 3,

                // Existing Mappings (FP/Float - Code 4)
                "fp" => 4,
                "float" => 4,

                // NEW Mapping for Float (Code 4)
                "real" => 4,     // New from your CSV (IEEE 754 Float/Real)

                // Existing Mapping (String - Code 5)
                "string" => 5,
                "uint" => 6,     // New from your CSV (Unsigned Int)
                "uint16" => 6,     // New from your CSV (Unsigned Int)
                "uint32" => 7,     // New from your CSV (Unsigned Int)

                _ => 1 // Default to Int16 for safety
            };
        }

        private int ParseBitNo(string dataTypeText, string bitValue)
        {
            // Logic to extract BitNo (0-15) only if dataTypeText is "Bit"
            if (!string.Equals(dataTypeText, "Bit", StringComparison.OrdinalIgnoreCase))
                return 0;

            if (int.TryParse(bitValue, out int bitNo))
            {
                // Enforce range 0 to 15
                return Math.Clamp(bitNo, 0, 15);
            }

            return 0;
        }

        // --- NEW Helper Methods ---

        /// <summary>
        /// FIX: Enforces the mandatory Modbus register length based on data type.
        /// </summary>
        private int EnforceDataLength(int dataType, int configuredLength)
        {
            switch (dataType)
            {
                case DataType_Int16: // 16-bit (1 register)
                case DataType_Bit:   // 1-bit (1 register)
                case DataType_UInt16:   // 1-bit (1 register)
                    return 1;

                case DataType_Word32: // 32-bit (2 registers)
                case DataType_FP:     // Float (32-bit, 2 registers)
                case DataType_UInt32:   // 1-bit (1 register)
                    return 2;

                case DataType_String:
                    // String length is configurable (Max 50 registers per requirement).
                    return Math.Clamp(configuredLength, 1, 50);

                default:
                    return 1;
            }
        }

        /// <summary>
        /// NEW: Parses a boolean value from the CSV (e.g., "1" or "TRUE").
        /// </summary>
        private bool ParseBoolean(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return false;
            s = s.Trim().ToLowerInvariant();
            return s == "true" || s == "1";
        }
    }
}