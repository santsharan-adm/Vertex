using IPCSoftware.Core.Interfaces;
using IPCSoftware.Core.Interfaces.AppLoggerInterface;
using IPCSoftware.CoreService.Bending.Service;
using IPCSoftware.Devices.Camera;
using IPCSoftware.Devices.PLC;
using IPCSoftware.Devices.UI;
using IPCSoftware.Engine;
using IPCSoftware.Shared.Models;
using IPCSoftware.Shared.Models.ConfigModels;
using Microsoft.Extensions.Options;

namespace IPCSoftware.CoreService.Bending
{
    public class Worker : BackgroundService
    {
        private readonly IAppLogger _logger;
        private readonly ILogManagerService _logManager;
        private readonly IPLCTagConfigurationService _tagService;
        private readonly IDeviceConfigurationService _deviceService;
        private readonly ConfigSettings _configuration;
        private readonly CCDTriggerServiceBending _ccdTrigger;
        private readonly CameraFtpService _cameraFtpService;
        private readonly DashboardInitializerBending _dashboard;
        private readonly AlgorithmAnalysisService _algo;
        private readonly PLCClientManager _plcManager;
        private readonly UiListener _uiListener;

        // Removed _plcManager and _dashboard fields; they will be local or managed by DashboardInitializer

        public Worker(IAppLogger logger,
            ILogManagerService logManager,
            IPLCTagConfigurationService tagService,
            AlgorithmAnalysisService algo,
            DashboardInitializerBending dashboard,
            CCDTriggerServiceBending ccdTrigger,
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
                SharedServiceHost.Initialize(_plcManager, _algo);  // ? still works via using IPCSoftware.Engine

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

                await _logManager.InitializeAsync();

                _ = Task.Run(async () =>
                {
                    while (!stoppingToken.IsCancellationRequested)
                    {
                        try
                        {
                            _logManager.CheckAndPerformBackups();
                            _logManager.CheckAndPerformPurge();
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError($"Backup Loop Error: {ex.Message}", LogType.Diagnostics);
                        }
                        await Task.Delay(60000, stoppingToken);
                    }
                }, stoppingToken);

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
                if (_cameraFtpService.IsRunning)
                {
                    await _cameraFtpService.StopAsync();
                }
            }
        }
    }
}
