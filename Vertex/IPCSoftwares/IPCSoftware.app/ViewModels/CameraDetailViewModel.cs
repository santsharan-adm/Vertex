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
    /// ViewModel for displaying and managing camera interface details 
    /// associated with a specific camera device.
    /// Provides functionality to add, edit, and delete camera interfaces.
    public class CameraDetailViewModel : BaseViewModel
    {
        private readonly IDeviceConfigurationService _deviceService;          // Service for device and camera interface operations
        private readonly INavigationService _nav;                              // Navigation service to switch between screens
        private DeviceModel _currentDevice;                                     // Currently selected camera device
        private ObservableCollection<CameraInterfaceModel> _cameraInterfaces;   // Collection of interfaces for the selected camera
        private CameraInterfaceModel _selectedCameraInterface;                 // Currently selected camera interface


        // ----------------- Public Properties (Bindable to UI) -----------------//

        /// Currently selected camera device (parent device).
        public DeviceModel CurrentDevice
        {
            get => _currentDevice;
            set => SetProperty(ref _currentDevice, value);
        }

        /// List of all camera interfaces linked to the current device.
        public ObservableCollection<CameraInterfaceModel> CameraInterfaces
        {
            get => _cameraInterfaces;
            set => SetProperty(ref _cameraInterfaces, value);
        }

        /// Currently selected camera interface (for edit/delete operations).

        public CameraInterfaceModel SelectedCameraInterface
        {
            get => _selectedCameraInterface;
            set => SetProperty(ref _selectedCameraInterface, value);
        }

        /// Page title displaying the current camera details (dynamic binding).

        public string PageTitle => $"Camera Interface Configuration - Camera {CurrentDevice?.DeviceNo}, {CurrentDevice?.DeviceName}";


        // -------------------- Commands (Bound to Buttons in View)-------------------//
        public ICommand AddInterfaceCommand { get; }         // Adds a new camera interface
        public ICommand EditInterfaceCommand { get; }        // Edits selected camera interface
        public ICommand DeleteInterfaceCommand { get; }       // Deletes selected camera interface
        public ICommand BackCommand { get; }                  // Navigates back to device list view


        // -------------------------Constructor---------------//

        /// Initializes the ViewModel, injects dependencies, 
        /// and sets up command bindings.
        public CameraDetailViewModel(IDeviceConfigurationService deviceService, INavigationService nav)
        {
            _deviceService = deviceService;
            _nav = nav;
            CameraInterfaces = new ObservableCollection<CameraInterfaceModel>();

            // Initialize commands
            AddInterfaceCommand = new RelayCommand(OnAddInterface);
            EditInterfaceCommand = new RelayCommand<CameraInterfaceModel>(OnEditInterface);
            DeleteInterfaceCommand = new RelayCommand<CameraInterfaceModel>(OnDeleteInterface);
            BackCommand = new RelayCommand(OnBack);
        }

        // ----------------------- Data Loading ----------------------//

        /// Loads the current device and its associated camera interfaces.
        /// Called when navigating to this view.
        public async Task LoadDevice(DeviceModel device)
        {
            CurrentDevice = device;
            OnPropertyChanged(nameof(PageTitle));    // Update dynamic title
            await LoadCameraInterfacesAsync();
        }



        /// Fetches all camera interfaces from the service for the current device.
        /// Refreshes the observable collection.
        private async Task LoadCameraInterfacesAsync()
        {
            if (CurrentDevice == null) return;

            var cameraInterfaces = await _deviceService.GetCameraInterfacesByDeviceNoAsync(CurrentDevice.DeviceNo);
            CameraInterfaces.Clear();
            foreach (var camInterface in cameraInterfaces)
            {
                CameraInterfaces.Add(camInterface);
            }
        }


        // --------------------Command Handlers --------------------//

        /// Navigates to the Camera Interface Configuration view to add a new interface.
        /// On save, reloads the camera interface list.
        private void OnAddInterface()
        {
            _nav.NavigateToCameraInterfaceConfiguration(CurrentDevice, null, async () =>
            {
                await LoadCameraInterfacesAsync();
            });
        }


        /// Opens the Camera Interface Configuration view to edit the selected interface.
        /// On save, reloads the camera interface list.
        private void OnEditInterface(CameraInterfaceModel cameraInterface)
        {
            if (cameraInterface == null) return;

            _nav.NavigateToCameraInterfaceConfiguration(CurrentDevice, cameraInterface, async () =>
            {
                await LoadCameraInterfacesAsync();
            });
        }


        /// Deletes the selected camera interface and reloads the list.
        private async void OnDeleteInterface(CameraInterfaceModel cameraInterface)
        {
            if (cameraInterface == null) return;

            // TODO: Add confirmation dialog
            await _deviceService.DeleteCameraInterfaceAsync(cameraInterface.Id);
            await LoadCameraInterfacesAsync();
        }


        /// Navigates back to the main device list view.
        private void OnBack()
        {
            _nav.NavigateToDeviceList();
        }
    }
}
