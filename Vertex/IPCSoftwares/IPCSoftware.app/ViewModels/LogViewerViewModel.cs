using IPCSoftware.Core.Interfaces;
using IPCSoftware.Core.Interfaces.AppLoggerInterface;
using IPCSoftware.Shared;
using IPCSoftware.Shared.Models;
using IPCSoftware.Shared.Models.ConfigModels;
using System;
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
                try
                {
                    if (SetProperty(ref _selectedFile, value) && value != null)
                    {
                        _ = LoadLogsAsync(value.FullPath);
                    }
                }
                catch (Exception)
                {
                    // Exception swallowed to prevent application crash
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

        private LogType _currentCategory;

        // Constructor
        public LogViewerViewModel(ILogService logService)
        {
            try
            {
                _logService = logService;
                // Task.Run(async () => await LoadFilesAsync(_currentCategory));
            }
            catch (Exception)
            {
                // Exception swallowed to prevent application crash
            }
        }

        // Call this method when navigating from the Main Window Sidebar
        public async Task LoadCategoryAsync(LogType category)
        {
            try
            {
                _currentCategory = category;
                CurrentCategoryName = $"{category} Logs";
                await LoadFilesAsync(category);
            }
            catch (Exception)
            {
                // Exception swallowed to prevent application crash
            }
        }

        private async Task LoadFilesAsync(LogType category)
        {
            try
            {
                LogFiles.Clear();
                LogEntries.Clear();

                var files = await _logService.GetLogFilesAsync(category);
                foreach (var file in files)
                {
                    LogFiles.Add(file);
                }
            }
            catch (Exception)
            {
                // Exception swallowed to prevent application crash
            }
        }

        private async Task LoadLogsAsync(string fullPath)
        {
            try
            {
                LogEntries.Clear();
                var logs = await _logService.ReadLogFileAsync(fullPath);
                foreach (var log in logs)
                {
                    LogEntries.Add(log);
                }
            }
            catch (Exception)
            {
                // Exception swallowed to prevent application crash
            }
        }
    }
}
