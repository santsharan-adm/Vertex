
using IPCSoftware.Core.Interfaces;
using IPCSoftware.Core.Interfaces.AppLoggerInterface;
using IPCSoftware.Shared.Models.ConfigModels;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using LogType = IPCSoftware.Shared.Models.ConfigModels.LogType;

namespace IPCSoftware.Services.ConfigServices
{
    public class LogConfigurationService : ILogConfigurationService
    {
        private readonly string _dataFolder;
        private readonly string _csvFilePath;
        private List<LogConfigurationModel> _configurations;
        private int _nextId = 1;

        public LogConfigurationService(IOptions<ConfigSettings> configSettings) 
        {
            var config = configSettings.Value;
            string dataFolderPath = config.DataFolder;
            _dataFolder = dataFolderPath ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data");

            if (!Directory.Exists(_dataFolder))
            {
                Directory.CreateDirectory(_dataFolder);
            }

            _csvFilePath = Path.Combine(_dataFolder, config.LogConfigFileName /* "LogConfigurations.csv"*/);
            _configurations = new List<LogConfigurationModel>();

            //  _ = InitializeAsync();
        }



        public async Task InitializeAsync()
        {
            try
            {
                if (!File.Exists(_csvFilePath))
                {
                    await InitializeDefaultConfigurationsAsync();
                }

                await LoadFromCsvAsync();
            }
            catch (Exception ex)
            {
               // _logger.LogError(ex.Message, LogType.Diagnostics);
            }
        }


        private async Task InitializeDefaultConfigurationsAsync()
        {
            //try
            //{
            //    // Try to extract embedded resource first
            //    if (await ExtractEmbeddedResourceAsync())
            //    {
            //        Console.WriteLine("Log configurations initialized from embedded resource.");
            //        return;
            //    }
            //}
            //catch (Exception ex)
            //{
            //    Console.WriteLine($"Could not extract embedded resource: {ex.Message}. Using hardcoded defaults.");
            //}

            // Fallback to hardcoded defaults
            await CreateHardcodedDefaultsAsync();
        }
      


        /// <summary>
        /// Create hardcoded default configurations (Fallback)
        /// </summary>
        private async Task CreateHardcodedDefaultsAsync()
        {
            try
            {
                string baseDir = AppDomain.CurrentDomain.BaseDirectory;

                var defaultConfigs = new List<LogConfigurationModel>
                {
                    new LogConfigurationModel
                    {
                        Id = 1,
                        LogName = "Audit",
                        LogType = LogType.Audit,
                        DataFolder = Path.Combine(baseDir, "Logs", "Audit"),
                        BackupFolder = Path.Combine(baseDir, "LogsBackup", "Audit"),
                        FileName = "Audit_{yyyyMMdd}",
                        LogRetentionTime = 90,
                        LogRetentionFileSize = 5,
                        AutoPurge = false,
                        BackupSchedule = BackupScheduleType.Daily,
                        BackupTime = new TimeSpan(02, 00, 00),
                        BackupDay = 0,
                        BackupDayOfWeek = null,
                        Description = "Audit log configuration",
                        Remark = "System audit trail logs",
                        Enabled = true
                    },
                    new LogConfigurationModel
                    {
                        Id = 2,
                        LogName = "Production",
                        LogType = LogType.Production,
                        DataFolder = Path.Combine(baseDir, "Logs", "Production"),
                          BackupFolder = Path.Combine(baseDir, "LogsBackup", "Production"),
                        FileName = "Production_{yyyyMMdd}",
                        LogRetentionTime = 30,
                        LogRetentionFileSize = 5,
                        AutoPurge = true,
                        BackupSchedule = BackupScheduleType.Weekly,
                        BackupTime = new TimeSpan(03, 00, 00),
                        BackupDay = 0,
                        BackupDayOfWeek = "Monday",
                        Description = "Production log configuration",
                        Remark = "Production system logs",
                        Enabled = true
                    },
                    new LogConfigurationModel
                    {
                        Id = 3,
                        LogName = "Error",
                        LogType = LogType.Error,
                        DataFolder = Path.Combine(baseDir, "Logs", "Error"),
                        BackupFolder = Path.Combine(baseDir, "LogsBackup","Error"),
                        FileName = "Error_{yyyyMMdd}",
                        LogRetentionTime = 60,
                        LogRetentionFileSize = 5,
                        AutoPurge = false,
                        BackupSchedule = BackupScheduleType.Daily,
                        BackupTime = new TimeSpan(04, 00, 00),
                        BackupDay = 0,
                        BackupDayOfWeek = null,
                        Description = "Error log configuration",
                        Remark = "System error logs",
                        Enabled = true
                    },

                    new LogConfigurationModel
                    {
                        Id = 3,
                        LogName = "Diagnostics",
                        LogType = LogType.Diagnostics,
                        DataFolder = Path.Combine(baseDir, "Logs", "Diagnostics"),
                        BackupFolder = Path.Combine(baseDir, "LogsBackup","Diagnostics"),
                        FileName = "Diagnostics_{yyyyMMdd}",
                        LogRetentionTime = 60,
                        LogRetentionFileSize = 5,
                        AutoPurge = false,
                        BackupSchedule = BackupScheduleType.Daily,
                        BackupTime = new TimeSpan(04, 00, 00),
                        BackupDay = 0,
                        BackupDayOfWeek = null,
                        Description = "Diagnostics log configuration",
                        Remark = "Diagnostics error logs",
                        Enabled = true
                    }
                };

                _configurations = defaultConfigs;
                await SaveToCsvAsync();
            }
            catch (Exception ex)
            {
              //  _logger.LogError(ex.Message, LogType.Diagnostics);
            }
        }
            

        public async Task<List<LogConfigurationModel>> GetAllAsync()
        {
            return await Task.FromResult(_configurations.ToList());
        }

        public async Task<LogConfigurationModel> GetByIdAsync(int id)
        {
            return await Task.FromResult(_configurations.FirstOrDefault(c => c.Id == id));
        }


        /// <summary>
        /// Get configuration by LogType
        /// </summary>
        public async Task<LogConfigurationModel> GetByLogTypeAsync(LogType logType)
        {
            return await Task.FromResult(_configurations.FirstOrDefault(c =>
                c.LogType == logType));
        }


        public async Task<LogConfigurationModel> AddAsync(LogConfigurationModel logConfig)
        {
            try
            {
                // Prevent adding duplicate LogTypes
                var existing = _configurations.FirstOrDefault(c =>
                    c.LogType == logConfig.LogType);

                if (existing != null)
                {
                    throw new InvalidOperationException($"Log configuration for type '{logConfig.LogType}' already exists. Use Update to modify.");
                }


                logConfig.Id = _nextId++;
                NormalizeBackupSettings(logConfig);
                _configurations.Add(logConfig);
                await SaveToCsvAsync();
                return logConfig;
            }
            catch (Exception ex)
            {
             //   _logger.LogError(ex.Message, LogType.Diagnostics);
                throw;
            }
        }

        public async Task<bool> UpdateAsync(LogConfigurationModel logConfig)
        {
            try
            {
                var existing = _configurations.FirstOrDefault(c => c.Id == logConfig.Id);
                if (existing == null)
                    return false;

                NormalizeBackupSettings(logConfig);
                var index = _configurations.IndexOf(existing);
                _configurations[index] = logConfig;
                await SaveToCsvAsync();
                ConfigurationChanged?.Invoke(this, EventArgs.Empty);
                return true;
            }
            catch (Exception ex)
            {
              //  _logger.LogError(ex.Message, LogType.Diagnostics);
                return false;
            }
        }

        public async Task<bool> DeleteAsync(int id)
        {
            try
            {
                var config = _configurations.FirstOrDefault(c => c.Id == id);
                if (config == null)
                    return false;
                _configurations.Remove(config);
                await SaveToCsvAsync();
                return true;
            }
            catch (Exception ex)
            {
               // _logger.LogError(ex.Message, LogType.Diagnostics);
                return false;
            }
        }

        public async Task<bool> SaveChangesAsync(List<LogConfigurationModel> configurations)
        {
            _configurations = configurations;
            await SaveToCsvAsync();
            return true;
        }

        /// <summary>
        /// Normalizes backup settings based on BackupSchedule
        /// This ensures only relevant fields are populated based on the schedule type
        /// </summary>
        private void NormalizeBackupSettings(LogConfigurationModel config)
        {
            if (config == null)
                return;

            switch (config.BackupSchedule)
            {
                case BackupScheduleType.Manual:
                    // Reset all backup-specific values to defaults
                    config.BackupTime = TimeSpan.Zero;
                    config.BackupDay = 0;
                    config.BackupDayOfWeek = null;
                    break;

                case BackupScheduleType.Daily:
                    // Only keep BackupTime, reset day-specific values
                    config.BackupDay = 0;
                    config.BackupDayOfWeek = null;
                    break;

                case BackupScheduleType.Weekly:
                    // Keep BackupTime and BackupDayOfWeek, reset day of month
                    config.BackupDay = 0;
                    if (string.IsNullOrWhiteSpace(config.BackupDayOfWeek))
                        config.BackupDayOfWeek = "Monday";
                    break;

                case BackupScheduleType.Monthly:
                    // Keep BackupTime and BackupDay, reset day of week
                    config.BackupDayOfWeek = null;
                    if (config.BackupDay <= 0 || config.BackupDay > 28)
                        config.BackupDay = 1;
                    break;

                default:
                    // Unknown schedule, reset everything
                    config.BackupTime = TimeSpan.Zero;
                    config.BackupDay = 0;
                    config.BackupDayOfWeek = null;
                    break;
            }
        }

        private async Task LoadFromCsvAsync()
        {
            if (!File.Exists(_csvFilePath))
            {
                // Create empty CSV with headers
                await SaveToCsvAsync();
                return;
            }

            try
            {
                var lines = await File.ReadAllLinesAsync(_csvFilePath);

                if (lines.Length <= 1) // Only header or empty
                    return;

                _configurations.Clear();

                for (int i = 1; i < lines.Length; i++)
                {
                    var line = lines[i];
                    if (string.IsNullOrWhiteSpace(line))
                        continue;

                    var config = ParseCsvLine(line);
                    if (config != null)
                    {
                        _configurations.Add(config);
                        if (config.Id >= _nextId)
                            _nextId = config.Id + 1;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading CSV: {ex.Message}");
            }
        }

        private async Task SaveToCsvAsync()
        {
            try
            {
                var sb = new StringBuilder();

                // Header - updated to include new backup fields
                sb.AppendLine("Id,LogName,LogType,DataFolder,BackupFolder,FileName,LogRetentionTime,LogRetentionFileSize,AutoPurge,BackupSchedule,BackupTime,BackupDay,BackupDayOfWeek,Description,Remark,Enabled");

                // Data rows
                foreach (var config in _configurations)
                {
                    sb.AppendLine(ToCsvLine(config));
                }

                await File.WriteAllTextAsync(_csvFilePath, sb.ToString(), Encoding.UTF8);
            }
            catch (Exception ex)
            {
              //  _logger.LogError($"Error saving CSV: {ex.Message}", LogType.Diagnostics);
                throw;
            }
        }

        private string ToCsvLine(LogConfigurationModel config)
        {
            try
            {
                // BackupDay: only included for Monthly schedule, otherwise 0
                // BackupDayOfWeek: only included for Weekly schedule, otherwise empty
                return $"{config.Id}," +
                       $"\"{EscapeCsv(config.LogName)}\"," +
                       $"\"{EscapeCsv(config.LogType.ToString())}\"," +
                       $"\"{EscapeCsv(config.DataFolder)}\"," +
                       $"\"{EscapeCsv(config.BackupFolder)}\"," +
                       $"\"{EscapeCsv(config.FileName)}\"," +
                       $"{config.LogRetentionTime}," +
                       $"{config.LogRetentionFileSize}," +
                       $"{config.AutoPurge}," +
                       $"\"{EscapeCsv(config.BackupSchedule.ToString())}\"," +
                       $"\"{config.BackupTime}\"," +
                       $"{config.BackupDay}," +
                       $"\"{EscapeCsv(config.BackupDayOfWeek)}\"," +
                       $"\"{EscapeCsv(config.Description)}\"," +
                       $"\"{EscapeCsv(config.Remark)}\"," +
                       $"{config.Enabled}";
            }
            catch (Exception ex)
            {
              // _logger.LogError(ex.Message, LogType.Diagnostics);
                throw;
            }
        }

        private LogConfigurationModel ParseCsvLine(string line)
        {
            try
            {
                var values = SplitCsvLine(line);

                // Updated to expect 16 columns (added BackupDay and BackupDayOfWeek)
                if (values.Count < 16)
                    return null;

                return new LogConfigurationModel
                {
                    Id = int.Parse(values[0]),
                    LogName = values[1],
                    LogType = Enum.Parse<LogType>(values[2]),
                    DataFolder = values[3],
                    BackupFolder = values[4],
                    FileName = values[5],
                    LogRetentionTime = int.Parse(values[6]),
                    LogRetentionFileSize = int.Parse(values[7]),
                    AutoPurge = bool.Parse(values[8]),
                    BackupSchedule = Enum.Parse <BackupScheduleType>( values[9]),
                    BackupTime = TimeSpan.Parse(values[10]),
                    BackupDay = string.IsNullOrWhiteSpace(values[11]) ? 0 : int.Parse(values[11]),
                    BackupDayOfWeek = string.IsNullOrWhiteSpace(values[12]) ? null : values[12],
                    Description = values[13],
                    Remark = values[14],
                    Enabled = bool.Parse(values[15])
                };
            }
            catch
            {
                return null;
            }
        }

        private List<string> SplitCsvLine(string line)
        {
            try
            {
                var values = new List<string>();
                var currentValue = new StringBuilder();
                bool inQuotes = false;

                for (int i = 0; i < line.Length; i++)
                {
                    char c = line[i];

                    if (c == '"')
                    {
                        if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                        {
                            currentValue.Append('"');
                            i++;
                        }
                        else
                        {
                            inQuotes = !inQuotes;
                        }
                    }
                    else if (c == ',' && !inQuotes)
                    {
                        values.Add(currentValue.ToString());
                        currentValue.Clear();
                    }
                    else
                    {
                        currentValue.Append(c);
                    }
                }

                values.Add(currentValue.ToString());
                return values;
            }
            catch (Exception ex)
            {
              //  _logger.LogError(ex.Message, LogType.Diagnostics);
                return null;
            }
        }

        private string EscapeCsv(string value)
        {
            if (string.IsNullOrEmpty(value))
                return string.Empty;

            if (value.Contains("\""))
                return value.Replace("\"", "\"\"");

            return value;
        }



        public event EventHandler ConfigurationChanged;
    }
}
