using IPCSoftware.App.NavServices;
using IPCSoftware.App.Services;
using IPCSoftware.App.Services.UI;
using IPCSoftware.App.ViewModels;
using IPCSoftware.App.Views;

using IPCSoftware.Core.Interfaces;
using IPCSoftware.Core.Interfaces.AppLoggerInterface;
using IPCSoftware.Core.Interfaces.CCD;
using IPCSoftware.CoreService;
using IPCSoftware.CoreService.Alarm;
using IPCSoftware.CoreService.Services.Algorithm;
using IPCSoftware.CoreService.Services.CCD;
using IPCSoftware.CoreService.Services.Dashboard;
using IPCSoftware.CoreService.Services.PLC;
using IPCSoftware.CoreService.Services.UI;
using IPCSoftware.Services;
using IPCSoftware.Services.AppLoggerServices;
using IPCSoftware.Services.ConfigServices;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.ComponentModel.Design.Serialization;

namespace IPCSoftware.App.DI
{
    public static class ServiceRegistration
    {
        public static void RegisterServices(IServiceCollection services/*, IConfiguration configuration*/)
        {
           // services.AddHostedService<Worker>();
            services.AddSingleton<IPLCTagConfigurationService, PLCTagConfigurationService>();
            services.AddSingleton<IDeviceConfigurationService, DeviceConfigurationService>();
            services.AddSingleton<ICycleManagerService, CycleManagerService>();
            services.AddSingleton<AlgorithmAnalysisService>();
            services.AddSingleton<DashboardInitializer>();
            services.AddSingleton<OeeEngine>();
            services.AddSingleton<SystemMonitorService>();
            services.AddSingleton<CCDTriggerService>();
            services.AddSingleton<PLCClientManager>();
            services.AddSingleton<CameraFtpService>();
            services.AddTransient<ProductionImageService>();
            services.AddSingleton<AlarmService>();

            services.AddSingleton<UiListener>(sp =>
            {
                return new UiListener(5050);
            });
            // When someone asks for IMessagePublisher, give them the EXISTING UiListener
            services.AddSingleton<IMessagePublisher>(sp => sp.GetRequiredService<UiListener>());


            //services.AddSingleton<IConfiguration>(configuration);
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
           // services.AddSingleton<IDeviceConfigurationService, DeviceConfigurationService>();
            services.AddSingleton<IAlarmConfigurationService, AlarmConfigurationService>();
            services.AddSingleton<IUserManagementService, UserManagementService>();
          //  services.AddSingleton<IPLCTagConfigurationService, PLCTagConfigurationService>();


            // CCD Serive
           // services.AddSingleton<ICycleManagerService, CycleManagerService>();

            services.AddSingleton<ILogService, LogService>();


            // ========== MAIN VIEWMODELS (Singleton) ==========
            services.AddSingleton<RibbonViewModel>();
            services.AddSingleton<MainWindowViewModel>();
            services.AddTransient<OEEDashboardViewModel>();
        //    services.AddSingleton<OeeDashboardNewViewModel>();
            services.AddSingleton<UiTcpClient>();




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

            // ========== Alaram VIEWMODELS (Transient) ========== 
            services.AddTransient<AlarmView>();
            services.AddTransient<AlarmViewModel>();
            services.AddSingleton<AlarmService>();


            // ========== PLC TAG CONFIGURATION VIEWMODELS (Transient) ========== 
            services.AddTransient<PLCTagListViewModel>();
            services.AddTransient<PLCTagConfigurationViewModel>();

            // Views
            services.AddTransient<LoginView>();
            services.AddTransient<RibbonView>();
            services.AddTransient<OEEDashboard>();
       
      
            services.AddTransient<DashboardView>();

            services.AddTransient<PLCIOView>();
            services.AddTransient<PLCIOViewModel>();

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

            //Tag Control
            services.AddTransient<TagControlView>();
            services.AddTransient<TagControlViewModel>();

            //services.AddTransient<SettingsView>();
            //services.AddTransient<LogsView>();
            //services.AddTransient<UserMgmtView>();
            //System Settings
            services.AddTransient<SystemSettingView>();
            services.AddTransient<SystemSettingViewModel>();
            services.AddTransient<IPLCService, PlcService>();

            // --- New Registration in ServiceRegistration.cs ---

            //  // Define the constants used for the network client
            //  const string IpAddress = "127.0.0.1";
            //  const int Port = 5050; // Or whatever port you are using

            //  // Register UiTcpClient with a factory method to supply constructor arguments
            ////  services.AddSingleton<UiTcpClient>(s => new UiTcpClient(IpAddress, Port));

            //  // Register CoreClient (which consumes UiTcpClient)
            services.AddSingleton<CoreClient>();


        }
    }
}
