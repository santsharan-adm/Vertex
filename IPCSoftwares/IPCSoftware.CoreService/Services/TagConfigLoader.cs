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
                        ModbusAddress = r[4],
                        Length = int.Parse(r[5]),
                        AlgNo = int.Parse(r[6]),
                        Offset = int.Parse(r[7]),
                        Span = int.Parse(r[8]),
                        Description = r[9],
                        Remark = r[10]
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
    }
}
