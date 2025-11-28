using IPCSoftware.Core.Interfaces;
using IPCSoftware.Shared;
using IPCSoftware.Shared.Models;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using IPCSoftware.App.Helpers;
using static IPCSoftware.AppLogger.Services.LogManager;
using IPCSoftware.AppLogger.Models;
using LogEntry = IPCSoftware.Core.Interfaces.LogEntry; // For RelayCommand

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

        // Constructor
        public LogViewerViewModel(ILogService logService)
        {
            _logService = logService;
            RefreshCommand = new RelayCommand(async () => await LoadFilesAsync(_currentCategory));
        }

        private LogType _currentCategory;

        // Call this method when navigating from the Main Window Sidebar
        public async Task LoadCategoryAsync(LogType category)
        {
            _currentCategory = category;
            CurrentCategoryName = $"{category} Logs";
            await LoadFilesAsync(category);
        }

        private async Task LoadFilesAsync(LogType category)
        {
            LogFiles.Clear();
            LogEntries.Clear(); // Clear old logs when switching category

            var files = await _logService.GetLogFilesAsync(category);
            foreach (var file in files)
            {
                LogFiles.Add(file);
            }
        }

        private async Task LoadLogsAsync(string fullPath)
        {
            LogEntries.Clear();
            var logs = await _logService.ReadLogFileAsync(fullPath);
            foreach (var log in logs)
            {
                LogEntries.Add(log);
            }
        }
    }
}