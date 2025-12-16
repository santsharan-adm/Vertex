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
    public class DeviceDetailViewModel : BaseViewModel
    {
        private readonly IDeviceConfigurationService _deviceService;
        private readonly INavigationService _nav;
        private DeviceModel _currentDevice;

        private ObservableCollection<DeviceInterfaceModel> _interfaces;
        private DeviceInterfaceModel _selectedInterface;

        public DeviceModel CurrentDevice
        {
            get => _currentDevice;
            set => SetProperty(ref _currentDevice, value);
        }

        public ObservableCollection<DeviceInterfaceModel> Interfaces
        {
            get => _interfaces;
            set => SetProperty(ref _interfaces, value);
        }

        public DeviceInterfaceModel SelectedInterface
        {
            get => _selectedInterface;
            set => SetProperty(ref _selectedInterface, value);
        }

        public string PageTitle => $"Device Interface Configuration - Device {CurrentDevice?.DeviceNo}, {CurrentDevice?.DeviceName}";

        public ICommand AddInterfaceCommand { get; }
        public ICommand EditInterfaceCommand { get; }
        public ICommand DeleteInterfaceCommand { get; }
        public ICommand BackCommand { get; }

        public DeviceDetailViewModel(IDeviceConfigurationService deviceService, INavigationService nav)
        {
            _deviceService = deviceService;
            _nav = nav;
            Interfaces = new ObservableCollection<DeviceInterfaceModel>();

            AddInterfaceCommand = new RelayCommand(OnAddInterface);
            EditInterfaceCommand = new RelayCommand<DeviceInterfaceModel>(OnEditInterface);
            DeleteInterfaceCommand = new RelayCommand<DeviceInterfaceModel>(OnDeleteInterface);
            BackCommand = new RelayCommand(OnBack);
        }

        public async Task LoadDevice(DeviceModel device)
        {
            CurrentDevice = device;
            OnPropertyChanged(nameof(PageTitle));
            await LoadInterfacesAsync();
        }

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

        private void OnAddInterface()
        {
            _nav.NavigateToInterfaceConfiguration(CurrentDevice, null, async () =>
            {
                await LoadInterfacesAsync();
            });
        }

        private void OnEditInterface(DeviceInterfaceModel deviceInterface)
        {
            if (deviceInterface == null) return;

            _nav.NavigateToInterfaceConfiguration(CurrentDevice, deviceInterface, async () =>
            {
                await LoadInterfacesAsync();
            });
        }

        private async void OnDeleteInterface(DeviceInterfaceModel deviceInterface)
        {
            if (deviceInterface == null) return;

            // TODO: Add confirmation dialog
            await _deviceService.DeleteInterfaceAsync(deviceInterface.Id);
            await LoadInterfacesAsync();
        }

        private void OnBack()
        {
            _nav.NavigateToDeviceList();
        }
    }
}
