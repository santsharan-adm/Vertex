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
        Task<DeviceModel> GetDeviceByIdAsync(int id);
        Task<DeviceModel> AddDeviceAsync(DeviceModel device);
        Task<bool> UpdateDeviceAsync(DeviceModel device);
        Task<bool> DeleteDeviceAsync(int id);

        // Interface operations
        Task<List<DeviceInterfaceModel>> GetInterfacesByDeviceNoAsync(int deviceNo);
        Task<DeviceInterfaceModel> GetInterfaceByIdAsync(int id);
        Task<DeviceInterfaceModel> AddInterfaceAsync(DeviceInterfaceModel deviceInterface);
        Task<bool> UpdateInterfaceAsync(DeviceInterfaceModel deviceInterface);
        Task<bool> DeleteInterfaceAsync(int id);
    }
}
