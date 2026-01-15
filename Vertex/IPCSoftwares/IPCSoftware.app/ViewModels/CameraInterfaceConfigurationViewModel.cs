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
    /// ViewModel that manages configuration of camera communication interfaces.
    /// Supports multiple protocols (FTP Server, FTP Client, etc.)
    /// and handles both creation and editing of interfaces for a camera device.
    public class CameraInterfaceConfigurationViewModel : BaseViewModel
    {
        private readonly IDeviceConfigurationService _deviceService;             // Handles device-related data operations
        private CameraInterfaceModel _currentInterface;                         // The active interface being edited or created
        private DeviceModel _parentDevice;                                      // Parent camera device owning this interface
        private bool _isEditMode;                                               // True if editing existing interface, false if new
        private string _title;                                                 // Page title (changes based on mode)


        // ---------------------- Header and Mode Info --------------------//

        /// Page title shown in the UI (changes based on Add/Edit mode).
        public string Title
        {
            get => _title;
            set => SetProperty(ref _title, value);
        }

        /// Indicates whether the form is in Edit mode or Add mode.
        public bool IsEditMode
        {
            get => _isEditMode;
            set => SetProperty(ref _isEditMode, value);
        }

        // ------------------------Device Info (Read-only) ---------------------//
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

        // --------------------Interface Info-------------------//
        private string _name;
        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        // -------------------Communication Protocol Selection-----------------//
        private string _selectedProtocol;
        public string SelectedProtocol
        {
            get => _selectedProtocol;
            set
            {
                if (SetProperty(ref _selectedProtocol, value))
                {
                    // Update visibility flags when protocol changes
                    OnPropertyChanged(nameof(IsFTPServer));
                    OnPropertyChanged(nameof(IsFTPClient));
                }
            }
        }

        /// List of available communication protocols.
        /// Bound to dropdown in UI.

        public ObservableCollection<string> Protocols { get; }


        // -----------Protocol Visibility Helpers -----------//


        /// True if currently selected protocol is FTP-Server.
        /// Used to toggle visibility of FTP-Server-specific UI fields.
        public bool IsFTPServer => SelectedProtocol == "FTP-Server";

        /// True if currently selected protocol is FTP-Client.
        /// Used to toggle visibility of FTP-Client-specific UI fields.
        public bool IsFTPClient => SelectedProtocol == "FTP-Client";


        // ----------------- Common Network Fields-------------------//
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


        // --------------Authentication---------------------------//

        private bool _allowAnonymous;
        public bool AllowAnonymous
        {
            get => _allowAnonymous;
            set
            {
                if (SetProperty(ref _allowAnonymous, value))
                {
                    // Clear credentials if anonymous access is enabled
                    if (_allowAnonymous)
                    {
                        Username = string.Empty;
                        Password = string.Empty;
                    }
                }
            }
        }

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


        // ----------------- FTP Server Specific Fields -------------------//
        private string _physicalPath;
        public string PhysicalPath
        {
            get => _physicalPath;
            set => SetProperty(ref _physicalPath, value);
        }

        // -------------------FTP Client Specific Fields ------------------//
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

        // -------------------Additional Info-------------------//

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


        // ----------------- Commands and Events ------------------//
        public ICommand SaveCommand { get; }            // Saves the configuration
        public ICommand CancelCommand { get; }           // Cancels and goes back

        public event EventHandler SaveCompleted;         // Raised after successful save
        public event EventHandler CancelRequested;       // Raised when cancel is clicked



        // ------------------Constructor---------------//

        /// Initializes available protocols and commands.
        public CameraInterfaceConfigurationViewModel(IDeviceConfigurationService deviceService)
        {
            _deviceService = deviceService;

            // Available communication protocols
            Protocols = new ObservableCollection<string>
            {
                "FTP-Server",
                "FTP-Client",
                "EthernetIP",
                "Ethercat",
                "Custom Protocol"
            };

            // Command bindings
            SaveCommand = new RelayCommand(async () => await OnSaveAsync(), CanSave);
            CancelCommand = new RelayCommand(OnCancel);
        }

        // ----------------Initialization Methods ----------------//

        /// Initializes a new interface configuration for a specific parent device.
        /// Sets default values (FTP-Server, Port 21, Enabled = true).
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

        /// Loads existing interface configuration for editing.
        public void LoadForEdit(DeviceModel parentDevice, CameraInterfaceModel cameraInterface)
        {
            Title = "Camera Interface Configuration - Edit";
            IsEditMode = true;
            _parentDevice = parentDevice;
            _currentInterface = cameraInterface.Clone();
            LoadFromModel(_currentInterface);
        }

        // ----------------------Model Binding Helpers------------------------//

        /// Loads model data into ViewModel properties for display.
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


        /// Updates the underlying model with values from the ViewModel.
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


        // ---------------------Save and Cancel Logic---------------//


        /// Determines whether the Save button should be enabled.
        private bool CanSave()
        {
            return !string.IsNullOrWhiteSpace(Name) &&
                   !string.IsNullOrWhiteSpace(SelectedProtocol);
        }


        /// Saves the configuration (creates or updates interface).
        /// Invokes SaveCompleted event after successful save.
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


        /// Cancels the configuration and triggers CancelRequested event.
        private void OnCancel()
        {
            CancelRequested?.Invoke(this, EventArgs.Empty);
        }
    }
}


