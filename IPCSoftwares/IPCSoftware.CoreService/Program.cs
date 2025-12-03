using IPCSoftware.CoreService;
using IPCSoftware.CoreService.Services.Dashboard; 
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
                    


                    services.AddHostedService<Worker>();
                })
                .Build();

            host.Run();
        }
    }
}