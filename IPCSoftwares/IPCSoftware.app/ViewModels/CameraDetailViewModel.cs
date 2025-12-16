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
    public class CameraDetailViewModel : BaseViewModel
    {
        private readonly IDeviceConfigurationService _deviceService;
        private readonly INavigationService _nav;
        private DeviceModel _currentDevice;
        private ObservableCollection<CameraInterfaceModel> _cameraInterfaces;
        private CameraInterfaceModel _selectedCameraInterface;

        public DeviceModel CurrentDevice
        {
            get => _currentDevice;
            set => SetProperty(ref _currentDevice, value);
        }

        public ObservableCollection<CameraInterfaceModel> CameraInterfaces
        {
            get => _cameraInterfaces;
            set => SetProperty(ref _cameraInterfaces, value);
        }

        public CameraInterfaceModel SelectedCameraInterface
        {
            get => _selectedCameraInterface;
            set => SetProperty(ref _selectedCameraInterface, value);
        }

        public string PageTitle => $"Camera Interface Configuration - Camera {CurrentDevice?.DeviceNo}, {CurrentDevice?.DeviceName}";

        public ICommand AddInterfaceCommand { get; }
        public ICommand EditInterfaceCommand { get; }
        public ICommand DeleteInterfaceCommand { get; }
        public ICommand BackCommand { get; }

        public CameraDetailViewModel(IDeviceConfigurationService deviceService,
            INavigationService nav, IAppLogger logger)/* : base (logger)*/
        {
            _deviceService = deviceService;
            _nav = nav;
            CameraInterfaces = new ObservableCollection<CameraInterfaceModel>();

            AddInterfaceCommand = new RelayCommand(OnAddInterface);
            EditInterfaceCommand = new RelayCommand<CameraInterfaceModel>(OnEditInterface);
            DeleteInterfaceCommand = new RelayCommand<CameraInterfaceModel>(OnDeleteInterface);
            BackCommand = new RelayCommand(OnBack);
        }

        public async Task LoadDevice(DeviceModel device)
        {
            CurrentDevice = device;
            OnPropertyChanged(nameof(PageTitle));
            await LoadCameraInterfacesAsync();
        }

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

        private void OnAddInterface()
        {
            _nav.NavigateToCameraInterfaceConfiguration(CurrentDevice, null, async () =>
            {
                await LoadCameraInterfacesAsync();
            });
        }

        private void OnEditInterface(CameraInterfaceModel cameraInterface)
        {
            if (cameraInterface == null) return;

            _nav.NavigateToCameraInterfaceConfiguration(CurrentDevice, cameraInterface, async () =>
            {
                await LoadCameraInterfacesAsync();
            });
        }

        private async void OnDeleteInterface(CameraInterfaceModel cameraInterface)
        {
            if (cameraInterface == null) return;

            // TODO: Add confirmation dialog
            await _deviceService.DeleteCameraInterfaceAsync(cameraInterface.Id);
            await LoadCameraInterfacesAsync();
        }

        private void OnBack()
        {
            _nav.NavigateToDeviceList();
        }
    }
}
