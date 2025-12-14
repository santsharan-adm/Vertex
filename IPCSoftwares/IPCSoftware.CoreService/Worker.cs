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
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

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
        private readonly IAlarmConfigurationService _alarmConfigService;
        private readonly IMessagePublisher _publisher;
        private readonly UiListener _uiListener;

        public Worker(ILogger<Worker> logger,
            IPLCTagConfigurationService tagService,
            CCDTriggerService ccdTrigger,
            IDeviceConfigurationService deviceService,
            IConfiguration configuration,
            CameraFtpService cameraFtpService,
            IAlarmConfigurationService alarmConfigService,
            IMessagePublisher publisher,
            UiListener uiListener)
        {
            _deviceService = deviceService;
            _tagService = tagService;
            _logger = logger;
            _configuration = configuration;
            _ccdTrigger = ccdTrigger;
            _cameraFtpService = cameraFtpService;
            _alarmConfigService = alarmConfigService;
            _publisher = publisher;
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

                await _alarmConfigService.InitializeAsync();
                var alarmDefinitions = await _alarmConfigService.GetAllAlarmsAsync();
                _logger.LogInformation($"Loaded {alarmDefinitions.Count} Alarm definitions.");

                var plcManager = new PLCClientManager(devices, tags);
                var algoService = new AlgorithmAnalysisService(tags);
                SharedServiceHost.Initialize(plcManager, algoService);

                // --- START UI LISTENER (TCP SERVER) ---
                Console.WriteLine("Starting UI Listener in background...");
                _ = Task.Run(async () =>
                {
                    try { await _uiListener.StartAsync(); }
                    catch (Exception ex) { Console.WriteLine($"UI Listener Startup Error: {ex.Message}"); }
                });

                await Task.Delay(1000);

                // 🚨 FIX: Pass _uiListener explicitly to DashboardInitializer
                var dashboard = new DashboardInitializer(plcManager, tags, _ccdTrigger, alarmDefinitions, _uiListener);
                await dashboard.StartAsync();

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

                await Task.Delay(Timeout.Infinite, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "FATAL ERROR during Core Service initialization.");
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
}