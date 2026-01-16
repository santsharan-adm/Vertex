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
using IPCSoftware.CoreService.Services.External;
using IPCSoftware.CoreService.Services.Logging;
using IPCSoftware.CoreService.Services.PLC;
using IPCSoftware.CoreService.Services.UI;
using IPCSoftware.Services;
using IPCSoftware.Services.AppLoggerServices;
using IPCSoftware.Services.ConfigServices;
using IPCSoftware.Shared.Models.ConfigModels;
using Microsoft.Extensions.DependencyInjection;
using System.ComponentModel.Design.Serialization;
using System.IO;

namespace IPCSoftware.App.DI
{
    public static class ServiceRegistration
    {
        public static void RegisterServices(IServiceCollection services/*, IConfiguration configuration*/)
        {
            // services.AddHostedService<Worker>();
            services.AddSingleton<IAppLogger, AppLoggerService>();
            services.AddSingleton<IPLCTagConfigurationService, PLCTagConfigurationService>();
            services.AddSingleton<IDeviceConfigurationService, DeviceConfigurationService>();
            services.AddSingleton<ICycleManagerService, CycleManagerService>();
            services.AddSingleton<ExternalInterfaceService>();
            services.AddSingleton<ICcdConfigService, CcdConfigService>();
            services.AddSingleton<AlgorithmAnalysisService>();
            services.AddSingleton<DashboardInitializer>();
            services.AddSingleton<OeeEngine>();
            services.AddSingleton<SystemMonitorService>();
            services.AddSingleton<IAlarmHistoryService, AlarmHistoryService>();
            // --- Updated registration for IProductionDataLogger ---
            services.AddSingleton<IProductionDataLogger>(sp =>
            {
                var logConfigService = sp.GetRequiredService<ILogConfigurationService>();
                var initTask = logConfigService.InitializeAsync();
                initTask.Wait();
                var prodLogConfigTask = logConfigService.GetByLogTypeAsync(LogType.Production);
                prodLogConfigTask.Wait();
                var prodLogConfig = prodLogConfigTask.Result;
                if (prodLogConfig == null || !prodLogConfig.Enabled)
                    throw new InvalidOperationException("Production log configuration not found or not enabled.");

                return new ProductionDataLogger(prodLogConfig);
            });
            services.AddSingleton<CCDTriggerService>();
            services.AddSingleton<PLCClientManager>();
            services.AddSingleton<CameraFtpService>();
            services.AddTransient<ProductionImageService>();
            services.AddSingleton<AlarmService>();
            services.AddSingleton<UiListener>(sp =>
            {
                var logger = sp.GetRequiredService<IAppLogger>();
                return new UiListener(5050, logger);
            });
            // When someone asks for IMessagePublisher, give them the EXISTING UiListener
            services.AddSingleton<IMessagePublisher>(sp => sp.GetRequiredService<UiListener>());


            //services.AddSingleton<IConfiguration>(configuration);
            //Auth service
            services.AddSingleton<IAuthService, AuthService>();
            //Credentials
            services.AddSingleton<ICredentialsService, CredentialsService>();
            services.AddSingleton<IAeLimitService, AeLimitService>();
            //Navigation
            services.AddSingleton<INavigationService, NavigationService>();
            //Dialog service
            services.AddSingleton<IDialogService, DialogService>();
            //AppLogger 
      
            services.AddSingleton<ILogManagerService, LogManagerService>();
            services.AddSingleton<IShiftManagementService, ShiftManagementService>();
          


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
            services.AddSingleton<ShiftResetService>();




            // ========== LOG CONFIGURATION VIEWMODELS (Transient) ==========
            services.AddTransient<ShiftConfigurationViewModel>();
            services.AddTransient<ShiftConfigurationView>();


            // ========== LOG CONFIGURATION VIEWMODELS (Transient) ==========
            services.AddTransient<LogListViewModel>();
            services.AddTransient<LogConfigurationViewModel>();


            // ========== Start Up Condition View (Transient) ==========
            services.AddTransient<StartupConditionView>();
            services.AddTransient<StartupConditionViewModel>();

            // ========== DEVICE CONFIGURATION VIEWMODELS (Transient) ==========
            services.AddTransient<DeviceListViewModel>();
            services.AddTransient<DeviceConfigurationViewModel>();
            services.AddTransient<DeviceDetailViewModel>();
            services.AddTransient<DeviceInterfaceConfigurationViewModel>();
            services.AddTransient<CameraDetailViewModel>();
            services.AddTransient<CameraInterfaceConfigurationViewModel>();
            
            services.AddTransient<AeLimitViewModel>();
            services.AddTransient<AeLimitView>();


            // ===== Produciton Image ViewModel =====
            services.AddTransient<ProductionImageView>();
            services.AddTransient<ProductionImageViewModel>();



            // ========== ALARM CONFIGURATION VIEWMODELS (Transient) ========== 
            services.AddTransient<AlarmListViewModel>();
            services.AddTransient<AlarmConfigurationViewModel>();
            services.AddTransient< BackupService>();
            services.AddTransient<TagConfigLoader>();


            // ========== USER MANAGEMENT VIEWMODELS (Transient) ========== 
            services.AddTransient<UserListViewModel>();
            services.AddTransient<UserConfigurationViewModel>();

            // ========== Alaram VIEWMODELS (Transient) ========== 
            services.AddTransient<AlarmView>();
            services.AddSingleton<AlarmViewModel>();
            services.AddSingleton<AlarmService>();


            // ========== PLC TAG CONFIGURATION VIEWMODELS (Transient) ========== 
            services.AddTransient<PLCTagListViewModel>();
            services.AddTransient<PLCTagConfigurationViewModel>();

            // ========== PLC TAG CONFIGURATION VIEWMODELS (Transient) ========== 
            services.AddTransient<ServoCalibrationView>();
            services.AddTransient<ServoCalibrationViewModel>();

            services.AddSingleton<IServoCalibrationService, ServoCalibrationService>();

            // Views
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
          //  services.AddTransient<ManualOperation>();
            services.AddTransient<ManualOperationView>();
            services.AddTransient<PLCIOMonitor>();

            services.AddTransient<ModeOfOperationViewModel>();
            //services.AddTransient<ManualOperationViewModel>();
            services.AddTransient<ManualOpViewModel>();
          //  services.AddTransient<PlcIoMonitorViewModel>();
            // PLC Tag Configuration Views 
            services.AddTransient<PLCTagListView>();
            services.AddTransient<PLCTagConfigurationView>();

            services.AddTransient<LogViewerViewModel>();

            services.AddTransient<LoginViewModel>();
            services.AddTransient<LoginView>();

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


            services.AddSingleton<ReportConfigViewModel>();
            services.AddSingleton<ReportConfigView>();
            services.AddSingleton<ReportViewerViewModel>();
            services.AddSingleton<ReportViewerView>();

            services.AddTransient<ApiTestViewModel>();
            services.AddTransient<ApiTestView>();

            services.AddTransient<ProcessSequenceViewModel>();
            services.AddTransient<ProcessSequenceWindow>();

            services.AddTransient<Func<ProcessSequenceWindow>>(sp => () => sp.GetRequiredService<ProcessSequenceWindow>());
        }
    }
}
