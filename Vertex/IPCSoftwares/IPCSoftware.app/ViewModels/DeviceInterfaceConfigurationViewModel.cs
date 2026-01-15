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
    /// ViewModel responsible for adding or editing a Device Interface configuration.
    /// Handles form data, validation, and communication with the device configuration service.
    public class DeviceInterfaceConfigurationViewModel : BaseViewModel
    {
        private readonly IDeviceConfigurationService _deviceService;         // Service for saving and loading interface configurations
        private DeviceInterfaceModel _currentInterface;                       // The current interface being edited or created
        private DeviceModel _parentDevice;                                  // Parent device to which this interface belongs
        private bool _isEditMode;                                           // Flag to determine whether editing or creating
        private string _title;                                                // Window/page title text

        // ----------------UI Properties (Bound to the View)----------------//

        /// Title displayed in the window or page header (e.g., "Add Interface" or "Edit Interface").

        public string Title
        {
            get => _title;
            set => SetProperty(ref _title, value);
        }

        /// Indicates whether the ViewModel is in edit mode (true) or add mode (false).
        public bool IsEditMode
        {
            get => _isEditMode;
            set => SetProperty(ref _isEditMode, value);
        }

        // ------------------Device Interface Properties-----------------------//

        private int _deviceNo;

        /// The device number this interface belongs to.
        public int DeviceNo
        {
            get => _deviceNo;
            set => SetProperty(ref _deviceNo, value);
        }

        private string _deviceName;

        /// The name of the parent device.
        public string DeviceName
        {
            get => _deviceName;
            set => SetProperty(ref _deviceName, value);
        }

        private int _unitNo;

        /// Unit number for identifying interface subcomponents or subunits.
        public int UnitNo
        {
            get => _unitNo;
            set => SetProperty(ref _unitNo, value);
        }

        private string _name;

        /// Name or label for this device interface.
        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        private string _selectedComProtocol;

        /// Selected communication protocol (e.g., Modbus Ethernet, EtherCAT, RTU).
        public string SelectedComProtocol
        {
            get => _selectedComProtocol;
            set => SetProperty(ref _selectedComProtocol, value);
        }

        private string _ipAddress;

        /// IP address assigned to the device interface.
        public string IPAddress
        {
            get => _ipAddress;
            set => SetProperty(ref _ipAddress, value);
        }

        private int _portNo;

        /// Port number used for communication with this interface.
        public int PortNo
        {
            get => _portNo;
            set => SetProperty(ref _portNo, value);
        }

        private string _gateway;

        /// Network gateway address (optional for some communication protocols).

        public string Gateway
        {
            get => _gateway;
            set => SetProperty(ref _gateway, value);
        }

        private string _description;

        /// Detailed description of the interface (optional).
        public string Description
        {
            get => _description;
            set => SetProperty(ref _description, value);
        }

        private string _remark;

        /// Additional notes or remarks related to the interface.
        public string Remark
        {
            get => _remark;
            set => SetProperty(ref _remark, value);
        }

        private bool _enabled;

        /// Indicates if this interface is currently enabled or active.
        public bool Enabled
        {
            get => _enabled;
            set => SetProperty(ref _enabled, value);
        }


        // -------------------ComboBox Data & Commands-------------------------//

        /// List of available communication protocols for the interface.

        public ObservableCollection<string> ComProtocols { get; }

        /// Command to save interface details (Add or Edit mode).
        
        public ICommand SaveCommand { get; }

        /// Command to cancel operation and return to the previous view.

        public ICommand CancelCommand { get; }

        /// Event triggered when saving is completed successfully.

        public event EventHandler SaveCompleted;

        /// Event triggered when the cancel button is pressed.
        
        public event EventHandler CancelRequested;

        // --------------Constructor----------------------//

        /// Initializes a new instance of DeviceInterfaceConfigurationViewModel.
        /// Sets default protocol list and initializes commands.

        public DeviceInterfaceConfigurationViewModel(IDeviceConfigurationService deviceService)
        {
            _deviceService = deviceService;

            // Predefined list of supported communication protocols

            ComProtocols = new ObservableCollection<string>
            {
                "Modbus Ethernet",
                "EthernetIP",
                "EtherCat",
                "RTU"
            };

            // Bind UI commands

            SaveCommand = new RelayCommand(async () => await OnSaveAsync(), CanSave);
            CancelCommand = new RelayCommand(OnCancel);
        }

        // ---------------Initialization Methods---------------//

        /// Initializes the ViewModel for creating a new device interface.
        public void InitializeNewInterface(DeviceModel parentDevice)
        {
            _parentDevice = parentDevice;
            Title = $"Add Interface - {parentDevice.DeviceName}";
            IsEditMode = false;
            _currentInterface = new DeviceInterfaceModel
            {
                DeviceNo = parentDevice.DeviceNo,
                DeviceName = parentDevice.DeviceName
            };
            LoadFromModel(_currentInterface);
        }

        /// Loads an existing interface for editing.


        // ------------------ Model Synchronization-----------------//

        /// Loads data from the interface model into ViewModel properties.
        public void LoadForEdit(DeviceModel parentDevice, DeviceInterfaceModel deviceInterface)
        {
            _parentDevice = parentDevice;
            Title = $"Edit Interface - {parentDevice.DeviceName}";
            IsEditMode = true;
            _currentInterface = deviceInterface.Clone();
            LoadFromModel(_currentInterface);
        }

        private void LoadFromModel(DeviceInterfaceModel deviceInterface)
        {
            DeviceNo = deviceInterface.DeviceNo;
            DeviceName = deviceInterface.DeviceName;
            UnitNo = deviceInterface.UnitNo;
            Name = deviceInterface.Name;
            SelectedComProtocol = deviceInterface.ComProtocol ?? "Modbus Ethernet";
            IPAddress = deviceInterface.IPAddress;
            PortNo = deviceInterface.PortNo;
            Gateway = deviceInterface.Gateway;
            Description = deviceInterface.Description;
            Remark = deviceInterface.Remark;
            Enabled = deviceInterface.Enabled;
        }

        /// Saves data from the ViewModel back into the interface model.
        private void SaveToModel()
        {
            _currentInterface.DeviceNo = DeviceNo;
            _currentInterface.DeviceName = DeviceName;
            _currentInterface.UnitNo = UnitNo;
            _currentInterface.Name = Name;
            _currentInterface.ComProtocol = SelectedComProtocol;
            _currentInterface.IPAddress = IPAddress;
            _currentInterface.PortNo = PortNo;
            _currentInterface.Gateway = Gateway;
            _currentInterface.Description = Description;
            _currentInterface.Remark = Remark;
            _currentInterface.Enabled = Enabled;
        }

        // -----------------Command Logic----------------//

        /// Determines whether the Save command is allowed.

        private bool CanSave()
        {
            return !string.IsNullOrWhiteSpace(Name) &&
                   !string.IsNullOrWhiteSpace(SelectedComProtocol);
        }

        /// Saves the interface (creates or updates based on the current mode).
        /// Triggers SaveCompleted event after a successful save.
        
        private async Task OnSaveAsync()
        {
            SaveToModel();

            if (IsEditMode)
            {
                await _deviceService.UpdateInterfaceAsync(_currentInterface);
            }
            else
            {
                await _deviceService.AddInterfaceAsync(_currentInterface);
            }

            SaveCompleted?.Invoke(this, EventArgs.Empty);
        }

        /// Cancels the operation and triggers CancelRequested event.
        private void OnCancel()
        {
            CancelRequested?.Invoke(this, EventArgs.Empty);
        }
    }
}
