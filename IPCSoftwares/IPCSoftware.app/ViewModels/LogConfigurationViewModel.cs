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
using System;
using Microsoft.WindowsAPICodePack.Dialogs;
using IPCSoftware.Shared;
using IPCSoftware.Core.Interfaces;

namespace IPCSoftware.App.ViewModels
{
    public class LogConfigurationViewModel : BaseViewModel
    {
        private readonly ILogConfigurationService _logService;
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
        public string SelectedLogType
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

        private string _dataFolder;
        public string DataFolder
        {
            get => _dataFolder;
            set => SetProperty(ref _dataFolder, value);
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
            set => SetProperty(ref _selectedBackupSchedule, value);
        }

        private TimeSpan _backupTime;
        public TimeSpan BackupTime
        {
            get => _backupTime;
            set => SetProperty(ref _backupTime, value);
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

        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }
        public ICommand BrowseDataFolderCommand { get; }


        public event EventHandler SaveCompleted;
        public event EventHandler CancelRequested;

        public LogConfigurationViewModel(ILogConfigurationService logService)
        {
            _logService = logService;

            LogTypes = new ObservableCollection<string> { "Production", "Audit", "Error" };
            BackupSchedules = new ObservableCollection<string> { "Never", "Hourly", "Daily", "Weekly", "Monthly" };

            SaveCommand = new RelayCommand(async () => await OnSaveAsync(), () => CanSave());
            CancelCommand = new RelayCommand(() => OnCancel());
            BrowseDataFolderCommand = new RelayCommand(() => OnBrowseDataFolder());



            InitializeNewLog();
        }

        public void InitializeNewLog()
        {
            Title = "System Log Configuration - New";
            IsEditMode = false;
            _currentLog = new LogConfigurationModel();
            FileName = $"{SelectedLogType}_{DateTime.Now:yyyyMMdd}";
            LoadFromModel(_currentLog);
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
            LogName = log.LogName;
            SelectedLogType = log.LogType ?? "Production";
            DataFolder = log.DataFolder;
            FileName = log.FileName;
            LogRetentionDays = log.LogRetentionTime;
            FileSize = log.LogRetentionFileSize;
            AutoPurge = log.AutoPurge;
            SelectedBackupSchedule = log.BackupSchedule ?? "Never";
            BackupTime = log.BackupTime;
            Description = log.Description;
            Remark = log.Remark;
            Enabled = log.Enabled;
        }

        private void SaveToModel()
        {
            _currentLog.LogName = LogName;
            _currentLog.LogType = SelectedLogType;
            _currentLog.DataFolder = DataFolder;
            _currentLog.FileName = FileName;
            _currentLog.LogRetentionTime = LogRetentionDays;
            _currentLog.LogRetentionFileSize = FileSize;
            _currentLog.AutoPurge = AutoPurge;
            _currentLog.BackupSchedule = SelectedBackupSchedule;
            _currentLog.BackupTime = BackupTime;
            _currentLog.Description = Description;
            _currentLog.Remark = Remark;
            _currentLog.Enabled = Enabled;
        }

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
                DataFolder = dialog.FileName;
            }
        }


        private void OnCancel()
        {
            CancelRequested?.Invoke(this, EventArgs.Empty);
        }


        private void UpdateFileName()
        {
            if (!string.IsNullOrWhiteSpace(SelectedLogType))
            {
                FileName = $"{SelectedLogType}_{DateTime.Now:yyyyMMdd}";
            }
        }


    }

}
