using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IPCSoftware.Shared.Models.ConfigModels
{
    public class CameraInterfaceModel
    {
        public int Id { get; set; }
        public int DeviceNo { get; set; }
        public string DeviceName { get; set; }
        public string Name { get; set; }
        public string Protocol { get; set; }  // FTP-Server, FTP-Client, EthernetIP, Ethercat, Custom
        public string IPAddress { get; set; }
        public int Port { get; set; }
        public string Gateway { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public bool AnonymousLogin { get; set; }
        public string RemotePath { get; set; }  // For FTP Server: Physical Path, For FTP Client: Remote Path
        public string LocalDirectory { get; set; }
        public bool Enabled { get; set; }
        public string Description { get; set; }
        public string Remark { get; set; }

        // ===== CCD SETTINGS FIELDS (NEW) =====
        // Primary fields

        //Added by Rishabh - date - 08/04/2026//
        public string QrCodeImagePath { get; set; }
        public string TempImgFolder { get; set; }
        public string ImageRootFolder { get; set; }
        public string MetadataStyle { get; set; }
        public string CurrentCycleStateFileName { get; set; }

        // Client metadata
        public string Client_Version { get; set; }
        public string Client_Date { get; set; }
        public string Client_Time { get; set; }
        public string Client_VisionVendor { get; set; }
        public string Client_StationID { get; set; }
        public string Client_StationNickname { get; set; }
        public string Client_DUTSerialNumber { get; set; }
        public string Client_ProcessCommand { get; set; }
        public string Client_CameraNumber { get; set; }
        public string Client_XPixelSizeMM { get; set; }
        public string Client_YPixelSizeMM { get; set; }
        public string Client_CameraGain { get; set; }
        public string Client_CameraExposure { get; set; }
        public string Client_NumberOfLightSettings { get; set; }
        public string Client_LightSetting1 { get; set; }
        public string Client_LightSettingN { get; set; }
        public string Client_DUTColor { get; set; }
        public string Client_ImageNickname { get; set; }

        // Vendor metadata
        public string Vendor_Version { get; set; }
        public string Vendor_Date { get; set; }
        public string Vendor_Time { get; set; }
        public string Vendor_VisionVendor { get; set; }
        public string Vendor_StationID { get; set; }
        public string Vendor_StationNickname { get; set; }
        public string Vendor_DUTSerialNumber { get; set; }
        public string Vendor_ProcessCommand { get; set; }
        public string Vendor_CameraNumber { get; set; }
        public string Vendor_XPixelSizeMM { get; set; }
        public string Vendor_YPixelSizeMM { get; set; }
        public string Vendor_CameraGain { get; set; }
        public string Vendor_CameraExposure { get; set; }
        public string Vendor_NumberOfLightSettings { get; set; }
        public string Vendor_LightSetting1 { get; set; }
        public string Vendor_LightSettingN { get; set; }
        public string Vendor_DUTColor { get; set; }
        public string Vendor_ImageNickname { get; set; }

        //Added by Rishabh - date - 08/04/2026//

        public CameraInterfaceModel()
        {
            Enabled = true;
            Protocol = "FTP-Server";
            AnonymousLogin = false;
            Port = 21;

            // Initialize CCD defaults
            QrCodeImagePath = @"D:\CCD\CAM\UI";
            TempImgFolder = @"D:\CCD\CAM";
            ImageRootFolder = "Production Images";
            MetadataStyle = "METADATASTYLE003";
            CurrentCycleStateFileName = "CurrentCycleState.json";
            Client_Version = "1.0";
            Vendor_Version = "1.0";
        }

        public CameraInterfaceModel Clone()
        {
            return new CameraInterfaceModel
            {
                Id = this.Id,
                DeviceNo = this.DeviceNo,
                DeviceName = this.DeviceName,
                Name = this.Name,
                Protocol = this.Protocol,
                IPAddress = this.IPAddress,
                Port = this.Port,
                Gateway = this.Gateway,
                Username = this.Username,
                Password = this.Password,
                AnonymousLogin = this.AnonymousLogin,
                RemotePath = this.RemotePath,
                LocalDirectory = this.LocalDirectory,
                Enabled = this.Enabled,
                Description = this.Description,
                Remark = this.Remark,
                // Clone CCD fields
                QrCodeImagePath = this.QrCodeImagePath,
                TempImgFolder = this.TempImgFolder,
                ImageRootFolder = this.ImageRootFolder,
                MetadataStyle = this.MetadataStyle,
                CurrentCycleStateFileName = this.CurrentCycleStateFileName,
                Client_Version = this.Client_Version,
                Client_Date = this.Client_Date,
                Client_Time = this.Client_Time,
                Client_VisionVendor = this.Client_VisionVendor,
                Client_StationID = this.Client_StationID,
                Client_StationNickname = this.Client_StationNickname,
                Client_DUTSerialNumber = this.Client_DUTSerialNumber,
                Client_ProcessCommand = this.Client_ProcessCommand,
                Client_CameraNumber = this.Client_CameraNumber,
                Client_XPixelSizeMM = this.Client_XPixelSizeMM,
                Client_YPixelSizeMM = this.Client_YPixelSizeMM,
                Client_CameraGain = this.Client_CameraGain,
                Client_CameraExposure = this.Client_CameraExposure,
                Client_NumberOfLightSettings = this.Client_NumberOfLightSettings,
                Client_LightSetting1 = this.Client_LightSetting1,
                Client_LightSettingN = this.Client_LightSettingN,
                Client_DUTColor = this.Client_DUTColor,
                Client_ImageNickname = this.Client_ImageNickname,
                Vendor_Version = this.Vendor_Version,
                Vendor_Date = this.Vendor_Date,
                Vendor_Time = this.Vendor_Time,
                Vendor_VisionVendor = this.Vendor_VisionVendor,
                Vendor_StationID = this.Vendor_StationID,
                Vendor_StationNickname = this.Vendor_StationNickname,
                Vendor_DUTSerialNumber = this.Vendor_DUTSerialNumber,
                Vendor_ProcessCommand = this.Vendor_ProcessCommand,
                Vendor_CameraNumber = this.Vendor_CameraNumber,
                Vendor_XPixelSizeMM = this.Vendor_XPixelSizeMM,
                Vendor_YPixelSizeMM = this.Vendor_YPixelSizeMM,
                Vendor_CameraGain = this.Vendor_CameraGain,
                Vendor_CameraExposure = this.Vendor_CameraExposure,
                Vendor_NumberOfLightSettings = this.Vendor_NumberOfLightSettings,
                Vendor_LightSetting1 = this.Vendor_LightSetting1,
                Vendor_LightSettingN = this.Vendor_LightSettingN,
                Vendor_DUTColor = this.Vendor_DUTColor,
                Vendor_ImageNickname = this.Vendor_ImageNickname
            };
        }
    }
}