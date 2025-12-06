using IPCSoftware.Core.Interfaces;
using IPCSoftware.CoreService;
using IPCSoftware.CoreService.Services.Dashboard;
using IPCSoftware.Services.ConfigServices;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.WindowsServices;


namespace IPCSoftware.CoreService
{
    public class Program
    {
        public static void Main(string[] args)
        {
            IHost host = Host.CreateDefaultBuilder(args)
                .UseWindowsService()
                .ConfigureServices(services =>
                {

                    //services.AddSingleton<DashboardInitializer>();

                    services.AddSingleton<IPLCTagConfigurationService, PLCTagConfigurationService>();

                    services.AddHostedService<Worker>();
                })
                .Build();

            host.Run();
        }
    }
}