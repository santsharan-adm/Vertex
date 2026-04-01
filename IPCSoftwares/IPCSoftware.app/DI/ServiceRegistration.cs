using IPCSoftware.Common.CommonFunctions;
using IPCSoftware.Common.UIClientComm;
using IPCSoftware.Services;
using IPCSoftware.UI.CommonViews.ViewModels;
using IPCSoftware.UI.CommonViews;
using IPCSoftware.UI.CommonViews.Views;
using IPCSoftware.Core.Interfaces;
using IPCSoftware.Core.Interfaces.AppLoggerInterface;
using IPCSoftware.Core.Interfaces.CCD;
using IPCSoftware.Engine;
using IPCSoftware.Devices.PLC;
using IPCSoftware.Devices.Camera;
using IPCSoftware.Devices.UI;
using IPCSoftware.Communication.External;
using IPCSoftware.Services.AppLoggerServices;
using IPCSoftware.Services.ConfigServices;
using IPCSoftware.App.Services;
using IPCSoftware.Shared.Models.ConfigModels;
using IPCSoftware.Datalogger;                           // ? Fixes: ITcpTrafficLogger, TcpTrafficLogger, IProductionDataLogger, ProductionDataLogger
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
using FullImageView = IPCSoftware.UI.CommonViews.Views.FullImageView;
using DashboardDetailWindow = IPCSoftware.UI.CommonViews.Views.DashboardDetailWindow;
using FullImageViewModel = IPCSoftware.UI.CommonViews.ViewModels.FullImageViewModel;
using DashboardDetailViewModel = IPCSoftware.UI.CommonViews.ViewModels.DashboardDetailViewModel;

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
            services.AddSingleton<IExternalInterfaceService>(sp =>  // ?
                sp.GetRequiredService<ExternalInterfaceService>());
            services.AddSingleton<ICcdConfigService, CcdConfigService>();
            services.AddSingleton<AlgorithmAnalysisService>();
            services.AddSingleton<DashboardInitializer>();
            services.AddSingleton<OeeEngine>();
            services.AddSingleton<SystemMonitorService>();
            services.AddSingleton<IAlarmHistoryService, AlarmHistoryService>();
            services.AddSingleton<ITcpTrafficLogger, TcpTrafficLogger>();          // ? Fixed
            services.AddSingleton<IProductionDataLogger>(sp =>                     // ? Fixed
            {
                var logConfigService = sp.GetRequiredService<ILogConfigurationService>();
                var initTask = logConfigService.InitializeAsync();
                initTask.Wait();
                var prodLogConfigTask = logConfigService.GetByLogTypeAsync(LogType.Production);
                prodLogConfigTask.Wait();
                var prodLogConfig = prodLogConfigTask.Result;
                if (prodLogConfig == null || !prodLogConfig.Enabled)
                    throw new InvalidOperationException("Production log configuration not found or not enabled.");
                return new ProductionDataLogger(prodLogConfig);                    // ? Fixed
            });
            services.AddSingleton<CCDTriggerServiceBase>();
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
            services.AddTransient<AlarmConfigurationView>();                       // ? Fixed: was 'addTransient' (lowercase 'a')
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
