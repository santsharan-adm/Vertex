using IPCSoftware.Core.Interfaces;
using IPCSoftware.Core.Interfaces.CCD;
using IPCSoftware.CoreService;
using IPCSoftware.CoreService.Services.CCD;
using IPCSoftware.Services.ConfigServices;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.WindowsServices;
using Microsoft.Extensions.Logging; // Added for ILogger injection if needed

namespace IPCSoftware.CoreService
{
    public class Program
    {
        public static void Main(string[] args)
        {
            IHost host = Host.CreateDefaultBuilder(args)
                        .ConfigureAppConfiguration((context, config) =>
                        {

                            var env = context.HostingEnvironment?.EnvironmentName ?? "Production";

                            // Read env var (case-insensitive on Windows)
                            var sharedConfigDir = Environment.GetEnvironmentVariable("CONFIG_DIR");

                            // Fallback to the app's base dir if not set/invalid
                            var baseDir = AppContext.BaseDirectory;
                            var configDir = !string.IsNullOrWhiteSpace(sharedConfigDir) && Directory.Exists(sharedConfigDir)
                                            ? sharedConfigDir
                                            : baseDir;

                            // 🔒 Make it deterministic
                            config.Sources.Clear();
                            config.SetBasePath(configDir);

                            // ✅ Force load from the base path (shared folder if present)
                            // Set optional:false for base so we fail fast if missing in dev
                            config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

                            // Environment-specific (Development/Production/etc.)
                            config.AddJsonFile($"appsettings.{env}.json", optional: true, reloadOnChange: true);

                           

                            config.AddEnvironmentVariables();
                            config.AddCommandLine(args);

                            // Optional: log where we're loading from (handy for diagnostics)
                            System.Console.WriteLine($"[CoreService] Config base path: {configDir}");
                            System.Console.WriteLine($"[CoreService] Environment: {env}");

                        })

                .UseWindowsService()
                .ConfigureServices((hostContext, services) =>
                {
                    // 1. Configuration/Logging
                    services.AddSingleton<IConfiguration>(hostContext.Configuration);

                    // 2. Configuration Service (Resolvable by DI)
                    services.AddSingleton<IPLCTagConfigurationService, PLCTagConfigurationService>();
                    services.AddSingleton<IDeviceConfigurationService, DeviceConfigurationService>();
                    services.AddSingleton<ICycleManagerService, CycleManagerService>();
                    services.AddSingleton<CCDTriggerService>();
                    services.AddSingleton<CameraFtpService>();


                    // 3. Hosted Services (These are the actual workers/watchers)
                    services.AddHostedService<Worker>();
                    // services.AddHostedService<TagChangeWatcherService>(); // Add this when ready

                    // 🛑 CRITICAL FIX: Removed the following lines that caused the AggregateException:
                    // services.AddSingleton<PLCClientManager>();
                    // services.AddSingleton<AlgorithmAnalysisService>();
                })
                .Build();

            host.Run();

            try
            {
                IHost host = Host.CreateDefaultBuilder(args)
                    .UseWindowsService()
                    .ConfigureServices((hostContext, services) =>
                    {
                        // 1. Configuration/Logging
                        services.AddSingleton<IConfiguration>(hostContext.Configuration);
                        
                        // 2. Configuration Service (Resolvable by DI)
                        services.AddSingleton<IPLCTagConfigurationService, PLCTagConfigurationService>();
                        services.AddSingleton<IDeviceConfigurationService, DeviceConfigurationService>();
                        services.AddSingleton<ICycleManagerService, CycleManagerService>();
                        services.AddSingleton<CCDTriggerService>();
                        services.AddSingleton<CameraFtpService>();


                        // 3. Hosted Services (These are the actual workers/watchers)
                        services.AddHostedService<Worker>();
                        // services.AddHostedService<TagChangeWatcherService>(); // Add this when ready

  
                    })
                    .Build();

                host.Run();
            }

            catch (Exception ex)
            {
                // Fallback logging if the Host fails to build or crash immediately
                // utilizing standard Console or EventLog since DI might not be ready
                Console.WriteLine($"[CRITICAL] Application startup failed: {ex.Message}");
                Console.WriteLine(ex.StackTrace);

                // If running as a service, exit code 1 indicates failure to Windows SCM
                Environment.Exit(1);
            }
        }


    }
}