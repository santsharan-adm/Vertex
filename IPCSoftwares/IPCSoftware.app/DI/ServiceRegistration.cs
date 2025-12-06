using IPCSoftware.App.NavServices;
using IPCSoftware.App.ViewModels;
using IPCSoftware.App.Views;

using IPCSoftware.Core.Interfaces;
using IPCSoftware.Core.Interfaces.AppLoggerInterface;
using IPCSoftware.Services;
using IPCSoftware.Services.AppLoggerServices;
using IPCSoftware.Services.ConfigServices;
using Microsoft.Extensions.DependencyInjection;

namespace IPCSoftware.App.DI
{
    public static class ServiceRegistration
    {
        public static void RegisterServices(IServiceCollection services)
        {
            //Auth service
            services.AddSingleton<IAuthService, AuthService>();
            //Credentials
            services.AddSingleton<ICredentialsService, CredentialsService>();
            //Navigation
            services.AddSingleton<INavigationService, NavigationService>();
            //Dialog service
            services.AddSingleton<IDialogService, DialogService>();
            //AppLogger 
            services.AddSingleton<IAppLogger, AppLoggerService>();
            services.AddSingleton<ILogManagerService, LogManagerService>();


            services.AddSingleton<ILogConfigurationService, LogConfigurationService>();
            services.AddSingleton<IDeviceConfigurationService, DeviceConfigurationService>();
            services.AddSingleton<IAlarmConfigurationService, AlarmConfigurationService>();
            services.AddSingleton<IUserManagementService, UserManagementService>();
            services.AddSingleton<IPLCTagConfigurationService, PLCTagConfigurationService>();

            services.AddSingleton<ILogService, LogService>();


            // ========== MAIN VIEWMODELS (Singleton) ==========
            services.AddSingleton<RibbonViewModel>();
            services.AddSingleton<MainWindowViewModel>();
            services.AddSingleton<OEEDashboardViewModel>();
            services.AddSingleton<OeeDashboardNewViewModel>();
            services.AddSingleton<PLCIOViewModel>();


            // ========== LOG CONFIGURATION VIEWMODELS (Transient) ==========
            services.AddTransient<LogListViewModel>();
            services.AddTransient<LogConfigurationViewModel>();

            // ========== DEVICE CONFIGURATION VIEWMODELS (Transient) ==========
            services.AddTransient<DeviceListViewModel>();
            services.AddTransient<DeviceConfigurationViewModel>();
            services.AddTransient<DeviceDetailViewModel>();
            services.AddTransient<DeviceInterfaceConfigurationViewModel>();
            services.AddTransient<CameraDetailViewModel>();
            services.AddTransient<CameraInterfaceConfigurationViewModel>();


            // ========== ALARM CONFIGURATION VIEWMODELS (Transient) ========== 
            services.AddTransient<AlarmListViewModel>();
            services.AddTransient<AlarmConfigurationViewModel>();

            // ========== USER MANAGEMENT VIEWMODELS (Transient) ========== 
            services.AddTransient<UserListViewModel>();
            services.AddTransient<UserConfigurationViewModel>();


            // ========== PLC TAG CONFIGURATION VIEWMODELS (Transient) ========== 
            services.AddTransient<PLCTagListViewModel>();
            services.AddTransient<PLCTagConfigurationViewModel>();

            // Views
            services.AddTransient<LoginView>();
            services.AddTransient<RibbonView>();
            services.AddTransient<OEEDashboard>();
            services.AddTransient<OeeDashboard2>();
            services.AddTransient<OeeDashboardNew>();
            services.AddTransient<DashboardView>();
            services.AddTransient<PLCIOView>();

            services.AddTransient<LogView>();

            // Log Configuration Views
            services.AddTransient<LogListView>();
            services.AddTransient<LogConfigurationView>();

            // Device Configuration Views
            services.AddTransient<DeviceListView>();
            services.AddTransient<DeviceConfigurationView>();
            services.AddTransient<DeviceDetailView>();
            services.AddTransient<DeviceInterfaceConfigurationView>();
            services.AddTransient<CameraDetailView>();
            services.AddTransient<CameraInterfaceConfigurationView>();

            // Alarm Configuration Views 
            services.AddTransient<AlarmListView>();
            services.AddTransient<AlarmConfigurationView>();

            // User Management Views 
            services.AddTransient<UserListView>();
            services.AddTransient<UserConfigurationView>();

            services.AddTransient<ModeOfOperation>();
            services.AddTransient<ManualOperation>();
            services.AddTransient<PLCIOMonitor>();

            services.AddTransient<ModeOfOperationViewModel>();
            services.AddTransient<ManualOperationViewModel>();
            services.AddTransient<PlcIoMonitorViewModel>();
            // PLC Tag Configuration Views 
            services.AddTransient<PLCTagListView>();
            services.AddTransient<PLCTagConfigurationView>();

            services.AddTransient<LogViewerViewModel>();

            //services.AddTransient<SettingsView>();
            //services.AddTransient<LogsView>();
            //services.AddTransient<UserMgmtView>();
            //System Settings
            services.AddTransient<SystemSettingView>();
            services.AddTransient<SystemSettingViewModel>();
            services.AddTransient<IPLCService, PlcService>();




        }
    }
}
