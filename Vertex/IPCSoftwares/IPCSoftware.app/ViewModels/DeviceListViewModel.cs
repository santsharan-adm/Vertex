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
    /// ViewModel responsible for displaying and managing the list of devices.
    /// Provides operations for adding, editing, deleting, and configuring devices.
    

    public class DeviceListViewModel : BaseViewModel
    {
        private readonly IDeviceConfigurationService _deviceService;                  // Handles data operations for devices
        private readonly INavigationService _nav;                                    // Manages navigation between views
        private ObservableCollection<DeviceModel> _devices;                          // Collection of devices bound to the UI
        private DeviceModel _selectedDevice;                                         // Currently selected device in the list


        // -------------------Public Properties (Bindable)------------------//

        /// List of all available devices displayed in the device list UI.
        public ObservableCollection<DeviceModel> Devices
        {
            get => _devices;
            set => SetProperty(ref _devices, value);
        }

        /// The currently selected device in the list (used for edit/delete/configure actions).
        public DeviceModel SelectedDevice
        {
            get => _selectedDevice;
            set => SetProperty(ref _selectedDevice, value);
        }

        // -------------------Commands (Bound to Buttons/Actions)-----------------------//

        /// Command to navigate to Add Device view.
        public ICommand AddDeviceCommand { get; }

        /// Command to edit the selected device.
        public ICommand EditDeviceCommand { get; }

        /// Command to delete the selected device from the list.
        public ICommand DeleteDeviceCommand { get; }

        /// Command to configure the selected device (opens its detail or interface configuration view).
        public ICommand ConfigDeviceCommand { get; }

        /// Command triggered when a device row is double-clicked (acts as a shortcut for configuration).
        public ICommand RowDoubleClickCommand { get; }


        // -----------------Constructor-------------------//

        /// Initializes a new instance of the DeviceListViewModel class.
        /// Sets up commands and loads the device list.
        public DeviceListViewModel(IDeviceConfigurationService deviceService, INavigationService nav)
        {
            _deviceService = deviceService;
            _nav = nav;
            Devices = new ObservableCollection<DeviceModel>();

            // Initialize commands and their corresponding handlers

            AddDeviceCommand = new RelayCommand(OnAddDevice);
            EditDeviceCommand = new RelayCommand<DeviceModel>(OnEditDevice);
            DeleteDeviceCommand = new RelayCommand<DeviceModel>(OnDeleteDevice);
            ConfigDeviceCommand = new RelayCommand<DeviceModel>(OnConfigDevice);
            RowDoubleClickCommand = new RelayCommand<DeviceModel>(OnConfigDevice);


            // Load device list asynchronously on startup
            _ = LoadDataAsync();
        }


        // --------------Data Loading-------------------//

        /// Asynchronously loads all devices from the device configuration service.
        /// Refreshes the device list displayed in the UI.
        public async Task LoadDataAsync()
        {
            var devices = await _deviceService.GetAllDevicesAsync();
            Devices.Clear();
            foreach (var device in devices)
            {
                Devices.Add(device);
            }
        }


        // ---------------- Command Handlers----------------//

        /// Opens the Add Device configuration view.
        /// Reloads the device list after the new device is added.
        private void OnAddDevice()
        {
            _nav.NavigateToDeviceConfiguration(null, async () =>
            {
                await LoadDataAsync();
            });
        }

        /// Opens the Edit Device configuration view for the selected device.
        /// Reloads the device list after saving changes.

        private void OnEditDevice(DeviceModel device)
        {
            if (device == null) return;

            _nav.NavigateToDeviceConfiguration(device, async () =>
            {
                await LoadDataAsync();
            });
        }

        /// Deletes the selected device after confirmation.
        /// Refreshes the device list upon completion.

        private async void OnDeleteDevice(DeviceModel device)
        {
            if (device == null) return;

            // TODO: Add confirmation dialog
            await _deviceService.DeleteDeviceAsync(device.Id);
            await LoadDataAsync();
        }

        /// Navigates to the appropriate configuration view based on the selected device type.
        /// Example:
        ///  - PLC → DeviceDetailView (interfaces)
        ///  - CCD → CameraDetailView (camera interfaces)
        ///  - Robot → RobotDetailView (future support)
        

        private void OnConfigDevice(DeviceModel device)
        {
            if (device == null) return;

            // Route based on device type
            if (device.DeviceType == "PLC")
            {
                _nav.NavigateToDeviceDetail(device);  // Opens DeviceDetailView (PLC interfaces)
            }
            else if (device.DeviceType == "CCD")
            {
                _nav.NavigateToCameraDetail(device);  // Opens CameraDetailView (Camera interfaces)
            }
            else if (device.DeviceType == "Robot")
            {
                // TODO: Navigate to RobotDetailView when implemented
                _nav.NavigateToDeviceDetail(device);  // Fallback to PLC view for now
            }
        }
    }
}
