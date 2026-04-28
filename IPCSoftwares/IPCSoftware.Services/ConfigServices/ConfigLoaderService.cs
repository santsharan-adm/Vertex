using IPCSoftware.Core.Interfaces.AppLoggerInterface;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.IO;
namespace IPCSoftware.Services.ConfigServices
{
    public class ConfigLoaderService
    {
        //Device Config
        public DeviceConfigLoader GetDeviceConfigLoader(string filename, IAppLogger logger)
        {
            DeviceConfigLoader _deviceLoader = null;
            if (Path.GetExtension(filename) == ".csv")
            {
                _deviceLoader = new DeviceConfigLoader(logger, new CsvManager(logger));
            }

            else
            {
                throw new NotImplementedException("Device Config Loader not implemented");
            }

            return _deviceLoader;
        }


        // Device Interface Config

        public DeviceInterfaceConfigLoader GetDeviceInterfaceConfigLoader(string filename, IAppLogger logger)
        {
            DeviceInterfaceConfigLoader _deviceInetrfaceLoader = null;
            if (Path.GetExtension(filename) == ".csv")
            {
                _deviceInetrfaceLoader = new DeviceInterfaceConfigLoader(logger, new CsvManager(logger));
            }

            else
            {
                throw new NotImplementedException("Device Interface Config Loader not implemented");
            }

            return _deviceInetrfaceLoader;
        }

        //Camera Config
        public CameraConfigLoader GetCameraConfigLoader(string filename, IAppLogger logger)
        {
            CameraConfigLoader _cameraLoader = null;
            if (Path.GetExtension(filename) == ".csv")
            {
                _cameraLoader = new CameraConfigLoader(logger, new CsvManager(logger));
            }

            else
            {
                throw new NotImplementedException("Camera Interface Config Loader not implemented");
            }

            return _cameraLoader;
        }
        //Tag Config
        public TagConfigLoader GetTagConfigLoader(string filename, IAppLogger logger)
        {
            TagConfigLoader _tagLoader = null;
            if (Path.GetExtension(filename) == ".csv")
            {
                _tagLoader = new TagConfigLoader(logger, new CsvManager(logger));
            }
            else
            {
                throw new NotImplementedException("Tag Config Loader not implemented");
            }
            return _tagLoader;
        }

    }
}
