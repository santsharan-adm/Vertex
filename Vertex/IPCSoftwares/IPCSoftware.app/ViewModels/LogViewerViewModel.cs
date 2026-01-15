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
    /// ViewModel responsible for displaying and managing application log files.
    /// 
    /// Features:
    /// - Lists available log files for a selected log category (Audit, Error, etc.)
    /// - Displays log entries from the selected file
    /// - Supports refreshing log file data
    public class LogViewerViewModel : BaseViewModel
    {
        
        // --------------Dependencies-----------------//


        private readonly ILogService _logService;       // Provides access to log files and entries from the file system


        // ----------------Observable Collections (Bound to UI)-----------------//

        /// Collection of available log files for the selected category.
        /// Bound to a ListBox or ComboBox in the UI.
        
        public ObservableCollection<LogFileInfo> LogFiles { get; } = new();

        /// Collection of log entries read from the currently selected log file.
        /// Bound to a DataGrid or ListView in the UI.
        public ObservableCollection<LogEntry> LogEntries { get; } = new();


        // -------------------Private Fields-----------------//
        
        private LogFileInfo _selectedFile;                  // Currently selected log file from LogFiles


        // ---------Public Properties (Bindable)---------------//

        /// The selected log file from the LogFiles list.
        /// When changed, automatically loads the corresponding log entries.
        public LogFileInfo SelectedFile                    
        {
            get => _selectedFile;                          
            set
            {
                // When a new file is selected, trigger log loading

                if (SetProperty(ref _selectedFile, value) && value != null)
                {
                    _ = LoadLogsAsync(value.FullPath);
                }
            }
        }

        private string _currentCategoryName;                   // Display name for the selected category

        /// The display name for the currently selected log category.
        /// Shown in the header or title bar (e.g., "Audit Logs").
        public string CurrentCategoryName
        {
            get => _currentCategoryName;
            set => SetProperty(ref _currentCategoryName, value);
        }


        //---------- Commands----------//

        /// Command used to refresh the list of log files and entries.
        public ICommand RefreshCommand { get; }


        // ----------------Constructor----------------//

        /// Initializes the Log Viewer ViewModel and prepares command bindings.

        public LogViewerViewModel(ILogService logService)
        {
            _logService = logService;

            // Example (could be uncommented and extended later)
            // RefreshCommand = new RelayCommand(async () => await LoadFilesAsync(_currentCategory));
        }

        private LogType _currentCategory;                              // Current log category (Production, Audit, Error)

        // Call this method when navigating from the Main Window Sidebar


        //-----------------Core Methods-----------------//

        /// Loads all available log files for the specified log category.
        /// 
        /// This method should be called when navigating to the log viewer
        /// from the sidebar (e.g., selecting "Audit Logs" or "Error Logs").
        public async Task LoadCategoryAsync(LogType category)
        {
            _currentCategory = category;
            CurrentCategoryName = $"{category} Logs";              // Example: “Error Logs”, “Audit Logs”
            await LoadFilesAsync(category);
        }

        /// Loads all log files for the selected category and populates the LogFiles collection.
        /// Clears any existing entries before loading new ones.
        
        private async Task LoadFilesAsync(LogType category)
        {
            LogFiles.Clear();
            LogEntries.Clear();                           // Reset log entries when category changes


            var files = await _logService.GetLogFilesAsync(category);
            foreach (var file in files)
            {
                LogFiles.Add(file);
            }
        }

        /// Loads all log entries from the selected log file and displays them in the LogEntries list.
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