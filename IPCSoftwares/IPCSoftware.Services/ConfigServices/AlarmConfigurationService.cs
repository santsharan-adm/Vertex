using IPCSoftware.Core.Interfaces;
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
    public class AlarmConfigurationService : IAlarmConfigurationService
    {
        private readonly string _dataFolder;
        private readonly string _csvFilePath;
        private List<AlarmConfigurationModel> _alarms;
        private int _nextId = 1;

        public AlarmConfigurationService(IOptions<ConfigSettings> configSettings )
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
            await LoadFromCsvAsync();
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
            alarm.Id = _nextId++;
            _alarms.Add(alarm);
            await SaveToCsvAsync();
            return alarm;
        }

        public async Task<bool> UpdateAlarmAsync(AlarmConfigurationModel alarm)
        {
            var existing = _alarms.FirstOrDefault(a => a.Id == alarm.Id);
            if (existing == null) return false;

            var index = _alarms.IndexOf(existing);
            _alarms[index] = alarm;
            await SaveToCsvAsync();
            return true;
        }

        public async Task<bool> DeleteAlarmAsync(int id)
        {
            var alarm = _alarms.FirstOrDefault(a => a.Id == id);
            if (alarm == null) return false;

            _alarms.Remove(alarm);
            await SaveToCsvAsync();
            return true;
        }

        public async Task<bool> AcknowledgeAlarmAsync(int id)
        {
            var alarm = _alarms.FirstOrDefault(a => a.Id == id);
            if (alarm == null) return false;

            alarm.AlarmAckTime = DateTime.Now;
            await SaveToCsvAsync();
            return true;
        }

        private async Task LoadFromCsvAsync()
        {
            if (!File.Exists(_csvFilePath))
            {
                await SaveToCsvAsync();
                return;
            }

            try
            {
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
                Console.WriteLine($"Error loading alarms CSV: {ex.Message}");
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
                Console.WriteLine($"Error saving alarms CSV: {ex.Message}");
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
            catch
            {
                return null;
            }
        }

        private List<string> SplitCsvLine(string line)
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
