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

namespace IPCSoftware.App.Services
{
 
    public class AlarmHistoryService : IAlarmHistoryService
    {
        private readonly string _dataFolder;
        private readonly IAppLogger _logger;

        public AlarmHistoryService(IOptions<ConfigSettings> configSettings, IAppLogger logger)
        {
            _logger = logger;
            var config = configSettings.Value;

            // Your specific folder logic
            string dataFolderPath = config.DataFolder;
            _dataFolder = dataFolderPath ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data");

            if (!Directory.Exists(_dataFolder))
            {
                Directory.CreateDirectory(_dataFolder);
            }
        }

        public async Task LogHistoryAsync(AlarmInstanceModel alarm, string user)
        {
            try
            {
                string fileName = $"AlarmHistory_{DateTime.Now:yyyyMMdd}.csv";
                string filePath = Path.Combine(_dataFolder, fileName);
                bool fileExists = File.Exists(filePath);

                // CSV Format: AlarmNo, Message, Severity, RaisedTime, ResetTime, ResetBy
                var csvLine = $"{alarm.AlarmNo},{EscapeCsv(alarm.AlarmText)},{alarm.Severity}," +
                              $"{alarm.AlarmTime:yyyy-MM-dd HH:mm:ss},{DateTime.Now:yyyy-MM-dd HH:mm:ss},{user}";

                using (var sw = new StreamWriter(filePath, true, Encoding.UTF8))
                {
                    if (!fileExists)
                    {
                        await sw.WriteLineAsync("AlarmNo,Message,Severity,RaisedTime,ResetTime,ResetBy");
                    }
                    await sw.WriteLineAsync(csvLine);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to log alarm history: {ex.Message}", LogType.Diagnostics);
            }
        }

        public async Task<List<AlarmHistoryModel>> GetHistoryAsync(DateTime date)
        {
            var list = new List<AlarmHistoryModel>();
            string fileName = $"AlarmHistory_{date:yyyyMMdd}.csv";
            string filePath = Path.Combine(_dataFolder, fileName);

            if (!File.Exists(filePath)) return list;

            try
            {
                var lines = await File.ReadAllLinesAsync(filePath);
                foreach (var line in lines.Skip(1)) // Skip header
                {
                    var parts = SplitCsvLine(line);
                    if (parts.Count >= 6)
                    {
                        list.Add(new AlarmHistoryModel
                        {
                            AlarmNo = int.Parse(parts[0]),
                            AlarmText = parts[1],
                            Severity = parts[2],
                            RaisedTime = DateTime.Parse(parts[3]),
                            ResetTime = DateTime.Parse(parts[4]),
                            ResetBy = parts[5]
                        });
                    }
                }
            }
            catch { }
            return list.OrderByDescending(x => x.ResetTime).ToList();
        }

        private string EscapeCsv(string text)
        {
            if (string.IsNullOrEmpty(text)) return "";
            return text.Contains(",") ? $"\"{text}\"" : text;
        }

        private List<string> SplitCsvLine(string line)
        {
            // Basic CSV Splitter
            var result = new List<string>();
            bool inQuotes = false;
            var sb = new StringBuilder();
            foreach (char c in line)
            {
                if (c == '\"') inQuotes = !inQuotes;
                else if (c == ',' && !inQuotes) { result.Add(sb.ToString()); sb.Clear(); }
                else sb.Append(c);
            }
            result.Add(sb.ToString());
            return result;
        }
    }
}