using IPCSoftware.Shared.Models;
using IPCSoftware.Shared.Models.ConfigModels;
using System;
using System.Text;

namespace IPCSoftware.CoreService.Services.CCD
{
    public static class MetadataFormatter
    {
        public static string Format(MetaDataBase meta, bool isClient)
        {
            if (meta == null) return string.Empty;

            var sb = new StringBuilder();

            // 1. Version
            sb.Append(meta.Version ?? "").Append(",");

            // 2. Date (YYYY_MM_dd)
            sb.Append(meta.Date ?? "").Append(",");

            // 3. Time (HH:mm:ss.fff)
            sb.Append(meta.Time ?? "").Append(",");

            // 4. VisionVendor
            sb.Append(meta.VisionVendor ?? "").Append(",");

            // 5. StationID (If Applicable)
            sb.Append(meta.StationID ?? "").Append(",");

            // 6. StationNickname (If Applicable)
            sb.Append(meta.StationNickname ?? "").Append(",");

            // 7. DUTSerialNumber (If Applicable)
            sb.Append(meta.DUTSerialNumber ?? "").Append(",");

            // 8. ProcessCommand (If Applicable) - SPECIAL HANDLING
            // ** Rule: Replace commas (0x2C) with BEL (0x07)
            string procCmd = meta.ProcessCommand ?? "";
            if (procCmd.Contains(","))
            {
                procCmd = procCmd.Replace(',', '\a'); // \a is 0x07 (BEL)
            }
            sb.Append(procCmd).Append(",");

            // 9. CameraNumber
            sb.Append(meta.CameraNumber ?? "").Append(",");

            // 10. XPixelSizeMM
            sb.Append(meta.XPixelSizeMM ?? "").Append(",");

            // 11. YPixelSizeMM
            sb.Append(meta.YPixelSizeMM ?? "").Append(",");

            // 12. CameraGain
            sb.Append(meta.CameraGain ?? "").Append(",");

            // 13. CameraExposure
            sb.Append(meta.CameraExposure ?? "").Append(",");

            // 14. #ofLightSettings
            sb.Append(meta.NumberOfLightSettings ?? "").Append(",");

            // 15. LightSetting1 (Format: Lxx_Cxx_Ixxx)
            sb.Append(meta.LightSetting1 ?? "").Append(",");

            // 16. LightSettingN (If used)
            sb.Append(meta.LightSettingN ?? "").Append(",");

            // 17. DUTColor
            sb.Append(meta.DUTColor ?? "AllColors").Append(",");

            // 18. ImageNickname (Last item, no trailing comma usually, but check requirements. 
            // Standard CSV does not end with comma, but concatenation might require separation if streams merge? 
            // Based on table, it's just the value.)
            sb.Append(meta.ImageNickname ?? "");

            return sb.ToString();
        }
    }
}