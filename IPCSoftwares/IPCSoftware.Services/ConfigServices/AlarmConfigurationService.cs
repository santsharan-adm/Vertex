using IPCSoftware.Core.Interfaces;
using IPCSoftware.Core.Interfaces.AppLoggerInterface;
using IPCSoftware.Shared.Models.ConfigModels;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IPCSoftware.Services.ConfigServices
{
    public class AlarmConfigurationService : BaseService, IAlarmConfigurationService
    {
        private readonly string _dataFolder;
        private readonly string _csvFilePath;
        private List<AlarmConfigurationModel> _alarms;
        private int _nextId = 1;

        public AlarmConfigurationService(IOptions<ConfigSettings> configSettings,
            IAppLogger logger) : base(logger)
        {
            var config = configSettings.Value;
            string dataFolderPath = config.DataFolder;

            _dataFolder = dataFolderPath ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data");

            if (!Directory.Exists(_dataFolder))
            {
                Directory.CreateDirectory(_dataFolder);
            }

            _csvFilePath = Path.Combine(_dataFolder, config.AlarmConfigFileName/* "AlarmConfigurations.csv"*/);
            _alarms = new List<AlarmConfigurationModel>();
        }

        public async Task InitializeAsync()
        {
            try
            {
              await LoadFromCsvAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message, LogType.Diagnostics);
                throw;
            }

        }

        public async Task<List<AlarmConfigurationModel>> GetAllAlarmsAsync()
        {
            return await Task.FromResult(_alarms.ToList());
        }

        public async Task<AlarmConfigurationModel> GetAlarmByIdAsync(int id)
        {
            return await Task.FromResult(_alarms.FirstOrDefault(a => a.Id == id));
        }

        public async Task<AlarmConfigurationModel> AddAlarmAsync(AlarmConfigurationModel alarm)
        {
            try
            {
                alarm.Id = _nextId++;
                _alarms.Add(alarm);
                await SaveToCsvAsync();
                return alarm;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message, LogType.Diagnostics);
                return alarm;
                throw;
            }
        }

        public async Task<bool> UpdateAlarmAsync(AlarmConfigurationModel alarm)
        {
            try
            {
                var existing = _alarms.FirstOrDefault(a => a.Id == alarm.Id);
                if (existing == null) return false;

                var index = _alarms.IndexOf(existing);
                _alarms[index] = alarm;
                await SaveToCsvAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message, LogType.Diagnostics);
                return false;
            }
        }

        public async Task<bool> DeleteAlarmAsync(int id)
        {
            try
            {
                var alarm = _alarms.FirstOrDefault(a => a.Id == id);
                if (alarm == null) return false;

                _alarms.Remove(alarm);
                await SaveToCsvAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message, LogType.Diagnostics);
                return false;
            }
        }

        public async Task<bool> AcknowledgeAlarmAsync(int id)
        {
            try
            {
                var alarm = _alarms.FirstOrDefault(a => a.Id == id);
                if (alarm == null) return false;

                alarm.AlarmAckTime = DateTime.Now;
                await SaveToCsvAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message, LogType.Diagnostics);
                return false;
            }
        }

        private async Task LoadFromCsvAsync()
        {
            try
            {
            if (!File.Exists(_csvFilePath))
            {
                await SaveToCsvAsync();
                return;
            }

                var lines = await File.ReadAllLinesAsync(_csvFilePath);
                if (lines.Length <= 1) return;

                _alarms.Clear();
                for (int i = 1; i < lines.Length; i++)
                {
                    var alarm = ParseCsvLine(lines[i]);
                    if (alarm != null)
                    {
                        _alarms.Add(alarm);
                        if (alarm.Id >= _nextId)
                            _nextId = alarm.Id + 1;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to load alarms CSV: {ex.Message}", LogType.Diagnostics);
              //  Console.WriteLine($"Error loading alarms CSV: {ex.Message}");

            }
        }

        private async Task SaveToCsvAsync()
        {
            try
            {
                var sb = new StringBuilder();
                sb.AppendLine("Id,AlarmNo,AlarmName,TagNo,Name,AlarmBit,AlarmText,Severity,AlarmTime,AlarmResetTime,AlarmAckTime,Description,Remark");

                foreach (var alarm in _alarms)
                {
                    sb.AppendLine($"{alarm.Id}," +
                        $"{alarm.AlarmNo}," +
                        $"\"{EscapeCsv(alarm.AlarmName)}\"," +
                        $"{alarm.TagNo}," +
                        $"\"{EscapeCsv(alarm.Name)}\"," +
                        $"\"{EscapeCsv(alarm.AlarmBit)}\"," +
                        $"\"{EscapeCsv(alarm.AlarmText)}\"," +
                        $"\"{EscapeCsv(alarm.Severity)}\"," +
                        $"\"{alarm.AlarmTime}\"," +
                        $"\"{alarm.AlarmResetTime}\"," +
                        $"\"{alarm.AlarmAckTime}\"," +
                        $"\"{EscapeCsv(alarm.Description)}\"," +
                        $"\"{EscapeCsv(alarm.Remark)}\"");
                }

                await File.WriteAllTextAsync(_csvFilePath, sb.ToString(), Encoding.UTF8);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to save alarms CSV: {ex.Message}", LogType.Diagnostics);
                throw;
            }
        }

        private AlarmConfigurationModel ParseCsvLine(string line)
        {
            try
            {
                var values = SplitCsvLine(line);
                if (values.Count < 13) return null;

                return new AlarmConfigurationModel
                {
                    Id = int.Parse(values[0]),
                    AlarmNo = int.Parse(values[1]),
                    AlarmName = values[2],
                    TagNo = int.Parse(values[3]),
                    Name = values[4],
                    AlarmBit = values[5],
                    AlarmText = values[6],
                    Severity = values[7],
                    AlarmTime = string.IsNullOrEmpty(values[8]) ? null : DateTime.Parse(values[8]),
                    AlarmResetTime = string.IsNullOrEmpty(values[9]) ? null : DateTime.Parse(values[9]),
                    AlarmAckTime = string.IsNullOrEmpty(values[10]) ? null : DateTime.Parse(values[10]),
                    Description = values[11],
                    Remark = values[12]
                };
            }
            catch(Exception ex)
            {
                //_logger.LogError($"Error parsing alarms CSV: {ex.Message}", LogType.Diagnostics);
                return null;
            }
        }

        private List<string> SplitCsvLine(string line)
        {
            var values = new List<string>();
            var currentValue = new StringBuilder();
            bool inQuotes = false;
            try
            {
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
                return values;
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
    }
}
