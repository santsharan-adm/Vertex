using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using IPCSoftware.Services.ConfigServices;
using IPCSoftware.Shared.Models.ConfigModels;
using IPCSoftware.Core.Interfaces;

namespace IPCSoftware.UI.CommonViews.ViewModels
{
    public class CcdSettingsViewModel : INotifyPropertyChanged
    {
        private CameraInterfaceModel _currentInterface;
        private readonly IDeviceConfigurationService _deviceService;
        private readonly IObservableCcdSettingsService _observableCcdSettings; // 

        public CcdSettingsViewModel(
            IDeviceConfigurationService deviceService = null,
            IObservableCcdSettingsService observableCcdSettings = null) // 
        {
            _deviceService = deviceService;
            _observableCcdSettings = observableCcdSettings; // 

            Title = "CCD Settings";
            // default values from your appsettings example
            QrCodeImagePath = @"D:\CCD\CAM\UI";
            TempImgFolder = @"D:\CCD\CAM";
            ImageRootFolder = "Production Images";
            MetadataStyle = "METADATASTYLE003";
            CurrentCycleStateFileName = "CurrentCycleState.json";

            Client_Version = "1.0";
            Client_Date = "2025-01-01";
            Client_Time = "12:00:00";
            Client_VisionVendor = "Vertex";
            Client_StationID = "ST-001";
            Client_StationNickname = "MainStation";
            Client_DUTSerialNumber = "SN-00000";
            Client_ProcessCommand = "INSPECT";
            Client_CameraNumber = "1";
            Client_XPixelSizeMM = "0.0345";
            Client_YPixelSizeMM = "0.0345";
            Client_CameraGain = "15";
            Client_CameraExposure = "5000";
            Client_NumberOfLightSettings = "1";
            Client_LightSetting1 = "100";
            Client_LightSettingN = "0";
            Client_DUTColor = "Black";
            Client_ImageNickname = "NickName";

            Vendor_Version = "1.0";
            Vendor_VisionVendor = "Vertex";
            Vendor_StationID = "ST-001";
            Vendor_StationNickname = "MainStation";
            Vendor_DUTSerialNumber = "SN-00000";
            Vendor_ProcessCommand = "INSPECT";
            Vendor_CameraNumber = "1";
            Vendor_XPixelSizeMM = "0.0345";
            Vendor_YPixelSizeMM = "0.0345";
            Vendor_CameraGain = "15";
            Vendor_CameraExposure = "5000";
            Vendor_NumberOfLightSettings = "1";
            Vendor_LightSetting1 = "100";
            Vendor_LightSettingN = "0";
            Vendor_DUTColor = "Black";
            Vendor_ImageNickname = "NickName";

            SaveCommand = new RelayCommand(_ => SaveAsync(), _ => true);
            CancelCommand = new RelayCommand(_ => Cancel(), _ => true);
        }

        public event EventHandler SaveCompleted;
        public event EventHandler CancelRequested;

        public string Title { get => _title; set => Set(ref _title, value); }
        private string _title;

        // Primary fields
        public string QrCodeImagePath { get => _qrCodeImagePath; set => Set(ref _qrCodeImagePath, value); }
        private string _qrCodeImagePath;

        public string TempImgFolder { get => _tempImgFolder; set => Set(ref _tempImgFolder, value); }
        private string _tempImgFolder;

        public string ImageRootFolder { get => _imageRootFolder; set => Set(ref _imageRootFolder, value); }
        private string _imageRootFolder;

        public string MetadataStyle { get => _metadataStyle; set => Set(ref _metadataStyle, value); }
        private string _metadataStyle;

        public string CurrentCycleStateFileName { get => _currentCycleStateFileName; set => Set(ref _currentCycleStateFileName, value); }
        private string _currentCycleStateFileName;

        // Client metadata (prefixed to avoid name collisions)
        public string Client_Version { get => _client_Version; set => Set(ref _client_Version, value); }
        private string _client_Version;
        public string Client_Date { get => _client_Date; set => Set(ref _client_Date, value); }
        private string _client_Date;
        public string Client_Time { get => _client_Time; set => Set(ref _client_Time, value); }
        private string _client_Time;
        public string Client_VisionVendor { get => _client_VisionVendor; set => Set(ref _client_VisionVendor, value); }
        private string _client_VisionVendor;
        public string Client_StationID { get => _client_StationID; set => Set(ref _client_StationID, value); }
        private string _client_StationID;
        public string Client_StationNickname { get => _client_StationNickname; set => Set(ref _client_StationNickname, value); }
        private string _client_StationNickname;
        public string Client_DUTSerialNumber { get => _client_DUTSerialNumber; set => Set(ref _client_DUTSerialNumber, value); }
        private string _client_DUTSerialNumber;
        public string Client_ProcessCommand { get => _client_ProcessCommand; set => Set(ref _client_ProcessCommand, value); }
        private string _client_ProcessCommand;
        public string Client_CameraNumber { get => _client_CameraNumber; set => Set(ref _client_CameraNumber, value); }
        private string _client_CameraNumber;
        public string Client_XPixelSizeMM { get => _client_XPixelSizeMM; set => Set(ref _client_XPixelSizeMM, value); }
        private string _client_XPixelSizeMM;
        public string Client_YPixelSizeMM { get => _client_YPixelSizeMM; set => Set(ref _client_YPixelSizeMM, value); }
        private string _client_YPixelSizeMM;
        public string Client_CameraGain { get => _client_CameraGain; set => Set(ref _client_CameraGain, value); }
        private string _client_CameraGain;
        public string Client_CameraExposure { get => _client_CameraExposure; set => Set(ref _client_CameraExposure, value); }
        private string _client_CameraExposure;
        public string Client_NumberOfLightSettings { get => _client_NumberOfLightSettings; set => Set(ref _client_NumberOfLightSettings, value); }
        private string _client_NumberOfLightSettings;
        public string Client_LightSetting1 { get => _client_LightSetting1; set => Set(ref _client_LightSetting1, value); }
        private string _client_LightSetting1;
        public string Client_LightSettingN { get => _client_LightSettingN; set => Set(ref _client_LightSettingN, value); }
        private string _client_LightSettingN;
        public string Client_DUTColor { get => _client_DUTColor; set => Set(ref _client_DUTColor, value); }
        private string _client_DUTColor;
        public string Client_ImageNickname { get => _client_ImageNickname; set => Set(ref _client_ImageNickname, value); }
        private string _client_ImageNickname;

        // Vendor metadata (prefixed)
        public string Vendor_Version { get => _vendor_Version; set => Set(ref _vendor_Version, value); }
        private string _vendor_Version;
        public string Vendor_Date { get => _vendor_Date; set => Set(ref _vendor_Date, value); }
        private string _vendor_Date;
        public string Vendor_Time { get => _vendor_Time; set => Set(ref _vendor_Time, value); }
        private string _vendor_Time;
        public string Vendor_VisionVendor { get => _vendor_VisionVendor; set => Set(ref _vendor_VisionVendor, value); }
        private string _vendor_VisionVendor;
        public string Vendor_StationID { get => _vendor_StationID; set => Set(ref _vendor_StationID, value); }
        private string _vendor_StationID;
        public string Vendor_StationNickname { get => _vendor_StationNickname; set => Set(ref _vendor_StationNickname, value); }
        private string _vendor_StationNickname;
        public string Vendor_DUTSerialNumber { get => _vendor_DUTSerialNumber; set => Set(ref _vendor_DUTSerialNumber, value); }
        private string _vendor_DUTSerialNumber;
        public string Vendor_ProcessCommand { get => _vendor_ProcessCommand; set => Set(ref _vendor_ProcessCommand, value); }
        private string _vendor_ProcessCommand;
        public string Vendor_CameraNumber { get => _vendor_CameraNumber; set => Set(ref _vendor_CameraNumber, value); }
        private string _vendor_CameraNumber;
        public string Vendor_XPixelSizeMM { get => _vendor_XPixelSizeMM; set => Set(ref _vendor_XPixelSizeMM, value); }
        private string _vendor_XPixelSizeMM;
        public string Vendor_YPixelSizeMM { get => _vendor_YPixelSizeMM; set => Set(ref _vendor_YPixelSizeMM, value); }
        private string _vendor_YPixelSizeMM;
        public string Vendor_CameraGain { get => _vendor_CameraGain; set => Set(ref _vendor_CameraGain, value); }
        private string _vendor_CameraGain;
        public string Vendor_CameraExposure { get => _vendor_CameraExposure; set => Set(ref _vendor_CameraExposure, value); }
        private string _vendor_CameraExposure;
        public string Vendor_NumberOfLightSettings { get => _vendor_NumberOfLightSettings; set => Set(ref _vendor_NumberOfLightSettings, value); }
        private string _vendor_NumberOfLightSettings;
        public string Vendor_LightSetting1 { get => _vendor_LightSetting1; set => Set(ref _vendor_LightSetting1, value); }
        private string _vendor_LightSetting1;
        public string Vendor_LightSettingN { get => _vendor_LightSettingN; set => Set(ref _vendor_LightSettingN, value); }
        private string _vendor_LightSettingN;
        public string Vendor_DUTColor { get => _vendor_DUTColor; set => Set(ref _vendor_DUTColor, value); }
        private string _vendor_DUTColor;
        public string Vendor_ImageNickname { get => _vendor_ImageNickname; set => Set(ref _vendor_ImageNickname, value); }
        private string _vendor_ImageNickname;

        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }


        public void LoadForEdit(CameraInterfaceModel cameraInterface)
        {
            if (cameraInterface == null) return;
            _currentInterface = cameraInterface;

            // Load all CCD fields from the model
            QrCodeImagePath = cameraInterface.QrCodeImagePath ?? "";
            TempImgFolder = cameraInterface.TempImgFolder ?? "";
            ImageRootFolder = cameraInterface.ImageRootFolder ?? "";
            MetadataStyle = cameraInterface.MetadataStyle ?? "";
            CurrentCycleStateFileName = cameraInterface.CurrentCycleStateFileName ?? "";

            // Client metadata
            Client_Version = cameraInterface.Client_Version ?? "1.0";
            Client_Date = cameraInterface.Client_Date ?? "";
            Client_Time = cameraInterface.Client_Time ?? "";
            Client_VisionVendor = cameraInterface.Client_VisionVendor ?? "";
            Client_StationID = cameraInterface.Client_StationID ?? "";
            Client_StationNickname = cameraInterface.Client_StationNickname ?? "";
            Client_DUTSerialNumber = cameraInterface.Client_DUTSerialNumber ?? "";
            Client_ProcessCommand = cameraInterface.Client_ProcessCommand ?? "";
            Client_CameraNumber = cameraInterface.Client_CameraNumber ?? "";
            Client_XPixelSizeMM = cameraInterface.Client_XPixelSizeMM ?? "";
            Client_YPixelSizeMM = cameraInterface.Client_YPixelSizeMM ?? "";
            Client_CameraGain = cameraInterface.Client_CameraGain ?? "";
            Client_CameraExposure = cameraInterface.Client_CameraExposure ?? "";
            Client_NumberOfLightSettings = cameraInterface.Client_NumberOfLightSettings ?? "";
            Client_LightSetting1 = cameraInterface.Client_LightSetting1 ?? "";
            Client_LightSettingN = cameraInterface.Client_LightSettingN ?? "";
            Client_DUTColor = cameraInterface.Client_DUTColor ?? "";
            Client_ImageNickname = cameraInterface.Client_ImageNickname ?? "";

            // Vendor metadata
            Vendor_Version = cameraInterface.Vendor_Version ?? "1.0";
            Vendor_Date = cameraInterface.Vendor_Date ?? "";
            Vendor_Time = cameraInterface.Vendor_Time ?? "";
            Vendor_VisionVendor = cameraInterface.Vendor_VisionVendor ?? "";
            Vendor_StationID = cameraInterface.Vendor_StationID ?? "";
            Vendor_StationNickname = cameraInterface.Vendor_StationNickname ?? "";
            Vendor_DUTSerialNumber = cameraInterface.Vendor_DUTSerialNumber ?? "";
            Vendor_ProcessCommand = cameraInterface.Vendor_ProcessCommand ?? "";
            Vendor_CameraNumber = cameraInterface.Vendor_CameraNumber ?? "";
            Vendor_XPixelSizeMM = cameraInterface.Vendor_XPixelSizeMM ?? "";
            Vendor_YPixelSizeMM = cameraInterface.Vendor_YPixelSizeMM ?? "";
            Vendor_CameraGain = cameraInterface.Vendor_CameraGain ?? "";
            Vendor_CameraExposure = cameraInterface.Vendor_CameraExposure ?? "";
            Vendor_NumberOfLightSettings = cameraInterface.Vendor_NumberOfLightSettings ?? "";
            Vendor_LightSetting1 = cameraInterface.Vendor_LightSetting1 ?? "";
            Vendor_LightSettingN = cameraInterface.Vendor_LightSettingN ?? "";
            Vendor_DUTColor = cameraInterface.Vendor_DUTColor ?? "";
            Vendor_ImageNickname = cameraInterface.Vendor_ImageNickname ?? "";
        }


        private async void SaveAsync()
        {
            try
            {
                if (_currentInterface == null)
                {
                    Cancel();
                    return;
                }

                // Copy all UI values back to the model
                _currentInterface.QrCodeImagePath = QrCodeImagePath;
                _currentInterface.TempImgFolder = TempImgFolder;
                _currentInterface.ImageRootFolder = ImageRootFolder;
                _currentInterface.MetadataStyle = MetadataStyle;
                _currentInterface.CurrentCycleStateFileName = CurrentCycleStateFileName;

                // Client metadata
                _currentInterface.Client_Version = Client_Version;
                _currentInterface.Client_Date = Client_Date;
                _currentInterface.Client_Time = Client_Time;
                _currentInterface.Client_VisionVendor = Client_VisionVendor;
                _currentInterface.Client_StationID = Client_StationID;
                _currentInterface.Client_StationNickname = Client_StationNickname;
                _currentInterface.Client_DUTSerialNumber = Client_DUTSerialNumber;
                _currentInterface.Client_ProcessCommand = Client_ProcessCommand;
                _currentInterface.Client_CameraNumber = Client_CameraNumber;
                _currentInterface.Client_XPixelSizeMM = Client_XPixelSizeMM;
                _currentInterface.Client_YPixelSizeMM = Client_YPixelSizeMM;
                _currentInterface.Client_CameraGain = Client_CameraGain;
                _currentInterface.Client_CameraExposure = Client_CameraExposure;
                _currentInterface.Client_NumberOfLightSettings = Client_NumberOfLightSettings;
                _currentInterface.Client_LightSetting1 = Client_LightSetting1;
                _currentInterface.Client_LightSettingN = Client_LightSettingN;
                _currentInterface.Client_DUTColor = Client_DUTColor;
                _currentInterface.Client_ImageNickname = Client_ImageNickname;

                // Vendor metadata
                _currentInterface.Vendor_Version = Vendor_Version;
                _currentInterface.Vendor_Date = Vendor_Date;
                _currentInterface.Vendor_Time = Vendor_Time;
                _currentInterface.Vendor_VisionVendor = Vendor_VisionVendor;
                _currentInterface.Vendor_StationID = Vendor_StationID;
                _currentInterface.Vendor_StationNickname = Vendor_StationNickname;
                _currentInterface.Vendor_DUTSerialNumber = Vendor_DUTSerialNumber;
                _currentInterface.Vendor_ProcessCommand = Vendor_ProcessCommand;
                _currentInterface.Vendor_CameraNumber = Vendor_CameraNumber;
                _currentInterface.Vendor_XPixelSizeMM = Vendor_XPixelSizeMM;
                _currentInterface.Vendor_YPixelSizeMM = Vendor_YPixelSizeMM;
                _currentInterface.Vendor_CameraGain = Vendor_CameraGain;
                _currentInterface.Vendor_CameraExposure = Vendor_CameraExposure;
                _currentInterface.Vendor_NumberOfLightSettings = Vendor_NumberOfLightSettings;
                _currentInterface.Vendor_LightSetting1 = Vendor_LightSetting1;
                _currentInterface.Vendor_LightSettingN = Vendor_LightSettingN;
                _currentInterface.Vendor_DUTColor = Vendor_DUTColor;
                _currentInterface.Vendor_ImageNickname = Vendor_ImageNickname;

                // Update observable settings service
                if (_observableCcdSettings != null)
                {
                    await _observableCcdSettings.UpdateFromCameraInterfaceAsync(_currentInterface);
                }

                // Persist to CSV via DeviceConfigurationService
                if (_deviceService != null)
                {
                    await _deviceService.UpdateCameraInterfaceAsync(_currentInterface);
                }

                SaveCompleted?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving CCD Settings: {ex.Message}");
                Cancel();
            }
        }

        private void Cancel()
        {
            CancelRequested?.Invoke(this, EventArgs.Empty);
        }

        #region INotify & helpers
        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        private void Set<T>(ref T field, T value, [CallerMemberName] string name = null)
        {
            if (Equals(field, value)) return;
            field = value;
            OnPropertyChanged(name);
        }

        private class RelayCommand : ICommand
        {
            private readonly System.Action<object?> _execute;
            private readonly System.Predicate<object?> _canExecute;
            public RelayCommand(System.Action<object?> execute, System.Predicate<object?> canExecute)
            {
                _execute = execute;
                _canExecute = canExecute;
            }
            public bool CanExecute(object? parameter) => _canExecute?.Invoke(parameter) ?? true;
            public void Execute(object? parameter) => _execute?.Invoke(parameter);
            public event System.EventHandler? CanExecuteChanged { add { } remove { } }
        }
        #endregion
    }
}
