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
        /// <summary>
        /// Appends one production cycle row into the CSV.
        /// </summary>
        /// <param name="record">Filled production data record.</param>
        void AppendRecord(ProductionDataRecord record);
    }

    /// <summary>
    /// Simple CSV-based production data logger.
    /// Each AppendRecord call appends one line into the CSV file.
    /// </summary>
    public class ProductionDataLogger : IProductionDataLogger
    {
        private readonly LogConfigurationModel _config;
        private string _filePath;
        private static readonly object _syncRoot = new object();
        private bool _headerWritten;
        private const int StationCount = 13; // Stations 0..12

        public ProductionDataLogger(LogConfigurationModel config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _filePath = BuildFilePath();
            EnsureFileAndHeader();
            PurgeOldLogsIfNeeded();
        }

        private string BuildFilePath()
        {
            var dir = _config.DataFolder;
            var name = _config.FileName;
            // Debug: Output the raw config value and ASCII codes
            System.Diagnostics.Debug.WriteLine($"[DEBUG] Config FileName: '{name}'");
            System.Diagnostics.Debug.WriteLine($"[DEBUG] Config FileName ASCII: {string.Join(",", name.Select(c => (int)c))}");

            if (string.IsNullOrWhiteSpace(name))
                name = $"Production_{DateTime.Now:yyyyMMdd}.csv";
            else
                name = ReplaceDateTokens(name);

            // Debug: Output after ReplaceDateTokens
            System.Diagnostics.Debug.WriteLine($"[DEBUG] FileName after ReplaceDateTokens: '{name}'");
            System.Diagnostics.Debug.WriteLine($"[DEBUG] FileName ASCII: {string.Join(",", name.Select(c => (int)c))}");

            // Ensure .csv extension
            if (!name.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
                name += ".csv";

            // Debug: Output the final file name
            System.Diagnostics.Debug.WriteLine($"[DEBUG] Final FileName: '{name}'");
            System.Diagnostics.Debug.WriteLine($"[DEBUG] Final FileName ASCII: {string.Join(",", name.Select(c => (int)c))}");

            return Path.Combine(dir, name);
        }

        /// <summary>
        /// Ensures that directory exists and header is present in the CSV file.
        /// </summary>
        private void EnsureFileAndHeader()
        {
            lock (_syncRoot)
            {
                var directory = Path.GetDirectoryName(_filePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                if (!File.Exists(_filePath))
                {
                    var header = BuildHeaderLine();
                    File.WriteAllText(_filePath, header + Environment.NewLine, Encoding.UTF8);
                }

                _headerWritten = true;
            }
        }

        private string BuildHeaderLine()
        {
            var sb = new StringBuilder();

            // First column: 2D code
            sb.Append("2D_Code");

            // Station columns: St0_result, St0_X, St0_Y, St0_Z, ..., St12_result, St12_X, St12_Y, St12_Z
            for (int i = 0; i < StationCount; i++)
            {
                sb.Append($",St{i}_result,St{i}_X,St{i}_Y,St{i}_Z");
            }

            // OEE and counters & time metrics
            sb.Append(",OEE,Availability,Performance,Quality");
            sb.Append(",Total_IN,OK,NG");
            sb.Append(",Uptime,Downtime,TotalTime,CT");

            return sb.ToString();
        }

        public void AppendRecord(ProductionDataRecord record)
        {
            if (record == null)
                throw new ArgumentNullException(nameof(record));

            var line = BuildDataLine(record);

            lock (_syncRoot)
            {
                // Always get the current file path (with up-to-date date)
                _filePath = BuildFilePath();

                if (!_headerWritten)
                {
                    EnsureFileAndHeader();
                }

                RotateLogIfNeeded();
                File.AppendAllText(_filePath, line + Environment.NewLine, Encoding.UTF8);
            }
        }

        private string BuildDataLine(ProductionDataRecord record)
        {
            string F(object? value)
            {
                if (value == null)
                    return string.Empty;

                var s = value.ToString() ?? string.Empty;

                // Basic CSV escaping: wrap in quotes if needed and escape inner quotes
                if (s.Contains(',') || s.Contains('"') || s.Contains('\n'))
                {
                    s = "\"" + s.Replace("\"", "\"\"") + "\"";
                }

                return s;
            }

            var sb = new StringBuilder();

            // 2D code
            sb.Append(F(record.TwoDCode));

            // Stations 0..12
            for (int i = 0; i < StationCount; i++)
            {
                var st = record.Stations[i] ?? new StationMeasurement();

                sb.Append(',');
                sb.Append(F(st.Result));
                sb.Append(',');
                sb.Append(F(st.X));
                sb.Append(',');
                sb.Append(F(st.Y));
                sb.Append(',');
                sb.Append(F(st.Z));
            }

            // OEE KPIs
            sb.Append(',').Append(F(record.OEE));
            sb.Append(',').Append(F(record.Availability));
            sb.Append(',').Append(F(record.Performance));
            sb.Append(',').Append(F(record.Quality));

            // Counters
            sb.Append(',').Append(F(record.Total_IN));
            sb.Append(',').Append(F(record.OK));
            sb.Append(',').Append(F(record.NG));

            // Time metrics
            sb.Append(',').Append(F(record.Uptime));
            sb.Append(',').Append(F(record.Downtime));
            sb.Append(',').Append(F(record.TotalTime));
            sb.Append(',').Append(F(record.CT));

            return sb.ToString();
        }

        private void RotateLogIfNeeded()
        {
            // File size rotation
            if (_config.LogRetentionFileSize > 0 && File.Exists(_filePath))
            {
                var fileInfo = new FileInfo(_filePath);
                long maxSizeBytes = _config.LogRetentionFileSize * 1024 * 1024; // MB to bytes
                if (fileInfo.Length > maxSizeBytes)
                {
                    BackupCurrentLog();
                    _filePath = BuildFilePath();
                    EnsureFileAndHeader();
                }
            }
            // Time-based rotation (daily, weekly, monthly)
            if (_config.BackupSchedule != BackupScheduleType.Manual)
            {
                DateTime now = DateTime.Now;
                bool shouldBackup = false;
                switch (_config.BackupSchedule)
                {
                    case BackupScheduleType.Daily:
                        shouldBackup = now.TimeOfDay >= _config.BackupTime;
                        break;
                    case BackupScheduleType.Weekly:
                        shouldBackup = now.DayOfWeek.ToString() == _config.BackupDayOfWeek && now.TimeOfDay >= _config.BackupTime;
                        break;
                    case BackupScheduleType.Monthly:
                        shouldBackup = now.Day == _config.BackupDay && now.TimeOfDay >= _config.BackupTime;
                        break;
                }
                if (shouldBackup)
                {
                    BackupCurrentLog();
                    _filePath = BuildFilePath();
                    EnsureFileAndHeader();
                }
            }
        }

        private void BackupCurrentLog()
        {
            if (!File.Exists(_filePath)) return;
            var backupDir = _config.BackupFolder;
            if (string.IsNullOrWhiteSpace(backupDir)) backupDir = Path.GetDirectoryName(_filePath);
            if (!Directory.Exists(backupDir)) Directory.CreateDirectory(backupDir);
            var backupName = Path.GetFileNameWithoutExtension(_filePath) + $"_backup_{DateTime.Now:yyyyMMdd_HHmmss}" + Path.GetExtension(_filePath);
            var backupPath = Path.Combine(backupDir, backupName);
            File.Copy(_filePath, backupPath, true);
            File.WriteAllText(_filePath, BuildHeaderLine() + Environment.NewLine, Encoding.UTF8); // Reset log file
        }

        private void PurgeOldLogsIfNeeded()
        {
            if (!_config.AutoPurge || _config.LogRetentionTime <= 0) return;
            var dir = _config.DataFolder;
            if (!Directory.Exists(dir)) return;
            var files = Directory.GetFiles(dir, "*.csv");
            var cutoff = DateTime.Now.AddDays(-_config.LogRetentionTime);
            foreach (var file in files)
            {
                var info = new FileInfo(file);
                if (info.CreationTime < cutoff)
                {
                    File.Delete(file);
                }
            }
        }

        private string ReplaceDateTokens(string fileName)
        {
            // Replace any {yyyyMMdd}, {yyyy-MM-dd}, etc. with the actual date
            return System.Text.RegularExpressions.Regex.Replace(
                fileName,
                @"{(.*?)}",
                m => DateTime.Now.ToString(m.Groups[1].Value)
            );
        }
    }
}
