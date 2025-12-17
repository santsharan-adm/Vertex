using IPCSoftware.Core.Interfaces.AppLoggerInterface;
using IPCSoftware.Shared.Models.ConfigModels;
using System.Collections.Generic;
using System.Linq;

namespace IPCSoftware.Services
{
    public class DeviceConfigLoader : BaseService
    {
        public DeviceConfigLoader(
            IAppLogger logger) : base(logger)
        { }
        private string Clean(string input)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(input))
                    return input;

                return input.Trim().Trim('"');
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message, LogType.Diagnostics);
                return string.Empty;
            }
        }

        public List<DeviceInterfaceModel> Load(string filePath)
        {
            try
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
            catch (Exception ex)
            {
                _logger.LogError(ex.Message, LogType.Diagnostics);
                return new  List<DeviceInterfaceModel>(); ;
            }
        }
    }
}
