using IPCSoftware.UI.CommonViews.ViewModels;
using IPCSoftware.UI.CommonViews;
using IPCSoftware.Core.Interfaces;
using IPCSoftware.Shared.Models;
using IPCSoftware.Shared.Models.ConfigModels;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace IPCSoftware.Common.CommonFunctions
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
            if (!CanNavigateFromCurrent()) return;

            var view = _provider.GetService<TView>();
            _mainContent.Content = view;
        }

        // ---------------- RIBBON AREA ----------------
        public void NavigateRibbon<TView>() where TView : UserControl
        {
            if (_ribbonHost == null)
                throw new InvalidOperationException("NavigationService not configured");

            var view = _provider.GetService<TView>();
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
            var configView = _provider.GetService<LogConfigurationView>();
            var configVM = _provider.GetService<LogConfigurationViewModel>();

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
            var configView = _provider.GetService<DeviceConfigurationView>();
            var configVM = _provider.GetService<DeviceConfigurationViewModel>();

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
            if (!CanNavigateFromCurrent()) return;
            var detailView = _provider.GetService<DeviceDetailView>();
            var detailVM = _provider.GetService<DeviceDetailViewModel>();
             
            detailView.DataContext = detailVM;
            await detailVM.LoadDevice(device);

            _mainContent.Content = detailView;
        }

        // --------------- Camera DETAIL ---------------
        public async void NavigateToCameraDetail(DeviceModel device)
        {
            if (!CanNavigateFromCurrent()) return;
            var detailView = _provider.GetService<CameraDetailView>();
            var detailVM = _provider.GetService<CameraDetailViewModel>();
             
            detailView.DataContext = detailVM;
            await detailVM.LoadDevice(device);

            _mainContent.Content = detailView;
        }



        // --------------- INTERFACE CONFIG ---------------
        public void NavigateToInterfaceConfiguration(DeviceModel parentDevice,
            DeviceInterfaceModel interfaceToEdit, Func<Task> onSaveCallback)
        {
            var configView = _provider.GetService<DeviceInterfaceConfigurationView>();
            var configVM = _provider.GetService<IPCSoftware.UI.CommonViews.ViewModels.DeviceInterfaceConfigurationViewModel>();

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
            var configView = _provider.GetService<CameraInterfaceConfigurationView>();
            var configVM = _provider.GetService<CameraInterfaceConfigurationViewModel>();

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



        //  CCD Settings Navigation           //Added by Rishabh - date - 08/04/2026//
        public void NavigateToCcdSettings(DeviceModel parentDevice, CameraInterfaceModel cameraInterface, Func<Task> onSaveCallback)
        {
            var ccdView = _provider.GetService<CcdSettingsView>();
            var ccdVM = _provider.GetService<CcdSettingsViewModel>();

            ccdView.DataContext = ccdVM;

            // Load CCD settings (implement in ViewModel as needed)
            ccdVM.LoadForEdit(cameraInterface);

            EventHandler saveHandler = null;
            EventHandler cancelHandler = null;

            saveHandler = async (s, e) =>
            {
                ccdVM.SaveCompleted -= saveHandler;
                ccdVM.CancelRequested -= cancelHandler;

                if (onSaveCallback != null)
                    await onSaveCallback();

                // Return to CameraInterfaceConfiguration
                NavigateToCameraInterfaceConfiguration(parentDevice, cameraInterface, null);
            };

            cancelHandler = (s, e) =>
            {
                ccdVM.SaveCompleted -= saveHandler;
                ccdVM.CancelRequested -= cancelHandler;

                // Return to CameraInterfaceConfiguration
                NavigateToCameraInterfaceConfiguration(parentDevice, cameraInterface, null);
            };

            ccdVM.SaveCompleted += saveHandler;
            ccdVM.CancelRequested += cancelHandler;

            _mainContent.Content = ccdView;
        }

        public void NavigateToAlarmList()
        {
            NavigateMain<AlarmListView>();
        }

        public void NavigateToAlarmConfiguration(AlarmConfigurationModel alarmToEdit, Func<Task> onSaveCallback)
        {
            var configView = _provider.GetService<AlarmConfigurationView>();
            var configVM = _provider.GetService<AlarmConfigurationViewModel>();

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
            var configView = _provider.GetService<UserConfigurationView>();
            var configVM = _provider.GetService<UserConfigurationViewModel>();

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
            var view = _provider.GetService<SystemSettingView>();
            var viewModel = _provider.GetService<SystemSettingViewModel>();
            if (!CanNavigateFromCurrent()) return;
            // Assign VM to View
            view.DataContext = viewModel;

            // Assign to MainContentPresenter
            _mainContent.Content = view;
        }
        public void NavigateToPLCTagList()
        {
            NavigateMain<PLCTagListView>();
        }

        //Added By Rishabh , Date -13/04/2026
        public void NavigateToServiceStartup()
        {
            NavigateMain<ServiceStartupView>();
        }

        public void NavigateToPLCTagConfiguration(PLCTagConfigurationModel tagToEdit, Func<Task> onSaveCallback)
        {

            var configView = _provider.GetService<PLCTagConfigurationView>();
            var configVM = _provider.GetService<PLCTagConfigurationViewModel>();

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
            if (!CanNavigateFromCurrent()) return;
            var view = _provider.GetRequiredService<LogView>();
            var vm = _provider.GetRequiredService<LogViewerViewModel>();
            _ = vm.LoadCategoryAsync(logType);
            view.DataContext = vm;

            _mainContent.Content = view;

        }

        // App-specific view navigation — types live in IPCSoftware.app assembly.
        // Resolve by scanning loaded assemblies and DI container.
        private void NavigateMainByTypeName(string typeName)
        {
            if (!CanNavigateFromCurrent()) return;

            // Find ALL matching UserControl types across all loaded assemblies
            var candidates = AppDomain.CurrentDomain.GetAssemblies()
                .Where(a => !a.IsDynamic)
                .SelectMany(a => { try { return a.GetTypes(); } catch { return Array.Empty<Type>(); } })
                .Where(t => t.Name == typeName && typeof(UserControl).IsAssignableFrom(t) && !t.IsAbstract)
                .ToList();

            // Try each candidate until one resolves from DI
            foreach (var viewType in candidates)
            {
                var view = _provider.GetService(viewType) as UserControl;
                if (view != null)
                {
                    if (_mainContent != null)
                        _mainContent.Content = view;
                    return;
                }
            }

            System.Diagnostics.Debug.WriteLine($"[NavigationService] WARNING: Could not resolve view '{typeName}' from DI. Candidates found: {candidates.Count}");
        }

        public void NavigateToManualOperation() => NavigateMainByTypeName("ManualOperationView");
        public void NavigateToOEEDashboard() => NavigateMainByTypeName("OEEDashboard");
        public void NavigateToAeLimit() => NavigateMainByTypeName("AeLimitView");

        public bool CanNavigateFromCurrent()
        {
            // 1. Get the current View
            if (_mainContent?.Content is FrameworkElement currentView)
            {
                // 2. Get the ViewModel from DataContext
                if (currentView.DataContext is INavigationalAware navAwareVM)
                {
                    // 3. Ask ViewModel if we can leave
                    if (!navAwareVM.OnNavigatingFrom())
                    {
                        return false; // ViewModel said NO
                    }
                }
            }
            return true; // Safe to navigate
        }


    }


}
