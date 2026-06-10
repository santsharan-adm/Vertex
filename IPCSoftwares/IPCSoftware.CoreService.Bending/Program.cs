using IPCSoftware.Core.Interfaces;
using IPCSoftware.Core.Interfaces.AppLoggerInterface;
using IPCSoftware.Core.Interfaces.CCD;
using IPCSoftware.CoreService.Bending;
using IPCSoftware.CoreService.Bending.Service;
using IPCSoftware.Devices.Camera;
using IPCSoftware.Devices.PLC;
using IPCSoftware.Devices.UI;
using IPCSoftware.Communication.External;
using IPCSoftware.Communication.Common;
using IPCSoftware.Datalogger;
using IPCSoftware.Engine;
using IPCSoftware.Services;
using IPCSoftware.Services.AppLoggerServices;
using IPCSoftware.Services.ConfigServices;
using IPCSoftware.Shared.Models;
using IPCSoftware.Shared.Models.ConfigModels;
using IPCSoftware.Shared.Models.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace IPCSoftware.CoreService.Bending
{
    public class Program
    {
        public static void Main(string[] args)
        {
            const string appName = "Global\\IPCSoftware_CoreService_Bending_UniqueID";
            bool createdNew;

            using (var mutex = new Mutex(true, appName, out createdNew))
            {
                if (!createdNew)
                {
                    Console.WriteLine("Instance already running. Exiting...");
                    return;
                }

                try
                {
                    IHost host = Host.CreateDefaultBuilder(args)
                        .ConfigureAppConfiguration((context, config) =>
                        {
                            var env = context.HostingEnvironment?.EnvironmentName ?? "Production";

                            var sharedConfigDir = Environment.GetEnvironmentVariable("CONFIG_DIR");

                            var baseDir = AppContext.BaseDirectory;
                            var configDir = !string.IsNullOrWhiteSpace(sharedConfigDir) && Directory.Exists(sharedConfigDir)
                                            ? sharedConfigDir
                                            : baseDir;

                            config.Sources.Clear();
                            config.SetBasePath(configDir);

                            config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
                            config.AddJsonFile($"appsettings.{env}.json", optional: true, reloadOnChange: true);

                            config.AddEnvironmentVariables();
                            config.AddCommandLine(args);

                            System.Console.WriteLine($"[CoreService.Bending] Config base path: {configDir}");
                            System.Console.WriteLine($"[CoreService.Bending] Environment: {env}");
                        })
                        .UseWindowsService()
                        .ConfigureServices((hostContext, services) =>
                        {
                            services.Configure<ConfigSettings>(hostContext.Configuration.GetSection("Config"));
                            services.Configure<CcdSettings>(hostContext.Configuration.GetSection("CCD"));
                            services.Configure<ExternalSettings>(hostContext.Configuration.GetSection("External"));

                            // Core services
                            // services.AddSingleton<IPLCTagConfigurationService, PLCTagConfigurationService>();
                            // services.AddSingleton<IAppLogger, AppLoggerService>();

                            services.AddSingleton<ConfigLoaderService>();           //Added by Rishabh - date - 27/04/2026//
                            services.AddSingleton<IAppLogger>(sp =>
                            {
                                var logger = sp.GetRequiredService<ILogManagerService>();
                                const string source = "Bending-CoreService";
                                const string log = "Application";
                                return new AppLoggerService(logger, source, log);
                            }
                            );
                            services.AddSingleton<ILogManagerService, LogManagerService>();
                            services.AddSingleton<ILogConfigurationService, LogConfigurationService>();
                            services.AddSingleton<IDeviceConfigurationService, DeviceConfigurationService>();
                            services.AddSingleton<IObservableCcdSettingsService, ObservableCcdSettingsService>(); //Added by Rishabh - date - 08/04/2026//
                            services.AddSingleton<IAeLimitService, AeLimitService>();
                            services.AddSingleton<ExternalInterfaceService>();
                            services.AddSingleton<IExternalInterfaceService>(sp =>
                                sp.GetRequiredService<ExternalInterfaceService>());
                            services.AddSingleton<ICycleManagerService, CycleManagerServiceBending>();
                            services.AddSingleton<IAlarmConfigurationService, AlarmConfigurationService>();
                            services.AddSingleton<IServoCalibrationService, ServoCalibrationService>();
                            services.AddSingleton<IProductConfigurationService, ProductConfigurationService>();
                            services.AddSingleton<AlgorithmAnalysisService>();
                            services.AddSingleton<DashboardInitializerBending>();
                            services.AddSingleton<OeeEngineBending>();
                            services.AddSingleton<AlarmService>();
                            services.AddSingleton<BendingProcessService>();
                            //services.AddTransient<TagConfigLoader>();
                            services.AddTransient<BackupService>();
                            services.AddSingleton<ShiftResetService>();
                            services.AddSingleton<ITcpTrafficLogger, TcpTrafficLogger>();

                            // Production data logger
                            services.AddSingleton<IProductionDataLogger>(sp =>
                            {
                                var logConfigService = sp.GetRequiredService<ILogConfigurationService>();
                                var initTask = logConfigService.InitializeAsync();
                                initTask.Wait();

                                var logManager = sp.GetRequiredService<ILogManagerService>();
                                var initTask2 = logManager.InitializeAsync();
                                initTask2.Wait();

                                var prodLogConfigTask = logConfigService.GetByLogTypeAsync(LogType.Production);
                                prodLogConfigTask.Wait();
                                var prodLogConfig = prodLogConfigTask.Result;
                                if (prodLogConfig == null || !prodLogConfig.Enabled)
                                    throw new InvalidOperationException("Production log configuration not found or not enabled.");

                                return new ProductionDataLogger(prodLogConfig);
                            });

                            services.AddSingleton<UiListener>(sp =>
                            {
                                var logger = sp.GetRequiredService<IAppLogger>();
                                return new UiListener(5050, logger);
                            });

                            services.AddSingleton<IMessagePublisher>(sp => sp.GetRequiredService<UiListener>());

                            services.AddSingleton<SystemMonitorServiceBending>();
                           // services.AddSingleton<CCDTriggerServiceBending>();

                            services.AddSingleton<CCDTriggerServiceBending>(sp =>    new CCDTriggerServiceBending(
                                     sp.GetRequiredService<ICycleManagerService>(),
                                     sp.GetRequiredService<IDeviceConfigurationService>(),
                                     sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<CcdSettings>>(),
                                     sp.GetRequiredService<IObservableCcdSettingsService>(),   //Added by Rishabh - date - 08/04/2026//
                                     sp.GetRequiredService<IAppLogger>()
    )
);

                            services.AddSingleton<PLCClientManager>();
                            services.AddSingleton<CameraFtpService>();
                            services.AddTransient<ProductionImageService>();
                            services.AddHostedService<Worker>();
                        })
                        .Build();

                    var config = host.Services.GetRequiredService<IConfiguration>();

                    var configSettings = new ConfigSettings();
                    var ccdSettings = new CcdSettings();
                    var external = new ExternalSettings();

                    config.GetSection("Config").Bind(configSettings);
                    config.GetSection("CCD").Bind(ccdSettings);
                    config.GetSection("External").Bind(external);

                    ConstantValues.Initialize(configSettings);
                    LoadCcdSettingsAsync(host.Services).GetAwaiter().GetResult();

                    host.Run();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[CRITICAL] Application startup failed: {ex.Message}");
                    Console.WriteLine(ex.StackTrace);
                    Environment.Exit(1);
                }
            }
        }

        private static async Task LoadCcdSettingsAsync(IServiceProvider services)
        {
            try
            {
                var deviceService = services.GetRequiredService<IDeviceConfigurationService>();
                var observableSettings = services.GetRequiredService<IObservableCcdSettingsService>();
                var logger = services.GetRequiredService<IAppLogger>();
                // logger.LogInfo("[TEST] AppLogger initialized successfully", LogType.Diagnostics);  //Only for testing if AppLogger is working at this point
                await deviceService.InitializeAsync();
                // 1. Load all camera interfaces
                var cameras = await deviceService.GetCameraDevicesAsync();
                if (cameras == null || cameras.Count == 0)
                {
                    logger.LogWarning("[CoreService] No camera interfaces found in configuration.", LogType.Diagnostics);
                    return;
                }
                // 2. Get the first enabled camera (or first one if none enabled)
                var activeCamera = cameras.FirstOrDefault(c => c.Enabled) ?? cameras.FirstOrDefault();
                if (activeCamera == null)
                {
                    logger.LogWarning("[CoreService] No valid camera interface found.", LogType.Diagnostics);
                    return;
                }
                // 3. Load CCD settings into observable service
                await observableSettings.UpdateFromCameraInterfaceAsync(activeCamera);
                logger.LogInfo(
                    $"[CoreService] Loaded CCD settings from camera: {activeCamera.Name} | " +
                    $"TempImgFolder: {observableSettings.TempImgFolder}",
                    LogType.Diagnostics);
            }
            catch (Exception ex)
            {
                var logger = services.GetRequiredService<IAppLogger>();
                logger.LogError($"[CoreService] Error loading CCD settings: {ex.Message}", LogType.Diagnostics);
            }
        }
    }
}
