using IPCSoftware.App.Views;
using IPCSoftware.Core.Interfaces;
using IPCSoftware.Services.ConfigServices;
using IPCSoftware.Shared;
using IPCSoftware.Shared.IPCSoftware.Shared;
using IPCSoftware.Shared.Models.ConfigModels;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace IPCSoftware.App.ViewModels
{
    /*  public class LogListViewModel : BaseViewModel
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

          public event EventHandler<LogConfigurationModel> EditRequested;
          public event EventHandler AddRequested;

          public LogListViewModel(ILogConfigurationService logService, INavigationService nav)
          {
              _logService = logService;
              _nav = nav;
              LogConfigurations = new ObservableCollection<LogConfigurationModel>();

              EditCommand = new RelayCommand(OnEdit, CanEdit);
              DeleteCommand = new RelayCommand(OnDelete, CanDelete);
              AddInterfaceCommand = new RelayCommand(OnAddInterface);
              RefreshCommand = new RelayCommand(async () => await LoadDataAsync());

              _ = LoadDataAsync();
          }

          private async Task LoadDataAsync()
          {
              var logs = await _logService.GetAllAsync();
              LogConfigurations.Clear();

              foreach (var log in logs)
              {
                  LogConfigurations.Add(log);
              }
          }

          private bool CanEdit()
          {
              return SelectedLog != null;
          }

          *//*  private void OnEdit()
            {
                if (SelectedLog != null)
                {
                    EditRequested?.Invoke(this, SelectedLog);
                }
            }*//*


          private void OnEdit(LogConfigurationModel log)
          {
              if (log == null) return;

              var configView = App.ServiceProvider.GetService<LogConfigurationView>();
              var configVM = (LogConfigurationViewModel)configView.DataContext;

              configVM.LoadForEdit(log);
              configVM.SaveCompleted += async (s, e) =>
              {
                  await LoadDataAsync();
                  _nav.NavigateMain<LogListView>();
              };
              configVM.CancelRequested += (s, e) =>
              {
                  _nav.NavigateMain<LogListView>();
              };

              _nav.NavigateMain(configView);
          }


          private bool CanDelete()
          {
              return SelectedLog != null;
          }

          private async void OnDelete()
          {
              if (SelectedLog != null)
              {
                  var result = System.Windows.MessageBox.Show(
                      $"Are you sure you want to delete '{SelectedLog.LogName}'?",
                      "Confirm Delete",
                      System.Windows.MessageBoxButton.YesNo,
                      System.Windows.MessageBoxImage.Question);

                  if (result == System.Windows.MessageBoxResult.Yes)
                  {
                      await _logService.DeleteAsync(SelectedLog.Id);
                      await LoadDataAsync();
                  }
              }
          }

          private void OnAddInterface()
          {
              AddRequested?.Invoke(this, EventArgs.Empty);
          }

          public async Task RefreshAsync()
          {
              await LoadDataAsync();
          }
      }
  */

  
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

            public LogListViewModel(ILogConfigurationService logService, INavigationService nav)
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
                var logs = await _logService.GetAllAsync();
                LogConfigurations.Clear();
                foreach (var log in logs)
                {
                    LogConfigurations.Add(log);
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
