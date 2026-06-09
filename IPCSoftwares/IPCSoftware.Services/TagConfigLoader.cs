using IPCSoftware.Core.Interfaces;
using IPCSoftware.Core.Interfaces.AppLoggerInterface;
using IPCSoftware.Shared.Models.ConfigModels;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace IPCSoftware.Services
{
    public class TagConfigLoader : BaseService
    {
        private readonly IFileHandler _fileHandler;
        public TagConfigLoader(IAppLogger logger , IFileHandler fileHandler) : base(logger)
        { _fileHandler = fileHandler; }
        // Constants matching definitions in AlgorithmAnalysisService/Requirements
        //Should come from a shared place if possible to avoid mismatches between loading and analysis logic - BMK-09-06-2026 -ReviewComment
        //private const int DataType_Int16 = 1;
        //private const int DataType_Word32 = 2;
        //private const int DataType_Bit = 3;
        //private const int DataType_FP = 4;
        //private const int DataType_String = 5;
        //private const int DataType_UInt16 = 6;
        //private const int DataType_UInt32 = 7;

        public List<PLCTagConfigurationModel> Load(string filePath)
        {
            try
            {
                var version = _fileHandler.Getversion(filePath);
                var rows = _fileHandler.Read(filePath);
                var tags = new List<PLCTagConfigurationModel>();
                if (version == "1.0")
                {
                    foreach (var r in rows)
                    {
                        // Must be at least 17 columns: indices [0]..[16] (EnableTraceLog is at [16])
                        if (r.Length < 15) continue;

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
                                Offset = double.Parse(r[9]),
                                Span = double.Parse(r[10]),
                                Description = r[11],
                                Remark = r[12],

                                // NEW: Read CanWrite (assuming column [13])
                                CanWrite = ParseBoolean(r[13]),
                                IOType = r[14],
                                UseEngMinMax = r.Length > 15 ? ParseBoolean(r[15]) : false,
                                EnableTraceLog = r.Length > 16 ? ParseBoolean(r[16]) : false
                                //DMAddress = r[15]

                            };

                            tags.Add(tag);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex.Message, LogType.Diagnostics);
                        }
                    }
                }
                else if (version == "2.0")
                {



                    foreach (var r in rows)
                    {
                        // Must be at least 18 columns: indices [0]..[17] (IOType is at [17])
                        if (r.Length < 18) continue;

                        try
                        {
                            // 1. Parse Data Type and Bit No first, as they determine Length
                            int dataType = ParseDataType(r[9]);
                            int bitNo = ParseBitNo(r[9], r[10]);
                            int configuredLength = int.Parse(r[8]);

                            // 2. Apply Length Enforcement (Fix for requirements C, D, F, G)
                            int enforcedLength = EnforceDataLength(dataType, configuredLength);

                            // 3. Load the base model
                            var tag = new PLCTagConfigurationModel
                            {
                                Id = int.Parse(r[0]),
                                TagNo = int.Parse(r[0]), // Use Id as TagNo for UI display
                                Name = r[3],
                                PLCNo = int.Parse(r[4]),
                                ModbusAddress = int.Parse(r[6]),

                                // Use the enforced length
                                Length = enforcedLength,

                                AlgNo = int.Parse(r[7]),
                                DataType = dataType,
                                BitNo = bitNo,
                                Offset = double.Parse(r[12]),
                                Span = double.Parse(r[13]),
                                Direction = r[16],
                                IOType = r[17],
                                CanWrite = ParseBoolean(r[15]),

                                // NEW: Read UseEngMinMax and EnableTraceLog (columns 18 and 19 in Bending CSV)
                                UseEngMinMax = r.Length > 11 ? ParseBoolean(r[11]) : false,
                                EnableTraceLog = r.Length > 18 ? ParseBoolean(r[18]) : false,

                                Description = r.Length > 19 ? r[19] : "",
                                Remark = r.Length > 20 ? r[20] : ""
                                //CanWrite = ParseBoolean(r[13]),
                                //DMAddress = r[15]

                            };

                            tags.Add(tag);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex.Message, LogType.Diagnostics);
                        }
                    }

                }

                return tags;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message, LogType.Diagnostics);
                return null;
            }
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
            try
            {
                if (!string.Equals(dataTypeText, "Bit", StringComparison.OrdinalIgnoreCase))
                    return 0;

                if (int.TryParse(bitValue, out int bitNo))
                {
                    // Enforce range 0 to 15
                    return Math.Clamp(bitNo, 0, 15);
                }

                return 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message, LogType.Diagnostics);
                return 0;
            }
            // Logic to extract BitNo (0-15) only if dataTypeText is "Bit"
        }

        // --- NEW Helper Methods ---

        /// <summary>
        /// FIX: Enforces the mandatory Modbus register length based on data type.
        /// </summary>
        private int EnforceDataLength(int dataType, int configuredLength)
        {
            switch (dataType)
            {
                case PLCTagTypeExtensions.DataType_Int16: // 16-bit (1 register)
                case PLCTagTypeExtensions.DataType_Bit:   // 1-bit (1 register)
                case PLCTagTypeExtensions.DataType_UInt16:   // 1-bit (1 register)
                    return 1;

                case PLCTagTypeExtensions.DataType_Int32: // 32-bit (2 registers)
                case PLCTagTypeExtensions.DataType_FP:     // Float (32-bit, 2 registers)
                case PLCTagTypeExtensions.DataType_UInt32:   // 1-bit (1 register)
                    return 2;

                case PLCTagTypeExtensions.DataType_String:
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
            try
            {
                if (string.IsNullOrWhiteSpace(s)) return false;
                s = s.Trim().ToLowerInvariant();
                return s == "true" || s == "1";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message, LogType.Diagnostics);
                return false;
            }
        }

        public async Task Save(string filepath, List<PLCTagConfigurationModel> tags)
        {
            try
            {
                var sb = new StringBuilder();
                string ver = _fileHandler.Getversion(filepath);
                string version = string.Format($"Version = {ver}");
                sb.AppendLine(version);
                string header = _fileHandler.GetHeader(filepath);
                sb.AppendLine(header);

                foreach (var tag in tags ?? new List<PLCTagConfigurationModel>())
                {
                    if (ver == "2.0")
                    {
                        sb.AppendLine($"{tag.Id}," +                            
                            $"{tag.Category}," +
                            $"{tag.DMAddress}," +
                            $"{_fileHandler.EscapeCsv(tag.Name)}," +         // <--- Was $"\"{EscapeCsv(tag.Name)}\","
                            $"{tag.PLCNo}," +
                             $"{tag.M40000}," +
                            $"{tag.ModbusAddress}," +
                            $"{tag.AlgNo}," +
                            $"{tag.Length}," +
                            $"{PLCTagTypeExtensions.GetDataTypeString(tag.DataType)}," + // Helper to convert int back to string (e.g. 1 -> Int16)
                            $"{tag.BitNo}," +
                            $"{tag.UseEngMinMax}," +
                            $"{tag.Offset}," +
                            $"{tag.Span}," +
                            $"{tag.Monitor}," +
                            $"{tag.CanWrite}," +
                            $"{tag.Direction}," +
                            $"{_fileHandler.EscapeCsv(tag.IOType)}," +
                            $"{tag.EnableTraceLog}," +
                            $"{_fileHandler.EscapeCsv(tag.Description)}," +  // <--- Was $"\"{EscapeCsv(tag.Description)}\","
                            $"{_fileHandler.EscapeCsv(tag.Remark)}");    
                         
                            
                            
                            
                    }
                    else
                    {
                        sb.AppendLine($"{tag.Id}," +
                       $"{tag.Id}," +
                       $"{_fileHandler.EscapeCsv(tag.Name)}," +         // <--- Was $"\"{EscapeCsv(tag.Name)}\","
                       $"{tag.PLCNo}," +
                       $"{tag.ModbusAddress}," +
                       $"{tag.Length}," +
                       $"{tag.AlgNo}," +
                       $"{PLCTagTypeExtensions.GetDataTypeString(tag.DataType)}," + // Helper to convert int back to string (e.g. 1 -> Int16)
                       $"{tag.BitNo}," +
                       $"{tag.Offset}," +
                       $"{tag.Span}," +
                       $"{_fileHandler.EscapeCsv(tag.Description)}," +  // <--- Was $"\"{EscapeCsv(tag.Description)}\","
                       $"{_fileHandler.EscapeCsv(tag.Remark)}," +       // <--- Was $"\"{EscapeCsv(tag.Remark)}\","
                       $"{tag.CanWrite}," +
                       $"{_fileHandler.EscapeCsv(tag.IOType)}");  // <--- Was $"\"{EscapeCsv(tag.Remark)}\","
                                                     // $"{EscapeCsv(tag.DMAddress)},") ;
                    }
                }
                await _fileHandler.WriteCsv(filepath, sb.ToString());             //Added by Rishabh - date - 25/04/2026// 
                                                                                  // await File.WriteAllTextAsync(filepath, sb.ToString(), Encoding.UTF8);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error saving devices CSV: {ex.Message}", LogType.Diagnostics);
                throw;
            }
        }



        // You likely need this helper to save "Int16" instead of "1" back to the CSV
        //Should come from a shared place if possible to avoid mismatches between loading and analysis logic - BMK-09-06-2026 -ReviewComment
        //private string GetDataTypeString(int typeId)
        //{
        //    return typeId switch
        //    {
        //        1 => "Int16",
        //        2 => "Word",
        //        3 => "Bit",
        //        4 => "Float",
        //        5 => "String",
        //        6 => "UInt16",
        //        7 => "UInt32",
        //        _ => "Int16"
        //    };
        //}
    }
}