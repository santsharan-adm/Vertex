
using IPCSoftware.Core.Interfaces;
using IPCSoftware.Core.Interfaces.AppLoggerInterface;
using IPCSoftware.Shared.Models;
using IPCSoftware.Shared.Models.ConfigModels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IPCSoftware.Services.AppLoggerServices
{
    public class LogManagerService : ILogManagerService
    {
        //  private readonly List<LogConfig> _configs;
        private  List<LogConfigurationModel> _logConfigs;
        private readonly ILogConfigurationService _logConfigService;

        private readonly BackupService _backupService;

        public LogManagerService(
            ILogConfigurationService logConfigService,
            BackupService backupService
            )
        {
            _logConfigService = logConfigService;
            _logConfigs = new List<LogConfigurationModel>();
            // _ = LoadDataAsync();
            _backupService = backupService; // new line
            _logConfigService.ConfigurationChanged += async (s, e) => await ReloadConfigAsync();
        }


        public async Task InitializeAsync()
        {
            await ReloadConfigAsync();
        }

        // Extract loading logic to a separate method
        private async Task ReloadConfigAsync()
        {

            var logs = await _logConfigService.GetAllAsync();


            // Create a NEW list first (Thread safety best practice)
            // If a log is writing RIGHT NOW, we don't want to Clear() the list while it's reading it.
            var newConfigList = new List<LogConfigurationModel>();

            foreach (var log in logs)
            {
                newConfigList.Add(log);
            }

            // Swap the reference
            _logConfigs = newConfigList;
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
            var config = _logConfigs
                .FirstOrDefault(c => c.Enabled && c.LogType == type);

            if (config == null)
                return null;

            // Ensure folder exists
            if (!Directory.Exists(config.DataFolder))
                Directory.CreateDirectory(config.DataFolder);
           

            // Build filename using pattern
            string fileName = config.FileName
                .Replace("yyyyMMdd", DateTime.Now.ToString("yyyyMMdd"));

            string fullPath = Path.Combine(config.DataFolder, fileName + ".csv");

            // Ensure CSV header exists
            if (!File.Exists(fullPath))
            {
                File.WriteAllText(fullPath, "Timestamp,Level,Message,Source\n");
            }

            return fullPath;
        }

        // Called by AppLogger to perform maintenance checks
        public void ApplyMaintenance(LogConfigurationModel config, string filePath)
        {
            if (config == null)
                return;

            // 1) File size retention (Audit + Error)
            if (config.LogType != LogType.Production &&
                config.LogRetentionFileSize > 0)
            {
                long currentSizeMB = new FileInfo(filePath).Length / (1024 * 1024);

                if (currentSizeMB >= config.LogRetentionFileSize)
                {
                    string newName = Path.Combine(
                        config.DataFolder,
                        config.FileName.Replace("{yyyyMMdd}",
                        DateTime.Now.ToString("yyyyMMdd")) + "_" +
                        Guid.NewGuid().ToString("N") + ".csv");

                    File.Move(filePath, newName);

                    // Create fresh file
                    File.WriteAllText(filePath, "Timestamp,Level,Message,Source\n");
                }
            }

            // 2) Time-based retention (Production only)
            if (config.LogType == LogType.Production &&
                config.LogRetentionTime > 0)
            {
                foreach (var file in Directory.GetFiles(config.DataFolder))
                {
                    DateTime lastWrite = File.GetLastWriteTime(file);
                    if ((DateTime.Now - lastWrite).TotalDays > config.LogRetentionTime)
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
                    if ((DateTime.Now - lastWrite).TotalDays > config.LogRetentionTime)
                    {
                        File.Delete(file);
                    }
                }
            }

            //do backuP
            _backupService.PerformBackup(config, filePath);
        }


        public LogConfigurationModel GetConfig(LogType type)
        {
            return _logConfigs.FirstOrDefault(x => x.LogType == type && x.Enabled);
        }



        public List<LogEntry> ReadLogs(LogType type, DateTime? date = null)
        {
            var config = GetConfig(type);
            if (config == null)
                return new List<LogEntry>();

            string targetDate = (date ?? DateTime.Now).ToString("yyyyMMdd");
            string fileName = config.FileName.Replace("{yyyyMMdd}", targetDate) + ".csv";
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
