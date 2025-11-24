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
    public class DeviceInterfaceConfigurationViewModel : BaseViewModel
    {
        private readonly IDeviceConfigurationService _deviceService;
        private DeviceInterfaceModel _currentInterface;
        private DeviceModel _parentDevice;
        private bool _isEditMode;
        private string _title;

        public string Title
        {
            get => _title;
            set => SetProperty(ref _title, value);
        }

        public bool IsEditMode
        {
            get => _isEditMode;
            set => SetProperty(ref _isEditMode, value);
        }

        // Properties
        private int _deviceNo;
        public int DeviceNo
        {
            get => _deviceNo;
            set => SetProperty(ref _deviceNo, value);
        }

        private string _deviceName;
        public string DeviceName
        {
            get => _deviceName;
            set => SetProperty(ref _deviceName, value);
        }

        private int _unitNo;
        public int UnitNo
        {
            get => _unitNo;
            set => SetProperty(ref _unitNo, value);
        }

        private string _name;
        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        private string _selectedComProtocol;
        public string SelectedComProtocol
        {
            get => _selectedComProtocol;
            set => SetProperty(ref _selectedComProtocol, value);
        }

        private string _ipAddress;
        public string IPAddress
        {
            get => _ipAddress;
            set => SetProperty(ref _ipAddress, value);
        }

        private int _portNo;
        public int PortNo
        {
            get => _portNo;
            set => SetProperty(ref _portNo, value);
        }

        private string _gateway;
        public string Gateway
        {
            get => _gateway;
            set => SetProperty(ref _gateway, value);
        }

        private string _description;
        public string Description
        {
            get => _description;
            set => SetProperty(ref _description, value);
        }

        private string _remark;
        public string Remark
        {
            get => _remark;
            set => SetProperty(ref _remark, value);
        }

        private bool _enabled;
        public bool Enabled
        {
            get => _enabled;
            set => SetProperty(ref _enabled, value);
        }

        public ObservableCollection<string> ComProtocols { get; }

        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }

        public event EventHandler SaveCompleted;
        public event EventHandler CancelRequested;

        public DeviceInterfaceConfigurationViewModel(IDeviceConfigurationService deviceService)
        {
            _deviceService = deviceService;

            ComProtocols = new ObservableCollection<string>
            {
                "Modbus Ethernet",
                "EthernetIP",
                "EtherCat",
                "RTU"
            };

            SaveCommand = new RelayCommand(async () => await OnSaveAsync(), CanSave);
            CancelCommand = new RelayCommand(OnCancel);
        }

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

        private bool CanSave()
        {
            return !string.IsNullOrWhiteSpace(Name) &&
                   !string.IsNullOrWhiteSpace(SelectedComProtocol);
        }

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

        private void OnCancel()
        {
            CancelRequested?.Invoke(this, EventArgs.Empty);
        }
    }
}
