using IPCSoftware.CoreService.Services.Dashboard;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Threading;
using System.Threading.Tasks;

namespace IPCSoftware.CoreService
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly DashboardInitializer _dashboard; 

        
        public Worker(ILogger<Worker> logger, DashboardInitializer dashboard)
        {
            _logger = logger;
            _dashboard = dashboard; 
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogWarning("=== WORKER EXECUTE CALLED ===");
            _logger.LogInformation("Dashboard Engine Starting...");


            
            _dashboard.Start();

            return Task.CompletedTask;
        }
    }
}