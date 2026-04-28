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
    public class DeviceConfigurationService : BaseService, IDeviceConfigurationService
    {
        private readonly string _dataFolder;
        private readonly string _devicesCsvPath;
        private readonly string _interfacesCsvPath;
        private readonly string _cameraInterfacesCsvPath;
        private readonly string _tagConfigFilePath;                          //Added by Rishabh - Date 27/04/2026

        private List<DeviceModel> _devices;
        private List<DeviceInterfaceModel> _interfaces;
        private List<CameraInterfaceModel> _cameraInterfaces;
        private List<PLCTagConfigurationModel> _tags;                        //Added by Rishabh - Date 27/04/2026

        private readonly CameraConfigLoader _cameraLoader;                    // Added by Rishabh - Date 15/04/2026
        private readonly DeviceInterfaceConfigLoader _deviceInterfaceLoader;  // Added by Rishabh - Date 17/04/2026
        private readonly DeviceConfigLoader _deviceLoader;                    // Added by Rishabh - Date 18/04/2026
        private readonly TagConfigLoader _tagLoader;                          // Added by Rishabh - Date 27/04/2026

        private int _nextDeviceId = 1;
        private int _nextInterfaceId = 1;
        private int _nextCameraId = 1;                                        //Added by Rishabh -Date 27-04-2026
        private int _nextTagId = 1;                                           //Added by Rishabh - Date 27/04/2026

        public DeviceConfigurationService(
            IOptions<ConfigSettings> configSettings,
            ConfigLoaderService configLoaderService ,                           // Added by Rishabh - Date 18/04/2026
            TagConfigLoader tagConfigLoader,                                    //Added by Rishabh - Date 27/04/2026
            IAppLogger logger) : base(logger)
        {
            var config = configSettings.Value;
            string dataFolderPath = config.DataFolder;
            // string dataFolderPath = _configuration.GetValue<string>("Config:DataFolder");
            _dataFolder = dataFolderPath ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data");

            if (!Directory.Exists(_dataFolder))
            {
                Directory.CreateDirectory(_dataFolder);
            }

            _devicesCsvPath = Path.Combine(_dataFolder, config.DeviceFileName /* "Devices.csv"*/);
            _interfacesCsvPath = Path.Combine(_dataFolder, config.DeviceInterfacesFileName /* "DeviceInterfaces.csv"*/);
            _cameraInterfacesCsvPath = Path.Combine(_dataFolder, config.CameraInterfacesFileName  /* "CameraInterfaces.csv"*/);
            _tagConfigFilePath = Path.Combine(_dataFolder, config.PlcTagsFileName  /* Added new * "PlcTags.csv*/);
            //_deviceInterfaceLoader = deviceInterfaceLoader;                 // Added by Rishabh - Date 17/04/2026
            //_cameraLoader = cameraLoader;                                   // Added by Rishabh - Date 15/04/2026
            //_deviceLoader = deviceLoader;                                   // Added by Rishabh - Date 18/04/2026
            _devices = new List<DeviceModel>();
            _interfaces = new List<DeviceInterfaceModel>();
            _cameraInterfaces = new List<CameraInterfaceModel>();
            

            //ConfigLoaderService configLoaderService = new ConfigLoaderService();
            _deviceLoader = configLoaderService.GetDeviceConfigLoader(_devicesCsvPath, logger);
            _deviceInterfaceLoader =configLoaderService.GetDeviceInterfaceConfigLoader(_interfacesCsvPath, logger);
            _cameraLoader = configLoaderService.GetCameraConfigLoader(_cameraInterfacesCsvPath, logger);
            _tagLoader = configLoaderService.GetTagConfigLoader(_tagConfigFilePath, logger);                         //Added by Rishabh - Date 27/04/2026
          //  _tagLoader = tagConfigLoader;                                                                            //Added by Rishabh - Date 27/04/2026
            _tags = new List<PLCTagConfigurationModel>();                                                            //Added by Rishabh - Date 27/04/2026


        }

        public async Task InitializeAsync()
        {
            try
            {
                await LoadDevicesFromCsvAsync();
                await LoadInterfacesFromCsvAsync();
                await LoadCameraInterfacesFromCsvAsync();
                await LoadTagsInternalAsync();                            //Added new Today
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

        public async Task<List<DeviceInterfaceModel>> GetDeviceInterfaceAsync() // Modified func() name by Rishabh - Date 17/04/2026
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
                await _deviceLoader.Save(_devicesCsvPath , _devices);   // Added by Rishabh - Date 19/04/2026
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
                if (existing == null) 
                    return false;

                var index = _devices.IndexOf(existing);
                _devices[index] = device;
                await _deviceLoader.Save(_devicesCsvPath , _devices);   // Added by Rishabh - Date 19/04/2026
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
                if (device == null) 
                    return false;

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
                await _deviceLoader.Save(_devicesCsvPath , _devices);               // Added by Rishabh - Date 19/04/2026
                await _deviceInterfaceLoader.Save(_interfacesCsvPath , _interfaces);   // Added by Rishabh - Date 19/04/2026
                await _cameraLoader.Save(_cameraInterfacesCsvPath , _cameraInterfaces);      // Added by Rishabh - Date 19/04/2026
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

        // Camera Interface CRUD Methods
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
                await _deviceInterfaceLoader.Save(_interfacesCsvPath , _interfaces);   // Added by Rishabh - Date 19/04/2026
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
                if (existing == null) 
                    return false;

                var index = _interfaces.IndexOf(existing);
                _interfaces[index] = deviceInterface;
                await _deviceInterfaceLoader.Save(_interfacesCsvPath,_interfaces);   // Added by Rishabh - Date 19/04/2026
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
                if (iface == null) 
                    return false;

                _interfaces.Remove(iface);
                await _deviceInterfaceLoader.Save(_interfacesCsvPath , _interfaces);   // Added by Rishabh - Date 19/04/2026
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message, LogType.Diagnostics);
                return false;
            }
        }

        public async Task<CameraInterfaceModel> AddCameraInterfaceAsync(CameraInterfaceModel cameraInterface)
        {
            try
            {
                // cameraInterface.Id = _cameraInterfaces.Any() ? _cameraInterfaces.Max(i => i.Id) + 1 : 1;
                cameraInterface.Id = +_nextCameraId++;                                                          //Added by Rishabh -Date 27-04-2026
                _cameraInterfaces.Add(cameraInterface);
                await _cameraLoader.Save(_cameraInterfacesCsvPath, _cameraInterfaces);   // Added by Rishabh - Date 19/04/2026
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
                if (existing == null) 
                    return false;

                var index = _cameraInterfaces.IndexOf(existing);
                _cameraInterfaces[index] = cameraInterface;
                await _cameraLoader.Save(_cameraInterfacesCsvPath, _cameraInterfaces);   // Added by Rishabh - Date 19/04/2026
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
                if (cameraInterface == null) 
                    return false;

                _cameraInterfaces.Remove(cameraInterface);
                await _cameraLoader.Save(_cameraInterfacesCsvPath, _cameraInterfaces);
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
                _devices = _deviceLoader.Load(_devicesCsvPath);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error loading devices CSV: {ex.Message}", LogType.Diagnostics);
            }
        }

        // ==================== CSV OPERATIONS - INTERFACES ====================

        private async Task LoadInterfacesFromCsvAsync()
        {
            try
            {
                _interfaces = _deviceInterfaceLoader.Load(_interfacesCsvPath);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error loading interfaces CSV: {ex.Message}", LogType.Diagnostics);
            }
        }

        // ==================== CSV OPERATIONS - CAMERA INTERFACES ====================

        private async Task LoadCameraInterfacesFromCsvAsync()
        {
            try
            {
                _cameraInterfaces = _cameraLoader.Load(_cameraInterfacesCsvPath);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error loading camera interfaces CSV: {ex.Message}", LogType.Diagnostics);
            }
        }


        // ================ PLC TAG CONFIGURATION OPERATIONS ====================//
        // =======-----------Added new methods for PLC Tag Configuration Management by Rishabh - Date 27/04/2026 ---------=======

        private async Task LoadTagsInternalAsync()
        {
            try
            {

                // FIX: Use the dedicated TagConfigLoader (now accessible via using directive)
                var reloadedTags = _tagLoader.Load(_tagConfigFilePath);

                // Thread-safe update of the internal cache list
                _tags = reloadedTags;



                // Update the next ID counter
                if (_tags.Any())
                {
                    _nextTagId = _tags.Max(t => t.Id) + 1;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error loading PLC tags CSV: {ex.Message}", LogType.Diagnostics);
            }
        }

        public async Task<List<PLCTagConfigurationModel>> GetAllTagsAsync()
        {
            try
            {
                if (_tags == null || _tags.Count == 0)
                {
                    await LoadTagsInternalAsync();                                     //Added new today
                }
                return _tags.ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message, LogType.Diagnostics);
                throw;
            }
        }

        // FIX CS0535: IMPLEMENT THE REQUIRED METHOD FOR DYNAMIC RELOAD
        public async Task<List<PLCTagConfigurationModel>> ReloadTagsAsync()
        {
            try
            {
                await LoadTagsInternalAsync();                                               //Added new today
                return _tags.ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message, LogType.Diagnostics);
                return null;
            }
        }

        public async Task<PLCTagConfigurationModel> GetTagByIdAsync(int id)
        {
            return await Task.FromResult(_tags.FirstOrDefault(t => t.Id == id));
        }

        public async Task<PLCTagConfigurationModel> AddTagAsync(PLCTagConfigurationModel tag)
        {
            try
            {
                tag.Id = _nextTagId++;
                _tags.Add(tag);
                await _tagLoader.Save(_tagConfigFilePath, _tags);
                //await SaveToCsvAsync();
                return tag;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message, LogType.Diagnostics);
                throw;
            }
        }

        public async Task<bool> UpdateTagAsync(PLCTagConfigurationModel tag)
        {
            try
            {
                var existing = _tags.FirstOrDefault(t => t.Id == tag.Id);
                if (existing == null) return false;

                var index = _tags.IndexOf(existing);
                _tags[index] = tag;
                await _tagLoader.Save(_tagConfigFilePath, _tags);
                // await SaveToCsvAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message, LogType.Diagnostics);
                return false;
            }
        }

        public async Task<bool> DeleteTagAsync(int id)
        {
            try
            {
                var tag = _tags.FirstOrDefault(t => t.Id == id);
                if (tag == null) return false;

                _tags.Remove(tag);
                await _tagLoader.Save(_tagConfigFilePath, _tags);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message, LogType.Diagnostics);
                return false;
            }
        }
    }
}
