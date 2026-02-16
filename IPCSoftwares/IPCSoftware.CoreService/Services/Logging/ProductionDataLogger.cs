using System;
using System.IO;
using System.Text;
using IPCSoftware.Shared.Models.Logging;
using IPCSoftware.Shared.Models.ConfigModels;
using System.Linq;

namespace IPCSoftware.CoreService.Services.Logging
{
    public interface IProductionDataLogger
    {
        void AppendRecord(ProductionDataRecord record);
    }

    public class ProductionDataLogger : IProductionDataLogger
    {
        private readonly LogConfigurationModel _config;
        private string _currentFilePath;
        private static readonly object _syncRoot = new object();

        // Removed const 13. Dynamic header generation logic below.
        private const int DefaultStationCount = 13;

        public ProductionDataLogger(LogConfigurationModel config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _currentFilePath = BuildFilePath();
            // Initial check on startup
            EnsureHeaderExists(_currentFilePath);
            PurgeOldLogsIfNeeded();
        }

        private string BuildFilePath()
        {
            var dir = _config.DataFolder;
            var name = _config.FileName;

            if (string.IsNullOrWhiteSpace(name))
                name = $"Production_{DateTime.Now:yyyyMMdd}.csv";
            else
                name = ReplaceDateTokens(name);

            if (!name.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
                name += ".csv";

            return Path.Combine(dir, name);
        }

        private void EnsureHeaderExists(string path)
        {
            lock (_syncRoot)
            {
                var directory = Path.GetDirectoryName(path);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                bool fileExists = File.Exists(path);
                bool isEmpty = false;

                if (fileExists)
                {
                    try { isEmpty = new FileInfo(path).Length == 0; } catch { isEmpty = true; }
                }

                // If file is new OR empty, write header
                if (!fileExists || isEmpty)
                {
                    var header = BuildHeaderLine();
                    // Use AppendAllText to create if missing or append if empty
                    File.WriteAllText(path, header + Environment.NewLine, Encoding.UTF8);
                }
            }
        }

        private string BuildHeaderLine()
        {
            var sb = new StringBuilder();
            sb.Append("Timestamp,2D_Code");

            // Note: Ideally pass dynamic station count here if available.
            // Using DefaultStationCount to match your existing CSV format.
            for (int i = 0; i < DefaultStationCount; i++)
            {
                sb.Append($",St{i}_result,St{i}_X,St{i}_Y,St{i}_Z");
            }

            sb.Append(",OEE,Availability,Performance,Quality,Total_IN,OK,NG,Uptime,Downtime,TotalTime,CT");
            return sb.ToString();
        }

        public void AppendRecord(ProductionDataRecord record)
        {
            if (record == null) throw new ArgumentNullException(nameof(record));

            lock (_syncRoot)
            {
                // 1. Calculate Path for THIS specific record (handles midnight switch)
                var newPath = BuildFilePath();

                // 2. If path changed (New Day), ensure header exists on the new file
                if (newPath != _currentFilePath)
                {
                    _currentFilePath = newPath;
                    EnsureHeaderExists(_currentFilePath);
                    PurgeOldLogsIfNeeded(); // Good time to purge old logs too
                }
                else
                {
                    // Even if path didn't change, double check size just in case file was deleted externally
                    if (!File.Exists(_currentFilePath) || new FileInfo(_currentFilePath).Length == 0)
                    {
                        EnsureHeaderExists(_currentFilePath);
                    }
                }

                // 3. Append Data
                RotateLogIfNeeded(); // Check size limits

                // Note: RotateLogIfNeeded might change _currentFilePath (backup rotation), 
                // so we use _currentFilePath which is updated inside Rotate if needed.

                var line = BuildDataLine(record);
                File.AppendAllText(_currentFilePath, line + Environment.NewLine, Encoding.UTF8);
            }
        }

        private string BuildDataLine(ProductionDataRecord record)
        {
            string F(object? v) => v?.ToString() ?? "";
            var sb = new StringBuilder();
            sb.Append(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")).Append(",").Append(F(record.TwoDCode));

            // Ensure we don't crash if record has fewer stations than header
            for (int i = 0; i < DefaultStationCount; i++)
            {
                var st = (record.Stations != null && i < record.Stations.Length)
                         ? record.Stations[i]
                         : new StationMeasurement();

                // Use Null Conditional to prevent null ref on 'st' if array index was null
                st = st ?? new StationMeasurement();

                sb.Append($",{F(st.Result)},{F(st.X)},{F(st.Y)},{F(st.Z)}");
            }
            sb.Append($",{F(record.OEE)},{F(record.Availability)},{F(record.Performance)},{F(record.Quality)}");
            sb.Append($",{F(record.Total_IN)},{F(record.OK)},{F(record.NG)}");
            sb.Append($",{F(record.Uptime)},{F(record.Downtime)},{F(record.TotalTime)},{F(record.CT)}");
            return sb.ToString();
        }

        private void RotateLogIfNeeded()
        {
            if (_config.LogRetentionFileSize > 0 && File.Exists(_currentFilePath))
            {
                if (new FileInfo(_currentFilePath).Length > _config.LogRetentionFileSize * 1024 * 1024)
                {
                    BackupCurrentLog();
                    // Re-calculate fresh path (might be same name, but empty now)
                    _currentFilePath = BuildFilePath();
                    EnsureHeaderExists(_currentFilePath);
                }
            }
        }

        private void BackupCurrentLog()
        {
            if (!File.Exists(_currentFilePath)) return;
            var backupDir = _config.BackupFolder ?? Path.GetDirectoryName(_currentFilePath);
            if (!Directory.Exists(backupDir)) Directory.CreateDirectory(backupDir);

            var backupName = Path.GetFileNameWithoutExtension(_currentFilePath) + $"_backup_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
            var backupPath = Path.Combine(backupDir, backupName);

            try
            {
                File.Copy(_currentFilePath, backupPath, true);
                File.Delete(_currentFilePath); // Delete original to start fresh
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Log Backup Error: {ex.Message}");
            }
        }

        private void PurgeOldLogsIfNeeded()
        {
            if (!_config.AutoPurge || _config.LogRetentionTime <= 0) return;
            try
            {
                var dir = Path.GetDirectoryName(_currentFilePath);
                if (!Directory.Exists(dir)) return;
                var files = Directory.GetFiles(dir, "*.csv");
                var cutoff = DateTime.Now.AddDays(-_config.LogRetentionTime);
                foreach (var file in files)
                {
                    if (new FileInfo(file).CreationTime < cutoff) File.Delete(file);
                }
            }
            catch { }
        }

        private string ReplaceDateTokens(string f)
        {
            return System.Text.RegularExpressions.Regex.Replace(f, @"{(.*?)}", m => DateTime.Now.ToString(m.Groups[1].Value));
        }
    }
}