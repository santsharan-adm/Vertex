using IPCSoftware.CoreService.Services;
using IPCSoftware.CoreService.Services.Dashboard;
using IPCSoftware.CoreService.Services.PLC;
using IPCSoftware.Shared.Models.ConfigModels;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Threading;
using System.Threading.Tasks;

namespace IPCSoftware.CoreService
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private PLCClientManager _plcManager;
        private  DashboardInitializer _dashboard;

        private List<DeviceInterfaceModel> _plcDevices;

        public Worker(ILogger<Worker> logger)
        {
            _logger = logger;
            //_dashboard = dashboard; 
            

        }

        //protected override Task ExecuteAsync(CancellationToken stoppingToken)
        //{
        //    var tagLoader = new TagConfigLoader();

        //    _plcManager = new PLCClientManager(devices, tags);
        //    //var tags = tagLoader.Load(tagConfigPath);

        //    // ----------------------------------------------------------
        //    // STEP 1: Locate IPCSoftware.App/Data folder dynamically
        //    // ----------------------------------------------------------

        //    // exePath = ...\IPCSoftware.CoreService\bin\Debug\net8.0-windows\
        //    var exePath = AppContext.BaseDirectory;

        //    // Move upward from CoreService → bin → Debug → net8 → CoreService project folder
        //    var coreServiceFolder = Directory.GetParent(exePath)     // net8.0-windows
        //                                    .Parent                 // Debug
        //                                    .Parent                 // bin
        //                                    .Parent;                // IPCSoftware.CoreService

        //    // Move to solution root
        //    var rootFolder = coreServiceFolder.Parent;               // root folder containing both projects

        //    // Now point to the App project's Data folder
        //    var appDataFolder = Path.Combine(
        //        rootFolder.FullName,
        //        "IPCSoftware.App",
        //        "bin",
        //        "Debug",
        //        "net8.0-windows",
        //        "Data"
        //    );

        //    var configPath = Path.Combine(appDataFolder, "DeviceInterfaces.csv");

        //    Console.WriteLine("Reading PLC Config from: " + configPath);

        //    // ----------------------------------------------------------
        //    // STEP 2: Load PLC configuration CSV
        //    // ----------------------------------------------------------

        //    var loader = new DeviceConfigLoader();
        //    _plcDevices = loader.Load(configPath);   // store globally

        //    Console.WriteLine($"Loaded {_plcDevices.Count} PLC devices.");

        //    // ----------------------------------------------------------
        //    // STEP 3: Core Service startup logs
        //    // ----------------------------------------------------------

        //    _logger.LogWarning("=== WORKER EXECUTE CALLED ===");
        //    _logger.LogInformation("Dashboard Engine Starting...");

        //    // ----------------------------------------------------------
        //    // STEP 4: Start Dashboard/PLC Engine
        //    // ----------------------------------------------------------
        //    _dashboard = new DashboardInitializer(_plcDevices);
        //    _dashboard.Start();

        //    return Task.CompletedTask;
        //}

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // STEP 1: Calculate correct App/Data path
            var exePath = AppContext.BaseDirectory;

            var root = Directory.GetParent(exePath)     // net8.0-windows
                                .Parent                 // Debug
                                .Parent                 // bin
                                .Parent;                // IPCSoftware.CoreService

            var appDataFolder = Path.Combine(root.Parent.FullName,
                                             "IPCSoftware.App",
                                             "bin",
                                             "Debug",
                                             "net8.0-windows",
                                             "Data");

            var configPath = Path.Combine(appDataFolder, "DeviceInterfaces.csv");
            var plcTagPath = Path.Combine(appDataFolder, "PLCTags.csv");

            Console.WriteLine("Reading PLC Config from: " + configPath);

            // STEP 2: Load device interfaces
            var deviceLoader = new DeviceConfigLoader();
            var devices = deviceLoader.Load(configPath);

            // STEP 3: Load modbus tag configurations
            var tagLoader = new TagConfigLoader();
            var tags = tagLoader.Load(plcTagPath);

            // STEP 4: Create PLC manager
            _plcManager = new PLCClientManager(devices, tags);

            // STEP 5: Start Dashboard engine
            _dashboard = new DashboardInitializer(_plcManager);
            _dashboard.Start();

            return Task.CompletedTask;
        }


    }
}