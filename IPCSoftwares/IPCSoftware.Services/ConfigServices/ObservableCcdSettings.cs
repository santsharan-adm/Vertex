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

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using IPCSoftware.Core.Interfaces;
using IPCSoftware.Core.Interfaces.AppLoggerInterface;
using IPCSoftware.Shared.Models.ConfigModels;

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
    }
}
