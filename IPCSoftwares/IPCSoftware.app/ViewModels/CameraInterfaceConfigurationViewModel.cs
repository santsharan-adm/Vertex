using IPCSoftware.Core.Interfaces;
using IPCSoftware.Shared;
using IPCSoftware.Shared.Models.ConfigModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace IPCSoftware.App.ViewModels
{
    public class CameraInterfaceConfigurationViewModel : BaseViewModel
    {
        private readonly IDeviceConfigurationService _deviceService;
        private CameraInterfaceModel _currentInterface;
        private DeviceModel _parentDevice;
        private bool _isEditMode;
        private string _title;

        public string Title
        {
            get => _title;
            set => SetProperty(ref _title, value);
        }

        public bool IsEditMode
        {
            get => _isEditMode;
            set => SetProperty(ref _isEditMode, value);
        }

        // Device Info (Read-only)
        private int _deviceNo;
        public int DeviceNo
        {
            get => _deviceNo;
            set => SetProperty(ref _deviceNo, value);
        }

        private string _cameraName;
        public string CameraName
        {
            get => _cameraName;
            set => SetProperty(ref _cameraName, value);
        }

        // Interface Name
        private string _name;
        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        // Com Protocol Dropdown
        private string _selectedProtocol;
        public string SelectedProtocol
        {
            get => _selectedProtocol;
            set
            {
                if (SetProperty(ref _selectedProtocol, value))
                {
                    OnPropertyChanged(nameof(IsFTPServer));
                    OnPropertyChanged(nameof(IsFTPClient));
                }
            }
        }

        public ObservableCollection<string> Protocols { get; }

        // Visibility Helpers
        public bool IsFTPServer => SelectedProtocol == "FTP-Server";
        public bool IsFTPClient => SelectedProtocol == "FTP-Client";

        // Common Fields (Both protocols)
        private string _ipAddress;
        public string IPAddress
        {
            get => _ipAddress;
            set => SetProperty(ref _ipAddress, value);
        }

        private int _port;
        public int Port
        {
            get => _port;
            set => SetProperty(ref _port, value);
        }

        private string _gateway;
        public string Gateway
        {
            get => _gateway;
            set => SetProperty(ref _gateway, value);
        }

        private bool _allowAnonymous;
        public bool AllowAnonymous
        {
            get => _allowAnonymous;
            set
            {
                if (SetProperty(ref _allowAnonymous, value))
                {
                    // If Anonymous is checked (true), clear credentials
                    if (_allowAnonymous)
                    {
                        Username = string.Empty;
                        Password = string.Empty;
                    }
                }
            }
        }

        // Authentication (Common)
        private string _username;
        public string Username
        {
            get => _username;
            set => SetProperty(ref _username, value);
        }

        private string _password;
        public string Password
        {
            get => _password;
            set => SetProperty(ref _password, value);
        }

        // FTP Server Specific
        private string _physicalPath;
        public string PhysicalPath
        {
            get => _physicalPath;
            set => SetProperty(ref _physicalPath, value);
        }

        // FTP Client Specific
        private string _remotePath;
        public string RemotePath
        {
            get => _remotePath;
            set => SetProperty(ref _remotePath, value);
        }

        private string _localDirectory;
        public string LocalDirectory
        {
            get => _localDirectory;
            set => SetProperty(ref _localDirectory, value);
        }

        // Additional Fields
        private bool _enabled;
        public bool Enabled
        {
            get => _enabled;
            set => SetProperty(ref _enabled, value);
        }

        private string _description;
        public string Description
        {
            get => _description;
            set => SetProperty(ref _description, value);
        }

        private string _remark;
        public string Remark
        {
            get => _remark;
            set => SetProperty(ref _remark, value);
        }

        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }

        public event EventHandler SaveCompleted;
        public event EventHandler CancelRequested;

        public CameraInterfaceConfigurationViewModel(IDeviceConfigurationService deviceService)
        {
            _deviceService = deviceService;

            Protocols = new ObservableCollection<string>
            {
                "FTP-Server",
                "FTP-Client",
                "EthernetIP",
                "Ethercat",
                "Custom Protocol"
            };

            SaveCommand = new RelayCommand(async () => await OnSaveAsync(), CanSave);
            CancelCommand = new RelayCommand(OnCancel);
        }

        public void InitializeNewInterface(DeviceModel parentDevice)
        {
            Title = "Camera Interface Configuration - New";
            IsEditMode = false;
            _parentDevice = parentDevice;
            _currentInterface = new CameraInterfaceModel
            {
                DeviceNo = parentDevice.DeviceNo,
                DeviceName = parentDevice.DeviceName,
                Protocol = "FTP-Server",
                Port = 21,
                Enabled = true
            };
            LoadFromModel(_currentInterface);
        }

        public void LoadForEdit(DeviceModel parentDevice, CameraInterfaceModel cameraInterface)
        {
            Title = "Camera Interface Configuration - Edit";
            IsEditMode = true;
            _parentDevice = parentDevice;
            _currentInterface = cameraInterface.Clone();
            LoadFromModel(_currentInterface);
        }

        private void LoadFromModel(CameraInterfaceModel model)
        {
            DeviceNo = model.DeviceNo;
            CameraName = model.DeviceName;
            Name = model.Name;
            SelectedProtocol = model.Protocol ?? "FTP-Server";
            IPAddress = model.IPAddress;
            Port = model.Port;
            Gateway = model.Gateway;
            AllowAnonymous = model.AnonymousLogin;
            Username = model.Username;
            Password = model.Password;
            Enabled = model.Enabled;
            Description = model.Description;
            Remark = model.Remark;

            // Load protocol-specific fields
            if (model.Protocol == "FTP-Server")
            {
                PhysicalPath = model.RemotePath;
            }
            else if (model.Protocol == "FTP-Client")
            {
                RemotePath = model.RemotePath;
                LocalDirectory = model.LocalDirectory;
            }
        }

        private void SaveToModel()
        {
            _currentInterface.DeviceNo = DeviceNo;
            _currentInterface.DeviceName = CameraName;
            _currentInterface.Name = Name;
            _currentInterface.Protocol = SelectedProtocol;
            _currentInterface.IPAddress = IPAddress;
            _currentInterface.Port = Port;
            _currentInterface.Gateway = Gateway;
            _currentInterface.AnonymousLogin = AllowAnonymous;
            _currentInterface.Username = Username;
            _currentInterface.Password = Password;
            _currentInterface.Enabled = Enabled;
            _currentInterface.Description = Description;
            _currentInterface.Remark = Remark;

            // Save protocol-specific fields
            if (SelectedProtocol == "FTP-Server")
            {
                _currentInterface.RemotePath = PhysicalPath;
                _currentInterface.LocalDirectory = null;
            }
            else if (SelectedProtocol == "FTP-Client")
            {
                _currentInterface.RemotePath = RemotePath;
                _currentInterface.LocalDirectory = LocalDirectory;
            }
        }

        private bool CanSave()
        {
            return !string.IsNullOrWhiteSpace(Name) &&
                   !string.IsNullOrWhiteSpace(SelectedProtocol);
        }

        private async Task OnSaveAsync()
        {
            SaveToModel();

            if (IsEditMode)
            {
                await _deviceService.UpdateCameraInterfaceAsync(_currentInterface);
            }
            else
            {
                await _deviceService.AddCameraInterfaceAsync(_currentInterface);
            }

            SaveCompleted?.Invoke(this, EventArgs.Empty);
        }

        private void OnCancel()
        {
            CancelRequested?.Invoke(this, EventArgs.Empty);
        }
    }
}


