using IPCSoftware.Core.Interfaces;
using IPCSoftware.CoreService.Services;
using IPCSoftware.CoreService.Services.Algorithm;
using IPCSoftware.CoreService.Services.CCD;
using IPCSoftware.CoreService.Services.Dashboard;
using IPCSoftware.CoreService.Services.PLC;
using IPCSoftware.Services;
using IPCSoftware.Shared.Models.ConfigModels;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Sources;

namespace IPCSoftware.CoreService
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly IPLCTagConfigurationService _tagService;
        private readonly IDeviceConfigurationService _deviceService;
        private readonly IConfiguration _configuration;
        private readonly CCDTriggerService _ccdTrigger;
        private readonly CameraFtpService _cameraFtpService;



        // Removed _plcManager and _dashboard fields; they will be local or managed by DashboardInitializer

        public Worker(ILogger<Worker> logger, 
            IPLCTagConfigurationService tagService,
            CCDTriggerService ccdTrigger,
            IDeviceConfigurationService deviceService, 
            IConfiguration configuration,
            CameraFtpService cameraFtpService)
        {
            _deviceService = deviceService;
            _tagService = tagService;
            _logger = logger;
            _configuration = configuration;
            _ccdTrigger = ccdTrigger;
            _cameraFtpService = cameraFtpService;

        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                // STEP 1 & 2: Calculate Paths and Load Configuration (Logic retained)
                // Assuming path calculation and DeviceConfigLoader logic runs successfully here...

              /*  string dataFolderName = _configuration.GetValue<string>("Config:DataFolder") ?? "Data";
                string deviceFileName = _configuration.GetValue<string>("Config:DeviceInterfacesFileName") ?? "DeviceInterfaces.csv";
                //string tagFileName = _configuration.GetValue<string>("Config:PlcTagsFileName") ?? "PLCTags.csv";

                var appRootPath = AppContext.BaseDirectory;
                var appDataFolder = Path.Combine(appRootPath, dataFolderName);
                var configPath = Path.Combine(appDataFolder, deviceFileName);

                var deviceLoader = new DeviceConfigLoader();
                var devices = deviceLoader.Load(configPath);*/

                var devices = await _deviceService.GetPlcDevicesAsync();

                var cameras = _deviceService.GetCameraDevicesAsync().GetAwaiter().GetResult();
                _logger.LogInformation($"Loaded {devices.Count} PLC devices.");
                _logger.LogInformation($"Loaded {cameras.Count} cameras devices.");

                // STEP 3: Load modbus tag configurations (via service)
                var tags = await _tagService.GetAllTagsAsync();
                _logger.LogInformation($"Loaded {tags.Count} Modbus tags.");

                // STEP 4: Initialize PLC manager and AlgorithmService MANUALLY
                // These instances are now created by the Worker, not the DI container.
                var plcManager = new PLCClientManager(devices, tags);

                var algoService = new AlgorithmAnalysisService(tags);

                // --- CRITICAL FIX: Make instances accessible to the Watcher Service ---
                // We must use a static holder to share these instances with other services 
                // that rely on DI (like TagChangeWatcherService).
                SharedServiceHost.Initialize(plcManager, algoService);


                CameraInterfaceModel myCamera = cameras.FirstOrDefault();

                if (myCamera?.Enabled == true)
                {
                    Console.WriteLine("Starting Camera FTP Service...");

                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            await _cameraFtpService.StartAsync(myCamera);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Camera FTP Service failed: {ex}");
                        }
                    });
                }
                else
                {
                    Console.WriteLine("Camera FTP Service is disabled in configuration.");
                }


                // STEP 5: Start Dashboard engine
                var dashboard = new DashboardInitializer(plcManager, tags, _ccdTrigger);
                await dashboard.StartAsync();

                // STEP 6: Start Camera FTP Service
              //  var myCamera = cameras[0]; // new CameraInterfaceModel();
         


                // STEP 6: BLOCK until service stops
                await Task.Delay(Timeout.Infinite, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "FATAL ERROR during Core Service initialization.");
               // throw;
            }

            finally
            {
                // Cleanup: Stop camera service if running
                if (_cameraFtpService.IsRunning)
                {
                    await _cameraFtpService.StopAsync();
                }
            }
        }
    }
}





// NEW: A simple static class to hold the runtime-initialized services
// This must be placed in a shared file or the same file for now.
public static class SharedServiceHost
{
    public static PLCClientManager? PlcManager { get; private set; }
    public static AlgorithmAnalysisService? AlgorithmService { get; private set; }

    public static void Initialize(PLCClientManager manager, AlgorithmAnalysisService algo)
    {
        PlcManager = manager;
        AlgorithmService = algo;
    }
}