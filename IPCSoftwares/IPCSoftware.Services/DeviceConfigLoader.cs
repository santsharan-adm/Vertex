/******************************************************************************
 * Project      : IPCSoftware-AOI /Bending
 * Module       : DeviceConfigLoader
 * File Name    : DeviceConfigLoader.cs
 * Author       : Rishabh
 * Organization : Motherson Technology Service Limited
 * Created Date : 2026-04-18
 *
 * Description  :
 * Loads and parses device configuration from CSV files, supporting multiple
 * file format versions. Handles device settings with backward compatibility.
 *
 * Change History:
 * ---------------------------------------------------------------------------
 * Date        Author        Version     Description
 * ---------------------------------------------------------------------------
 * 2026-04-18  Rishabh       1.0         Initial creation
 * 2026-04-25  Rishabh       2.0         Refactored to use IFileHandler interface
 *                                       for dependency injection and loose coupling
 *
 ******************************************************************************/

using IPCSoftware.Core.Interfaces.AppLoggerInterface;
using IPCSoftware.Shared.Models.ConfigModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using IPCSoftware.Core.Interfaces;

namespace IPCSoftware.Services
{
    public class DeviceConfigLoader : BaseService
    {
        private readonly IFileHandler _fileHandler;
        private List<DeviceModel> _devices = new List<DeviceModel>();

        public DeviceConfigLoader(IAppLogger logger, IFileHandler fileHandler) : base(logger)
        {
            _fileHandler = fileHandler;
        }

        public List<DeviceModel> Load(string filePath)
        {
            try
            {
                var version = _fileHandler.Getversion(filePath);
                var rows = _fileHandler.Read(filePath);
                var devices = new List<DeviceModel>();

                if (rows.Count == 0)
                {
                    _logger.LogError("Device Configuration Settings Not found", LogType.Error);
                    return devices;
                }

                _devices.Clear();

                if (version == "1.0")
                {
                    foreach (var row in rows)
                    {
                        var device = ParseDeviceCsvLine(row);
                        if (device != null)
                        {
                            _devices.Add(device);
                        }
                    }
                }
                else if (version == "2.0")
                {
                    foreach (var row in rows)
                    {
                        var device = ParseDeviceCsvLine(row);
                        if (device != null)
                        {
                            _devices.Add(device);
                        }
                    }
                }

                return _devices;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message, LogType.Diagnostics);
                return null;
            }
        }

        private DeviceModel ParseDeviceCsvLine(string[] values)
        {
            try
            {
                // DeviceModel requires minimum 9 fields
                if (values.Length < 9)
                    return null;

                var device = new DeviceModel
                {
                    Id = int.Parse(values[0]),
                    DeviceNo = int.Parse(values[1]),
                    DeviceName = values[2],
                    DeviceType = values[3],
                    Make = values[4],
                    Model = values[5],
                    Description = values[6],
                    Remark = values[7],
                    Enabled = bool.Parse(values[8])
                };

                return device;
            }
            catch
            {
                return null;
            }
        }

        // Added by Rishabh - date - 19/04/2026//
        public async Task Save(string filepath ,List<DeviceModel> devices)
        {
            try
            {
                var sb = new StringBuilder();
                string header = _fileHandler.GetHeader(filepath);
                sb.AppendLine(header);

                foreach (var device in devices ?? new List<DeviceModel>())
                {
                    sb.AppendLine($"{device.Id},{device.DeviceNo}," +
                        $"\"{_fileHandler.EscapeCsv(device.DeviceName)}\"," +
                        $"\"{_fileHandler.EscapeCsv(device.DeviceType)}\"," +
                        $"\"{_fileHandler.EscapeCsv(device.Make)}\"," +
                        $"\"{_fileHandler.EscapeCsv(device.Model)}\"," +
                        $"\"{_fileHandler.EscapeCsv(device.Description)}\"," +
                        $"\"{_fileHandler.EscapeCsv(device.Remark)}\"," +
                        $"{device.Enabled}");
                }
                await _fileHandler.WriteCsv(filepath , sb.ToString());             //Added by Rishabh - date - 25/04/2026// 
                                                                                   // await File.WriteAllTextAsync(filepath, sb.ToString(), Encoding.UTF8);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error saving devices CSV: {ex.Message}", LogType.Diagnostics);
                throw;
            }
        }
    }
}