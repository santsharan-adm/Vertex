using IPCSoftware.AppLogger.Models;
using IPCSoftware.AppLogger.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace IPCSoftware.AppLogger.Services
{
    public class LogManager
    {
        private readonly List<LogConfig> _configs;

        private readonly BackupService _backupService;

        public LogManager(LogConfigService configService)
        {
            _configs = configService.LoadConfigs();
            _backupService = new BackupService(); // new line
        }

        private string[] SplitCsv(string line)
        {
            var values = new List<string>();
            bool inQuotes = false;
            string current = "";

            foreach (char ch in line)
            {
                if (ch == '"')
                {
                    inQuotes = !inQuotes;
                    continue;
                }

                if (ch == ',' && !inQuotes)
                {
                    values.Add(current);
                    current = "";
                }
                else
                {
                    current += ch;
                }
            }

            values.Add(current);
            return values.ToArray();
        }


        // Main method called by AppLogger
        public string ResolveLogFile(LogType type)
        {
            // Select config
            var config = _configs
                .FirstOrDefault(c => c.Enabled && c.Type == type);

            if (config == null)
                return null;

            // Ensure folder exists
            if (!Directory.Exists(config.DataFolder))
                Directory.CreateDirectory(config.DataFolder);

            // Build filename using pattern
            string fileName = config.FileNamePattern
                .Replace("{yyyyMMdd}", DateTime.Now.ToString("yyyyMMdd"));

            string fullPath = Path.Combine(config.DataFolder, fileName + ".csv");

            // Ensure CSV header exists
            if (!File.Exists(fullPath))
            {
                File.WriteAllText(fullPath, "Timestamp,Level,Message,Source\n");
            }

            return fullPath;
        }

        // Called by AppLogger to perform maintenance checks
        public void ApplyMaintenance(LogConfig config, string filePath)
        {
            if (config == null)
                return;

            // 1) File size retention (Audit + Error)
            if (config.Type != LogType.Production &&
                config.LogRetentionFileSizeMB > 0)
            {
                long currentSizeMB = new FileInfo(filePath).Length / (1024 * 1024);

                if (currentSizeMB >= config.LogRetentionFileSizeMB)
                {
                    string newName = Path.Combine(
                        config.DataFolder,
                        config.FileNamePattern.Replace("{yyyyMMdd}",
                        DateTime.Now.ToString("yyyyMMdd")) + "_" +
                        Guid.NewGuid().ToString("N") + ".csv");

                    File.Move(filePath, newName);

                    // Create fresh file
                    File.WriteAllText(filePath, "Timestamp,Level,Message,Source\n");
                }
            }

            // 2) Time-based retention (Production only)
            if (config.Type == LogType.Production &&
                config.LogRetentionDays > 0)
            {
                foreach (var file in Directory.GetFiles(config.DataFolder))
                {
                    DateTime lastWrite = File.GetLastWriteTime(file);
                    if ((DateTime.Now - lastWrite).TotalDays > config.LogRetentionDays)
                    {
                        File.Delete(file);
                    }
                }
            }
            
            // 3) Auto Purge after retention
            if (config.AutoPurge)
            {
                foreach (var file in Directory.GetFiles(config.DataFolder))
                {
                    DateTime lastWrite = File.GetLastWriteTime(file);
                    if ((DateTime.Now - lastWrite).TotalDays > config.LogRetentionDays)
                    {
                        File.Delete(file);
                    }
                }
            }

            //do backuP
            _backupService.PerformBackup(config, filePath);
        }


        public LogConfig GetConfig(LogType type)
        {
            return _configs.FirstOrDefault(x => x.Type == type && x.Enabled);
        }

        public class LogEntry
        {
            public DateTime Timestamp { get; set; }
            public string Level { get; set; }
            public string Message { get; set; }
            public string Source { get; set; }
        }

        public List<LogEntry> ReadLogs(LogType type, DateTime? date = null)
        {
            var config = GetConfig(type);
            if (config == null)
                return new List<LogEntry>();

            string targetDate = (date ?? DateTime.Now).ToString("yyyyMMdd");
            string fileName = config.FileNamePattern.Replace("{yyyyMMdd}", targetDate) + ".csv";
            string fullPath = Path.Combine(config.DataFolder, fileName);

            var result = new List<LogEntry>();

            if (!File.Exists(fullPath))
                return result;

            var lines = File.ReadAllLines(fullPath);

            // Skip header (Timestamp,Level,...)
            foreach (var line in lines.Skip(1))
            {
                var parts = SplitCsv(line);
                if (parts.Length < 4)
                    continue;

                result.Add(new LogEntry
                {
                    Timestamp = DateTime.Parse(parts[0]),
                    Level = parts[1],
                    Message = parts[2].Trim('"'),
                    Source = parts[3]
                });
            }

            return result;
        }

    }
}
