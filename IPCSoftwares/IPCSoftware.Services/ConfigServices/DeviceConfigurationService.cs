using IPCSoftware.Core.Interfaces;
using IPCSoftware.Core.Interfaces.AppLoggerInterface;
using IPCSoftware.Shared.Models;
using IPCSoftware.Shared.Models.ConfigModels;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading.Tasks;
using IPCSoftware.Services;
using System.Windows.Controls;
using System.Windows.Media.Media3D;

namespace IPCSoftware.Services.ConfigServices
{
    public class DeviceConfigurationService :BaseService , IDeviceConfigurationService
    {
        private readonly string _dataFolder;
        private readonly string _devicesCsvPath;
        private readonly string _interfacesCsvPath;

        private List<DeviceModel> _devices;
        private List<DeviceInterfaceModel> _interfaces;

        private readonly string _cameraInterfacesCsvPath;
        private List<CameraInterfaceModel> _cameraInterfaces;
        private readonly CameraConfigLoader _cameraLoader;  //Added by Rishabh -Date 15/04/2026//
        private readonly DeviceConfigLoader _deviceInterfaceLoader;  //Added by Rishabh -Date 17/04/2026//

        private int _nextDeviceId = 1;
        private int _nextInterfaceId = 1;

        public DeviceConfigurationService(
            IOptions<ConfigSettings> configSettings,
            DeviceConfigLoader deviceInterfaceLoader,      //Added by Rishabh -Date 17/04/2026//
            CameraConfigLoader cameraLoader,              //Added by Rishabh -Date 15/04/2026//
            IAppLogger logger) : base (logger)
        {
            var config = configSettings.Value;
            string dataFolderPath = config.DataFolder;
            //   string dataFolderPath = _configuration.GetValue<string>("Config:DataFolder");
            _dataFolder = dataFolderPath ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data");

            if (!Directory.Exists(_dataFolder))
            {
                Directory.CreateDirectory(_dataFolder);
            }

            _devicesCsvPath = Path.Combine(_dataFolder, config.DeviceFileName /* "Devices.csv"*/);
            _interfacesCsvPath = Path.Combine(_dataFolder, config.DeviceInterfacesFileName /* "DeviceInterfaces.csv"*/);
            _cameraInterfacesCsvPath = Path.Combine(_dataFolder,config.CameraInterfacesFileName  /* "CameraInterfaces.csv"*/);
            _deviceInterfaceLoader = deviceInterfaceLoader;    //Added by Rishabh -Date 17/04/2026//
            _cameraLoader = cameraLoader;                //Added by Rishabh -Date 15/04/2026//
            _devices = new List<DeviceModel>();
            _interfaces = new List<DeviceInterfaceModel>();
            _cameraInterfaces = new List<CameraInterfaceModel>();
        }

        public async Task InitializeAsync()
        {
            try
            {
                await LoadDevicesFromCsvAsync();
                await LoadInterfacesFromCsvAsync();
                await LoadCameraInterfacesFromCsvAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message, LogType.Diagnostics);
            }
        }



        // ==================== DEVICE OPERATIONS ====================

        public async Task<List<DeviceModel>> GetAllDevicesAsync()
        {
            return await Task.FromResult(_devices.ToList());
        }

        public async Task<List<DeviceInterfaceModel>> GetDeviceInterfaceAsync() //Modified func() name  by Rishabh - Date -17/04/2026
        {
            try
            {
                if (_interfaces.Count == 0)
                {
                    await LoadInterfacesFromCsvAsync();
                }
                return _interfaces.ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message, LogType.Diagnostics);
                return _interfaces.ToList();
            }
        } 

        public async Task<List<CameraInterfaceModel>> GetCameraDevicesAsync()
        {
            try
            {
                if (_cameraInterfaces.Count == 0)
                {
                    await LoadCameraInterfacesFromCsvAsync();
                }
                return _cameraInterfaces.ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message, LogType.Diagnostics);
                return _cameraInterfaces.ToList();
            }
        }

        public async Task<DeviceModel> GetDeviceByIdAsync(int id)
        {
            return await Task.FromResult(_devices.FirstOrDefault(d => d.Id == id));
        }

        public async Task<DeviceModel> AddDeviceAsync(DeviceModel device)
        {
            try
            {
                device.Id = _nextDeviceId++;
                _devices.Add(device);
                await SaveDevicesToCsvAsync();
                return device;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message, LogType.Diagnostics);
                return device;
            }
        }

        public async Task<bool> UpdateDeviceAsync(DeviceModel device)
        {
            try
            {
                var existing = _devices.FirstOrDefault(d => d.Id == device.Id);
                if (existing == null) return false;

                var index = _devices.IndexOf(existing);
                _devices[index] = device;
                await SaveDevicesToCsvAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message, LogType.Diagnostics);
                return false;
            }
        }

        public async Task<bool> DeleteDeviceAsync(int id)
        {
            try
            {
                var device = _devices.FirstOrDefault(d => d.Id == id);
                if (device == null) return false;

                // Also delete all interfaces for this device
                var interfacesToDelete = _interfaces.Where(i => i.DeviceNo == device.DeviceNo).ToList();
                foreach (var iface in interfacesToDelete)
                {
                    _interfaces.Remove(iface);
                }

                var cameraInterfacesToDelete = _cameraInterfaces.Where(i => i.DeviceNo == device.DeviceNo).ToList();
                foreach (var camIface in cameraInterfacesToDelete)
                {
                    _cameraInterfaces.Remove(camIface);
                }

                _devices.Remove(device);
                await SaveDevicesToCsvAsync();
                await SaveInterfacesToCsvAsync();
                await SaveCameraInterfacesToCsvAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message, LogType.Diagnostics);
                return false;
            }
        }

        // ==================== INTERFACE OPERATIONS ====================

        public async Task<List<DeviceInterfaceModel>> GetInterfacesByDeviceNoAsync(int deviceNo)
        {
            return await Task.FromResult(_interfaces.Where(i => i.DeviceNo == deviceNo).ToList());
        }

        public async Task<DeviceInterfaceModel> GetInterfaceByIdAsync(int id)
        {
            return await Task.FromResult(_interfaces.FirstOrDefault(i => i.Id == id));
        }


        //Camera Interface CRUD Methods
        public async Task<List<CameraInterfaceModel>> GetCameraInterfacesByDeviceNoAsync(int deviceNo)
        {
            return await Task.FromResult(_cameraInterfaces
                .Where(i => i.DeviceNo == deviceNo)
                .ToList());
        }

        public async Task<CameraInterfaceModel> GetCameraInterfaceByIdAsync(int id)
        {
            return await Task.FromResult(_cameraInterfaces.FirstOrDefault(i => i.Id == id));
        }

        public async Task<DeviceInterfaceModel> AddInterfaceAsync(DeviceInterfaceModel deviceInterface)
        {
            try
            {
                deviceInterface.Id = _nextInterfaceId++;
                _interfaces.Add(deviceInterface);
                await SaveInterfacesToCsvAsync();
                return deviceInterface;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message, LogType.Diagnostics);
                throw;
            }

        }

        public async Task<bool> UpdateInterfaceAsync(DeviceInterfaceModel deviceInterface)
        {
            try
            {
                var existing = _interfaces.FirstOrDefault(i => i.Id == deviceInterface.Id);
                if (existing == null) return false;

                var index = _interfaces.IndexOf(existing);
                _interfaces[index] = deviceInterface;
                await SaveInterfacesToCsvAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message, LogType.Diagnostics);
                return false;
            }
        }

        public async Task<bool> DeleteInterfaceAsync(int id)
        {
            try
            {
                var iface = _interfaces.FirstOrDefault(i => i.Id == id);
                if (iface == null) return false;

                _interfaces.Remove(iface);
                await SaveInterfacesToCsvAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message, LogType.Diagnostics);
                return false ;
            }
        }


        public async Task<CameraInterfaceModel> AddCameraInterfaceAsync(CameraInterfaceModel cameraInterface)
        {
            try
            {
                cameraInterface.Id = _cameraInterfaces.Any()
                    ? _cameraInterfaces.Max(i => i.Id) + 1
                    : 1;

                _cameraInterfaces.Add(cameraInterface);
                await SaveCameraInterfacesToCsvAsync();
                return cameraInterface;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message, LogType.Diagnostics);
                throw;
            }
        }

        public async Task<bool> UpdateCameraInterfaceAsync(CameraInterfaceModel cameraInterface)
        {
            try
            {
                var existing = _cameraInterfaces.FirstOrDefault(i => i.Id == cameraInterface.Id);
                if (existing == null) return false;

                var index = _cameraInterfaces.IndexOf(existing);
                _cameraInterfaces[index] = cameraInterface;
                await SaveCameraInterfacesToCsvAsync();
                //await SaveInterfacesToCsvAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message, LogType.Diagnostics);
                return false;
            }
        }

        public async Task<bool> DeleteCameraInterfaceAsync(int id)
        {
            try
            {
                var cameraInterface = _cameraInterfaces.FirstOrDefault(i => i.Id == id);
                if (cameraInterface == null) return false;

                _cameraInterfaces.Remove(cameraInterface);
                await SaveCameraInterfacesToCsvAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message, LogType.Diagnostics);
                return false;
            }
        }


        // ==================== CSV OPERATIONS - DEVICES ====================

        private async Task LoadDevicesFromCsvAsync()
        {
            try
            {
            
            //if (!File.Exists(_devicesCsvPath)) 
            //    {
            //    await SaveDevicesToCsvAsync();
            //    return;
            //}
                var rows = CsvReader.Read(_devicesCsvPath);  //Modified by Rishabh - date - 13/04/2026//
                 if (rows.Count == 0) return;
                _devices.Clear();
                //var lines = await File.ReadAllLinesAsync(_devicesCsvPath);
                //if (lines.Length <= 1) return;

                // _devices.Clear();
                //for (int i = 1; i < lines.Length; i++)
                //{
                //    var device = ParseDeviceCsvLine(lines[i]);
                //    if (device != null)
                //    {
                //        _devices.Add(device);
                //        if (device.Id >= _nextDeviceId)
                //            _nextDeviceId = device.Id + 1;
                //    }
                //}
                //Modified by Rishabh - date - 13/04/2026//
                foreach (var values in rows)
                {
                    var device = ParseDeviceCsvLine(values);
                    if (device != null)
                    {
                        _devices.Add(device);
                        if (device.Id >= _nextDeviceId) 
                            _nextDeviceId = device.Id + 1;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error loading devices CSV: {ex.Message}", LogType.Diagnostics);
            }
        }

        private async Task SaveDevicesToCsvAsync()
        {
            try
            {
                var sb = new StringBuilder();
                sb.AppendLine("Id,DeviceNo,DeviceName,DeviceType,Make,Model,Description,Remark,Enabled");

                foreach (var device in _devices)
                {
                    sb.AppendLine($"{device.Id},{device.DeviceNo}," +
                        $"\"{EscapeCsv(device.DeviceName)}\"," +
                        $"\"{EscapeCsv(device.DeviceType)}\"," +
                        $"\"{EscapeCsv(device.Make)}\"," +
                        $"\"{EscapeCsv(device.Model)}\"," +
                        $"\"{EscapeCsv(device.Description)}\"," +
                        $"\"{EscapeCsv(device.Remark)}\"," +
                        $"{device.Enabled}");
                }

                await File.WriteAllTextAsync(_devicesCsvPath, sb.ToString(), Encoding.UTF8);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error saving devices CSV: {ex.Message}", LogType.Diagnostics);
                throw;
            }
        }



        // Camera Interface CSV Methods
        //Modifed by Rishabh -Date 15/04/2026//
        private async Task LoadCameraInterfacesFromCsvAsync()
        {

            try
            {         
                  _cameraInterfaces =_cameraLoader.Load(_cameraInterfacesCsvPath);           //Modified by Rishabh - date - 15/04/2026//
              
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error loading camera interfaces CSV: {ex.Message}", LogType.Diagnostics);
            }
        }





        private DeviceModel ParseDeviceCsvLine(string[] values)        //Modified by Rishabh - date - 13/04/2026//
        {
            try
            {
                //var values = SplitCsvLine(line);
                if (values.Length < 9) return null;

                return new DeviceModel
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
            }
            catch
            {
                return null;
            }
        }



        // ==================== CSV OPERATIONS - INTERFACES ====================

        private async Task LoadInterfacesFromCsvAsync()                          //Modified by Rishabh - date - 17/04/2026//
        {
            try
            {   
                _interfaces = _deviceInterfaceLoader.Load(_interfacesCsvPath);     //Added by Rishabh - date - 17/04/2026//
             }
            catch (Exception ex)
            {
                _logger.LogError($"Error loading interfaces CSV: {ex.Message}", LogType.Diagnostics);
            }
        }
        //Modfied by Rishabh - date - 08/04/2026//

        private async Task SaveCameraInterfacesToCsvAsync()
        {
            try
            {
                var sb = new StringBuilder();
                sb.AppendLine("Id,DeviceNo,DeviceName,Name,Protocol,IPAddress,Port,Gateway,Username,Password,AnonymousLogin,RemotePath,LocalDirectory,Enabled,Description,Remark," +
                    "QrCodeImagePath,TempImgFolder,ImageRootFolder,MetadataStyle,CurrentCycleStateFileName," +
                    "Client_Version,Client_Date,Client_Time,Client_VisionVendor,Client_StationID,Client_StationNickname,Client_DUTSerialNumber,Client_ProcessCommand,Client_CameraNumber," +
                    "Client_XPixelSizeMM,Client_YPixelSizeMM,Client_CameraGain,Client_CameraExposure,Client_NumberOfLightSettings,Client_LightSetting1,Client_LightSettingN,Client_DUTColor,Client_ImageNickname," +
                    "Vendor_Version,Vendor_Date,Vendor_Time,Vendor_VisionVendor,Vendor_StationID,Vendor_StationNickname,Vendor_DUTSerialNumber,Vendor_ProcessCommand,Vendor_CameraNumber," +
                    "Vendor_XPixelSizeMM,Vendor_YPixelSizeMM,Vendor_CameraGain,Vendor_CameraExposure,Vendor_NumberOfLightSettings,Vendor_LightSetting1,Vendor_LightSettingN,Vendor_DUTColor,Vendor_ImageNickname");

                foreach (var cam in _cameraInterfaces)
                {
                    sb.AppendLine($"{cam.Id}," +
                        $"{cam.DeviceNo}," +
                        $"\"{EscapeCsv(cam.DeviceName)}\"," +
                        $"\"{EscapeCsv(cam.Name)}\"," +
                        $"\"{EscapeCsv(cam.Protocol)}\"," +
                        $"\"{EscapeCsv(cam.IPAddress)}\"," +
                        $"{cam.Port}," +
                        $"\"{EscapeCsv(cam.Gateway)}\"," +
                        $"\"{EscapeCsv(cam.Username)}\"," +
                        $"\"{EscapeCsv(cam.Password)}\"," +
                        $"{cam.AnonymousLogin}," +
                        $"\"{EscapeCsv(cam.RemotePath)}\"," +
                        $"\"{EscapeCsv(cam.LocalDirectory)}\"," +
                        $"{cam.Enabled}," +
                        $"\"{EscapeCsv(cam.Description)}\"," +
                        $"\"{EscapeCsv(cam.Remark)}\"," +
                        // CCD Primary fields
                        $"\"{EscapeCsv(cam.QrCodeImagePath)}\"," +
                        $"\"{EscapeCsv(cam.TempImgFolder)}\"," +
                        $"\"{EscapeCsv(cam.ImageRootFolder)}\"," +
                        $"\"{EscapeCsv(cam.MetadataStyle)}\"," +
                        $"\"{EscapeCsv(cam.CurrentCycleStateFileName)}\"," +
                        // Client metadata
                        $"\"{EscapeCsv(cam.Client_Version)}\"," +
                        $"\"{EscapeCsv(cam.Client_Date)}\"," +
                        $"\"{EscapeCsv(cam.Client_Time)}\"," +
                        $"\"{EscapeCsv(cam.Client_VisionVendor)}\"," +
                        $"\"{EscapeCsv(cam.Client_StationID)}\"," +
                        $"\"{EscapeCsv(cam.Client_StationNickname)}\"," +
                        $"\"{EscapeCsv(cam.Client_DUTSerialNumber)}\"," +
                        $"\"{EscapeCsv(cam.Client_ProcessCommand)}\"," +
                        $"\"{EscapeCsv(cam.Client_CameraNumber)}\"," +
                        $"\"{EscapeCsv(cam.Client_XPixelSizeMM)}\"," +
                        $"\"{EscapeCsv(cam.Client_YPixelSizeMM)}\"," +
                        $"\"{EscapeCsv(cam.Client_CameraGain)}\"," +
                        $"\"{EscapeCsv(cam.Client_CameraExposure)}\"," +
                        $"\"{EscapeCsv(cam.Client_NumberOfLightSettings)}\"," +
                        $"\"{EscapeCsv(cam.Client_LightSetting1)}\"," +
                        $"\"{EscapeCsv(cam.Client_LightSettingN)}\"," +
                        $"\"{EscapeCsv(cam.Client_DUTColor)}\"," +
                        $"\"{EscapeCsv(cam.Client_ImageNickname)}\"," +
                        // Vendor metadata
                        $"\"{EscapeCsv(cam.Vendor_Version)}\"," +
                        $"\"{EscapeCsv(cam.Vendor_Date)}\"," +
                        $"\"{EscapeCsv(cam.Vendor_Time)}\"," +
                        $"\"{EscapeCsv(cam.Vendor_VisionVendor)}\"," +
                        $"\"{EscapeCsv(cam.Vendor_StationID)}\"," +
                        $"\"{EscapeCsv(cam.Vendor_StationNickname)}\"," +
                        $"\"{EscapeCsv(cam.Vendor_DUTSerialNumber)}\"," +
                        $"\"{EscapeCsv(cam.Vendor_ProcessCommand)}\"," +
                        $"\"{EscapeCsv(cam.Vendor_CameraNumber)}\"," +
                        $"\"{EscapeCsv(cam.Vendor_XPixelSizeMM)}\"," +
                        $"\"{EscapeCsv(cam.Vendor_YPixelSizeMM)}\"," +
                        $"\"{EscapeCsv(cam.Vendor_CameraGain)}\"," +
                        $"\"{EscapeCsv(cam.Vendor_CameraExposure)}\"," +
                        $"\"{EscapeCsv(cam.Vendor_NumberOfLightSettings)}\"," +
                        $"\"{EscapeCsv(cam.Vendor_LightSetting1)}\"," +
                        $"\"{EscapeCsv(cam.Vendor_LightSettingN)}\"," +
                        $"\"{EscapeCsv(cam.Vendor_DUTColor)}\"," +
                        $"\"{EscapeCsv(cam.Vendor_ImageNickname)}\"");
                }
                await File.WriteAllTextAsync(_cameraInterfacesCsvPath, sb.ToString(), Encoding.UTF8);
            }
            catch (Exception ex)
            {
                    _logger.LogError($"Error saving interfaces CSV: {ex.Message}", LogType.Diagnostics);
                throw;
            }
        }


        private async Task SaveInterfacesToCsvAsync()
        {
            try
            {
                var sb = new StringBuilder();
                sb.AppendLine("Id,DeviceNo,DeviceName,UnitNo,Name,ComProtocol,IPAddress,PortNo,Gateway,Description,Remark,Enabled");

                foreach (var iface in _interfaces)
                {
                    sb.AppendLine($"{iface.Id},{iface.DeviceNo}," +
                        $"\"{EscapeCsv(iface.DeviceName)}\"," +
                        $"{iface.UnitNo}," +
                        $"\"{EscapeCsv(iface.Name)}\"," +
                        $"\"{EscapeCsv(iface.ComProtocol)}\"," +
                        $"\"{EscapeCsv(iface.IPAddress)}\"," +
                        $"{iface.PortNo}," +
                        $"\"{EscapeCsv(iface.Gateway)}\"," +
                        $"\"{EscapeCsv(iface.Description)}\"," +
                        $"\"{EscapeCsv(iface.Remark)}\"," +
                        $"{iface.Enabled}");
                }

                await File.WriteAllTextAsync(_interfacesCsvPath, sb.ToString(), Encoding.UTF8);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error saving interfaces CSV: {ex.Message}", LogType.Diagnostics);
                throw;
            }
        }

       

        // ==================== HELPERS ====================

        
        private string EscapeCsv(string value)
        {
            if (string.IsNullOrEmpty(value))
                return string.Empty;

            if (value.Contains("\""))
                return value.Replace("\"", "\"\"");

            return value;
        }





    }
}
