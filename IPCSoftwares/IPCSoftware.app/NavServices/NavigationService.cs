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
    /// Central navigation service responsible for switching views
    /// in Main content area and Ribbon/Top area.
    /// Implements INavigationService for MVVM-based navigation.
    public class NavigationService : INavigationService
    {
        // Holds reference to main content presenter (center area)
        private ContentControl _mainContent;

        // Holds reference to ribbon/top content presenter
        private ContentControl _ribbonHost;

        // Dependency Injection service provider
        private readonly IServiceProvider _provider;

        /// Constructor injecting IServiceProvider
        public NavigationService(IServiceProvider provider)
        {
            _provider = provider;
        }

        /// Configures navigation targets (must be called once on startup)
        public void Configure(ContentControl mainContent, ContentControl ribbonHost)
        {
            _mainContent = mainContent;
            _ribbonHost = ribbonHost;
        }

        // ---------------- MAIN CONTENT NAVIGATION ----------------

        /// Navigates to a view in the main content area
        public void NavigateMain<TView>() where TView : UserControl
        {
            if (_mainContent == null)
                throw new InvalidOperationException("NavigationService not configured");

            // Resolve view from DI container
            var view = App.ServiceProvider.GetService<TView>();

            // Set as main content
            _mainContent.Content = view;
        }

        // ----------------RIBBON / TOP NAVIGATION----------------

        /// Navigates to a view in the ribbon/top area
        public void NavigateRibbon<TView>() where TView : UserControl
        {
            if (_ribbonHost == null)
                throw new InvalidOperationException("NavigationService not configured");

            var view = App.ServiceProvider.GetService<TView>();
            _ribbonHost.Content = view;
        }

        /// Clears the ribbon/top area
        public void ClearTop()
        {
            if (_ribbonHost != null)
                _ribbonHost.Content = null;
        }

        /// Navigates to a ribbon view using either a Type or an instance
        public void NavigateTop(object view)
        {
            if (_ribbonHost == null) return;

            // If Type is passed, resolve via DI
            if (view is Type type)
            {
                var resolved = _provider.GetService(type);
                _ribbonHost.Content = resolved ?? Activator.CreateInstance(type);
            }
            else
            {

                // Direct instance
                _ribbonHost.Content = view;
            }
        }

        // --------------- LOG CONFIGURATION ---------------

        /// Opens Log Configuration screen (New or Edit)
        public void NavigateToLogConfiguration(LogConfigurationModel logToEdit, Func<Task> onSaveCallback)
        {
            var configView = App.ServiceProvider.GetService<LogConfigurationView>();
            var configVM = App.ServiceProvider.GetService<LogConfigurationViewModel>();

            configView.DataContext = configVM;

            // Decide between New or Edit
            if (logToEdit == null)
                configVM.InitializeNewLog();
            else
                configVM.LoadForEdit(logToEdit);

            // Attach Save / Cancel handlers
            EventHandler saveHandler = null;
            EventHandler cancelHandler = null;

            saveHandler = async (s, e) =>
            {
                // Cleanup event handlers
                configVM.SaveCompleted -= saveHandler;
                configVM.CancelRequested -= cancelHandler;

                // Execute callback after save
                if (onSaveCallback != null)
                    await onSaveCallback();

                // Navigate back to list
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

        // ---------------DEVICE NAVIGATION ---------------

        /// Navigates to Device List view
        public void NavigateToDeviceList()
        {
            NavigateMain<DeviceListView>();
        }

        // --------------- DEVICE CONFIG ---------------

        /// Navigates to Device Configuration (New/Edit)
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

        /// Navigates to Device Detail view
        public async void NavigateToDeviceDetail(DeviceModel device)
        {
            var detailView = App.ServiceProvider.GetService<DeviceDetailView>();
            var detailVM = App.ServiceProvider.GetService<DeviceDetailViewModel>();
             
            detailView.DataContext = detailVM;

            // Load device data asynchronously
            await detailVM.LoadDevice(device);

            _mainContent.Content = detailView;
        }

        // --------------- Camera DETAIL ---------------

        /// Navigates to Camera Detail view
        public async void NavigateToCameraDetail(DeviceModel device)
        {
            var detailView = App.ServiceProvider.GetService<CameraDetailView>();
            var detailVM = App.ServiceProvider.GetService<CameraDetailViewModel>();
             
            detailView.DataContext = detailVM;
            await detailVM.LoadDevice(device);

            _mainContent.Content = detailView;
        }



        // --------------- INTERFACE CONFIGURATION ---------------

        /// Navigates to Device Interface Configuration
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

        // --------------- CAMERA CONFIGURATION ---------------

        /// Navigates to Camera Interface Configuration screen
        /// Used for creating or editing a camera interface of a device
        public void NavigateToCameraInterfaceConfiguration(DeviceModel parentDevice, CameraInterfaceModel cameraInterfaceToEdit, Func<Task> onSaveCallback)
        {
            // Resolve View and ViewModel from DI container

            var configView = App.ServiceProvider.GetService<CameraInterfaceConfigurationView>();
            var configVM = App.ServiceProvider.GetService<CameraInterfaceConfigurationViewModel>();

            // Assign ViewModel to View
            configView.DataContext = configVM;

            // Decide whether to create new interface or edit existing one
            if (cameraInterfaceToEdit == null)
            {
                configVM.InitializeNewInterface(parentDevice);
            }
            else
            {
                configVM.LoadForEdit(parentDevice, cameraInterfaceToEdit);
            }

            // Event handlers for Save and Cancel actions

            EventHandler saveHandler = null;
            EventHandler cancelHandler = null;

            // Save event handler

            saveHandler = async (s, e) =>
            {

                // Remove handlers to avoid memory leaks
                configVM.SaveCompleted -= saveHandler;
                configVM.CancelRequested -= cancelHandler;

                // Execute callback after save (if provided)

                if (onSaveCallback != null)
                    await onSaveCallback();

                // Navigate back to Camera Detail view
                NavigateToCameraDetail(parentDevice);  
            };
            // Cancel event handler
            cancelHandler = (s, e) =>
            {
                // Remove handlers
                configVM.SaveCompleted -= saveHandler;
                configVM.CancelRequested -= cancelHandler;

                // Navigate back to Camera Detail view
                NavigateToCameraDetail(parentDevice);  
            };

            // Attach handlers
            configVM.SaveCompleted += saveHandler;
            configVM.CancelRequested += cancelHandler;

            // Show configuration view in main content area
            _mainContent.Content = configView;
        }


        /// Navigates to Alarm List screen
        public void NavigateToAlarmList()
        {
            NavigateMain<AlarmListView>();
        }

        //--------------------Alarm Configuration----------------

        /// Navigates to Alarm Configuration screen (New/Edit)

        public void NavigateToAlarmConfiguration(AlarmConfigurationModel alarmToEdit, Func<Task> onSaveCallback)
        {
            var configView = App.ServiceProvider.GetService<AlarmConfigurationView>();
            var configVM = App.ServiceProvider.GetService<AlarmConfigurationViewModel>();

            configView.DataContext = configVM;

            // New alarm or edit existing alarm
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

        /// Navigates to User List screen
        public void NavigateToUserList()
        {
            NavigateMain<UserListView>();
        }

        //--------------------- User Configuration ---------------------------

        /// Navigates to User Configuration screen (New/Edit)
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


        /// Navigates to System Settings screen
        public void NavigateToSystemSettings()
        {
            // Resolve View and ViewModel
            var view = App.ServiceProvider.GetService<SystemSettingView>();
            var viewModel = App.ServiceProvider.GetService<SystemSettingViewModel>();

            // Assign ViewModel
            view.DataContext = viewModel;

            // Show in main content area
            _mainContent.Content = view;
        }

        //--------------------- PLC Tag Navigation --------------------

        /// Navigates to PLC Tag List screen
        public void NavigateToPLCTagList()
        {
            NavigateMain<PLCTagListView>();
        }

        /// Navigates to PLC Tag Configuration screen (New/Edit)
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
        public void NavigateToLogs(LogType logType)
        {

            // Resolve View and ViewModel
            var view = App.ServiceProvider.GetRequiredService<LogView>();
            var vm = App.ServiceProvider.GetRequiredService<LogViewerViewModel>();

            // Load logs asynchronously (fire & forget)
            _ = vm.LoadCategoryAsync(logType);
            view.DataContext = vm;

            _mainContent.Content = view;

        }


    }


}
