using IPCSoftware.Core.Interfaces;
using IPCSoftware.Core.Interfaces.AppLoggerInterface;
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
    public class DeviceListViewModel : BaseViewModel
    {
        private readonly IDeviceConfigurationService _deviceService;
        private readonly INavigationService _nav;
        private ObservableCollection<DeviceModel> _devices;
        private DeviceModel _selectedDevice;

        public ObservableCollection<DeviceModel> Devices
        {
            get => _devices;
            set => SetProperty(ref _devices, value);
        }

        public DeviceModel SelectedDevice
        {
            get => _selectedDevice;
            set => SetProperty(ref _selectedDevice, value);
        }

        public ICommand AddDeviceCommand { get; }
        public ICommand EditDeviceCommand { get; }
        public ICommand DeleteDeviceCommand { get; }
        public ICommand ConfigDeviceCommand { get; }  // Opens device detail with interfaces
        public ICommand RowDoubleClickCommand { get; }

        public DeviceListViewModel(
            IDeviceConfigurationService deviceService, 
            INavigationService nav, IAppLogger logger) : base(logger)
        {
            _deviceService = deviceService;
            _nav = nav;
            Devices = new ObservableCollection<DeviceModel>();

            AddDeviceCommand = new RelayCommand(OnAddDevice);
            EditDeviceCommand = new RelayCommand<DeviceModel>(OnEditDevice);
            DeleteDeviceCommand = new RelayCommand<DeviceModel>(OnDeleteDevice);
            ConfigDeviceCommand = new RelayCommand<DeviceModel>(OnConfigDevice);
            RowDoubleClickCommand = new RelayCommand<DeviceModel>(OnConfigDevice);

            _ = LoadDataAsync();
        }

        public async Task LoadDataAsync()
        {
            var devices = await _deviceService.GetAllDevicesAsync();
            Devices.Clear();
            foreach (var device in devices)
            {
                Devices.Add(device);
            }
        }

        private void OnAddDevice()
        {
            _nav.NavigateToDeviceConfiguration(null, async () =>
            {
                await LoadDataAsync();
            });
        }

        private void OnEditDevice(DeviceModel device)
        {
            if (device == null) return;

            _nav.NavigateToDeviceConfiguration(device, async () =>
            {
                await LoadDataAsync();
            });
        }

        private async void OnDeleteDevice(DeviceModel device)
        {
            if (device == null) return;

            // TODO: Add confirmation dialog
            await _deviceService.DeleteDeviceAsync(device.Id);
            await LoadDataAsync();
        }

        // Opens device detail view showing interfaces
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
