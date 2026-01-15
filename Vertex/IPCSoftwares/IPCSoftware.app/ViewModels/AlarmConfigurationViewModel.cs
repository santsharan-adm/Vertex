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
    /// ViewModel responsible for managing alarm configuration logic.
    /// Handles creation, editing, and saving of alarm details between 
    /// the View and the IAlarmConfigurationService.
    public class AlarmConfigurationViewModel : BaseViewModel
    {
        private readonly IAlarmConfigurationService _alarmService;          // Service for CRUD operations
        private AlarmConfigurationModel _currentAlarm;                      // The active alarm being edited or created
        private bool _isEditMode;                                           // Flag: indicates Edit mode vs. New mode
        private string _title;                                              // Window or page title


        // UI Binding Properties
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

        // Alarm fields bound to input controls in the UI
        private int _alarmNo;
        public int AlarmNo
        {
            get => _alarmNo;
            set => SetProperty(ref _alarmNo, value);
        }

        private string _alarmName;
        public string AlarmName
        {
            get => _alarmName;
            set => SetProperty(ref _alarmName, value);
        }

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

        private string _alarmBit;
        public string AlarmBit
        {
            get => _alarmBit;
            set => SetProperty(ref _alarmBit, value);
        }

        private string _alarmText;
        public string AlarmText
        {
            get => _alarmText;
            set => SetProperty(ref _alarmText, value);
        }

        private string _severity;
        public string Severity
        {
            get => _severity;
            set => SetProperty(ref _severity, value);
        }

        private string _alarmTime;
        public string AlarmTime
        {
            get => _alarmTime;
            set => SetProperty(ref _alarmTime, value);
        }

        private string _alarmResetTime;
        public string AlarmResetTime
        {
            get => _alarmResetTime;
            set => SetProperty(ref _alarmResetTime, value);
        }

        private string _alarmAckTime;
        public string AlarmAckTime
        {
            get => _alarmAckTime;
            set => SetProperty(ref _alarmAckTime, value);
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

        // List of selectable alarm bits for combo/dropdown UI
        public ObservableCollection<string> AlarmBits { get; }


        // Commands for Save and Cancel button actions
        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }

        // Events raised after save or cancel operations (used for navigation)
        public event EventHandler SaveCompleted;
        public event EventHandler CancelRequested;

        /// Constructor — initializes collections, commands, and sets up a new alarm by default.
        public AlarmConfigurationViewModel(IAlarmConfigurationService alarmService)
        {
            _alarmService = alarmService;

            // Initialize list of alarm bits (Bit 0 to Bit 15)
            AlarmBits = new ObservableCollection<string>();
            for (int i = 0; i < 16; i++)
            {
                AlarmBits.Add($"Bit {i}");
            }

            // Initialize commands
            SaveCommand = new RelayCommand(async () => await OnSaveAsync(), CanSave);
            CancelCommand = new RelayCommand(OnCancel);

            // Load blank alarm template
            InitializeNewAlarm();
        }


        // ------------------------------- Initialization Methods  -------------------------------//

        /// Prepares the ViewModel for a new alarm entry.
        /// Sets defaults and updates title to "New".
        public void InitializeNewAlarm()
        {
            Title = "Alarm Configuration - New";
            IsEditMode = false;
            _currentAlarm = new AlarmConfigurationModel();
            LoadFromModel(_currentAlarm);
        }

        /// Loads an existing alarm model for editing.
        /// Clones the model to avoid modifying the original until saved.
        public void LoadForEdit(AlarmConfigurationModel alarm)
        {
            Title = "Alarm Configuration - Edit";
            IsEditMode = true;
            _currentAlarm = alarm.Clone();
            LoadFromModel(_currentAlarm);
        }


        // --------------------Model Synchronization --------//


        /// Copies model values into ViewModel properties for UI binding.
        private void LoadFromModel(AlarmConfigurationModel alarm)
        {
            AlarmNo = alarm.AlarmNo;
            AlarmName = alarm.AlarmName;
            TagNo = alarm.TagNo;
            Name = alarm.Name;
            AlarmBit = alarm.AlarmBit;
            AlarmText = alarm.AlarmText;
            Severity = alarm.Severity ?? "Error";
            AlarmTime = alarm.AlarmTime?.ToString("dd-MMM-yyyy HH:mm:ss") ?? string.Empty;
            AlarmResetTime = alarm.AlarmResetTime?.ToString("dd-MMM-yyyy HH:mm:ss") ?? string.Empty;
            AlarmAckTime = alarm.AlarmAckTime?.ToString("dd-MMM-yyyy HH:mm:ss") ?? string.Empty;
            Description = alarm.Description;
            Remark = alarm.Remark;
        }


        /// Updates the Alarm model with data from the ViewModel fields.
        /// Converts string timestamps back into DateTime if valid.
        private void SaveToModel()
        {
            _currentAlarm.AlarmNo = AlarmNo;
            _currentAlarm.AlarmName = AlarmName;
            _currentAlarm.TagNo = TagNo;
            _currentAlarm.Name = Name;
            _currentAlarm.AlarmBit = AlarmBit;
            _currentAlarm.AlarmText = AlarmText;
            _currentAlarm.Severity = Severity;
            _currentAlarm.Description = Description;
            _currentAlarm.Remark = Remark;

            // Parse time fields if needed (or keep as entered)
            if (!string.IsNullOrEmpty(AlarmTime) && DateTime.TryParse(AlarmTime, out var alarmTime))
                _currentAlarm.AlarmTime = alarmTime;

            if (!string.IsNullOrEmpty(AlarmResetTime) && DateTime.TryParse(AlarmResetTime, out var resetTime))
                _currentAlarm.AlarmResetTime = resetTime;

            if (!string.IsNullOrEmpty(AlarmAckTime) && DateTime.TryParse(AlarmAckTime, out var ackTime))
                _currentAlarm.AlarmAckTime = ackTime;
        }

        // --------------------Validation & Commands--------//

        /// Determines if the Save button should be enabled.
        private bool CanSave()
        {
            return AlarmNo > 0 &&
                   !string.IsNullOrWhiteSpace(AlarmName);
        }

        /// Saves the alarm — either adds a new entry or updates an existing one.
        /// Invokes the SaveCompleted event after success.
        private async Task OnSaveAsync()
        {
            SaveToModel();

            if (IsEditMode)
            {
                await _alarmService.UpdateAlarmAsync(_currentAlarm);
            }
            else
            {
                await _alarmService.AddAlarmAsync(_currentAlarm);
            }

            SaveCompleted?.Invoke(this, EventArgs.Empty);
        }

        /// Cancels the edit and notifies the navigation service.
        private void OnCancel()
        {
            CancelRequested?.Invoke(this, EventArgs.Empty);
        }
    }
}
