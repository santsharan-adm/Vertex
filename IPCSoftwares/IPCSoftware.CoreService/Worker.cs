using IPCSoftware.Core.Interfaces;
using IPCSoftware.CoreService.Services;
using IPCSoftware.CoreService.Services.Algorithm;
using IPCSoftware.CoreService.Services.CCD;
using IPCSoftware.CoreService.Services.Dashboard;
using IPCSoftware.CoreService.Services.PLC;
using IPCSoftware.CoreService.Services.UI;
using IPCSoftware.Services;
using IPCSoftware.Shared.Models.ConfigModels;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
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
        private readonly ConfigSettings _configuration;
        private readonly CCDTriggerService _ccdTrigger;
        private readonly CameraFtpService _cameraFtpService;
        private readonly DashboardInitializer _dashboard;
        private readonly AlgorithmAnalysisService _algo;
        private readonly PLCClientManager _plcManager;
        private readonly UiListener _uiListener;

        // Removed _plcManager and _dashboard fields; they will be local or managed by DashboardInitializer

        public Worker(ILogger<Worker> logger, 
            IPLCTagConfigurationService tagService,
            AlgorithmAnalysisService algo,
            DashboardInitializer dashboard,
            CCDTriggerService ccdTrigger,
            IDeviceConfigurationService deviceService, 
            IOptions<ConfigSettings> configuration,
            CameraFtpService cameraFtpService,
            PLCClientManager plcManger,
            UiListener uiListener)
        {
            _deviceService = deviceService;
            _tagService = tagService;
            _logger = logger;
            _algo = algo;
            _plcManager = plcManger;   
            _dashboard = dashboard;
            _configuration = configuration.Value;
            _ccdTrigger = ccdTrigger;
            _cameraFtpService = cameraFtpService;
            _uiListener = uiListener;

        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                var devices = await _deviceService.GetPlcDevicesAsync();
                var cameras = _deviceService.GetCameraDevicesAsync().GetAwaiter().GetResult();
                _logger.LogInformation($"Loaded {devices.Count} PLC devices.");
                _logger.LogInformation($"Loaded {cameras.Count} cameras devices.");
                var tags = await _tagService.GetAllTagsAsync();
                _logger.LogInformation($"Loaded {tags.Count} Modbus tags.");
                SharedServiceHost.Initialize(_plcManager, _algo);

                // --- START UI LISTENER (TCP SERVER) ---
                Console.WriteLine("Starting UI Listener in background...");
                _ = Task.Run(async () =>
                {
                    try { await _uiListener.StartAsync(); }
                    catch (Exception ex) { Console.WriteLine($"UI Listener Startup Error: {ex.Message}"); }
                });

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

                await _dashboard.StartAsync();
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