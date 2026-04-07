using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using IPCSoftware.Shared.Models.ConfigModels;
namespace IPCSoftware.UI.CommonViews.ViewModels
{
    public class CcdSettingsViewModel : INotifyPropertyChanged
    {
        private CameraInterfaceModel _currentInterface;
        public CcdSettingsViewModel()
        {
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

            SaveCommand = new RelayCommand(_ => Save(), _ => true);
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


        public void LoadForEdit( CameraInterfaceModel cameraInterface)
        {
            if (cameraInterface == null) return;
            _currentInterface = cameraInterface;
            // TODO: Load CCD settings from appsettings.json or configuration tied to this camera interface.
            // For now, use default values already set in constructor.
            // In a real scenario, you might deserialize from:
            // - appsettings.Development.json [CCD] section
            // - A database
            // - A configuration service injected into this VM
            // Example: If you had a service to load from config:
            // var ccdSettings = _ccdConfigService.LoadCcdSettings(cameraInterface.Id);
            // QrCodeImagePath = ccdSettings.QrCodeImagePath;
            // etc.
        }


        private void Save()
        {
            // TODO: Persist changes to appsettings.json
            SaveCompleted?.Invoke(this, EventArgs.Empty);
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