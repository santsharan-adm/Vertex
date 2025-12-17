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
    public class DeviceConfigurationViewModel : BaseViewModel
    {
        private readonly IDeviceConfigurationService _deviceService;
        private DeviceModel _currentDevice;
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

        private string _selectedDeviceType;
        public string SelectedDeviceType
        {
            get => _selectedDeviceType;
            set => SetProperty(ref _selectedDeviceType, value);
        }

        private string _make;
        public string Make
        {
            get => _make;
            set => SetProperty(ref _make, value);
        }

        private string _model;
        public string Model
        {
            get => _model;
            set => SetProperty(ref _model, value);
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

        public ObservableCollection<string> DeviceTypes { get; }

        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }

        public event EventHandler SaveCompleted;
        public event EventHandler CancelRequested;

        public DeviceConfigurationViewModel(
            IDeviceConfigurationService deviceService,
            IAppLogger logger) : base(logger)
        {
            _deviceService = deviceService;

            DeviceTypes = new ObservableCollection<string> { "PLC", "Robo", "CCD" };

            SaveCommand = new RelayCommand(async () => await OnSaveAsync(), CanSave);
            CancelCommand = new RelayCommand(OnCancel);

            InitializeNewDevice();
        }

        public void InitializeNewDevice()
        {
            Title = "Add Device";
            IsEditMode = false;
            _currentDevice = new DeviceModel();
            LoadFromModel(_currentDevice);
        }

        public void LoadForEdit(DeviceModel device)
        {
            Title = "Edit Device";
            IsEditMode = true;
            _currentDevice = device.Clone();
            LoadFromModel(_currentDevice);
        }

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

        private bool CanSave()
        {
            return DeviceNo > 0 &&
                   !string.IsNullOrWhiteSpace(DeviceName) &&
                   !string.IsNullOrWhiteSpace(SelectedDeviceType);
        }

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

        private void OnCancel()
        {
            CancelRequested?.Invoke(this, EventArgs.Empty);
        }
    }
}
