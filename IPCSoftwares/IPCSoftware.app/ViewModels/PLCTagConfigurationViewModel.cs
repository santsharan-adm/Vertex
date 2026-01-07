using IPCSoftware.Core.Interfaces;
using IPCSoftware.Core.Interfaces.AppLoggerInterface;
using IPCSoftware.Shared;
using IPCSoftware.Shared.Models;
using IPCSoftware.Shared.Models.ConfigModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Window;

namespace IPCSoftware.App.ViewModels
{
    public class PLCTagConfigurationViewModel : BaseViewModel
    {
        private readonly IPLCTagConfigurationService _tagService;
        private readonly IDeviceConfigurationService _deviceConfigService;
        private PLCTagConfigurationModel _currentTag;
        private bool _isEditMode;
        private string _title;
        private readonly IDialogService _dialog;

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
        private int _tagNo;
        public int TagNo
        {
            get => _tagNo;
            set => SetProperty(ref _tagNo, value);
        }

        private string _name;
        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        private int _plcNo;


        public int PLCNo
        {
            get => _plcNo;
            set => SetProperty(ref _plcNo, value);
        }

        private int _modbusAddress;
        public int ModbusAddress
        {
            get => _modbusAddress;
            set => SetProperty(ref _modbusAddress, value);
        }

        private string _dmAddress;
        public string DMAddress
        {
            get => _dmAddress;
            set => SetProperty(ref _dmAddress, value);
        }


        private int _length;
        public int Length
        {
            get => _length;
            set => SetProperty(ref _length, value);
        }

        // UPDATED: Use AlgorithmType object
        private AlgorithmType _selectedAlgorithm;
        public AlgorithmType SelectedAlgorithm
        {
            get => _selectedAlgorithm;
            set
            {
                if (SetProperty(ref _selectedAlgorithm, value))
                {
                    // Trigger logic whenever selection changes
                    UpdateAlgorithmState();
                }
            }

            //set => SetProperty(ref _selectedAlgorithm, value);
        }


        private string _selectedIOType;
        public string SelectedIOType
        {
            get => _selectedIOType;
            set => SetProperty(ref _selectedIOType, value);
            
        }

        public ObservableCollection<string> IOTypes { get; }
        private int _dataType;
        public int DataType
        {
            get => _dataType;
            set => SetProperty(ref _dataType, value);
        }

        private int _bitNo;
        public int BitNo
        {
            get => _bitNo;
            set => SetProperty(ref _bitNo, value);
        }

        private double _offset;
        public double Offset
        {
            get => _offset;
            set => SetProperty(ref _offset, value);
        }

        private double _span=0.0f;
        public double Span
        {
            get => _span;
            set => SetProperty(ref _span, value);
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

        private bool _isOffsetSpanVisible;
        public bool IsOffsetSpanVisible
        {
            get => _isOffsetSpanVisible;
            set => SetProperty(ref _isOffsetSpanVisible, value);
        }

        private bool _isLengthEnabled;
        public bool IsLengthEnabled
        {
            get => _isLengthEnabled;
            set => SetProperty(ref _isLengthEnabled, value);
        }

        private bool _canWrite;
        public bool CanWrite
        {
            get => _canWrite;
            set => SetProperty(ref _canWrite, value);
        }




        // UPDATED: Collection of AlgorithmType objects
        public ObservableCollection<AlgorithmType> AlgorithmTypes { get; }
        public ObservableCollection<DataTypeOption> DataTypes { get; }
        public ObservableCollection<DeviceInterfaceModel> AvailablePLCs { get; }

        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }

        public event EventHandler SaveCompleted;
        public event EventHandler CancelRequested;

        public PLCTagConfigurationViewModel(
            IPLCTagConfigurationService tagService,
            IDeviceConfigurationService deviceConfigService,
            IDialogService dialog,
            IAppLogger logger) : base(logger)
        {
            _tagService = tagService;
            _deviceConfigService = deviceConfigService;
            _dialog = dialog;


            IOTypes = new ObservableCollection<string>
            {
                "Input",
                "Output",
                "None"
            };

            DataTypes = new ObservableCollection<DataTypeOption>
            {
                new DataTypeOption { Id = 1, Name = "Int16" },
                new DataTypeOption { Id = 2, Name = "Word" },
                new DataTypeOption { Id = 3, Name = "Bit" },
                new DataTypeOption { Id = 4, Name = "Float" },
                new DataTypeOption { Id = 5, Name = "String" },
                new DataTypeOption { Id = 6, Name = "UInt16" },
                new DataTypeOption { Id = 7, Name = "UInt32" }
            };

            // Initialize algorithm types with Value and DisplayName
            AlgorithmTypes = new ObservableCollection<AlgorithmType>
            {
                new AlgorithmType(0, "None"),
                new AlgorithmType(1, "Linear scale"),
                new AlgorithmType(2, "FP"),
                new AlgorithmType(3, "String")
            };
            AvailablePLCs = new ObservableCollection<DeviceInterfaceModel>();

            // Fire and forget load (or call from a parent OnNavigatedTo)
            Task.Run(LoadPlcDevicesAsync);

            SaveCommand = new RelayCommand(async () => await OnSaveAsync(), CanSave);
            CancelCommand = new RelayCommand(OnCancel);

            InitializeNewTag();
        }

        public void InitializeNewTag()
        {
            Title = "PLC Tag Configuration - New";
            IsEditMode = false;
            _currentTag = new PLCTagConfigurationModel();
            LoadFromModel(_currentTag);
        }

        private async Task LoadPlcDevicesAsync()
        {
            try
            {
                var plcs = await _deviceConfigService.GetPlcDevicesAsync();

                // Marshal to UI Thread to update Collection
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    AvailablePLCs.Clear();
                    foreach (var plc in plcs)
                    {
                        AvailablePLCs.Add(plc);
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to load PLCs: {ex.Message}", LogType.Diagnostics);
            }
        }

        public void LoadForEdit(PLCTagConfigurationModel tag)
        {
            Title = "PLC Tag Configuration - Edit";
            IsEditMode = true;
            _currentTag = tag.Clone();
            LoadFromModel(_currentTag);
        }

        private void LoadFromModel(PLCTagConfigurationModel tag)
        {
            try
            {
                TagNo = tag.TagNo;
                Name = tag.Name;
                PLCNo = tag.PLCNo;
                ModbusAddress = tag.ModbusAddress;
              //  DMAddress = tag.DMAddress; // Bind DMAddress
                Length = tag.Length;

                // Map int AlgNo to AlgorithmType object
                SelectedAlgorithm = AlgorithmTypes.FirstOrDefault(a => a.Value == tag.AlgNo)
                                    ?? AlgorithmTypes[0]; // Default to first (Linear scale)

                DataType = tag.DataType;
                BitNo = tag.BitNo;

                Offset = tag.Offset;
                Span = tag.Span;
                Description = tag.Description;
                Remark = tag.Remark;
                CanWrite = tag.CanWrite;
                // Bind IOType
                SelectedIOType = tag.IOType;
                UpdateAlgorithmState();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message, LogType.Diagnostics);
            }
        }

        private void SaveToModel()
        {
            try
            {
                _currentTag.TagNo = TagNo;
                _currentTag.Name = Name;
                _currentTag.PLCNo = PLCNo;
                _currentTag.ModbusAddress = ModbusAddress;
               // _currentTag.DMAddress = DMAddress;
                _currentTag.Length = Length;

                // Save the numeric value (1, 2, or 3)
                _currentTag.AlgNo = SelectedAlgorithm?.Value ?? 1;
                _currentTag.DataType = DataType;
                _currentTag.BitNo = BitNo;

                _currentTag.Offset = Offset;
                _currentTag.Span = Span;
                _currentTag.Description = Description;
                _currentTag.Remark = Remark;
                _currentTag.CanWrite = CanWrite;

                _currentTag.IOType = SelectedIOType;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message, LogType.Diagnostics);
            }
        }

        private bool CanSave()
        {
            return TagNo > 0 &&
                   !string.IsNullOrWhiteSpace(Name) &&
                   PLCNo > 0;
        }

        private async Task OnSaveAsync()
        {
            SaveToModel();
            try
            {

                if (IsEditMode)
                {
                    await _tagService.UpdateTagAsync(_currentTag);
                }
                else
                {
                    await _tagService.AddTagAsync(_currentTag);
                }

                SaveCompleted?.Invoke(this, EventArgs.Empty);
            }
            catch (InvalidOperationException ex)
            {
                // Capture the "Username taken" message and show it in the UI

                _dialog.ShowWarning(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message, LogType.Diagnostics);
            }
        }

        private void OnCancel()
        {
            CancelRequested?.Invoke(this, EventArgs.Empty);
        }

        private void UpdateAlgorithmState()
        {
            if (_selectedAlgorithm == null) return;

            // Logic: 1 = Linear, 2 = FP, 3 = String

            // Requirement 1: Hide Offset/Span for FP (2) and String (3)
            // They are only visible for Linear (1)
            IsOffsetSpanVisible = _selectedAlgorithm.Value == 1;

            // Requirement 2: For FP (2), disable Length and fix to 4
            if (_selectedAlgorithm.Value == 2)
            {
                Length = 4;             // Force value to 4
                IsLengthEnabled = false; // Disable TextBox
            }
            else
            {
                IsLengthEnabled = true;  // Enable for Linear and String
            }
        }

   


    }
    public class DataTypeOption
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }
    //
}
