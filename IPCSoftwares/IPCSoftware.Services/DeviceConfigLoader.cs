/******************************************************************************
 * Project      : IPCSoftware-AOI /Bending
 * Module       : DeviceConfigLoader
 * File Name    : DeviceConfigLoader.cs
 * Author       : Rishabh
 * Organization : Vertex Automtion System Pvt Ltd
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
 * 
 *
 ******************************************************************************/

using IPCSoftware.Core.Interfaces.AppLoggerInterface;
using IPCSoftware.Shared.Models.ConfigModels;
using System;
using System.Collections.Generic;
using System.Linq;

namespace IPCSoftware.Services
{
    public class DeviceConfigLoader : BaseService
    {
        private List<DeviceModel> _devices = new List<DeviceModel>();

        public DeviceConfigLoader(IAppLogger logger) : base(logger)
        {
        }

        public List<DeviceModel> Load(string filePath)
        {
            try
            {
                var version = CsvReader.Getversion(filePath);
                var rows = CsvReader.Read(filePath);
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
    }
}