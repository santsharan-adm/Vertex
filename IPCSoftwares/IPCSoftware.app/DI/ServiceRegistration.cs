using IPCSoftware.Common.CommonFunctions;
using IPCSoftware.Common.UIClientComm;
using IPCSoftware.Services;
using IPCSoftware.UI.CommonViews.ViewModels;
using IPCSoftware.UI.CommonViews;
using IPCSoftware.UI.CommonViews.Views;
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
using IPCSoftware.Services.AppLoggerServices;
using IPCSoftware.Services.ConfigServices;
using IPCSoftware.App.Services;
using IPCSoftware.Shared.Models.ConfigModels;
using Microsoft.Extensions.DependencyInjection;
using System.ComponentModel.Design.Serialization;
using System.IO;

// Aliases for app-specific types (will be migrated in later phases)
using OEEDashboard = IPCSoftware.App.Views.OEEDashboard;
using OEEDashboardViewModel = IPCSoftware.App.ViewModels.OEEDashboardViewModel;
using ManualOperationView = IPCSoftware.App.Views.ManualOperationView;
using ManualOpViewModel = IPCSoftware.App.ViewModels.ManualOpViewModel;
using AeLimitView = IPCSoftware.App.Views.AeLimitView;
using AeLimitViewModel = IPCSoftware.App.ViewModels.AeLimitViewModel;
using ProductSettingsView = IPCSoftware.App.Views.ProductSettingsView;
using FullImageView = IPCSoftware.UI.CommonViews.Views.FullImageView;  // ? Migrated
using DashboardDetailWindow = IPCSoftware.UI.CommonViews.Views.DashboardDetailWindow;  // ? Migrated
using FullImageViewModel = IPCSoftware.UI.CommonViews.ViewModels.FullImageViewModel;  // ? Migrated
using DashboardDetailViewModel = IPCSoftware.UI.CommonViews.ViewModels.DashboardDetailViewModel;  // ? NEW

namespace IPCSoftware.App.DI
{
    public static class ServiceRegistration
    {
        public static void RegisterServices(IServiceCollection services)
        {
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
            services.AddSingleton<ITcpTrafficLogger, TcpTrafficLogger>();
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
            services.AddSingleton(sp =>
                   {
                       var logger = sp.GetRequiredService<IAppLogger>();
                       return new UiListener(5050, logger);
                   });
            services.AddSingleton<IMessagePublisher>(sp => sp.GetRequiredService<UiListener>());
            services.AddSingleton<IApiTestSettingsService, ApiTestSettingsService>();
            services.AddSingleton<IAuthService, AuthService>();
            services.AddSingleton<IAeLimitService, AeLimitService>();
            services.AddSingleton<INavigationService, NavigationService>();
            services.AddSingleton<IDialogService, DialogService>();
            services.AddSingleton<ILogManagerService, LogManagerService>();
            services.AddSingleton<IShiftManagementService, ShiftManagementService>();
            services.AddSingleton<ILogConfigurationService, LogConfigurationService>();
            services.AddSingleton<IAlarmConfigurationService, AlarmConfigurationService>();
            services.AddSingleton<IUserManagementService, UserManagementService>();
            services.AddSingleton<IProductConfigurationService, ProductConfigurationService>();
            services.AddSingleton<ILogService, LogService>();

   // ========== MAIN VIEWMODELS ==========
            services.AddSingleton<RibbonViewModel>();
            services.AddSingleton<MainWindowViewModel>();
            services.AddTransient<OEEDashboardViewModel>();
            services.AddSingleton<UiTcpClient>();
            services.AddSingleton<ShiftResetService>();

            // ========== COMMON VIEWS & VIEWMODELS ==========
            services.AddTransient<ShiftConfigurationViewModel>();
            services.AddTransient<ShiftConfigurationView>();
            services.AddTransient<LogListViewModel>();
            services.AddTransient<LogConfigurationViewModel>();
            services.AddTransient<StartupConditionView>();
            services.AddTransient<StartupConditionViewModel>();
            services.AddTransient<DeviceListViewModel>();
            services.AddTransient<DeviceConfigurationViewModel>();
            services.AddTransient<DeviceDetailViewModel>();
            services.AddTransient<DeviceInterfaceConfigurationViewModel>();
            services.AddTransient<CameraDetailViewModel>();
            services.AddTransient<CameraInterfaceConfigurationViewModel>();
            services.AddTransient<AeLimitView>();
            services.AddTransient<AeLimitViewModel>();
            services.AddTransient<AboutView>();
            services.AddTransient<AboutViewModel>();
            services.AddTransient<ProductionImageView>();
            services.AddTransient<ProductionImageViewModel>();
            services.AddTransient<ProductSettingsView>();
            services.AddTransient<IPCSoftware.UI.CommonViews.ProductSettingsView>();
            services.AddTransient<ProductSettingsViewModel>();
            services.AddTransient<AlarmLogView>();
            services.AddTransient<AlarmLogViewModel>();
            services.AddTransient<AlarmListViewModel>();
            services.AddTransient<AlarmConfigurationViewModel>();
            services.AddTransient<BackupService>();
            services.AddTransient<TagConfigLoader>();
            services.AddTransient<UserListViewModel>();
            services.AddTransient<UserConfigurationViewModel>();
            services.AddTransient<AlarmView>();
            services.AddSingleton<AlarmViewModel>();
            services.AddSingleton<AlarmService>();
            services.AddTransient<PLCTagListViewModel>();
            services.AddTransient<PLCTagConfigurationViewModel>();
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
            services.AddTransient<LogListView>();
            services.AddTransient<LogConfigurationView>();
            services.AddTransient<DeviceListView>();
            services.AddTransient<DeviceConfigurationView>();
            services.AddTransient<DeviceDetailView>();
            services.AddTransient<DeviceInterfaceConfigurationView>();
            services.AddTransient<CameraDetailView>();
            services.AddTransient<CameraInterfaceConfigurationView>();
            services.AddTransient<AlarmListView>();
            services.AddTransient<AlarmConfigurationView>();
            services.AddTransient<UserListView>();
            services.AddTransient<UserConfigurationView>();
            services.AddTransient<ModeOfOperation>();
            services.AddTransient<ManualOperationView>();
            services.AddTransient<ModeOfOperationViewModel>();
            services.AddTransient<ManualOpViewModel>();
            services.AddTransient<PLCTagListView>();
            services.AddTransient<PLCTagConfigurationView>();
            services.AddTransient<LogViewerViewModel>();
            services.AddTransient<LoginViewModel>();
            services.AddTransient<LoginView>();
            services.AddTransient<TagControlView>();
            services.AddTransient<TagControlViewModel>();
            services.AddTransient<SystemSettingView>();
            services.AddTransient<SystemSettingViewModel>();
            services.AddTransient<IPLCService, PlcService>();
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
