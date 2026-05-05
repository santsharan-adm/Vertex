using IPCSoftware.Core.Interfaces.AppLoggerInterface;
using IPCSoftware.Shared.Models.ConfigModels;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using IPCSoftware.Core.Interfaces;

namespace IPCSoftware.Services
{
    public class DeviceInterfaceConfigLoader : BaseService
    {
        private readonly IFileHandler _fileHandler;
        private List<DeviceInterfaceModel> _deviceInterfaces = new List<DeviceInterfaceModel>();

        public DeviceInterfaceConfigLoader(IAppLogger logger, IFileHandler fileHandler) : base(logger)
        {
            _fileHandler = fileHandler;
        }

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
                string version = _fileHandler.Getversion(filePath);
                var rows = _fileHandler.Read(filePath);  // static call, returns string[]
                _deviceInterfaces.Clear();
                // var devices = new List<DeviceInterfaceModel>();

                if (version == "2.0")
                {
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

                            _deviceInterfaces.Add(device);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex.Message, LogType.Diagnostics);
                            return null;
                        }
                    }
                }

                else 
                {
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

                            _deviceInterfaces.Add(device);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex.Message, LogType.Diagnostics);
                            return null;
                        }
                    }
                }


                    return _deviceInterfaces;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message, LogType.Diagnostics);
                return new List<DeviceInterfaceModel>();
            }
        }

        //Added by Rishabh - date - 19/04/2026//
        public async Task Save(string filepath , List<DeviceInterfaceModel> DeviceInterfaces)
        {
            try
            {
                var sb = new StringBuilder();
                string header = _fileHandler.GetHeader(filepath);
                sb.AppendLine(header);

                foreach (var iface in DeviceInterfaces ?? new List<DeviceInterfaceModel>())
                {
                    sb.AppendLine($"{iface.Id},{iface.DeviceNo}," +
                        $"\"{_fileHandler.EscapeCsv(iface.DeviceName)}\"," +
                        $"{iface.UnitNo}," +
                        $"\"{_fileHandler.EscapeCsv(iface.Name)}\"," +
                        $"\"{_fileHandler.EscapeCsv(iface.ComProtocol)}\"," +
                        $"\"{_fileHandler.EscapeCsv(iface.IPAddress)}\"," +
                        $"{iface.PortNo}," +
                        $"\"{_fileHandler.EscapeCsv(iface.Gateway)}\"," +
                        $"\"{_fileHandler.EscapeCsv(iface.Description)}\"," +
                        $"\"{_fileHandler.EscapeCsv(iface.Remark)}\"," +
                        $"{iface.Enabled}");
                }
                await _fileHandler.WriteCsv(filepath, sb.ToString());          //Added by Rishabh - date - 25/04/2026//
                                                                               //await File.WriteAllTextAsync(filepath, sb.ToString(), Encoding.UTF8);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error saving interfaces CSV: {ex.Message}", LogType.Diagnostics);
                throw;
            }
        }
    }
}