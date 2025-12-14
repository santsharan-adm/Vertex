using IPCSoftware.Core.Interfaces;
using IPCSoftware.Core.Interfaces.CCD;
using IPCSoftware.CoreService;
using IPCSoftware.CoreService.Services.CCD;
using IPCSoftware.Services.ConfigServices;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.WindowsServices;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using IPCSoftware.CoreService.Services.UI; // <-- UiListener is here!

namespace IPCSoftware.CoreService
{
    public class Program
    {
        public static void Main(string[] args)
        {
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

                        // 🚨 CRITICAL DI FIX (Step 50): Use a Factory method to inject the port into UiListener.
                        services.AddSingleton<UiListener>(sp =>
                        {
                            var config = sp.GetRequiredService<IConfiguration>();
                            // Read port from configuration, default to a standard port if missing
                            int port = config.GetValue<int>("CoreServiceSettings:UiPort", 5050);
                            return new UiListener(port);
                        });
                        // Register the SAME instance as the IMessagePublisher interface.
                        services.AddSingleton<IMessagePublisher>(provider => provider.GetRequiredService<UiListener>());


                        // Assuming you can inject IConfiguration to get the data path:
                        services.AddSingleton<IAlarmConfigurationService>(sp =>
                        {
                            var config = sp.GetRequiredService<IConfiguration>();
                            string dataFolder = config.GetValue<string>("Config:DataFolder");
                            return new AlarmConfigurationService(dataFolder);
                        });

                        // 3. Hosted Services
                        services.AddHostedService<Worker>();

                        // Removed unused singletons
                    })
                    .Build();

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
}