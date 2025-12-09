using IPCSoftware.Shared.Models.ConfigModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IPCSoftware.Core.Interfaces
{
    public interface IDeviceConfigurationService
    {
        Task InitializeAsync();

        // Device operations
        Task<List<DeviceModel>> GetAllDevicesAsync();
        Task<List<DeviceInterfaceModel>> GetPlcDevicesAsync();
        Task<List<CameraInterfaceModel>> GetCameraDevicesAsync();
        Task<DeviceModel> GetDeviceByIdAsync(int id);
        Task<DeviceModel> AddDeviceAsync(DeviceModel device);
        Task<bool> UpdateDeviceAsync(DeviceModel device);
        Task<bool> DeleteDeviceAsync(int id);

        // PLC Interface Methods
        Task<List<DeviceInterfaceModel>> GetInterfacesByDeviceNoAsync(int deviceNo);
        Task<DeviceInterfaceModel> GetInterfaceByIdAsync(int id);
        Task<DeviceInterfaceModel> AddInterfaceAsync(DeviceInterfaceModel deviceInterface);
        Task<bool> UpdateInterfaceAsync(DeviceInterfaceModel deviceInterface);
        Task<bool> DeleteInterfaceAsync(int id);


        // Camera Interface Methods - NEW
        Task<List<CameraInterfaceModel>> GetCameraInterfacesByDeviceNoAsync(int deviceNo);
        Task<CameraInterfaceModel> GetCameraInterfaceByIdAsync(int id);
        Task<CameraInterfaceModel> AddCameraInterfaceAsync(CameraInterfaceModel cameraInterface);
        Task<bool> UpdateCameraInterfaceAsync(CameraInterfaceModel cameraInterface);
        Task<bool> DeleteCameraInterfaceAsync(int id);

    }
}
