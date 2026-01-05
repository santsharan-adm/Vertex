using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IPCSoftware.Shared.Models
{
    public class MetaDataBase
    {
        //[JsonProperty("Version")]
        public string Version { get; set; }

       // [JsonProperty("Date")]
        public string Date { get; set; }

       // [JsonProperty("Time")]
        public string Time { get; set; }

        //[JsonProperty("VisionVendor")]
        public string VisionVendor { get; set; }

        //[JsonProperty("StationID")]
        public string StationID { get; set; }

       // [JsonProperty("StationNickname")]
        public string StationNickname { get; set; }

       // [JsonProperty("DUTSerialNumber")]
        public string DUTSerialNumber { get; set; }

       // [JsonProperty("ProcessCommand")]
        public string ProcessCommand { get; set; }

       // [JsonProperty("CameraNumber")]
        public string CameraNumber { get; set; }

       // [JsonProperty("XPixelSizeMM")]
        public string XPixelSizeMM { get; set; }

       // [JsonProperty("YPixelSizeMM")]
        public string YPixelSizeMM { get; set; }

       // [JsonProperty("CameraGain")]
        public string CameraGain { get; set; }

       // [JsonProperty("CameraExposure")]
        public string CameraExposure { get; set; }

        // Special handling for the '#' character in the JSON key
       // [JsonProperty("#ofLightSettings")]
        public string NumberOfLightSettings { get; set; }

       // [JsonProperty("LightSetting1")]
        public string LightSetting1 { get; set; }

       // [JsonProperty("LightSettingN")]
        public string LightSettingN { get; set; }

       // [JsonProperty("DUTColor")]
        public string DUTColor { get; set; }

       // [JsonProperty("ImageNickname")]
        public string ImageNickname { get; set; }
    }

    public class ClientMetaData : MetaDataBase
    {
       
    }

    // 3. VendorMetaData inheriting from Base (Maps to "VendorMetaDataParams")
    public class VendorMetaData : MetaDataBase
    {
       
    }
}
