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
        public ICommand ConfigDeviceCommand { get; }
        public ICommand RowDoubleClickCommand { get; }

        public DeviceListViewModel(IDeviceConfigurationService deviceService, INavigationService nav)
        {
            try
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
            catch (Exception)
            {
                // Exception swallowed to prevent application crash
            }
        }

        public async Task LoadDataAsync()
        {
            try
            {
                var devices = await _deviceService.GetAllDevicesAsync();
                Devices.Clear();
                foreach (var device in devices)
                {
                    Devices.Add(device);
                }
            }
            catch (Exception)
            {
                // Exception swallowed to prevent application crash
            }
        }

        private void OnAddDevice()
        {
            try
            {
                _nav.NavigateToDeviceConfiguration(null, async () =>
                {
                    await LoadDataAsync();
                });
            }
            catch (Exception)
            {
                // Exception swallowed to prevent application crash
            }
        }

        private void OnEditDevice(DeviceModel device)
        {
            try
            {
                if (device == null) return;

                _nav.NavigateToDeviceConfiguration(device, async () =>
                {
                    await LoadDataAsync();
                });
            }
            catch (Exception)
            {
                // Exception swallowed to prevent application crash
            }
        }

        private async void OnDeleteDevice(DeviceModel device)
        {
            try
            {
                if (device == null) return;

                await _deviceService.DeleteDeviceAsync(device.Id);
                await LoadDataAsync();
            }
            catch (Exception)
            {
                // Exception swallowed to prevent application crash
            }
        }

        // Opens device detail view showing interfaces
        private void OnConfigDevice(DeviceModel device)
        {
            try
            {
                if (device == null) return;

                if (device.DeviceType == "PLC")
                {
                    _nav.NavigateToDeviceDetail(device);
                }
                else if (device.DeviceType == "CCD")
                {
                    _nav.NavigateToCameraDetail(device);
                }
                else if (device.DeviceType == "Robot")
                {
                    _nav.NavigateToDeviceDetail(device);
                }
            }
            catch (Exception)
            {
                // Exception swallowed to prevent application crash
            }
        }
    }
}
