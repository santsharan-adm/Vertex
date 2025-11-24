using IPCSoftware.App.ViewModels;
using IPCSoftware.App.Views;
using IPCSoftware.Core.Interfaces;
using IPCSoftware.Shared.Models.ConfigModels;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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

        public void NavigateMain<TView>() where TView : UserControl
        {
            if (_mainContent == null)
                throw new InvalidOperationException("NavigationService not configured");

            var view = App.ServiceProvider.GetService<TView>();
            _mainContent.Content = view;
        }




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

            // If view is a TYPE (e.g. typeof(RibbonView))
            if (view is Type type)
            {
                var resolved = _provider.GetService(type);
                _ribbonHost.Content = resolved ?? Activator.CreateInstance(type);
            }
            else
            {
                // If view is already an instance
                _ribbonHost.Content = view;
            }
        }




        // NEW: Navigate to LogConfiguration with context
        public void NavigateToLogConfiguration(LogConfigurationModel logToEdit, Func<Task> onSaveCallback)
        {
            var configView = App.ServiceProvider.GetService<LogConfigurationView>();
            var configVM = App.ServiceProvider.GetService<LogConfigurationViewModel>();

            configView.DataContext = configVM;

            // Setup the view model
            if (logToEdit == null)
            {
                configVM.InitializeNewLog();
            }
            else
            {
                configVM.LoadForEdit(logToEdit);
            }

            // Wire up events
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



        public void NavigateToDeviceList()
        {
            NavigateMain<DeviceListView>();
        }

        public void NavigateToDeviceConfiguration(DeviceModel deviceToEdit, Func<Task> onSaveCallback)
        {
            var configView = App.ServiceProvider.GetService<DeviceConfigurationView>();
            var configVM = App.ServiceProvider.GetService<DeviceConfigurationViewModel>();

            configView.DataContext = configVM;

            if (deviceToEdit == null)
            {
                configVM.InitializeNewDevice();
            }
            else
            {
                configVM.LoadForEdit(deviceToEdit);
            }

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

        public async void NavigateToDeviceDetail(DeviceModel device)
        {
            var detailView = App.ServiceProvider.GetService<DeviceDetailView>();
            var detailVM = App.ServiceProvider.GetService<DeviceDetailViewModel>();

            detailView.DataContext = detailVM;
            await detailVM.LoadDevice(device);

            _mainContent.Content = detailView;
        }

        public void NavigateToInterfaceConfiguration(DeviceModel parentDevice, DeviceInterfaceModel interfaceToEdit, Func<Task> onSaveCallback)
        {
            var configView = App.ServiceProvider.GetService<DeviceInterfaceConfigurationView>();
            var configVM = App.ServiceProvider.GetService<DeviceInterfaceConfigurationViewModel>();

            configView.DataContext = configVM;

            if (interfaceToEdit == null)
            {
                configVM.InitializeNewInterface(parentDevice);
            }
            else
            {
                configVM.LoadForEdit(parentDevice, interfaceToEdit);
            }

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


    }
}