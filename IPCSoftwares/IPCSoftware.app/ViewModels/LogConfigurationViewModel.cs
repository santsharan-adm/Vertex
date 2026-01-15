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
using IPCSoftware.Core.Interfaces.AppLoggerInterface;

namespace IPCSoftware.App.ViewModels
{
    public class LogConfigurationViewModel : BaseViewModel
    {
        private readonly ILogConfigurationService _logService;
        private readonly ILogManagerService _logManager;
        private readonly ICcdConfigService _ccdService;
        private readonly IDialogService _dialog;
        private LogConfigurationModel _currentLog;
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

        // Properties bound to UI
        private string _logName;
        public string LogName
        {
            get => _logName;
            set => SetProperty(ref _logName, value);
        }


        private string _selectedLogType;
    /*    public string SelectedLogType
        {
            get => _selectedLogType;
            set
            {
                if (SetProperty(ref _selectedLogType, value))
                {
                    UpdateFileName();
                }
            }
        }
*/
        public string SelectedLogType
        {
            get => _selectedLogType;
            set
            {
                if (SetProperty(ref _selectedLogType, value))
                {
                    UpdateFileName();

                    // 2. Determine if we are in Production Mode
                    IsProductionLog = (value == "Production");

                    // 3. If Production, Load from JSON
                    if (IsProductionLog)
                    {
                        LoadJsonPaths();
                    }
                }
            }
        }

        private string _dataFolder;
        public string DataFolder
        {
            get => _dataFolder;
            set => SetProperty(ref _dataFolder, value);
        }

        private string _backupFolder;
        public string BackupFolder
        {
            get => _backupFolder;
            set => SetProperty(ref _backupFolder, value);
        }

        private string _productionImagePath;
        public string ProductionImagePath
        {
            get => _productionImagePath;
            set => SetProperty(ref _productionImagePath, value);
        }

        private string _productionImageBackupPath;
        public string ProductionImageBackupPath
        {
            get => _productionImageBackupPath;
            set => SetProperty(ref _productionImageBackupPath, value);
        }

        private bool _isProductionLog;
        public bool IsProductionLog
        {
            get => _isProductionLog;
            set => SetProperty(ref _isProductionLog, value);
        }

        private string _fileName;
        public string FileName
        {
            get => _fileName;
            set => SetProperty(ref _fileName, value);
        }

        private int _logRetentionDays;
        public int LogRetentionDays
        {
            get => _logRetentionDays;
            set => SetProperty(ref _logRetentionDays, value);
        }

        private int _fileSize;
        public int FileSize
        {
            get => _fileSize;
            set => SetProperty(ref _fileSize, value);
        }

        private bool _autoPurge;
        public bool AutoPurge
        {
            get => _autoPurge;
            set => SetProperty(ref _autoPurge, value);
        }

        private string _selectedBackupSchedule;
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
        public TimeSpan BackupTime
        {
            get => _backupTime;
            set => SetProperty(ref _backupTime, value);
        }

        // NEW: Day of month for monthly backup (1-28)
        private int _selectedBackupDay;
        public int SelectedBackupDay
        {
            get => _selectedBackupDay;
            set => SetProperty(ref _selectedBackupDay, value);
        }

        // NEW: Day of week for weekly backup
        private string _selectedBackupDayOfWeek;
        public string SelectedBackupDayOfWeek
        {
            get => _selectedBackupDayOfWeek;
            set => SetProperty(ref _selectedBackupDayOfWeek, value);
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

        public ObservableCollection<string> LogTypes { get; }
        public ObservableCollection<string> BackupSchedules { get; }
        public ObservableCollection<int> BackupDays { get; }
        public ObservableCollection<string> DaysOfWeek { get; }

        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }
        public ICommand BackUpCommand { get; }
        public ICommand RestoreCommand { get; }
        public ICommand BrowseDataFolderCommand { get; }
        public ICommand BrowseBackupFolderCommand { get; }

        public ICommand BrowseProdImageCommand { get; }
        public ICommand BrowseProdBackupCommand { get; }

        public event EventHandler SaveCompleted;
        public event EventHandler CancelRequested;

        public LogConfigurationViewModel(ILogConfigurationService logService,
            IDialogService dialog,
            ILogManagerService logManager,
            ICcdConfigService ccdService, IAppLogger logger) : base(logger)
        {
            _dialog = dialog;
            _logManager = logManager;
            _logService = logService;
            _ccdService = ccdService;

            LogTypes = new ObservableCollection<string> { "Production", "Audit", "Error", "Diagnostics" };
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
            RestoreCommand = new RelayCommand(() => OnRestore());
            BrowseProdImageCommand = new RelayCommand(() => ProductionImagePath = BrowseFolder("Select Production Image Folder"));
            BrowseProdBackupCommand = new RelayCommand(() => ProductionImageBackupPath = BrowseFolder("Select Production Backup Folder"));

            InitializeNewLog();
        }

        public void InitializeNewLog()
        {
            Title = "System Log Configuration - New";
            IsEditMode = false;
            _currentLog = new LogConfigurationModel();
            FileName = $"{SelectedLogType}_yyyyMMdd";
            LoadFromModel(_currentLog);
        }

        private void LoadJsonPaths()
        {
            try
            {
                // Use the service to get paths
                var paths = _ccdService.LoadCcdPaths();
                ProductionImagePath = paths.ImagePath;
                ProductionImageBackupPath = paths.BackupPath;
            }
            catch { }
        }

        public void LoadForEdit(LogConfigurationModel log)
        {
            Title = "System Log Configuration - Edit";
            IsEditMode = true;
            _currentLog = log.Clone();
            LoadFromModel(_currentLog);
        }

        private void LoadFromModel(LogConfigurationModel log)
        {
            try
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
            catch (Exception ex)
            {
                _logger.LogError(ex.Message, LogType.Diagnostics);
            }
        }

        private void SaveToModel()
        {
            try
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
            catch (Exception ex)
            {
                _logger.LogError(ex.Message, LogType.Diagnostics);
            }
        }

        private bool CanSave()
        {
            return !string.IsNullOrWhiteSpace(LogName) &&
                   !string.IsNullOrWhiteSpace(SelectedLogType);
        }

        private async Task OnSaveAsync()
        {
            try
            {
                SaveToModel();
                if (IsProductionLog)
                {
                    _ccdService.SaveCcdPaths(ProductionImagePath, ProductionImageBackupPath);
                }

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
            catch (Exception ex)
            {
                _logger.LogError(ex.Message, LogType.Diagnostics);
            }
        }

        private string BrowseFolder(string title)
        {
            try
            {
                var dialog = new CommonOpenFileDialog
                {
                    IsFolderPicker = true,
                    Title = title,
                    AllowNonFileSystemItems = false,
                    Multiselect = false
                };
                if (dialog.ShowDialog() == CommonFileDialogResult.Ok) return dialog.FileName;
            }
            catch { }
            return string.Empty;
        }

        private void OnBrowseDataFolder()
        {
            try
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
            catch (Exception ex)
            {
                _logger.LogError(ex.Message, LogType.Diagnostics);
            }
        }


        private void OnBrowseBackupFolder()
        {
            try
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
            catch (Exception ex)
            {
                _logger.LogError(ex.Message, LogType.Diagnostics);
            }
        }




        private void OnCancel()
        {
            CancelRequested?.Invoke(this, EventArgs.Empty);
        }

        private void OnBackUp()
        {
            _logManager.PerformManualBackup(_currentLog.Id);

            Task.Delay(1599);
            _dialog.ShowMessage("Backup completed sucessfully.");
            // Execute manual backup logic
            // TODO: Implement backup logic
        }

        private void OnRestore()
        {
            _logManager.PerformManualRestore(_currentLog.Id);

            Task.Delay(1599);
            _dialog.ShowMessage("Backup completed sucessfully.");
            // Execute manual backup logic
            // TODO: Implement backup logic
        }



        private void OnBackupScheduleChanged()
        {
            try
            {
                // Reset values when schedule changes
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
            catch (Exception ex)
            {
                _logger.LogError(ex.Message, LogType.Diagnostics);
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
    