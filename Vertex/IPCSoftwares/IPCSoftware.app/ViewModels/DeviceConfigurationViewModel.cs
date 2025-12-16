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

    /// ViewModel responsible for managing device configuration.
    /// Handles creation, editing, and saving of device details between 
    /// the UI and the IDeviceConfigurationService.
    public class DeviceConfigurationViewModel : BaseViewModel
    {
        private readonly IDeviceConfigurationService _deviceService;           // Service for CRUD operations
        private DeviceModel _currentDevice;                                    // The device currently being created or edited
        private bool _isEditMode;                                             // True when editing, false when creating a new device
        private string _title;                                               // Title shown in the UI (Add/Edit)


        // ---------------- Title and Mode ----------------//

        /// Window or page title, changes based on mode (Add/Edit).
        public string Title
        {
            get => _title;
            set => SetProperty(ref _title, value);
        }

        /// Indicates if the current view is in Edit mode (true) or Add mode (false).
        public bool IsEditMode
        {
            get => _isEditMode;
            set => SetProperty(ref _isEditMode, value);
        }

        // -----------------Device Properties (Bound to UI) -----------------//
        private int _deviceNo;

        /// Unique device number assigned to the device.
        public int DeviceNo
        {
            get => _deviceNo;
            set => SetProperty(ref _deviceNo, value);
        }

        private string _deviceName;

        /// Name or identifier of the device.
        public string DeviceName
        {
            get => _deviceName;
            set => SetProperty(ref _deviceName, value);
        }

        private string _selectedDeviceType;

        /// Selected type of device (e.g., PLC, Robo, CCD).
        public string SelectedDeviceType
        {
            get => _selectedDeviceType;
            set => SetProperty(ref _selectedDeviceType, value);
        }

        private string _make;

        /// Manufacturer or brand of the device.
        public string Make
        {
            get => _make;
            set => SetProperty(ref _make, value);
        }

        private string _model;

        /// Model number or series of the device.
        public string Model
        {
            get => _model;
            set => SetProperty(ref _model, value);
        }

        private string _description;

        /// Detailed description or specifications of the device.
        public string Description
        {
            get => _description;
            set => SetProperty(ref _description, value);
        }

        private string _remark;

        /// Additional notes or remarks related to the device.

        public string Remark
        {
            get => _remark;
            set => SetProperty(ref _remark, value);
        }

        private bool _enabled;

        /// Indicates if the device is currently active/enabled.
        public bool Enabled
        {
            get => _enabled;
            set => SetProperty(ref _enabled, value);
        }


        // ---------------Collections and Commands--------------------//

        /// List of device types available for selection (e.g., PLC, Robo, CCD).
        public ObservableCollection<string> DeviceTypes { get; }

        /// Command to save device details to the database.
        public ICommand SaveCommand { get; }
        /// Command to cancel the current operation and return to the list view.
        

        public ICommand CancelCommand { get; }

        /// Raised when save operation completes successfully.

        public event EventHandler SaveCompleted;

        /// Raised when the user cancels the operation.
        /// 
        public event EventHandler CancelRequested;


        // -----------------Constructor--------------//

        /// Initializes a new instance of the DeviceConfigurationViewModel.
        /// Sets up device types, commands, and initializes a blank device.
        public DeviceConfigurationViewModel(IDeviceConfigurationService deviceService)
        {
            _deviceService = deviceService;

            // Initialize available device types
            DeviceTypes = new ObservableCollection<string> { "PLC", "Robo", "CCD" };

            // Initialize commands
            SaveCommand = new RelayCommand(async () => await OnSaveAsync(), CanSave);
            CancelCommand = new RelayCommand(OnCancel);

            // Initialize for new device creation
            InitializeNewDevice();
        }

        // -------------------Initialization Methods -------------------//

        /// Prepares the view for adding a new device.
        /// Sets default title and resets all fields.
        public void InitializeNewDevice()
        {
            Title = "Add Device";
            IsEditMode = false;
            _currentDevice = new DeviceModel();
            LoadFromModel(_currentDevice);
        }

        /// Loads an existing device for editing.
        /// Clones the model to prevent modifying the original until saved.
        public void LoadForEdit(DeviceModel device)
        {
            Title = "Edit Device";
            IsEditMode = true;
            _currentDevice = device.Clone();
            LoadFromModel(_currentDevice);
        }

        // -------------------Model Synchronization-------------------//

        /// Copies data from a DeviceModel into ViewModel properties for display.
        private void LoadFromModel(DeviceModel device)
        {
            DeviceNo = device.DeviceNo;
            DeviceName = device.DeviceName;
            SelectedDeviceType = device.DeviceType ?? "PLC";
            Make = device.Make;
            Model = device.Model;
            Description = device.Description;
            Remark = device.Remark;
            Enabled = device.Enabled;
        }

        /// Saves ViewModel field values back into the DeviceModel.
        private void SaveToModel()
        {
            _currentDevice.DeviceNo = DeviceNo;
            _currentDevice.DeviceName = DeviceName;
            _currentDevice.DeviceType = SelectedDeviceType;
            _currentDevice.Make = Make;
            _currentDevice.Model = Model;
            _currentDevice.Description = Description;
            _currentDevice.Remark = Remark;
            _currentDevice.Enabled = Enabled;
        }

        // -----------------Command Logic------------------//

        /// Determines whether the Save button should be enabled.
        private bool CanSave()
        {
            return DeviceNo > 0 &&
                   !string.IsNullOrWhiteSpace(DeviceName) &&
                   !string.IsNullOrWhiteSpace(SelectedDeviceType);
        }

        /// Saves the current device (either adds or updates based on mode).
        /// Triggers SaveCompleted event after successful save.
        private async Task OnSaveAsync()
        {
            SaveToModel();

            if (IsEditMode)
            {
                await _deviceService.UpdateDeviceAsync(_currentDevice);
            }
            else
            {
                await _deviceService.AddDeviceAsync(_currentDevice);
            }

            SaveCompleted?.Invoke(this, EventArgs.Empty);
        }

        /// Cancels the current operation and triggers CancelRequested event.
        private void OnCancel()
        {
            CancelRequested?.Invoke(this, EventArgs.Empty);
        }
    }
}
