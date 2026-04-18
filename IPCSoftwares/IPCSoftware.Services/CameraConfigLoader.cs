/******************************************************************************
 * Project      : IPCSoftware-AOI /Bending
 * Module       : CameraConfigLoader
 * File Name    : CameraConfigLoader.cs
 * Author       : Rishabh
 * Organization : Motherson Technology Service Limited
 * Created Date : 2026-04-15
 *
 * Description  :
 * Loads and parses camera interface configuration from CSV files, supporting multiple
 * file format versions. Handles CCD settings and metadata fields with backward compatibility.
 *
 * Change History:
 * ---------------------------------------------------------------------------
 * Date        Author        Version     Description
 * ---------------------------------------------------------------------------
 * 2026-04-15  Rishabh       1.0         Initial creation
 * 
 *
 ******************************************************************************/



using IPCSoftware.Core.Interfaces.AppLoggerInterface;
using IPCSoftware.Shared.Models.ConfigModels;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IPCSoftware.Services
{
    public class CameraConfigLoader :BaseService
    {   
        private List<CameraInterfaceModel> _cameraInterfaces = new List<CameraInterfaceModel>();
        public CameraConfigLoader(IAppLogger logger) : base(logger)
        { }

        public List<CameraInterfaceModel> Load(string filePath) 
        {
            try
            {
                var version = CsvReader.Getversion(filePath);
                var rows = CsvReader.Read(filePath);
                var cameraInterface = new List<CameraInterfaceModel>();
                if (rows.Count == 0) { _logger.LogError("Camera Configuration Settings Not found", LogType.Error); return cameraInterface; }
                _cameraInterfaces.Clear();
                if (version == "1.0")
                {
                   foreach (var row in rows)
                    {
                        var cameraIntr = ParseCameraInterfaceCsvLine(row);
                        if (cameraIntr != null)
                        {
                            _cameraInterfaces.Add(cameraIntr);
                        }
                    } 

                }
                else if (version =="2.0")
                {
                    foreach (var row in rows)
                    {
                        var cameraIntr = ParseCameraInterfaceCsvLine(row);
                        if (cameraIntr != null)
                        {
                            _cameraInterfaces.Add(cameraIntr);
                        }
                    }
                }
                return _cameraInterfaces;
            }

            catch (Exception ex)
            {
                _logger.LogError(ex.Message, LogType.Diagnostics);
                return null;
            }


        }

        private CameraInterfaceModel ParseCameraInterfaceCsvLine(string[] values)
        {
            try
            {
                //var values = SplitCsvLine(line);
                // Header now has 54 columns (16 original + 5 primary + 19 client + 19 vendor - 5 overlap = 54)
                if (values.Length < 16) return null;
                //if (CsvReader.Getversion == "2.0") return null;

                var cam = new CameraInterfaceModel
                {
                    Id = int.Parse(values[0]),
                    DeviceNo = int.Parse(values[1]),
                    DeviceName = values[2],
                    Name = values[3],
                    Protocol = values[4],
                    IPAddress = values[5],
                    Port = int.Parse(values[6]),
                    Gateway = values[7],
                    Username = values[8],
                    Password = values[9],
                    AnonymousLogin = bool.Parse(values[10]),
                    RemotePath = values[11],
                    LocalDirectory = values[12],
                    Enabled = bool.Parse(values[13]),
                    Description = values[14],
                    Remark = values[15]
                };

                // Load CCD fields if they exist (backward compatible)
                //Added by Rishabh - date - 08/04/2026//
                if (values.Length > 16)
                {
                    cam.QrCodeImagePath = values.Length > 16 ? values[16] : "";
                    cam.TempImgFolder = values.Length > 17 ? values[17] : "";
                    cam.ImageRootFolder = values.Length > 18 ? values[18] : "";
                    cam.MetadataStyle = values.Length > 19 ? values[19] : "";
                    cam.CurrentCycleStateFileName = values.Length > 20 ? values[20] : "";

                    // Client metadata (indices 21-39)
                    cam.Client_Version = values.Length > 21 ? values[21] : "";
                    cam.Client_Date = values.Length > 22 ? values[22] : "";
                    cam.Client_Time = values.Length > 23 ? values[23] : "";
                    cam.Client_VisionVendor = values.Length > 24 ? values[24] : "";
                    cam.Client_StationID = values.Length > 25 ? values[25] : "";
                    cam.Client_StationNickname = values.Length > 26 ? values[26] : "";
                    cam.Client_DUTSerialNumber = values.Length > 27 ? values[27] : "";
                    cam.Client_ProcessCommand = values.Length > 28 ? values[28] : "";
                    cam.Client_CameraNumber = values.Length > 29 ? values[29] : "";
                    cam.Client_XPixelSizeMM = values.Length > 30 ? values[30] : "";
                    cam.Client_YPixelSizeMM = values.Length > 31 ? values[31] : "";
                    cam.Client_CameraGain = values.Length > 32 ? values[32] : "";
                    cam.Client_CameraExposure = values.Length > 33 ? values[33] : "";
                    cam.Client_NumberOfLightSettings = values.Length > 34 ? values[34] : "";
                    cam.Client_LightSetting1 = values.Length > 35 ? values[35] : "";
                    cam.Client_LightSettingN = values.Length > 36 ? values[36] : "";
                    cam.Client_DUTColor = values.Length > 37 ? values[37] : "";
                    cam.Client_ImageNickname = values.Length > 38 ? values[38] : "";

                    // Vendor metadata (indices 39-57)
                    cam.Vendor_Version = values.Length > 39 ? values[39] : "";
                    cam.Vendor_Date = values.Length > 40 ? values[40] : "";
                    cam.Vendor_Time = values.Length > 41 ? values[41] : "";
                    cam.Vendor_VisionVendor = values.Length > 42 ? values[42] : "";
                    cam.Vendor_StationID = values.Length > 43 ? values[43] : "";
                    cam.Vendor_StationNickname = values.Length > 44 ? values[44] : "";
                    cam.Vendor_DUTSerialNumber = values.Length > 45 ? values[45] : "";
                    cam.Vendor_ProcessCommand = values.Length > 46 ? values[46] : "";
                    cam.Vendor_CameraNumber = values.Length > 47 ? values[47] : "";
                    cam.Vendor_XPixelSizeMM = values.Length > 48 ? values[48] : "";
                    cam.Vendor_YPixelSizeMM = values.Length > 49 ? values[49] : "";
                    cam.Vendor_CameraGain = values.Length > 50 ? values[50] : "";
                    cam.Vendor_CameraExposure = values.Length > 51 ? values[51] : "";
                    cam.Vendor_NumberOfLightSettings = values.Length > 52 ? values[52] : "";
                    cam.Vendor_LightSetting1 = values.Length > 53 ? values[53] : "";
                    cam.Vendor_LightSettingN = values.Length > 54 ? values[54] : "";
                    cam.Vendor_DUTColor = values.Length > 55 ? values[55] : "";
                    cam.Vendor_ImageNickname = values.Length > 56 ? values[56] : "";
                }

                return cam;
            }
            catch
            {
                return null;
            }
        }


    }
}
