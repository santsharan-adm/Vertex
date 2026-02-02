using IPCSoftware.Core.Interfaces;
using IPCSoftware.Core.Interfaces.AppLoggerInterface;
using IPCSoftware.Shared;
using IPCSoftware.Shared.Models;
using IPCSoftware.Shared.Models.ConfigModels;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;


namespace IPCSoftware.App.ViewModels
{
    public class LogViewerViewModel : BaseViewModel
    {
        private readonly ILogService _logService;
        
        // Data Collections
        public ObservableCollection<LogFileInfo> LogFiles { get; } = new();
        public ObservableCollection<LogEntry> LogEntries { get; } = new();

        // State
        private LogFileInfo _selectedFile;
        public LogFileInfo SelectedFile
        {
            get => _selectedFile;
            set
            {
                if (SetProperty(ref _selectedFile, value) && value != null)
                {
                    _ = LoadLogsAsync(value.FullPath);
                }
            }
        }

        private string _currentCategoryName;
        public string CurrentCategoryName
        {
            get => _currentCategoryName;
            set => SetProperty(ref _currentCategoryName, value);
        }

        public ICommand RefreshCommand { get; }
        public ICommand RefreshLogsCommand { get; }

        // Constructor
        public LogViewerViewModel(
            ILogService logService,
            IAppLogger logger) : base(logger)
        {
            _logService = logService;
         RefreshCommand = new RelayCommand(async () => await LoadFilesAsync(_currentCategory));
            RefreshLogsCommand = new RelayCommand(async () =>
            {
                // 1. Guard clause: Check if null before acting
                if (SelectedFile == null) return;

                // 2. Safe to access FullPath now
                await LoadLogsAsync(SelectedFile.FullPath);
            });
        }

        private LogType _currentCategory;

        // Call this method when navigating from the Main Window Sidebar
        public async Task LoadCategoryAsync(LogType category)
        {
            try
            {
                _currentCategory = category;
                CurrentCategoryName = $"{category} Logs";
                await LoadFilesAsync(category);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message, LogType.Diagnostics);
            }
        }

        private async Task LoadFilesAsync(LogType category)
        {
            try
            {
                LogFiles.Clear();
                LogEntries.Clear(); // Clear old logs when switching category

                var files = await _logService.GetLogFilesAsync(category);
                foreach (var file in files)
                {
                    LogFiles.Add(file);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message, LogType.Diagnostics);
            }
        }

        private async Task LoadLogsAsync(string fullPath)
        {
            if (fullPath == null) return;
            try
            {
                LogEntries.Clear();
                var logs = await _logService.ReadLogFileAsync(fullPath);
                foreach (var log in logs)
                {
                    LogEntries.Add(log);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message, LogType.Diagnostics);
            }
        }
    }
}