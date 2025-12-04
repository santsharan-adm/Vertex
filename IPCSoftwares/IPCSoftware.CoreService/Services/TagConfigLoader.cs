using IPCSoftware.Shared.Models.ConfigModels;
using System.Collections.Generic;

namespace IPCSoftware.CoreService.Services
{
    public class TagConfigLoader
    {
        public List<PLCTagConfigurationModel> Load(string filePath)
        {
            var rows = CsvReader.Read(filePath);   // same reader used for PLC devices
            var tags = new List<PLCTagConfigurationModel>();

            foreach (var r in rows)
            {
                try
                {
                    var tag = new PLCTagConfigurationModel
                    {
                        Id = int.Parse(r[0]),
                        TagNo = int.Parse(r[1]),
                        Name = r[2],
                        PLCNo = int.Parse(r[3]),
                        ModbusAddress = int.Parse(r[4]),
                        Length = int.Parse(r[5]),
                        AlgNo = int.Parse(r[6]),
                        DataType = ParseDataType(r[7]),    // NEW
                        BitNo = ParseBitNo(r[7], r[8]), // NEW (handles blank safely)
                        Offset = int.Parse(r[9]),
                        Span = int.Parse(r[10]),
                        Description = r[11],
                        Remark = r[12]
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

        private int ParseDataType(string s)
        {
            if (string.IsNullOrWhiteSpace(s))
                return 1; // default Int16

            s = s.Trim().ToLowerInvariant();

            return s switch
            {
                "int" => 1,
                "int16" => 1,
                "word" => 2,
                "dint" => 2,
                "bit" => 3,
                "bool" => 3,
                "fp" => 4,
                "float" => 4,
                "string" => 5,
                _ => 1 // default safe
            };
        }

        private int ParseBitNo(string dataTypeText, string bitValue)
        {
            // Only valid for DataType = Bit
            if (!string.Equals(dataTypeText, "Bit", StringComparison.OrdinalIgnoreCase))
                return 0;

            if (int.TryParse(bitValue, out int bitNo))
            {
                if (bitNo < 0) bitNo = 0;
                if (bitNo > 15) bitNo = 15;
                return bitNo;
            }

            return 0;
        }

    }
}
