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
        Task<List<DeviceInterfaceModel>> GetDeviceInterfaceAsync(); //Modified by Rishabh -Date -17/04/2026
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



        // ================ PLC TAG CONFIGURATION OPERATIONS ====================//


        Task<List<PLCTagConfigurationModel>> GetAllTagsAsync();


        // FIX CS0535: IMPLEMENT THE REQUIRED METHOD FOR DYNAMIC RELOAD
        Task<List<PLCTagConfigurationModel>> ReloadTagsAsync();


        Task<PLCTagConfigurationModel> GetTagByIdAsync(int id);


        Task<PLCTagConfigurationModel> AddTagAsync(PLCTagConfigurationModel tag);


        Task<bool> UpdateTagAsync(PLCTagConfigurationModel tag);


        Task<bool> DeleteTagAsync(int id);


    }
}
