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
    }
}
