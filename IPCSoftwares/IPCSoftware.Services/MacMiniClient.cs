/*using IPCSoftware.Core.Interfaces.AppLoggerInterface;
using IPCSoftware.Shared.Models;
using IPCSoftware.Shared.Models.ConfigModels;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IPCSoftware.Services
{
    public interface IMacMiniClient
    {
        bool IsConnected { get; }
        Task<List<bool>> GetCavityStatusesAsync();
        Task<MacMiniDataModel> GetRawStatusDataAsync();
    }

    /// <summary>
    /// File-based Mac Mini client that reads status from a shared JSON file.
    /// Implements connection heartbeat via file LastWriteTime monitoring.
    /// </summary>
    public class SharedFileMacMiniClient : IMacMiniClient
    {
        private readonly string _sharedFilePath;
        private readonly IAppLogger _logger;
        private DateTime _lastFileWriteTime;
        private bool _isConnected;
        private readonly double _timeoutSeconds;

        public bool IsConnected => _isConnected;

        public SharedFileMacMiniClient(
            IOptions<CcdSettings> settings,
            IAppLogger logger)
        {
            _logger = logger;
            // Path to Mac Mini shared JSON file
            _sharedFilePath = Path.Combine(settings.Value.MacMiniSharedPath, "status.json");
            _timeoutSeconds = 5.0; // Configurable timeout
        }

        /// <summary>
        /// Gets cavity statuses as boolean array (Index = Cavity-1).
        /// Returns all NG (false) if Mac Mini disconnected.
        /// </summary>
        public async Task<List<bool>> GetCavityStatusesAsync()
        {
            // Default: All NG
            var result = new List<bool>(Enumerable.Repeat(false, 12));

            try
            {
                // 1. CONNECTION CHECK
                if (!File.Exists(_sharedFilePath))
                {
                    if (_isConnected)
                    {
                        _isConnected = false;
                        _logger.LogWarning(
                            $"[MacMini] File not found: {_sharedFilePath}. Connection LOST.",
                            LogType.Diagnostics);
                    }
                    return result;
                }

                // 2. HEARTBEAT CHECK (Last Write Time)
                DateTime currentWriteTime = File.GetLastWriteTime(_sharedFilePath);
                double secondsSinceUpdate = (DateTime.Now - currentWriteTime).TotalSeconds;

                if (secondsSinceUpdate > _timeoutSeconds)
                {
                    if (_isConnected)
                    {
                        _isConnected = false;
                        _logger.LogWarning(
                            $"[MacMini] Heartbeat LOST. File not updated for {secondsSinceUpdate:F1}s (threshold: {_timeoutSeconds}s).",
                            LogType.Diagnostics);
                    }
                    return result;
                }

                // Connection is healthy
                if (!_isConnected)
                {
                    _isConnected = true;
                    _logger.LogInfo("[MacMini] Connection ESTABLISHED.", LogType.Audit);
                }

                _lastFileWriteTime = currentWriteTime;

                // 3. READ & PARSE JSON
                string jsonContent = await ReadFileWithRetryAsync(_sharedFilePath);
                if (string.IsNullOrEmpty(jsonContent))
                {
                    return result;
                }

                var data = JsonConvert.DeserializeObject<MacMiniDataModel>(jsonContent);
                if (data == null)
                {
                    _logger.LogError("[MacMini] Failed to deserialize JSON.", LogType.Diagnostics);
                    return result;
                }

                // 4. MAP OK CAVITIES TO BOOLEAN ARRAY
                if (data.OkCavities != null && data.OkCavities.Count > 0)
                {
                    foreach (int cavityId in data.OkCavities)
                    {
                        if (cavityId >= 1 && cavityId <= 12)
                        {
                            result[cavityId - 1] = true; // Index 0 = Cavity 1
                        }
                    }

                    _logger.LogInfo(
                        $"[MacMini] Status Read: {string.Join(",", data.OkCavities.OrderBy(x => x))} are OK",
                        LogType.Diagnostics);
                }
            }
            catch (Exception ex)
            {
                _isConnected = false;
                _logger.LogError($"[MacMini] Read Error: {ex.Message}", LogType.Diagnostics);
            }

            return result;
        }

        /// <summary>
        /// Returns raw Mac Mini data model for advanced processing.
        /// </summary>
        public async Task<MacMiniDataModel> GetRawStatusDataAsync()
        {
            try
            {
                if (!File.Exists(_sharedFilePath))
                {
                    return null;
                }

                string jsonContent = await ReadFileWithRetryAsync(_sharedFilePath);
                if (string.IsNullOrEmpty(jsonContent))
                {
                    return null;
                }

                return JsonConvert.DeserializeObject<MacMiniDataModel>(jsonContent);
            }
            catch (Exception ex)
            {
                _logger.LogError($"[MacMini] GetRawStatusData Error: {ex.Message}", LogType.Diagnostics);
                return null;
            }
        }

        /// <summary>
        /// Reads file with retry logic to handle file-in-use scenarios.
        /// </summary>
        private async Task<string> ReadFileWithRetryAsync(string filePath, int maxRetries = 3)
        {
            for (int i = 0; i < maxRetries; i++)
            {
                try
                {
                    using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    using (var sr = new StreamReader(fs))
                    {
                        return await sr.ReadToEndAsync();
                    }
                }
                catch (IOException) when (i < maxRetries - 1)
                {
                    await Task.Delay(50); // Wait before retry
                }
            }

            throw new IOException($"Unable to read file after {maxRetries} retries: {filePath}");
        }
    }
}*/