using IPCSoftware.Core.Interfaces;
using IPCSoftware.Services.ConfigServices;
using IPCSoftware.Shared;
using IPCSoftware.Shared.Models.ConfigModels;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace IPCSoftware.App.ViewModels
{
    /// ViewModel responsible for managing and displaying the list of
    /// system log configurations (Production, Audit, Error, etc.).
    /// It supports operations like:
    /// - Viewing all log configurations
    /// - Adding new configurations
    /// - Editing existing configurations
    /// - Deleting configurations

    public class LogListViewModel : BaseViewModel
        {
        // ---------------------Dependencies----------------------//


        private readonly ILogConfigurationService _logService;                  // Service to manage log configuration data (CRUD)
        private readonly INavigationService _nav;                              // Handles navigation to configuration screens


        // ---------------------Private Fields------------------//

        private ObservableCollection<LogConfigurationModel> _logConfigurations;  // Collection of log configurations displayed in UI
        private LogConfigurationModel _selectedLog;                               // Currently selected log configuration


        // -------------------------Public Properties (Bindable to View)------------------//

        /// Collection of log configurations displayed in a list/grid.

        public ObservableCollection<LogConfigurationModel> LogConfigurations
            {
                get => _logConfigurations;
                set => SetProperty(ref _logConfigurations, value);
            }

        /// The currently selected log configuration in the UI.
        public LogConfigurationModel SelectedLog
            {
                get => _selectedLog;
                set => SetProperty(ref _selectedLog, value);
            }

        // ------------------Commands (Bound to Buttons/Actions in View)-------------------//


        /// Command to edit an existing log configuration.

        public ICommand EditCommand { get; }

        /// Command to delete a selected log configuration.
        public ICommand DeleteCommand { get; }

        /// Command to create a new log configuration entry.

        public ICommand AddInterfaceCommand { get; }

        /// Command to refresh/reload the list of logs.

        public ICommand RefreshCommand { get; }

        // ------------------------ Constructor----------------//

        /// Initializes the LogListViewModel and loads the list of log configurations.

        public LogListViewModel(ILogConfigurationService logService, INavigationService nav)
            {
                _logService = logService;
                _nav = nav;
                LogConfigurations = new ObservableCollection<LogConfigurationModel>();

            // Initialize all commands with their respective actions

            EditCommand = new RelayCommand<LogConfigurationModel>(OnEdit);
                DeleteCommand = new RelayCommand<LogConfigurationModel>(OnDelete);
                AddInterfaceCommand = new RelayCommand(OnAddInterface);
                RefreshCommand = new RelayCommand(async () => await LoadDataAsync());

            // Load the log list asynchronously at startup
            _ = LoadDataAsync();
            }

        // ---------------------Data Loading---------------------//

        private async Task LoadDataAsync()
            {
                var logs = await _logService.GetAllAsync();
                LogConfigurations.Clear();

            // Populate observable collection (triggers UI update)
            foreach (var log in logs)
                {
                    LogConfigurations.Add(log);
                }
            }

        // ------------------Command Handlers------------------//

        /// Opens the selected log configuration in edit mode.
        /// After saving, refreshes the list.

        private void OnEdit(LogConfigurationModel log)
            {
                if (log == null) return;

            // Navigate to log configuration page and reload data after save
            _nav.NavigateToLogConfiguration(log, async () =>
                {
                    // Callback after save
                    await LoadDataAsync();
                });
            }

            private async void OnDelete(LogConfigurationModel log)
            {
                if (log == null) return;

            // Ask for confirmation before deleting

            var result = System.Windows.MessageBox.Show(
                   $"Are you sure you want to delete '{SelectedLog.LogName}'?",
                   "Confirm Delete",
                   System.Windows.MessageBoxButton.YesNo,
                   System.Windows.MessageBoxImage.Question);

                if (result == System.Windows.MessageBoxResult.Yes)
                {

                    // TODO: Add confirmation dialog using IDialogService
                    await _logService.DeleteAsync(log.Id);
                    await LoadDataAsync();
                }
            }

        /// Opens a blank log configuration form to create a new log entry.
        /// After saving, reloads the log list.
        private void OnAddInterface()
            {
                // Navigate to new log configuration
                _nav.NavigateToLogConfiguration(null, async () =>
                {
                    // Callback after save
                    await LoadDataAsync();
                });
            }
        }
    

}
