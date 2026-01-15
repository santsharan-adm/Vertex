using IPCSoftware.App.ViewModels;
using IPCSoftware.App.Views;
using IPCSoftware.Core.Interfaces;
using IPCSoftware.Shared.Models;
using IPCSoftware.Shared.Models.ConfigModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualBasic.Logging;
using System;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace IPCSoftware.App.NavServices
{

    /// Handles navigation between views (UserControls) in the application.
    /// Uses dependency injection to resolve View and ViewModel instances,
    /// and dynamically loads them into the main content or ribbon areas.
    public class NavigationService : INavigationService
    {
        private ContentControl _mainContent;                           // Host for main page content
        private ContentControl _ribbonHost;                            // Host for ribbon (top bar) content
        private readonly IServiceProvider _provider;                   // DI provider for resolving views and viewmodels

        public NavigationService(IServiceProvider provider)
        {
            _provider = provider;
        }

        /// Initializes the navigation service by assigning host content controls.
        /// Must be called before navigation can occur.
        public void Configure(ContentControl mainContent, ContentControl ribbonHost)
        {
            _mainContent = mainContent;
            _ribbonHost = ribbonHost;
        }

        // ---------------- MAIN AREA ----------------

        /// Navigates the main area to the specified view.
        public void NavigateMain<TView>() where TView : UserControl
        {
            if (_mainContent == null)
                throw new InvalidOperationException("NavigationService not configured");

            var view = App.ServiceProvider.GetService<TView>();
            _mainContent.Content = view;
        }

        // ---------------- RIBBON AREA ----------------

        /// Navigates the ribbon (top bar) area to the specified view.
        public void NavigateRibbon<TView>() where TView : UserControl
        {
            if (_ribbonHost == null)
                throw new InvalidOperationException("NavigationService not configured");

            var view = App.ServiceProvider.GetService<TView>();
            _ribbonHost.Content = view;
        }

        /// Clears any existing content from the ribbon host.
        public void ClearTop()
        {
            if (_ribbonHost != null)
                _ribbonHost.Content = null;
        }

        /// Navigates the ribbon host to a given object or Type.
        /// If a Type is provided, it is resolved via DI; otherwise, the object is used directly.

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

        /// Opens the Log Configuration screen for editing or creating logs.
        /// Handles Save and Cancel events to return back to the Log List screen.
        public void NavigateToLogConfiguration(LogConfigurationModel logToEdit, Func<Task> onSaveCallback)
        {
            var configView = App.ServiceProvider.GetService<LogConfigurationView>();
            var configVM = App.ServiceProvider.GetService<LogConfigurationViewModel>();

            configView.DataContext = configVM;

            if (logToEdit == null)
                configVM.InitializeNewLog();
            else
                configVM.LoadForEdit(logToEdit);

            // Handlers for Save and Cancel actions
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

        /// Navigates to the Device List screen.
        public void NavigateToDeviceList()
        {
            NavigateMain<DeviceListView>();
        }

        // --------------- DEVICE CONFIG ---------------

        /// Opens the Device Configuration screen for adding or editing a device.
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

        /// Loads and navigates to the Device Detail screen for a specific device.
        public async void NavigateToDeviceDetail(DeviceModel device)
        {
            var detailView = App.ServiceProvider.GetService<DeviceDetailView>();
            var detailVM = App.ServiceProvider.GetService<DeviceDetailViewModel>();
             
            detailView.DataContext = detailVM;
            await detailVM.LoadDevice(device);

            _mainContent.Content = detailView;
        }

        // --------------- Camera DETAIL ---------------

        /// Loads and navigates to the Camera Detail screen for a specific camera device.
        public async void NavigateToCameraDetail(DeviceModel device)
        {
            var detailView = App.ServiceProvider.GetService<CameraDetailView>();
            var detailVM = App.ServiceProvider.GetService<CameraDetailViewModel>();
             
            detailView.DataContext = detailVM;
            await detailVM.LoadDevice(device);

            _mainContent.Content = detailView;
        }



        // --------------- INTERFACE CONFIG ---------------

        /// Opens Interface Configuration screen for a device and handles save/cancel navigation.
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

        /// Opens Camera Interface Configuration screen and handles save/cancel navigation.
        
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

        // --------------- ALARM SCREENS ---------------

        public void NavigateToAlarmList()
        {
            NavigateMain<AlarmListView>();
        }

        /// Opens Alarm Configuration screen for adding or editing alarms.
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

        // --------------- USER SCREENS ---------------
        public void NavigateToUserList()
        {
            NavigateMain<UserListView>();
        }

        /// Opens User Configuration screen for adding or editing users.
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

        // --------------- SYSTEM SETTINGS ---------------

        /// Navigates to the System Settings screen.
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
        // --------------- PLC TAG SCREENS ---------------
        public void NavigateToPLCTagList()
        {
            NavigateMain<PLCTagListView>();
        }

        /// Opens PLC Tag Configuration screen for adding or editing tags.
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


        // --------------- LOG VIEWER ---------------

        /// Opens the Log Viewer screen for a specific log category.
        public void NavigateToLogs(LogType logType)
        {
            var view = App.ServiceProvider.GetRequiredService<LogView>();
            var vm = App.ServiceProvider.GetRequiredService<LogViewerViewModel>();
            _ = vm.LoadCategoryAsync(logType);
            view.DataContext = vm;

            _mainContent.Content = view;

        }


    }


}
