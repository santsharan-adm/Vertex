using IPCSoftware.Core.Interfaces;
using IPCSoftware.Core.Interfaces.AppLoggerInterface;
using IPCSoftware.CoreService.Services;
using IPCSoftware.CoreService.Services.Algorithm;
using IPCSoftware.CoreService.Services.CCD;
using IPCSoftware.CoreService.Services.Dashboard;
using IPCSoftware.CoreService.Services.PLC;
using IPCSoftware.CoreService.Services.UI;
using IPCSoftware.Services;
using IPCSoftware.Services.AppLoggerServices;
using IPCSoftware.Shared.Models;
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
        private readonly IAppLogger _logger;
        private readonly ILogManagerService _logManager;
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

        public Worker(IAppLogger logger, 
            ILogManagerService logManager, 
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
            _logManager = logManager;
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
                _logger.LogInfo($"Loaded {devices.Count} PLC devices.", LogType.Diagnostics);
                _logger.LogInfo($"Loaded {cameras.Count} cameras devices.", LogType.Diagnostics);
                var tags = await _tagService.GetAllTagsAsync();
                _logger.LogInfo($"Loaded {tags.Count} Modbus tags.", LogType.Diagnostics);
                SharedServiceHost.Initialize(_plcManager, _algo);

                // --- START UI LISTENER (TCP SERVER) ---
                _logger.LogInfo("Starting UI Listener in background...", LogType.Diagnostics);
                _ = Task.Run(async () =>
                {   
                    try { await _uiListener.StartAsync(); }
                    catch (Exception ex) 
                    { 
                        _logger.LogError($"UI Listener Startup Error: {ex.Message}", LogType.Diagnostics);
                    }
                });

                await _logManager.InitializeAsync(); // Ensure configs loaded

             

                _ = Task.Run(async () =>
                {
                    while (!stoppingToken.IsCancellationRequested)
                    {
                        try
                        {
                            // Trigger Auto-Backup Check
                            _logManager.CheckAndPerformBackups();
                            _logManager.CheckAndPerformPurge();
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError($"Backup Loop Error: {ex.Message}", LogType.Diagnostics);
                        }

                        // Wait 60 seconds before next check
                        await Task.Delay(60000, stoppingToken);
                    }
                }, stoppingToken);


                /* CameraInterfaceModel myCamera = cameras.FirstOrDefault();

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
                             _logger.LogError($"Camera FTP Service failed: {ex}", LogType.Diagnostics );
                         }
                     });
                 }
                 else
                 {

                     _logger.LogError("Camera FTP Service is disabled in configuration.", LogType.Diagnostics);
                 }*/

                await _dashboard.StartAsync();
                await Task.Delay(Timeout.Infinite, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError($"FATAL ERROR during Core Service initialization: {ex.Message}", LogType.Diagnostics);
                throw;
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