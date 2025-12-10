

using IPCSoftware.Shared.Models;
using IPCSoftware.Shared.Models.ConfigModels;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;


namespace IPCSoftware.CoreService.Services.CCD
{
    /// <summary>
    /// Service to handle FTP communication with cameras
    /// Can be started from the main Worker or run independently
    /// </summary>
    public class CameraFtpService
    {
        private readonly ILogger<CameraFtpService> _logger;
        private readonly IConfiguration _configuration;
        private FtpClient _ftpClient;
        private string _downloadPath;
        private int _pollingIntervalSeconds;
        private CancellationTokenSource _cancellationTokenSource;
        private Task _backgroundTask;
        private bool _isRunning;

        public CameraFtpService(ILogger<CameraFtpService> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
        }

        /// <summary>
        /// Initializes and starts the FTP service
        /// </summary>
        public async Task StartAsync( CameraInterfaceModel camera)
        {
            if (_isRunning)
            {
                _logger.LogWarning("CameraFtpService is already running.");
                return;
            }

            try
            {
                // Load configuration from appsettings.json
                //  string cameraIp = _configuration.GetValue<string>("Camera:FtpHost") ?? "192.168.1.100";
                //  int ftpPort = _configuration.GetValue<int>("Camera:FtpPort", 21);
                //string ftpUsername = _configuration.GetValue<string>("Camera:FtpUsername") ?? "admin";
                //  string ftpPassword = _configuration.GetValue<string>("Camera:FtpPassword") ?? "admin";
            //    string downloadFolder = camera.LocalDirectory;// _configuration.GetValue<string>("Camera:DownloadFolder") ?? "CameraFiles";
                _pollingIntervalSeconds = ConstantValues.PollingIntervalFTP;// _configuration.GetValue<int>("Camera:PollingIntervalSeconds", 30);

                // Setup download path
              //  var appRootPath = AppContext.BaseDirectory;
                _downloadPath = camera.LocalDirectory;// Path.Combine(appRootPath, downloadFolder);
                Directory.CreateDirectory(_downloadPath);

                // Create FTP client
                var ftpLogger = _logger as ILogger<FtpClient>;
                _ftpClient = new FtpClient(camera);

              //  _logger.LogInformation($"CameraFtpService configured: Host={cameraIp}:{ftpPort}, Download Path={_downloadPath}");

                // Test connection
                if (!await _ftpClient.TestConnectionAsync())
                {
                    //_logger.LogWarning("Initial FTP connection test failed. Service will continue and retry...");
                }

                // Start background processing
                _cancellationTokenSource = new CancellationTokenSource();
                _backgroundTask = Task.Run(() => ExecuteAsync(_cancellationTokenSource.Token));
                _isRunning = true;

               // Console.WriteLine("CameraFtpService started successfully at: {time}", DateTimeOffset.Now);
            }
            catch (Exception ex)
            {
                Console.WriteLine( "Failed to start CameraFtpService");
                //throw;
            }
        }


        /// <summary>
        /// Stops the FTP service gracefully
        /// </summary>
        public async Task StopAsync()
        {
            if (!_isRunning)
            {
                return;
            }

            Console.WriteLine("Stopping CameraFtpService...");

            _cancellationTokenSource?.Cancel();

            if (_backgroundTask != null)
            {
                await _backgroundTask;
            }

            _cancellationTokenSource?.Dispose();
            _isRunning = false;

            Console.WriteLine("CameraFtpService stopped at: {time}", DateTimeOffset.Now);
        }

        private async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            Console.WriteLine("CameraFtpService background task started");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ProcessCameraFilesAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    Console.WriteLine( "Error processing camera files");
                }

                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(_pollingIntervalSeconds), stoppingToken);
                }
                catch (TaskCanceledException)
                {
                    // Expected when stopping
                    break;
                }
            }

            Console.WriteLine("CameraFtpService background task completed");
        }

        private async Task ProcessCameraFilesAsync(CancellationToken cancellationToken)
        {
            _logger.LogDebug("Checking for new camera files...");

            try
            {
                // List files on the camera
                var files = await _ftpClient.ListDirectoryAsync("/");

                if (files.Count == 0)
                {
                    _logger.LogDebug("No files found on camera FTP server");
                    return;
                }

                _logger.LogInformation($"Found {files.Count} items on camera FTP server");

                foreach (var file in files)
                {
                    if (cancellationToken.IsCancellationRequested)
                        break;

                    // Filter for image/video files
                    if (IsMediaFile(file))
                    {
                        await DownloadAndProcessFileAsync(file);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ProcessCameraFilesAsync");
            }
        }

        private async Task DownloadAndProcessFileAsync(string remoteFileName)
        {
            try
            {
                string localFilePath = Path.Combine(_downloadPath, remoteFileName);

                // Skip if already downloaded
                if (File.Exists(localFilePath))
                {
                    _logger.LogTrace($"File already exists locally: {remoteFileName}");
                    return;
                }

                _logger.LogInformation($"Downloading: {remoteFileName}");

                bool success = await _ftpClient.DownloadFileAsync(remoteFileName, localFilePath);

                if (success)
                {
                    _logger.LogInformation($"Successfully downloaded: {remoteFileName} ({new FileInfo(localFilePath).Length} bytes)");

                    // Process the file
                    await ProcessDownloadedFileAsync(localFilePath);


                    //********* Auto Delete post *********//
                      // await _ftpClient.DeleteFileAsync(remoteFileName);



                    // Optionally delete from camera after successful download and processing
                    //bool autoDelete = _configuration.GetValue<bool>("Camera:AutoDeleteAfterDownload", false);
                    //if (autoDelete)
                    //{
                    //    _logger.LogInformation($"Deleted from camera: {remoteFileName}");
                    //}
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error downloading file: {remoteFileName}");
            }
        }

        private async Task ProcessDownloadedFileAsync(string filePath)
        {
            // Add your custom file processing logic here
            // Examples:
            // - Trigger image analysis via CCD system
            // - Store file path in database
            // - Update PLC tags
            // - Forward to algorithm service
            // - Trigger inspection workflow

            _logger.LogInformation($"Processing file: {Path.GetFileName(filePath)}");

            // Example: You could integrate with your existing services
            // var plcManager = SharedServiceHost.PlcManager;
            // var algoService = SharedServiceHost.AlgorithmService;
            // if (plcManager != null)
            // {
            //     // Write to PLC that new image is available
            //     await plcManager.WriteTagAsync("CameraImageReady", true);
            // }

            await Task.CompletedTask;
        }

        private bool IsMediaFile(string fileName)
        {
            string ext = Path.GetExtension(fileName).ToLowerInvariant();
            return ext == ".jpg" || ext == ".jpeg" || ext == ".png" ||
                   ext == ".bmp" || ext == ".tiff" || ext == ".tif" ;
        }

        /// <summary>
        /// Manually trigger a camera file check (useful for testing or on-demand checks)
        /// </summary>
        public async Task TriggerCheckAsync()
        {
            if (!_isRunning)
            {
                _logger.LogWarning("Cannot trigger check - service is not running");
                return;
            }

            _logger.LogInformation("Manual trigger: Checking for camera files...");
            await ProcessCameraFilesAsync(CancellationToken.None);
        }

        /// <summary>
        /// Get the current download path
        /// </summary>
        public string GetDownloadPath() => _downloadPath;

        /// <summary>
        /// Check if service is running
        /// </summary>
        public bool IsRunning => _isRunning;
    }
}
