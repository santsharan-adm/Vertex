using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IPCSoftware.Shared.Models
{
    public class AppConfigSettings
    {
        public ConfigSettings Config { get; set; } = new();

        public CcdSettings CCD { get; set; } = new();
    }
    public class ConfigSettings
    {
        public string DataFolder { get; set; }
        public string DeviceInterfacesFileName { get; set; }
        public string CameraInterfacesFileName { get; set; }
        public string DeviceFileName { get; set; }
        public string PlcTagsFileName { get; set; }
        public string AlarmConfigFileName { get; set; }
        public string LogConfigFileName { get; set; }
        public string ServoCalibrationFileName { get; set; }
        public string UserFileName { get; set; }

        public bool SwapBytes { get; set; } = true;
        public bool SwapStringBytes { get; set; } = true;
        public int DefaultModBusAddress { get; set; }

        public TagMappingSettings TagMapping { get; set; } = new TagMappingSettings();

    }

    public class CcdSettings
    {
        public string QrCodeImagePath { get; set; }
        public string TempImgFolder { get; set; }
        public string BaseOutputDir { get; set; }
        public string BaseOutputDirBackup { get; set; }
        public string MetadataStyle { get; set; }
        public string CurrentCycleStateFileName { get; set; }
       // [JsonProperty("AppleMetaDataParams")]
        public ClientMetaData ClientMetaDataParams { get; set; }
        //[JsonProperty("VendorMetaDataParams")]
        public VendorMetaData VendorMetaDataParams { get; set; }
    }

}
