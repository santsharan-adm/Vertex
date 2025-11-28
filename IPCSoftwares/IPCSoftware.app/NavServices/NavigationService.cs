using IPCSoftware.App.ViewModels;
using IPCSoftware.App.Views;
using IPCSoftware.AppLogger.Models;
using IPCSoftware.Core.Interfaces;
using IPCSoftware.Shared.Models;
using IPCSoftware.Shared.Models.ConfigModels;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace IPCSoftware.App.NavServices
{
    public class NavigationService : INavigationService
    {
        private ContentControl _mainContent;
        private ContentControl _ribbonHost;
        private readonly IServiceProvider _provider;

        public NavigationService(IServiceProvider provider)
        {
            _provider = provider;
        }

        public void Configure(ContentControl mainContent, ContentControl ribbonHost)
        {
            _mainContent = mainContent;
            _ribbonHost = ribbonHost;
        }

        // ---------------- MAIN AREA ----------------
        public void NavigateMain<TView>() where TView : UserControl
        {
            if (_mainContent == null)
                throw new InvalidOperationException("NavigationService not configured");

            var view = App.ServiceProvider.GetService<TView>();
            _mainContent.Content = view;
        }

        // ---------------- RIBBON AREA ----------------
        public void NavigateRibbon<TView>() where TView : UserControl
        {
            if (_ribbonHost == null)
                throw new InvalidOperationException("NavigationService not configured");

            var view = App.ServiceProvider.GetService<TView>();
            _ribbonHost.Content = view;
        }

        public void ClearTop()
        {
            if (_ribbonHost != null)
                _ribbonHost.Content = null;
        }

        public void NavigateTop(object view)
        {
            if (_ribbonHost == null) return;

            if (view is Type type)
            {
                var resolved = _provider.GetService(type);
                _ribbonHost.Content = resolved ?? Activator.CreateInstance(type);
            }
            else
            {
                _ribbonHost.Content = view;
            }
        }

        // --------------- LOG CONFIGURATION ---------------
        public void NavigateToLogConfiguration(LogConfigurationModel logToEdit, Func<Task> onSaveCallback)
        {
            var configView = App.ServiceProvider.GetService<LogConfigurationView>();
            var configVM = App.ServiceProvider.GetService<LogConfigurationViewModel>();

            configView.DataContext = configVM;

            if (logToEdit == null)
                configVM.InitializeNewLog();
            else
                configVM.LoadForEdit(logToEdit);

            EventHandler saveHandler = null;
            EventHandler cancelHandler = null;

            saveHandler = async (s, e) =>
            {
                configVM.SaveCompleted -= saveHandler;
                configVM.CancelRequested -= cancelHandler;

                if (onSaveCallback != null)
                    await onSaveCallback();

                NavigateMain<LogListView>();
            };

            cancelHandler = (s, e) =>
            {
                configVM.SaveCompleted -= saveHandler;
                configVM.CancelRequested -= cancelHandler;

                NavigateMain<LogListView>();
            };

            configVM.SaveCompleted += saveHandler;
            configVM.CancelRequested += cancelHandler;

            _mainContent.Content = configView;
        }

        // --------------- DEVICE LIST ---------------
        public void NavigateToDeviceList()
        {
            NavigateMain<DeviceListView>();
        }

        // --------------- DEVICE CONFIG ---------------
        public void NavigateToDeviceConfiguration(DeviceModel deviceToEdit, Func<Task> onSaveCallback)
        {
            var configView = App.ServiceProvider.GetService<DeviceConfigurationView>();
            var configVM = App.ServiceProvider.GetService<DeviceConfigurationViewModel>();

            configView.DataContext = configVM;

            if (deviceToEdit == null)
                configVM.InitializeNewDevice();
            else
                configVM.LoadForEdit(deviceToEdit);

            EventHandler saveHandler = null;
            EventHandler cancelHandler = null;

            saveHandler = async (s, e) =>
            {
                configVM.SaveCompleted -= saveHandler;
                configVM.CancelRequested -= cancelHandler;

                if (onSaveCallback != null)
                    await onSaveCallback();

                NavigateMain<DeviceListView>();
            };

            cancelHandler = (s, e) =>
            {
                configVM.SaveCompleted -= saveHandler;
                configVM.CancelRequested -= cancelHandler;

                NavigateMain<DeviceListView>();
            };

            configVM.SaveCompleted += saveHandler;
            configVM.CancelRequested += cancelHandler;

            _mainContent.Content = configView;
        }

        // --------------- DEVICE DETAIL ---------------
        public async void NavigateToDeviceDetail(DeviceModel device)
        {
            var detailView = App.ServiceProvider.GetService<DeviceDetailView>();
            var detailVM = App.ServiceProvider.GetService<DeviceDetailViewModel>();
             
            detailView.DataContext = detailVM;
            await detailVM.LoadDevice(device);

            _mainContent.Content = detailView;
        }

        // --------------- Camera DETAIL ---------------
        public async void NavigateToCameraDetail(DeviceModel device)
        {
            var detailView = App.ServiceProvider.GetService<CameraDetailView>();
            var detailVM = App.ServiceProvider.GetService<CameraDetailViewModel>();
             
            detailView.DataContext = detailVM;
            await detailVM.LoadDevice(device);

            _mainContent.Content = detailView;
        }



        // --------------- INTERFACE CONFIG ---------------
        public void NavigateToInterfaceConfiguration(DeviceModel parentDevice,
            DeviceInterfaceModel interfaceToEdit, Func<Task> onSaveCallback)
        {
            var configView = App.ServiceProvider.GetService<DeviceInterfaceConfigurationView>();
            var configVM = App.ServiceProvider.GetService<DeviceInterfaceConfigurationViewModel>();

            configView.DataContext = configVM;

            if (interfaceToEdit == null)
                configVM.InitializeNewInterface(parentDevice);
            else
                configVM.LoadForEdit(parentDevice, interfaceToEdit);

            EventHandler saveHandler = null;
            EventHandler cancelHandler = null;

            saveHandler = async (s, e) =>
            {
                configVM.SaveCompleted -= saveHandler;
                configVM.CancelRequested -= cancelHandler;

                if (onSaveCallback != null)
                    await onSaveCallback();

                NavigateToDeviceDetail(parentDevice);
            };

            cancelHandler = (s, e) =>
            {
                configVM.SaveCompleted -= saveHandler;
                configVM.CancelRequested -= cancelHandler;

                NavigateToDeviceDetail(parentDevice);
            };

            configVM.SaveCompleted += saveHandler;
            configVM.CancelRequested += cancelHandler;

            _mainContent.Content = configView;
        }


        public void NavigateToCameraInterfaceConfiguration(DeviceModel parentDevice, CameraInterfaceModel cameraInterfaceToEdit, Func<Task> onSaveCallback)
        {
            var configView = App.ServiceProvider.GetService<CameraInterfaceConfigurationView>();
            var configVM = App.ServiceProvider.GetService<CameraInterfaceConfigurationViewModel>();

            configView.DataContext = configVM;

            if (cameraInterfaceToEdit == null)
            {
                configVM.InitializeNewInterface(parentDevice);
            }
            else
            {
                configVM.LoadForEdit(parentDevice, cameraInterfaceToEdit);
            }

            EventHandler saveHandler = null;
            EventHandler cancelHandler = null;

            saveHandler = async (s, e) =>
            {
                configVM.SaveCompleted -= saveHandler;
                configVM.CancelRequested -= cancelHandler;

                if (onSaveCallback != null)
                    await onSaveCallback();

                NavigateToCameraDetail(parentDevice);  // Return to CameraDetailView
            };

            cancelHandler = (s, e) =>
            {
                configVM.SaveCompleted -= saveHandler;
                configVM.CancelRequested -= cancelHandler;
                NavigateToCameraDetail(parentDevice);  // Return to CameraDetailView
            };

            configVM.SaveCompleted += saveHandler;
            configVM.CancelRequested += cancelHandler;

            _mainContent.Content = configView;
        }


        public void NavigateToAlarmList()
        {
            NavigateMain<AlarmListView>();
        }

        public void NavigateToAlarmConfiguration(AlarmConfigurationModel alarmToEdit, Func<Task> onSaveCallback)
        {
            var configView = App.ServiceProvider.GetService<AlarmConfigurationView>();
            var configVM = App.ServiceProvider.GetService<AlarmConfigurationViewModel>();

            configView.DataContext = configVM;

            if (alarmToEdit == null)
            {
                configVM.InitializeNewAlarm();
            }
            else
            {
                configVM.LoadForEdit(alarmToEdit);
            }

            EventHandler saveHandler = null;
            EventHandler cancelHandler = null;

            saveHandler = async (s, e) =>
            {
                configVM.SaveCompleted -= saveHandler;
                configVM.CancelRequested -= cancelHandler;

                if (onSaveCallback != null)
                    await onSaveCallback();

                NavigateMain<AlarmListView>();
            };

            cancelHandler = (s, e) =>
            {
                configVM.SaveCompleted -= saveHandler;
                configVM.CancelRequested -= cancelHandler;
                NavigateMain<AlarmListView>();
            };

            configVM.SaveCompleted += saveHandler;
            configVM.CancelRequested += cancelHandler;

            _mainContent.Content = configView;
        }


        public void NavigateToUserList()
        {
            NavigateMain<UserListView>();
        }

        public void NavigateToUserConfiguration(UserConfigurationModel userToEdit, Func<Task> onSaveCallback)
        {
            var configView = App.ServiceProvider.GetService<UserConfigurationView>();
            var configVM = App.ServiceProvider.GetService<UserConfigurationViewModel>();

            configView.DataContext = configVM;

            if (userToEdit == null)
            {
                configVM.InitializeNewUser();
            }
            else
            {
                configVM.LoadForEdit(userToEdit);
            }

            EventHandler saveHandler = null;
            EventHandler cancelHandler = null;

            saveHandler = async (s, e) =>
            {
                configVM.SaveCompleted -= saveHandler;
                configVM.CancelRequested -= cancelHandler;

                if (onSaveCallback != null)
                    await onSaveCallback();

                NavigateMain<UserListView>();
            };

            cancelHandler = (s, e) =>
            {
                configVM.SaveCompleted -= saveHandler;
                configVM.CancelRequested -= cancelHandler;
                NavigateMain<UserListView>();
            };

            configVM.SaveCompleted += saveHandler;
            configVM.CancelRequested += cancelHandler;

            _mainContent.Content = configView;
        }

        public void NavigateToSystemSettings()
        {
            // Create View + ViewModel via DI container
            var view = App.ServiceProvider.GetService<SystemSettingView>();
            var viewModel = App.ServiceProvider.GetService<SystemSettingViewModel>();

            // Assign VM to View
            view.DataContext = viewModel;

            // Assign to MainContentPresenter
            _mainContent.Content = view;
        }
        public void NavigateToPLCTagList()
        {
            NavigateMain<PLCTagListView>();
        }

        public void NavigateToPLCTagConfiguration(PLCTagConfigurationModel tagToEdit, Func<Task> onSaveCallback)
        {
            var configView = App.ServiceProvider.GetService<PLCTagConfigurationView>();
            var configVM = App.ServiceProvider.GetService<PLCTagConfigurationViewModel>();

            configView.DataContext = configVM;

            if (tagToEdit == null)
            {
                configVM.InitializeNewTag();
            }
            else
            {
                configVM.LoadForEdit(tagToEdit);
            }

            EventHandler saveHandler = null;
            EventHandler cancelHandler = null;

            saveHandler = async (s, e) =>
            {
                configVM.SaveCompleted -= saveHandler;
                configVM.CancelRequested -= cancelHandler;

                if (onSaveCallback != null)
                    await onSaveCallback();

                NavigateMain<PLCTagListView>();
            };

            cancelHandler = (s, e) =>
            {
                configVM.SaveCompleted -= saveHandler;
                configVM.CancelRequested -= cancelHandler;
                NavigateMain<PLCTagListView>();
            };

            configVM.SaveCompleted += saveHandler;
            configVM.CancelRequested += cancelHandler;

            _mainContent.Content = configView;
        }


        // Inside MainViewModel.cs

        // 1. Define the Navigation Command
        public void NavigateToLogs(string logType)
        {
            // Resolve the ViewModel from DI
            var vm = App.ServiceProvider.GetRequiredService<LogViewerViewModel>();

            // Convert string command parameter to Enum
            if (Enum.TryParse(logType, out LogType category))
            {
                


                NavigateMain<LogView>();
                // Navigate
               
            }
        }


    }


}
