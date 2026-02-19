
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;
using IPCSoftware.Services.ConfigServices;
using IPCSoftware.Shared.Models;
using IPCSoftware.Core.Interfaces.AppLoggerInterface;
using IPCSoftware.Shared.Models.ConfigModels;

namespace IPCSoftware_UnitTesting.Service_Tests.ConfigServices
{
    public class DeviceConfigurationServiceTests : IDisposable
    {
        private readonly string _tempDir;
        private readonly Mock<IAppLogger> _loggerMock;

        public DeviceConfigurationServiceTests()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), "DeviceConfigTests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_tempDir);
            _loggerMock = new Mock<IAppLogger>();
        }

        private DeviceConfigurationService CreateService(string dataFolder = null)
        {
            var cfg = new ConfigSettings
            {
                DataFolder = dataFolder ?? _tempDir,
                DeviceFileName = "Devices.csv",
                DeviceInterfacesFileName = "DeviceInterfaces.csv",
                CameraInterfacesFileName = "CameraInterfaces.csv"
            };
            var options = Options.Create(cfg);
            return new DeviceConfigurationService(options, _loggerMock.Object);
        }

        [Fact]
        public async Task Devices_CRUD_Workflow()
        {
            var svc = CreateService();

            await svc.InitializeAsync();

            // Initially empty
            var all = await svc.GetAllDevicesAsync();
            Assert.Empty(all);

            // Add device
            var device = new DeviceModel { DeviceNo = 10, DeviceName = "DevA", DeviceType = "PLC", Make = "M", Model = "X", Description = "D", Remark = "R", Enabled = true };
            var added = await svc.AddDeviceAsync(device);
            Assert.Equal(1, added.Id);
            Assert.Equal("DevA", added.DeviceName);

            // Get All / ById
            all = await svc.GetAllDevicesAsync();
            Assert.Single(all);
            var byId = await svc.GetDeviceByIdAsync(added.Id);
            Assert.NotNull(byId);
            Assert.Equal(added.DeviceNo, byId.DeviceNo);

            // Update
            added.DeviceName = "DevA-Updated";
            var updated = await svc.UpdateDeviceAsync(added);
            Assert.True(updated);
            var afterUpdate = await svc.GetDeviceByIdAsync(added.Id);
            Assert.Equal("DevA-Updated", afterUpdate.DeviceName);

            // Delete
            var deleted = await svc.DeleteDeviceAsync(added.Id);
            Assert.True(deleted);
            var afterDelete = await svc.GetAllDevicesAsync();
            Assert.Empty(afterDelete);
        }

        [Fact]
        public async Task Interface_CRUD_Workflow()
        {
            var svc = CreateService();

            await svc.InitializeAsync();

            // Prepare device
            var device = await svc.AddDeviceAsync(new DeviceModel { DeviceNo = 20, DeviceName = "DevB" });

            var iface = new DeviceInterfaceModel
            {
                DeviceNo = device.DeviceNo,
                DeviceName = device.DeviceName,
                UnitNo = 1,
                Name = "IFACE1",
                ComProtocol = "Modbus",
                IPAddress = "192.168.0.1",
                PortNo = 502,
                Gateway = "GW",
                Description = "desc",
                Remark = "remark",
                Enabled = true
            };

            var added = await svc.AddInterfaceAsync(iface);
            Assert.Equal(1, added.Id);

            var byDeviceNo = await svc.GetInterfacesByDeviceNoAsync(device.DeviceNo);
            Assert.Single(byDeviceNo);
            Assert.Equal("IFACE1", byDeviceNo[0].Name);

            var byId = await svc.GetInterfaceByIdAsync(added.Id);
            Assert.NotNull(byId);
            Assert.Equal(502, byId.PortNo);

            // Update
            added.Name = "IFACE1-Updated";
            var updateResult = await svc.UpdateInterfaceAsync(added);
            Assert.True(updateResult);
            var afterUpdate = await svc.GetInterfaceByIdAsync(added.Id);
            Assert.Equal("IFACE1-Updated", afterUpdate.Name);

            // Delete
            var deleteResult = await svc.DeleteInterfaceAsync(added.Id);
            Assert.True(deleteResult);
            var afterDelete = await svc.GetInterfacesByDeviceNoAsync(device.DeviceNo);
            Assert.Empty(afterDelete);
        }

        [Fact]
        public async Task CameraInterface_CRUD_Workflow()
        {
            var svc = CreateService();

            await svc.InitializeAsync();

            // Prepare device
            var device = await svc.AddDeviceAsync(new DeviceModel { DeviceNo = 30, DeviceName = "DevC" });

            var cam = new CameraInterfaceModel
            {
                DeviceNo = device.DeviceNo,
                DeviceName = device.DeviceName,
                Name = "Cam1",
                Protocol = "FTP",
                IPAddress = "10.0.0.1",
                Port = 21,
                Gateway = "GW",
                Username = "user",
                Password = "pass",
                AnonymousLogin = false,
                RemotePath = "/remote",
                LocalDirectory = "C:\\local",
                Enabled = true,
                Description = "cam desc",
                Remark = "cam rem"
            };

            var added = await svc.AddCameraInterfaceAsync(cam);
            Assert.Equal(1, added.Id);

            var byDeviceNo = await svc.GetCameraInterfacesByDeviceNoAsync(device.DeviceNo);
            Assert.Single(byDeviceNo);
            Assert.Equal("Cam1", byDeviceNo[0].Name);

            var byId = await svc.GetCameraInterfaceByIdAsync(added.Id);
            Assert.NotNull(byId);
            Assert.Equal(21, byId.Port);

            // Update
            added.Name = "Cam1-Updated";
            var updateResult = await svc.UpdateCameraInterfaceAsync(added);
            Assert.True(updateResult);
            var afterUpdate = await svc.GetCameraInterfaceByIdAsync(added.Id);
            Assert.Equal("Cam1-Updated", afterUpdate.Name);

            // Delete
            var deleteResult = await svc.DeleteCameraInterfaceAsync(added.Id);
            Assert.True(deleteResult);
            var afterDelete = await svc.GetCameraInterfacesByDeviceNoAsync(device.DeviceNo);
            Assert.Empty(afterDelete);
        }

        [Fact]
        public async Task Persistence_Between_Service_Instances()
        {
            // Create first instance and add data
            var svc1 = CreateService();
            await svc1.InitializeAsync();

            var deviceA = await svc1.AddDeviceAsync(new DeviceModel { DeviceNo = 40, DeviceName = "PersistDev" });
            var iface = await svc1.AddInterfaceAsync(new DeviceInterfaceModel { DeviceNo = deviceA.DeviceNo, DeviceName = deviceA.DeviceName, UnitNo = 5, Name = "PersistIF", ComProtocol = "P" });
            var cam = await svc1.AddCameraInterfaceAsync(new CameraInterfaceModel { DeviceNo = deviceA.DeviceNo, DeviceName = deviceA.DeviceName, Name = "PersistCam", Protocol = "FTP", Port = 2121 });

            // Create a new service instance pointing to same folder to ensure loading from CSV works
            var svc2 = CreateService(_tempDir);
            await svc2.InitializeAsync();

            var devices2 = await svc2.GetAllDevicesAsync();
            Assert.Single(devices2);
            Assert.Equal("PersistDev", devices2[0].DeviceName);

            var ifaces2 = await svc2.GetInterfacesByDeviceNoAsync(deviceA.DeviceNo);
            Assert.Single(ifaces2);
            Assert.Equal("PersistIF", ifaces2[0].Name);

            var cams2 = await svc2.GetCameraInterfacesByDeviceNoAsync(deviceA.DeviceNo);
            Assert.Single(cams2);
            Assert.Equal("PersistCam", cams2[0].Name);

            // Add another device to svc2 to verify id increments after load
            var deviceB = await svc2.AddDeviceAsync(new DeviceModel { DeviceNo = 41, DeviceName = "SecondDev" });
            Assert.Equal(2, deviceB.Id);
        }

        public void Dispose()
        {
            try
            {
                if (Directory.Exists(_tempDir))
                    Directory.Delete(_tempDir, true);
            }
            catch
            {
                // best-effort cleanup
            }
        }
    }
}