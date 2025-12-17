using IPCSoftware.Core.Interfaces;
using IPCSoftware.Core.Interfaces.AppLoggerInterface;
using IPCSoftware.Services.ConfigServices;
using IPCSoftware.Shared;
using IPCSoftware.Shared.Models.ConfigModels;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace IPCSoftware.App.ViewModels
{
  
        public class LogListViewModel : BaseViewModel
        {
            private readonly ILogConfigurationService _logService;
            private readonly INavigationService _nav;
            private ObservableCollection<LogConfigurationModel> _logConfigurations;
            private LogConfigurationModel _selectedLog;

            public ObservableCollection<LogConfigurationModel> LogConfigurations
            {
                get => _logConfigurations;
                set => SetProperty(ref _logConfigurations, value);
            }

            public LogConfigurationModel SelectedLog
            {
                get => _selectedLog;
                set => SetProperty(ref _selectedLog, value);
            }

            public ICommand EditCommand { get; }
            public ICommand DeleteCommand { get; }
            public ICommand AddInterfaceCommand { get; }
            public ICommand RefreshCommand { get; }

            public LogListViewModel(
                ILogConfigurationService logService, 
                INavigationService nav, 
                IAppLogger logger) : base(logger)
            {
                _logService = logService;
                _nav = nav;
                LogConfigurations = new ObservableCollection<LogConfigurationModel>();

                EditCommand = new RelayCommand<LogConfigurationModel>(OnEdit);
                DeleteCommand = new RelayCommand<LogConfigurationModel>(OnDelete);
                AddInterfaceCommand = new RelayCommand(OnAddInterface);
                RefreshCommand = new RelayCommand(async () => await LoadDataAsync());

                _ = LoadDataAsync();
            }

            private async Task LoadDataAsync()
            {
                try
                {
                    var logs = await _logService.GetAllAsync();
                        LogConfigurations.Clear();
                        foreach (var log in logs)
                        {
                            LogConfigurations.Add(log);
                        }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex.Message, LogType.Diagnostics);
                }
            }

            private void OnEdit(LogConfigurationModel log)
            {
                if (log == null) return;

                // Navigate and pass the log model to edit
                _nav.NavigateToLogConfiguration(log, async () =>
                {
                    // Callback after save
                    await LoadDataAsync();
                });
            }

            private async void OnDelete(LogConfigurationModel log)
            {
                try
                {
                    if (log == null) return;

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
                catch (Exception ex)
                {
                    _logger.LogError(ex.Message, LogType.Diagnostics);
                }
            }

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
