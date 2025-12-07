using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using IPCSoftware.Core.Interfaces;
using System;
using System.Collections.Generic;

// Note: Ensure SharedServiceHost is defined and accessible.
// Note: Ensure IPCSoftware.CoreService.Services.PLC and .Algorithm namespaces are referenced.

namespace IPCSoftware.CoreService.Services.Config
{
    public class TagChangeWatcherService : BackgroundService
    {
        private readonly ILogger<TagChangeWatcherService> _logger;
        private readonly IPLCTagConfigurationService _tagService;
        private readonly FileSystemWatcher _watcher;
        private readonly string _tagFilePath;
        private Timer? _reloadTimer;

        // CRITICAL FIX: Removed PLCClientManager and AlgorithmAnalysisService from the constructor.
        // We now rely on the Worker to build and store them in SharedServiceHost.
        public TagChangeWatcherService(
            ILogger<TagChangeWatcherService> logger,
            IConfiguration configuration,
            IPLCTagConfigurationService tagService)
        {
            _logger = logger;
            _tagService = tagService;

            // Use the configuration path logic established in Worker.cs
            string dataFolderName = configuration.GetValue<string>("Config:DataFolder") ?? "Data";
            string tagFileName = configuration.GetValue<string>("Config:PlcTagsFileName") ?? "PLCTags.csv";

            var appRootPath = AppContext.BaseDirectory;
            var appDataFolder = Path.Combine(appRootPath, dataFolderName);
            _tagFilePath = Path.Combine(appDataFolder, tagFileName);

            // Setup the FileSystemWatcher
            _watcher = new FileSystemWatcher(appDataFolder)
            {
                Filter = tagFileName,
                EnableRaisingEvents = true,
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName
            };

            // Set up event handlers
            _watcher.Changed += OnTagFileChanged;
            _watcher.Renamed += OnTagFileChanged;
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Tag Change Watcher started, monitoring: {path}", _tagFilePath);
            return Task.CompletedTask;
        }

        private void OnTagFileChanged(object sender, FileSystemEventArgs e)
        {
            // Debounce: Prevents multiple rapid reloads often triggered by file saving.
            _reloadTimer?.Dispose();
            // Using Task.Run to ensure the heavy I/O operation doesn't block the timer thread
            _reloadTimer = new Timer(async _ =>
            {
                await ReloadConfiguration();
            }, null, 500, Timeout.Infinite); // Wait 500ms before reloading
        }

        private async Task ReloadConfiguration()
        {
            _logger.LogWarning("PLCTags.csv file change detected. Attempting configuration reload...");

            // 1. Check if the core services (PLC Manager/Algo Service) have been initialized by the Worker.
            var manager = SharedServiceHost.PlcManager;
            var algoService = SharedServiceHost.AlgorithmService;

            if (manager == null || algoService == null)
            {
                _logger.LogWarning("Initialization incomplete. Cannot reload configuration yet.");
                return;
            }

            try
            {
                // 2. Reload the configuration from CSV
                var newTags = await _tagService.ReloadTagsAsync();

                // 3. Update all dependent services with the new tags
                manager.UpdateTags(newTags);
                algoService.UpdateTags(newTags);

                _logger.LogInformation("Configuration reloaded successfully. {count} tags updated.", newTags.Count);
            }
            catch (Exception ex)
            {
                // Log and continue running the service.
                _logger.LogError(ex, "Failed to reload PLC tag configuration from CSV.");
            }
        }

        public override void Dispose()
        {
            _watcher.Dispose();
            _reloadTimer?.Dispose();
            base.Dispose();
        }
    }
}