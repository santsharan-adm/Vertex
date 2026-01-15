using IPCSoftware.Services.ConfigServices;
using IPCSoftware.Shared;
using IPCSoftware.Shared.Models.ConfigModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.WindowsAPICodePack.Dialogs;
using IPCSoftware.Core.Interfaces;
using System.IO;

namespace IPCSoftware.App.ViewModels
{
    /// ViewModel responsible for configuring system log settings.
    /// This includes:
    /// - Selecting log type (Production, Audit, Error)
    /// - Setting data and backup folders
    /// - Defining file naming, retention, and backup schedule
    /// - Handling save, cancel, and folder browsing operations
    

    public class LogConfigurationViewModel : BaseViewModel
    {
        private readonly ILogConfigurationService _logService;                 // Service for CRUD operations on log configurations
        private LogConfigurationModel _currentLog;                              // Current log configuration being edited/created
        private bool _isEditMode;                                                 // True if editing existing configuration
        private string _title;                                                  // View title (New/Edit)


        // ----------------General Properties------------------------//

        /// Title displayed in the configuration window header.
        public string Title
        {
            get => _title;
            set => SetProperty(ref _title, value);
        }

        /// Indicates whether the form is in Edit mode or New mode.
        public bool IsEditMode
        {
            get => _isEditMode;
            set => SetProperty(ref _isEditMode, value);
        }

        // Log Configuration Properties (Bound to UI)

        private string _logName;

        /// Name assigned to the log configuration (e.g., “ProductionLog”).
        public string LogName
        {
            get => _logName;
            set => SetProperty(ref _logName, value);
        }

        private string _selectedLogType;

        /// Selected log type — Production, Audit, or Error.
        public string SelectedLogType
        {
            get => _selectedLogType;
            set
            {
                if (SetProperty(ref _selectedLogType, value))
                {
                    UpdateFileName();              // Auto-update file name when log type changes
                }
            }
        }

        private string _dataFolder;

        /// Folder path where log files will be stored.
        public string DataFolder
        {
            get => _dataFolder;
            set => SetProperty(ref _dataFolder, value);
        }

        private string _backupFolder;

        /// Folder path where log backups will be saved.
        public string BackupFolder
        {
            get => _backupFolder;
            set => SetProperty(ref _backupFolder, value);
        }

        private string _fileName;

        /// Log file name pattern (e.g., “Production_yyyyMMdd”).
        public string FileName
        {
            get => _fileName;
            set => SetProperty(ref _fileName, value);
        }

        private int _logRetentionDays;

        /// Number of days to keep logs before purging.
        public int LogRetentionDays
        {
            get => _logRetentionDays;
            set => SetProperty(ref _logRetentionDays, value);
        }

        private int _fileSize;

        /// Maximum size of a single log file before rotation (in MB).
        public int FileSize
        {
            get => _fileSize;
            set => SetProperty(ref _fileSize, value);
        }

        private bool _autoPurge;

        /// If true, old log files are automatically deleted after retention period.
        public bool AutoPurge
        {
            get => _autoPurge;
            set => SetProperty(ref _autoPurge, value);
        }

        private string _selectedBackupSchedule;

        /// Selected backup schedule type (Manual, Daily, Weekly, Monthly).
        public string SelectedBackupSchedule
        {
            get => _selectedBackupSchedule;
            set
            {
                if (SetProperty(ref _selectedBackupSchedule, value))
                {
                    OnBackupScheduleChanged();
                }
            }
        }

        private TimeSpan _backupTime;

        /// Time of day when backups occur (for Daily, Weekly, Monthly).
        public TimeSpan BackupTime
        {
            get => _backupTime;
            set => SetProperty(ref _backupTime, value);
        }

        // NEW: Day of month for monthly backup (1-28)
        private int _selectedBackupDay;

        /// Day of month for monthly backups (1–28).
        public int SelectedBackupDay
        {
            get => _selectedBackupDay;
            set => SetProperty(ref _selectedBackupDay, value);
        }

        // NEW: Day of week for weekly backup
        private string _selectedBackupDayOfWeek;

        /// Day of the week for weekly backups.
        public string SelectedBackupDayOfWeek
        {
            get => _selectedBackupDayOfWeek;
            set => SetProperty(ref _selectedBackupDayOfWeek, value);
        }

        private string _description;

        /// Description or purpose of this log configuration.
        public string Description
        {
            get => _description;
            set => SetProperty(ref _description, value);
        }

        private string _remark;

        /// Additional notes or comments.
        public string Remark
        {
            get => _remark;
            set => SetProperty(ref _remark, value);
        }

        private bool _enabled;

        /// Indicates if the log configuration is currently active.
        public bool Enabled
        {
            get => _enabled;
            set => SetProperty(ref _enabled, value);
        }

        // ---------------- Collections for ComboBoxes-----------------//

        public ObservableCollection<string> LogTypes { get; }                    // Available log types
        public ObservableCollection<string> BackupSchedules { get; }             // Available backup schedules
        public ObservableCollection<int> BackupDays { get; }                    // Days for monthly backup

        public ObservableCollection<string> DaysOfWeek { get; }                  // Weekdays for weekly backup


        // ----------------- Commands------------//
        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }
        public ICommand BackUpCommand { get; }
        public ICommand BrowseDataFolderCommand { get; }
        public ICommand BrowseBackupFolderCommand { get; }

        public event EventHandler SaveCompleted;
        public event EventHandler CancelRequested;


        // -------------- Constructor -------------------//

        /// Initializes the Log Configuration ViewModel with lists and commands.
        public LogConfigurationViewModel(ILogConfigurationService logService)
        {
            _logService = logService;

            LogTypes = new ObservableCollection<string> { "Production", "Audit", "Error" };
            BackupSchedules = new ObservableCollection<string> { "Manual", "Daily", "Weekly", "Monthly" };

            // Days 1-28 for monthly backup
            BackupDays = new ObservableCollection<int>(Enumerable.Range(1, 28));

            // Days of week
            DaysOfWeek = new ObservableCollection<string>
            {
                "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday", "Sunday"
            };

            SaveCommand = new RelayCommand(async () => await OnSaveAsync(), () => CanSave());
            CancelCommand = new RelayCommand(() => OnCancel());
            BrowseDataFolderCommand = new RelayCommand(() => OnBrowseDataFolder());
            BrowseBackupFolderCommand = new RelayCommand(() => OnBrowseBackupFolder());
            BackUpCommand = new RelayCommand(() => OnBackUp());

            InitializeNewLog();
        }


        // ------------------- Initialization Methods-------------------//

        /// Initializes a blank new log configuration.

        public void InitializeNewLog()
        {
            Title = "System Log Configuration - New";
            IsEditMode = false;
            _currentLog = new LogConfigurationModel();
            FileName = $"{SelectedLogType}_yyyyMMdd";
            LoadFromModel(_currentLog);
        }


        /// Loads an existing log configuration for editing.
        public void LoadForEdit(LogConfigurationModel log)
        {
            Title = "System Log Configuration - Edit";
            IsEditMode = true;
            _currentLog = log.Clone();
            LoadFromModel(_currentLog);
        }


        // ---------------- Model Mapping Methods -------------------//
        private void LoadFromModel(LogConfigurationModel log)
        {
            LogName = log.LogName;
            SelectedLogType = log.LogType.ToString() ?? "Production";
            DataFolder = log.DataFolder;
            BackupFolder = log.BackupFolder;
            FileName = log.FileName;
            LogRetentionDays = log.LogRetentionTime;
            FileSize = log.LogRetentionFileSize;
            AutoPurge = log.AutoPurge;
            SelectedBackupSchedule = log.BackupSchedule.ToString() ?? "Manual";
            BackupTime = log.BackupTime;
            SelectedBackupDay = log.BackupDay > 0 ? log.BackupDay : 1;
            SelectedBackupDayOfWeek = log.BackupDayOfWeek ?? "Monday";
            Description = log.Description;
            Remark = log.Remark;
            Enabled = log.Enabled;
        }

        private void SaveToModel()
        {
            _currentLog.LogName = LogName;
            _currentLog.LogType = Enum.Parse<LogType>(SelectedLogType);
            _currentLog.DataFolder = DataFolder;
            _currentLog.BackupFolder = BackupFolder;
            _currentLog.FileName = FileName;
            _currentLog.LogRetentionTime = LogRetentionDays;
            _currentLog.LogRetentionFileSize = FileSize;
            _currentLog.AutoPurge = AutoPurge;
            _currentLog.BackupSchedule = Enum.Parse<BackupScheduleType>( SelectedBackupSchedule);
            _currentLog.BackupTime = BackupTime;

            // Reset backup-related values based on schedule type
            switch (SelectedBackupSchedule)
            {
                case "Manual":
                    _currentLog.BackupDay = 0;
                    _currentLog.BackupDayOfWeek = null;
                    break;

                case "Daily":
                    _currentLog.BackupDay = 0;
                    _currentLog.BackupDayOfWeek = null;
                    break;

                case "Weekly":
                    _currentLog.BackupDay = 0;
                    _currentLog.BackupDayOfWeek = SelectedBackupDayOfWeek;
                    break;

                case "Monthly":
                    _currentLog.BackupDay = SelectedBackupDay;
                    _currentLog.BackupDayOfWeek = null;
                    break;
            }

            _currentLog.Description = Description;
            _currentLog.Remark = Remark;
            _currentLog.Enabled = Enabled;
        }


        // ------------------Command Logic ------------------//
        private bool CanSave()
        {
            return !string.IsNullOrWhiteSpace(LogName) &&
                   !string.IsNullOrWhiteSpace(SelectedLogType);
        }

        private async Task OnSaveAsync()
        {
            SaveToModel();

            if (IsEditMode)
            {
                await _logService.UpdateAsync(_currentLog);
            }
            else
            {
                await _logService.AddAsync(_currentLog);
            }

            SaveCompleted?.Invoke(this, EventArgs.Empty);
        }

        private void OnBrowseDataFolder()
        {
            var dialog = new CommonOpenFileDialog
            {
                IsFolderPicker = true,
                Title = "Select Data Folder",
                AllowNonFileSystemItems = false,
                Multiselect = false
            };

            if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                DataFolder = Path.Combine(dialog.FileName, "Logs", SelectedLogType);

            }
        }


        private void OnBrowseBackupFolder()
        {
            var dialog = new CommonOpenFileDialog
            {
                IsFolderPicker = true,
                Title = "Select Backup Folder",
                AllowNonFileSystemItems = false,
                Multiselect = false
            };

            if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                BackupFolder = Path.Combine(dialog.FileName, "LogsBackup", SelectedLogType);
            }
        }




        private void OnCancel()
        {
            CancelRequested?.Invoke(this, EventArgs.Empty);
        }

        private void OnBackUp()
        {
            // Manual backup logic to be implemented later
        }

        private void OnBackupScheduleChanged()
        {
            // Adjust default values when backup schedule type changes

            if (SelectedBackupSchedule == "Manual")
            {
                SelectedBackupDay = 1;
                SelectedBackupDayOfWeek = "Monday";
            }
            else if (SelectedBackupSchedule == "Daily")
            {
                SelectedBackupDay = 1;
                SelectedBackupDayOfWeek = "Monday";
            }
            else if (SelectedBackupSchedule == "Weekly")
            {
                SelectedBackupDay = 1;
                // Keep SelectedBackupDayOfWeek
            }
            else if (SelectedBackupSchedule == "Monthly")
            {
                // Keep SelectedBackupDay
                SelectedBackupDayOfWeek = "Monday";
            }
        }

        private void UpdateFileName()
        {
            if (!string.IsNullOrWhiteSpace(SelectedLogType))
            {
                FileName = $"{SelectedLogType}_yyyyMMdd";
            }
        }
    }
}
    