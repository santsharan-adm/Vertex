// IPCSoftware.Services/AppLoggerServices/LogService.cs
using IPCSoftware.Core.Interfaces;
using IPCSoftware.Core.Interfaces.AppLoggerInterface;
using IPCSoftware.Shared.Models;
using IPCSoftware.Shared.Models.ConfigModels;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IPCSoftware.Services.AppLoggerServices
{
    public class LogService : BaseService, ILogService
    {
        private readonly ILogConfigurationService _logConfigService;

        public LogService(ILogConfigurationService logConfigService,
            IAppLogger logger) : base(logger)
        {
            _logConfigService = logConfigService;
        }

        /// <summary>
        /// Get log files for a specific LogType category
        /// Reads the data folder path from LogConfigurationService
        /// </summary>
        public async Task<List<LogFileInfo>> GetLogFilesAsync(LogType category)
        {

            return await Task.Run(async () =>
            {
                // Get config for this log type
                var config = await _logConfigService.GetByLogTypeAsync(category);

                if (config == null || !config.Enabled)
                {
                    _logger.LogError($"Log configuration for {category} not found or disabled", LogType.Error);
                    return new List<LogFileInfo>();
                }

                // Use the configured folder path
                string folderPath = config.DataFolder;

                if (!Directory.Exists(folderPath))
                {
                    _logger.LogError($"Log folder does not exist: {folderPath}", LogType.Error);
                    return new List<LogFileInfo>();
                }

                try
                {
                    var directory = new DirectoryInfo(folderPath);

                    // Get CSV files, sort by newest first
                    return directory.GetFiles("*.csv")
                        .OrderByDescending(f => f.LastWriteTime)
                        .Select(f => new LogFileInfo
                        {
                            FileName = f.Name,
                            FullPath = f.FullName,
                            LastModified = f.LastWriteTime,
                            DisplaySize = $"{f.Length / 1024} KB"
                        }).ToList();
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error reading log files from {folderPath}: {ex.Message}", LogType.Diagnostics);
                    return new List<LogFileInfo>();
                }
            });
        }

        /// <summary>
        /// Read log entries from a file
        /// </summary>
        public async Task<List<LogEntry>> ReadLogFileAsync(string filePath)
        {
            var logs = new List<LogEntry>();
            try
            {

            if (!File.Exists(filePath))
            {
                _logger.LogError($"Log file not found: {filePath}", LogType.Error);
                return logs;
            }

                var lines = await File.ReadAllLinesAsync(filePath);

                // Skip Header Row (index 0)
                foreach (var line in lines.Skip(1))
                {
                    if (string.IsNullOrWhiteSpace(line))
                        continue;

                    var parts = SplitCsvLine(line);

                    if (parts.Count >= 4)
                    {
                        //if (DateTime.TryParse(parts[0], out var timestamp))
                            if (DateTime.TryParseExact(
                            parts[0],
                            "yyyy-MM-dd HH:mm:ss:fff",
                            CultureInfo.InvariantCulture,
                            DateTimeStyles.None,
                            out var timestamp))
                            {
                            logs.Add(new LogEntry
                            {
                                Timestamp = timestamp,
                                Level = parts[1],
                                Message = parts[2].Trim('"'),
                                Source = parts[3]
                            });
                        }
                    }
                }

                return logs.OrderByDescending(x => x.Timestamp).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error reading log file {filePath}: {ex.Message}", LogType.Diagnostics);
                return logs;
            }
        }

        /// <summary>
        /// Split CSV line respecting quotes
        /// </summary>
        private List<string> SplitCsvLine(string line)
        {
            var result = new List<string>();
            bool inQuotes = false;
            string currentToken = "";

            foreach (char c in line)
            {
                if (c == '"')
                {
                    inQuotes = !inQuotes;
                }
                else if (c == ',' && !inQuotes)
                {
                    result.Add(currentToken);
                    currentToken = "";
                }
                else
                {
                    currentToken += c;
                }
            }

            result.Add(currentToken);
            return result;
        }

    }
}
