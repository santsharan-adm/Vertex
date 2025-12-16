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

        public string PageTitle =>
            $"Camera Interface Configuration - Camera {CurrentDevice?.DeviceNo}, {CurrentDevice?.DeviceName}";

        public ICommand AddInterfaceCommand { get; }
        public ICommand EditInterfaceCommand { get; }
        public ICommand DeleteInterfaceCommand { get; }
        public ICommand BackCommand { get; }

        public CameraDetailViewModel(IDeviceConfigurationService deviceService, INavigationService nav)
        {
            try
            {
                _deviceService = deviceService;
                _nav = nav;
                CameraInterfaces = new ObservableCollection<CameraInterfaceModel>();

                AddInterfaceCommand = new RelayCommand(OnAddInterface);
                EditInterfaceCommand = new RelayCommand<CameraInterfaceModel>(OnEditInterface);
                DeleteInterfaceCommand = new RelayCommand<CameraInterfaceModel>(OnDeleteInterface);
                BackCommand = new RelayCommand(OnBack);
            }
            catch (Exception)
            {
                
            }
        }

        public async Task LoadDevice(DeviceModel device)
        {
            try
            {
                CurrentDevice = device;
                OnPropertyChanged(nameof(PageTitle));
                await LoadCameraInterfacesAsync();
            }
            catch (Exception)
            {
                
            }
        }

        private async Task LoadCameraInterfacesAsync()
        {
            try
            {
                if (CurrentDevice == null) return;

                var cameraInterfaces =
                    await _deviceService.GetCameraInterfacesByDeviceNoAsync(CurrentDevice.DeviceNo);

                CameraInterfaces.Clear();
                foreach (var camInterface in cameraInterfaces)
                {
                    CameraInterfaces.Add(camInterface);
                }
            }
            catch (Exception)
            {
                
            }
        }

        private void OnAddInterface()
        {
            try
            {
                _nav.NavigateToCameraInterfaceConfiguration(CurrentDevice, null, async () =>
                {
                    await LoadCameraInterfacesAsync();
                });
            }
            catch (Exception)
            {
                
            }
        }

        private void OnEditInterface(CameraInterfaceModel cameraInterface)
        {
            try
            {
                if (cameraInterface == null) return;

                _nav.NavigateToCameraInterfaceConfiguration(CurrentDevice, cameraInterface, async () =>
                {
                    await LoadCameraInterfacesAsync();
                });
            }
            catch (Exception)
            {
                
            }
        }

        private async void OnDeleteInterface(CameraInterfaceModel cameraInterface)
        {
            try
            {
                if (cameraInterface == null) return;

                await _deviceService.DeleteCameraInterfaceAsync(cameraInterface.Id);
                await LoadCameraInterfacesAsync();
            }
            catch (Exception)
            {
                
            }
        }

        private void OnBack()
        {
            try
            {
                _nav.NavigateToDeviceList();
            }
            catch (Exception)
            {
                
            }
        }
    }
}
