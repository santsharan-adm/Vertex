/******************************************************************************
 * Project      : IPCSoftware-AOI /Bending
 * Module       : ObservableCcdSettings
 * File Name    : ObservableCcdSettings.cs
 * Author       : Rishabh
 * Organization : Vertex Automtion System Pvt Ltd
 * Created Date : 2026-04-08
 *
 * Description  :
 * Provides an observable CCD settings service that manages and broadcasts camera
 * configuration changes (image paths, metadata styles, etc.) to all subscribers in real-time.
 *
 * Change History:
 * ---------------------------------------------------------------------------
 * Date        Author        Version     Description
 * ---------------------------------------------------------------------------
 * 2026-04-08  Rishabh       1.0         Initial creation
 * 
 * 
 *
 ******************************************************************************/

using IPCSoftware.Core.Interfaces;
using IPCSoftware.Core.Interfaces.AppLoggerInterface;
using IPCSoftware.Shared.Models;
using IPCSoftware.Shared.Models.ConfigModels;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace IPCSoftware.Services.ConfigServices
{
    /// <summary>
    /// Provides observable CCD settings that can be updated at runtime.
    /// When settings change, subscribers are notified immediately.
    /// </summary>
    public interface IObservableCcdSettingsService : INotifyPropertyChanged
    {
        string TempImgFolder { get; set; }
        string QrCodeImagePath { get; set; }
        string ImageRootFolder { get; set; }
        string MetadataStyle { get; set; }
        string CurrentCycleStateFileName { get; set; }

        ClientMetaData ClientMetaDataParams { get; set; }
        VendorMetaData VendorMetaDataParams { get; set; }

        event EventHandler<CcdSettingsChangedEventArgs> SettingsChanged;
        Task UpdateFromCameraInterfaceAsync(CameraInterfaceModel cameraInterface);
    }

    public class CcdSettingsChangedEventArgs : EventArgs
    {
        public string PropertyName { get; set; }
        public object OldValue { get; set; }
        public object NewValue { get; set; }
    }

    public class ObservableCcdSettingsService : IObservableCcdSettingsService
    {
        private string _tempImgFolder;
        private string _qrCodeImagePath;
        private string _imageRootFolder;
        private string _metadataStyle;
        private string _currentCycleStateFileName;
        private ClientMetaData _clientMetaDataParams;
        private VendorMetaData _vendorMetaDataParams;

        public event PropertyChangedEventHandler PropertyChanged;
        public event EventHandler<CcdSettingsChangedEventArgs> SettingsChanged;

        private readonly IAppLogger _logger;

        public ObservableCcdSettingsService(IAppLogger logger)
        {
            _logger = logger;
        }

        public string TempImgFolder
        {
            get => _tempImgFolder;
            set => SetProperty(ref _tempImgFolder, value, nameof(TempImgFolder));
        }

        public string QrCodeImagePath
        {
            get => _qrCodeImagePath;
            set => SetProperty(ref _qrCodeImagePath, value, nameof(QrCodeImagePath));
        }

        public string ImageRootFolder
        {
            get => _imageRootFolder;
            set => SetProperty(ref _imageRootFolder, value, nameof(ImageRootFolder));
        }

        public string MetadataStyle
        {
            get => _metadataStyle;
            set => SetProperty(ref _metadataStyle, value, nameof(MetadataStyle));
        }

        public string CurrentCycleStateFileName
        {
            get => _currentCycleStateFileName;
            set => SetProperty(ref _currentCycleStateFileName, value, nameof(CurrentCycleStateFileName));
        }


        public ClientMetaData ClientMetaDataParams
        {
            get => _clientMetaDataParams;
            set => SetProperty(ref _clientMetaDataParams, value, nameof(ClientMetaDataParams));
        }

        public VendorMetaData VendorMetaDataParams
        {
            get => _vendorMetaDataParams;
            set => SetProperty(ref _vendorMetaDataParams, value, nameof(VendorMetaDataParams));
        }

        /// <summary>
        /// Load settings from a CameraInterfaceModel and update observable properties
        /// </summary>
        public async Task UpdateFromCameraInterfaceAsync(CameraInterfaceModel cameraInterface)
        {
            try
            {
                if (cameraInterface == null)
                {
                    _logger?.LogWarning("CameraInterfaceModel is null", LogType.Diagnostics);
                    return;
                }

                TempImgFolder = cameraInterface.TempImgFolder ?? "";
                QrCodeImagePath = cameraInterface.QrCodeImagePath ?? "";
                ImageRootFolder = cameraInterface.ImageRootFolder ?? "";
                MetadataStyle = cameraInterface.MetadataStyle ?? "";
                CurrentCycleStateFileName = cameraInterface.CurrentCycleStateFileName ?? "";
                ClientMetaDataParams = BuildClientMetaDataFromInterface(cameraInterface);
                VendorMetaDataParams = BuildVendorMetaDataFromInterface(cameraInterface);

                _logger?.LogInfo($"[CcdSettings] Updated from CameraInterface. TempImgFolder: {TempImgFolder}", LogType.Diagnostics);
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger?.LogError($"Error updating CCD settings: {ex.Message}", LogType.Diagnostics);
            }
        }

        private void SetProperty<T>(ref T field, T value, string propertyName)
        {
            if (Equals(field, value)) return;

            T oldValue = field;
            field = value;

            OnPropertyChanged(propertyName);
            SettingsChanged?.Invoke(this, new CcdSettingsChangedEventArgs
            {
                PropertyName = propertyName,
                OldValue = oldValue,
                NewValue = value
            });

            _logger?.LogInfo($"[CcdSettings] {propertyName} changed from '{oldValue}' to '{value}'", LogType.Diagnostics);
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }


        private ClientMetaData BuildClientMetaDataFromInterface(CameraInterfaceModel camera)
        {
            if (camera == null) return null;
            return new ClientMetaData
            {
                Version = camera.Client_Version ?? "1.0",
                Date = camera.Client_Date ?? "",
                Time = camera.Client_Time ?? "",
                VisionVendor = camera.Client_VisionVendor ?? "Client",
                StationID = camera.Client_StationID ?? "",
                StationNickname = camera.Client_StationNickname ?? "",
                DUTSerialNumber = camera.Client_DUTSerialNumber ?? "",
                ProcessCommand = camera.Client_ProcessCommand ?? "",
                CameraNumber = camera.Client_CameraNumber ?? "",
                XPixelSizeMM = camera.Client_XPixelSizeMM ?? "",
                YPixelSizeMM = camera.Client_YPixelSizeMM ?? "",
                CameraGain = camera.Client_CameraGain ?? "",
                CameraExposure = camera.Client_CameraExposure ?? "",
                NumberOfLightSettings = camera.Client_NumberOfLightSettings ?? "",
                LightSetting1 = camera.Client_LightSetting1 ?? "",
                LightSettingN = camera.Client_LightSettingN ?? "",
                DUTColor = camera.Client_DUTColor ?? "",
                ImageNickname = camera.Client_ImageNickname ?? ""

            };
        }

        private VendorMetaData BuildVendorMetaDataFromInterface(CameraInterfaceModel camera)
        {
            if (camera == null) return null;
            return new VendorMetaData
            {
                Version = camera.Vendor_Version ?? "1.0",
                Date = camera.Vendor_Date ?? "",
                Time = camera.Vendor_Time ?? "",
                VisionVendor = camera.Vendor_VisionVendor ?? "Vendor",
                StationID = camera.Vendor_StationID ?? "",
                StationNickname = camera.Vendor_StationNickname ?? "",
                DUTSerialNumber = camera.Vendor_DUTSerialNumber ?? "",
                ProcessCommand = camera.Vendor_ProcessCommand ?? "",
                CameraNumber = camera.Vendor_CameraNumber ?? "",
                XPixelSizeMM = camera.Vendor_XPixelSizeMM ?? "",
                YPixelSizeMM = camera.Vendor_YPixelSizeMM ?? "",
                CameraGain = camera.Vendor_CameraGain ?? "",
                CameraExposure = camera.Vendor_CameraExposure ?? "",
                NumberOfLightSettings = camera.Vendor_NumberOfLightSettings ?? "",
                LightSetting1 = camera.Vendor_LightSetting1 ?? "",
                LightSettingN = camera.Vendor_LightSettingN ?? "",
                DUTColor = camera.Vendor_DUTColor ?? "",
                ImageNickname = camera.Vendor_ImageNickname ?? ""


            };
        }
    }
}
