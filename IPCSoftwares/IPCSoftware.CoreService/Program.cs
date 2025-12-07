using IPCSoftware.Core.Interfaces;
using IPCSoftware.CoreService;
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
                .UseWindowsService()
                .ConfigureServices((hostContext, services) =>
                {
                    // 1. Configuration/Logging
                    services.AddSingleton<IConfiguration>(hostContext.Configuration);

                    // 2. Configuration Service (Resolvable by DI)
                    services.AddSingleton<IPLCTagConfigurationService, PLCTagConfigurationService>();
                    services.AddSingleton<IDeviceConfigurationService, DeviceConfigurationService>();

                    // 3. Hosted Services (These are the actual workers/watchers)
                    services.AddHostedService<Worker>();
                    // services.AddHostedService<TagChangeWatcherService>(); // Add this when ready

                    // 🛑 CRITICAL FIX: Removed the following lines that caused the AggregateException:
                    // services.AddSingleton<PLCClientManager>();
                    // services.AddSingleton<AlgorithmAnalysisService>();
                })
                .Build();

            host.Run();



        }
    }
}