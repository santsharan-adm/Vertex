

using IPCSoftware.App.Bending.ViewModels;
using IPCSoftware.App.Bending.Views;
using IPCSoftware.App.Services;
using IPCSoftware.Common.CommonFunctions;
using IPCSoftware.Common.UIClientComm;
using IPCSoftware.Communication.External;
using IPCSoftware.Core.Interfaces;
using IPCSoftware.Core.Interfaces.AppLoggerInterface;
using IPCSoftware.Core.Interfaces.CCD;
using IPCSoftware.CoreService.Bending;
using IPCSoftware.Datalogger;
using IPCSoftware.Devices.Camera;
using IPCSoftware.Devices.PLC;
using IPCSoftware.Devices.UI;
//using IPCSoftware.Engine;
using IPCSoftware.Services;
using IPCSoftware.Services.AppLoggerServices;
using IPCSoftware.Services.ConfigServices;
using IPCSoftware.Shared.Models;
using IPCSoftware.Shared.Models.ConfigModels;
using IPCSoftware.UI.CommonViews;
using IPCSoftware.UI.CommonViews.Services;
using IPCSoftware.UI.CommonViews.ViewModels;
using IPCSoftware.UI.CommonViews.Views;
using Microsoft.Extensions.DependencyInjection;
using System.ComponentModel.Design.Serialization;
using System.IO;
using System.Net.Quic;
//using AeLimitView = IPCSoftware.App.Views.AeLimitView;
//using AeLimitViewModel = IPCSoftware.App.ViewModels.AeLimitViewModel;
using DashboardDetailViewModel = IPCSoftware.UI.CommonViews.ViewModels.DashboardDetailViewModel;
using DashboardDetailWindow = IPCSoftware.UI.CommonViews.Views.DashboardDetailWindow;
using FullImageView = IPCSoftware.UI.CommonViews.Views.FullImageView;
using FullImageViewModel = IPCSoftware.UI.CommonViews.ViewModels.FullImageViewModel;
//using ManualOperationView = IPCSoftware.App.Views.ManualOperationView;
//using ManualOpViewModel = IPCSoftware.App.ViewModels.ManualOpViewModel;
// Aliases for app-specific types (will be migrated in later phases)
//using OEEDashboard = IPCSoftware.App.Views.OEEDashboard;
//using OEEDashboardViewModel = IPCSoftware.App.ViewModels.OEEDashboardViewModel;
//using ProductSettingsView = IPCSoftware.App.Views.ProductSettingsView;

namespace IPCSoftware.App.Bending.DI
{
    public static class ServiceRegistration
    {
        public static void RegisterServices(IServiceCollection services)
        {
            // services.AddSingleton<IAppLogger, AppLoggerService>();
            // services.AddSingleton<IPLCTagConfigurationService, PLCTagConfigurationService>();
            services.AddSingleton<IDeviceConfigurationService, DeviceConfigurationService>();

            services.AddSingleton<IAppLogger>(sp =>
            {
                var coreClient = sp.GetRequiredService<CoreClient>();
                var dialog = sp.GetRequiredService<IDialogService>();
                const string eventSource = "Bending- UI Specific";
                const string eventLog = "Application";

                return new UiErrorLogger(coreClient, dialog, eventSource, eventLog);
            });

            //  NEW: Register Observable CCD Settings Service (Singleton - shared across all services)
            services.AddSingleton<IObservableCcdSettingsService, ObservableCcdSettingsService>(); //Added by Rishabh - date - 08/04/2026//


            //services.AddSingleton<ICycleManagerService, CycleManagerServiceAOI>();
            services.AddSingleton<ExternalInterfaceService>();
            services.AddSingleton<IExternalInterfaceService>(sp =>
                sp.GetRequiredService<ExternalInterfaceService>());
            services.AddSingleton<ICcdConfigService, CcdConfigService>();
            services.AddSingleton<AlgorithmAnalysisService>();
            //services.AddSingleton<DashboardInitializerAOI>();
            //services.AddSingleton<OeeEngineAOI>();
            //services.AddSingleton<SystemMonitorService>();
            services.AddSingleton<IAlarmHistoryService, AlarmHistoryService>();
            services.AddSingleton<ITcpTrafficLogger, TcpTrafficLogger>();
            services.AddSingleton<IProductionDataLogger>(sp =>
            {
                var logConfigService = sp.GetRequiredService<ILogConfigurationService>();
                var prodLogConfig = logConfigService.GetByLogTypeAsync(LogType.Production)
                                                    .GetAwaiter().GetResult();

                if (prodLogConfig == null || !prodLogConfig.Enabled)
                    throw new InvalidOperationException("Production log configuration not found or not enabled.");
                return new ProductionDataLogger(prodLogConfig);
            });

            //  UPDATED: CCDTriggerServiceAOI now includes IObservableCcdSettingsService
            //services.AddSingleton<CCDTriggerServiceAOI>(sp =>
            //    new CCDTriggerServiceAOI(
            //        sp.GetRequiredService<ICycleManagerService>(),
            //        sp.GetRequiredService<IDeviceConfigurationService>(),
            //        sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<CcdSettings>>(),
            //        sp.GetRequiredService<IObservableCcdSettingsService>(),  //  //Added by Rishabh - date - 08/04/2026//
            //        sp.GetRequiredService<IAppLogger>()
            //    )
            //);
            //services.AddSingleton<IFileHandler, CsvManager>();
            services.AddSingleton<ConfigLoaderService>();                       //Added by Rishabh - date - 25/04/2026//
            //services.AddSingleton<DeviceConfigLoader>();                             //Added by Rishabh - date - 18/04/2026//
            //services.AddSingleton<DeviceInterfaceConfigLoader>();                    //Modified by Rishabh - date - 15/04/2026//
            //services.AddSingleton<CameraConfigLoader>();                             //Added by Rishabh - date - 15/04/2026//
            services.AddSingleton<PLCClientManager>();
            services.AddSingleton<CameraFtpService>();
            services.AddTransient<ProductionImageService>();
            //services.AddSingleton<AlarmService>();
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
            // Register base types for dependency injection
            services.AddSingleton<RibbonViewModelBase>(sp => sp.GetRequiredService<RibbonViewModelBending>());
            services.AddSingleton<RibbonViewModelBending>();

            services.AddSingleton<MainWindowViewModelBase>(sp => sp.GetRequiredService<MainWindowViewModelBending>());
            services.AddSingleton<MainWindowViewModelBending>();
            //services.AddTransient<OEEDashboardViewModel>();
            services.AddSingleton<UiTcpClient>();
            //services.AddSingleton<ShiftResetService>();

            // 4. Post-registration: Set loggers
            services.AddSingleton(sp =>
            {
                var logger = sp.GetRequiredService<IAppLogger>();
                var tcpClient = sp.GetRequiredService<UiTcpClient>();
                var coreClient = sp.GetRequiredService<CoreClient>();

                tcpClient.SetLogger(logger);
                coreClient.SetLogger(logger);

                return sp; // Dummy return
            });



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
            services.AddTransient<ServiceStartupView>();                 //Added by Rishabh -date - 15-04-2026
            services.AddTransient<ServiceStartupViewModel>();            //Added by Rishabh -date - 15-04-2026
            services.AddTransient<ServiceStartupView>();                 //Added by Rishabh -date - 15-04-2026
            services.AddTransient<ServiceStartupViewModel>(sp =>         //Added by Rishabh -date - 05-05-2026
            {
                var coreClient = sp.GetRequiredService<CoreClient>();
                var logger = sp.GetRequiredService<IAppLogger>();
                const string targetServiceName = "IPCSoftware.CoreService.Bending";

                return new ServiceStartupViewModel(coreClient, logger, targetServiceName);
            });



            //  UPDATED: CcdSettingsViewModel now includes IObservableCcdSettingsService
            services.AddTransient<CcdSettingsViewModel>(sp =>
                new CcdSettingsViewModel(
                    sp.GetRequiredService<IDeviceConfigurationService>(),
                    sp.GetRequiredService<IObservableCcdSettingsService>()  // //Added by Rishabh - date - 08/04/2026//

                )
            );

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
            //services.AddTransient<TagConfigLoader>();
            services.AddTransient<UserListViewModel>();
            services.AddTransient<UserConfigurationViewModel>();
            services.AddTransient<AlarmView>();
            services.AddSingleton<AlarmViewModel>();
            //services.AddSingleton<AlarmService>();
            services.AddTransient<PLCTagListViewModel>();
            services.AddTransient<PLCTagConfigurationViewModel>();
            services.AddTransient<ServoCalibrationView>();
            services.AddTransient<ServoCalibrationViewModel>();
            services.AddSingleton<IServoCalibrationService, ServoCalibrationService>();

            // Views
            services.AddTransient<RibbonView>();
            //services.AddTransient<OEEDashboard>();
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
            services.AddTransient<CcdSettingsView>();
            services.AddTransient<AlarmListView>();
            services.AddTransient<AlarmConfigurationView>();
            services.AddTransient<UserListView>();
            services.AddTransient<UserConfigurationView>();
            services.AddTransient<ModeOfOperation>();
            //services.AddTransient<ManualOperationView>();
            services.AddTransient<ModeOfOperationViewModel>();
            //services.AddTransient<ManualOpViewModel>();
            services.AddTransient<PLCTagListView>();
            services.AddTransient<PLCTagConfigurationView>();
            services.AddTransient<LogViewerViewModel>();
            // Register LoginViewModelBase to resolve LoginViewModelBending
            services.AddTransient<LoginViewModelBase>(sp => sp.GetRequiredService<LoginViewModelBending>());
            services.AddTransient<LoginViewModelBending>();
            services.AddTransient<LoginView>();
            services.AddTransient<TagControlView>();
            services.AddTransient<TagControlViewModel>();
            services.AddTransient<SystemSettingView>();
            services.AddTransient<SystemSettingViewModel>();
            // services.AddTransient<ServiceStartupView>();
            //services.AddTransient<ServiceStartupViewModel>();
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


            ///Registring Bending Dashaboard specific views and viewmodels
            services.AddTransient<RibbonView>();
            services.AddTransient<Bending1MonitorViewModel>();
            services.AddTransient<Bending1MonitorView>();
            services.AddTransient<Bending2MonitorViewModel>();
            services.AddTransient<Bending2MonitorView>();
            services.AddTransient<Bending3MonitorViewModel>();
            services.AddTransient<Bending3MonitorView>();
            services.AddTransient<Dashboard1ViewModel>();
            services.AddTransient<Dashboard1>();
            services.AddTransient<Dashboard2ViewModel>();
            services.AddTransient<IPCSoftware.App.Bending.Views.Dashboard2>();
            services.AddTransient<PostBendingMonitorViewModel>();
            //services.AddTransient<PostBendingMonitor>();
            services.AddTransient<WelcomePageViewModel>();
            services.AddTransient<WelcomePageView>();

        }
    }
}