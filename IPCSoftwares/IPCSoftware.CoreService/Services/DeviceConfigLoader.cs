using System.Collections.Generic;
using System.Linq;
using IPCSoftware.Shared.Models.ConfigModels;

namespace IPCSoftware.CoreService.Services
{
    public class DeviceConfigLoader
    {
        private string Clean(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return input;

            return input.Trim().Trim('"');
        }

        public List<DeviceInterfaceModel> Load(string filePath)
        {
            var rows = CsvReader.Read(filePath);  // static call, returns string[]

            var devices = new List<DeviceInterfaceModel>();

            foreach (var r in rows)
            {
                // skip empty or malformed rows
                if (r.Length < 12)
                    continue;

                try
                {
                    var device = new DeviceInterfaceModel
                    {
                        Id = int.Parse(Clean(r[0])),
                        DeviceNo = int.Parse(Clean(r[1])),
                        DeviceName = Clean(r[2]),
                        UnitNo = int.Parse(Clean(r[3])),
                        Name = Clean(r[4]),
                        ComProtocol = Clean(r[5]),
                        IPAddress = Clean(r[6]),
                        PortNo = int.Parse(Clean(r[7])),
                        Gateway = Clean(r[8]),
                        Description = Clean(r[9]),
                        Remark = Clean(r[10]),
                        Enabled = bool.Parse(Clean(r[11]))
                    };

                    devices.Add(device);
                }
                catch
                {
                    // skip this row if any parsing fails
                    continue;
                }
            }

            return devices.Where(d => d.Enabled).ToList();
        }
    }
}
