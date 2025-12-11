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