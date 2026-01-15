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
    /// ViewModel responsible for managing and displaying 
    /// the details of a specific device and its associated interfaces.
    /// 
    /// Supports viewing, adding, editing, and deleting device interfaces.
    public class DeviceDetailViewModel : BaseViewModel
    {
        private readonly IDeviceConfigurationService _deviceService;         // Service for managing devices and interfaces
        private readonly INavigationService _nav;                             // Service for navigating between views
        private DeviceModel _currentDevice;                                  // Currently selected device

        private ObservableCollection<DeviceInterfaceModel> _interfaces;       // List of interfaces belonging to the device
        private DeviceInterfaceModel _selectedInterface;                       // Currently selected interface


        // -----------------Public Properties (Bindable to UI)------------------//


        /// The device whose details are currently displayed.
        public DeviceModel CurrentDevice
        {
            get => _currentDevice;
            set => SetProperty(ref _currentDevice, value);
        }

        /// Collection of interfaces associated with the current device.
        public ObservableCollection<DeviceInterfaceModel> Interfaces
        {
            get => _interfaces;
            set => SetProperty(ref _interfaces, value);
        }

        /// The currently selected device interface (used for edit/delete actions).
        public DeviceInterfaceModel SelectedInterface
        {
            get => _selectedInterface;
            set => SetProperty(ref _selectedInterface, value);
        }

        /// Page title shown in the UI, dynamically generated using device information.
        public string PageTitle => $"Device Interface Configuration - Device {CurrentDevice?.DeviceNo}, {CurrentDevice?.DeviceName}";


        // --------------Commands (Bound to Buttons)----------------//
        public ICommand AddInterfaceCommand { get; }                       // Command to add a new interface
        public ICommand EditInterfaceCommand { get; }                      // Command to edit an existing interface
        public ICommand DeleteInterfaceCommand { get; }                    // Command to delete selected interface
        public ICommand BackCommand { get; }                                // Command to navigate back to device list


        // ------------------Constructor--------------------//

        /// Initializes the ViewModel, command bindings, and collections.
        public DeviceDetailViewModel(IDeviceConfigurationService deviceService, INavigationService nav)
        {
            _deviceService = deviceService;
            _nav = nav;
            Interfaces = new ObservableCollection<DeviceInterfaceModel>();

            // Initialize commands
            AddInterfaceCommand = new RelayCommand(OnAddInterface);
            EditInterfaceCommand = new RelayCommand<DeviceInterfaceModel>(OnEditInterface);
            DeleteInterfaceCommand = new RelayCommand<DeviceInterfaceModel>(OnDeleteInterface);
            BackCommand = new RelayCommand(OnBack);
        }

        // ------------- Data Loading Methods--------------//

        /// Loads the given device and retrieves all its interfaces from the service.
        public async Task LoadDevice(DeviceModel device)
        {
            CurrentDevice = device;
            OnPropertyChanged(nameof(PageTitle));                     // Update UI title
            await LoadInterfacesAsync();
        }


        /// Fetches all interfaces for the currently loaded device.
        /// Updates the Interfaces collection bound to the UI.
        private async Task LoadInterfacesAsync()
        {
            if (CurrentDevice == null) return;

            var interfaces = await _deviceService.GetInterfacesByDeviceNoAsync(CurrentDevice.DeviceNo);
            Interfaces.Clear();
            foreach (var iface in interfaces)
            {
                Interfaces.Add(iface);
            }
        }

        // ---------------Command Handlers----------------//

        /// Navigates to the Interface Configuration view to add a new interface.
        /// After saving, reloads the interface list.

        private void OnAddInterface()
        {
            _nav.NavigateToInterfaceConfiguration(CurrentDevice, null, async () =>
            {
                await LoadInterfacesAsync();
            });
        }

        /// Opens the Interface Configuration view to edit the selected interface.
        /// After saving, reloads the interface list.
        private void OnEditInterface(DeviceInterfaceModel deviceInterface)
        {
            if (deviceInterface == null) return;

            _nav.NavigateToInterfaceConfiguration(CurrentDevice, deviceInterface, async () =>
            {
                await LoadInterfacesAsync();
            });
        }

        /// Deletes the selected interface after confirmation.
        /// Then refreshes the interface list.

        private async void OnDeleteInterface(DeviceInterfaceModel deviceInterface)
        {
            if (deviceInterface == null) return;

            // TODO: Add confirmation dialog
            await _deviceService.DeleteInterfaceAsync(deviceInterface.Id);
            await LoadInterfacesAsync();
        }

        /// Navigates back to the device list screen.
        private void OnBack()
        {
            _nav.NavigateToDeviceList();
        }
    }
}
