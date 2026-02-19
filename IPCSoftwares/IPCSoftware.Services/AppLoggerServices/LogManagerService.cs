
using IPCSoftware.Core.Interfaces;
using IPCSoftware.Core.Interfaces.AppLoggerInterface;
using IPCSoftware.Shared.Models;
using IPCSoftware.Shared.Models.ConfigModels;
using Microsoft.Extensions.Options;
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
        private readonly CcdSettings _ccdSettings;
        private readonly ExternalSettings _extSettings;

        private readonly BackupService _backupService;

        private readonly Dictionary<int, DateTime> _lastBackupRun = new();
        private readonly Dictionary<int, DateTime> _lastPurgeRun = new();

        public LogManagerService(
            ILogConfigurationService logConfigService,
            IOptions <CcdSettings> ccdSettings,
            IOptions <ExternalSettings> extSettings,

            BackupService backupService
            )
        {
            _ccdSettings = ccdSettings.Value;
            _extSettings = extSettings.Value;
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

        private async Task ReloadConfigAsync()
        {
            var logs = await _logConfigService.GetAllAsync();
            var newConfigList = new List<LogConfigurationModel>(logs);
            _logConfigs = newConfigList;
        }


        // Called periodically by Core Service Worker
        public void CheckAndPerformBackups()
        {
            var now = DateTime.Now;

            foreach (var config in _logConfigs.Where(c => c.Enabled && c.BackupSchedule != BackupScheduleType.Manual))
            {
                // 1. Check if backup was already done recently (within same minute)
                if (_lastBackupRun.TryGetValue(config.Id, out var lastRun))
                {
                    if (now.Date == lastRun.Date && now.Hour == lastRun.Hour && now.Minute == lastRun.Minute)
                        continue; // Already ran this minute
                }

                // 2. Check if Schedule Matches
                bool isDue = false;

                // Time check (Hour & Minute match)
                if (now.Hour == config.BackupTime.Hours && now.Minute == config.BackupTime.Minutes)
                {
                    switch (config.BackupSchedule)
                    {
                        case BackupScheduleType.Daily:
                            isDue = true;
                            break;

                        case BackupScheduleType.Weekly:
                            if (now.DayOfWeek.ToString() == config.BackupDayOfWeek)
                                isDue = true;
                            break;

                        case BackupScheduleType.Monthly:
                            if (now.Day == config.BackupDay)
                                isDue = true;
                            break;
                    }
                }

                // 3. Perform Backup
                if (isDue)
                {
                    PerformBackupForConfig(config);
                    _lastBackupRun[config.Id] = now;
                }
            }
        }

        // --- MANUAL BACKUP LOGIC ---
        // Called by UI ViewModel
        public BackupResult PerformManualBackup(int logConfigId)
        {
            var config = _logConfigs.FirstOrDefault(c => c.Id == logConfigId);
            if (config != null)
            {
                return _backupService.PerformBackup(config);
            }
            return new BackupResult { FailedFiles = 0, CopiedFiles = 0, TotalFiles = 0 }; // Or error state
        }

        public BackupResult PerformManualRestore(int logConfigId)
        {
            var config = _logConfigs.FirstOrDefault(c => c.Id == logConfigId);
            if (config != null)
            {
                return _backupService.PerformRestore(config);
            }
            return new BackupResult();
        }


        private void PerformBackupForConfig(LogConfigurationModel config)
        {
            // Resolve current active file
            //string currentFile = ResolveLogFile(config.LogType);
          /*  if (!string.IsNullOrEmpty(currentFile) && File.Exists(currentFile))
            {
            }*/
                _backupService.PerformBackup(config);
        }

        private void PerformRestoreForConfig(LogConfigurationModel config)
        {
            // Resolve current active file
            //string currentFile = ResolveLogFile(config.LogType);
          /*  if (!string.IsNullOrEmpty(currentFile) && File.Exists(currentFile))
            {
            }*/
                _backupService.PerformRestore(config);
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

        public void CheckAndPerformPurge()
        {
            var now = DateTime.Now;

            foreach (var config in _logConfigs.Where(c => c.Enabled && c.AutoPurge && c.LogRetentionTime > 0))
            {
                // 1. Throttle: Only run heavy disk-scanning purge once an hour PER CONFIG
                if (_lastPurgeRun.TryGetValue(config.Id, out var lastRun))
                {
                    if ((now - lastRun).TotalHours < 1)
                        continue;
                }

                try
                {
                    // Calculate the exact date/time threshold for deletion
                    var cutoffDate = now.AddDays(-config.LogRetentionTime);

                    // 2. Purge DataFolder 
                    // Using EnumerateFiles instead of GetFiles prevents massive RAM spikes with millions of files
                    if (Directory.Exists(config.DataFolder))
                    {
                        foreach (var file in Directory.EnumerateFiles(config.DataFolder))
                        {
                            var fileInfo = new FileInfo(file);
                            // Check if file is older than cutoff (No longer restricting to just .csv)
                            if (fileInfo.LastWriteTime < cutoffDate)
                            {
                                try { fileInfo.Delete(); } catch { }
                            }
                        }
                    }

                    // 3. Purge Production Images (High Volume handling)
                    if (config.LogType == LogType.Production && !string.IsNullOrEmpty(config.ProductionImagePath))
                    {
                        string machineCode = _extSettings.AOIMachineCode ;
                        string exactTargetPath = Path.Combine(config.ProductionImagePath, machineCode, _ccdSettings.ImageRootFolder);
                        if (Directory.Exists(exactTargetPath))
                        {
                            // Enumerate directories (Batch folders) to save memory
                            foreach (var dir in Directory.EnumerateDirectories(exactTargetPath))
                            {
                                var dirInfo = new DirectoryInfo(dir);
                                if (dirInfo.CreationTime < cutoffDate)
                                {
                                    try { dirInfo.Delete(true); } catch { } // true = recursive delete
                                }
                            }

                            // Enumerate loose files in the root of the production folder just in case
                            foreach (var file in Directory.EnumerateFiles(config.ProductionImagePath))
                            {
                                var fileInfo = new FileInfo(file);
                                if (fileInfo.LastWriteTime < cutoffDate)
                                {
                                    try { fileInfo.Delete(); } catch { }
                                }
                            }
                        }
                    }

                    // Update last run time for this specific config
                    _lastPurgeRun[config.Id] = now;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Auto-Purge Error for {config.LogName}: {ex.Message}");
                }
            }
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
