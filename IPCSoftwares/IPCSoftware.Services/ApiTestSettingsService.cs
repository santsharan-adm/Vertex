using IPCSoftware.Core.Interfaces;
using IPCSoftware.Core.Interfaces.AppLoggerInterface;
using IPCSoftware.Shared;
using IPCSoftware.Shared.Models;
using IPCSoftware.Shared.Models.ApiTest;
using IPCSoftware.Shared.Models.ConfigModels;
using LogTypeEnum = IPCSoftware.Shared.Models.ConfigModels.LogType;
using Microsoft.Extensions.Options;
using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace IPCSoftware.Services
{
    public class ApiTestSettingsService : IApiTestSettingsService
    {
        private readonly string _settingsPath;
        private readonly JsonSerializerOptions _serializerOptions;
        private readonly SemaphoreSlim _fileLock = new(1, 1);
        private readonly IAppLogger _logger;

        public ApiTestSettingsService(IOptions<ConfigSettings> configOptions, IAppLogger logger)
        {
            _logger = logger;
            var config = configOptions?.Value ?? new ConfigSettings();
            var dataFolder = string.IsNullOrWhiteSpace(config.DataFolder)
                ? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data")
                : config.DataFolder;

            Directory.CreateDirectory(dataFolder);
            _settingsPath = Path.Combine(dataFolder, "ApiTestSettings.json");
            _serializerOptions = new JsonSerializerOptions
            {
                WriteIndented = true
            };
        }

        public async Task<ApiTestSettings> LoadAsync()
        {
            await _fileLock.WaitAsync().ConfigureAwait(false);
            try
            {
                if (!File.Exists(_settingsPath))
                {
                    return ApiTestSettings.CreateDefault();
                }

                var json = await File.ReadAllTextAsync(_settingsPath).ConfigureAwait(false);
                var settings = JsonSerializer.Deserialize<ApiTestSettings>(json, _serializerOptions);
                return settings ?? ApiTestSettings.CreateDefault();
            }
            catch (Exception ex)
            {
                _logger?.LogError($"[API TEST] Failed to load settings: {ex.Message}", LogTypeEnum.Diagnostics);
                return ApiTestSettings.CreateDefault();
            }
            finally
            {
                _fileLock.Release();
            }
        }

        public async Task SaveAsync(ApiTestSettings settings)
        {
            if (settings == null)
            {
                throw new ArgumentNullException(nameof(settings));
            }

            await _fileLock.WaitAsync().ConfigureAwait(false);
            try
            {
                var directory = Path.GetDirectoryName(_settingsPath);
                if (!string.IsNullOrEmpty(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                var json = JsonSerializer.Serialize(settings, _serializerOptions);
                await File.WriteAllTextAsync(_settingsPath, json).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger?.LogError($"[API TEST] Failed to save settings: {ex.Message}", LogTypeEnum.Diagnostics);
                throw;
            }
            finally
            {
                _fileLock.Release();
            }
        }
    }
}
