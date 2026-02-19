using IPCSoftware.App.Helpers;
using IPCSoftware.Core.Interfaces.AppLoggerInterface;
using IPCSoftware.Shared;
using IPCSoftware.Shared.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace IPCSoftware.App.ViewModels
{
    public class AlarmLogViewModel : BaseViewModel
    {
        private readonly string _alarmDataFolder;

        // Collections
        public ObservableCollection<LogFileInfo> AlarmFiles { get; } = new();
        public ObservableCollection<AlarmHistoryItem> SelectedAlarmRecords { get; } = new();

        // State
        private LogFileInfo _selectedFile;
        public LogFileInfo SelectedFile
        {
            get => _selectedFile;
            set
            {
                if (SetProperty(ref _selectedFile, value) && value != null)
                {
                    _ = LoadRecordsAsync(value.FullPath);
                }
            }
        }

        public ICommand RefreshFilesCommand { get; }

        public AlarmLogViewModel(IAppLogger logger) : base(logger)
        {
            // Path to alarm logs (e.g. Data/alarm)
            _alarmDataFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "alarm");

            RefreshFilesCommand = new RelayCommand(LoadFiles);

            // Initial Load
            LoadFiles();
        }

        private void LoadFiles()
        {
            try
            {
                AlarmFiles.Clear();
                SelectedAlarmRecords.Clear();

                if (!Directory.Exists(_alarmDataFolder)) return;

                var files = Directory.GetFiles(_alarmDataFolder, "alarm_*.csv")
                                     .OrderByDescending(f => f); // Newest first

                foreach (var file in files)
                {
                    var info = new FileInfo(file);
                    AlarmFiles.Add(new LogFileInfo
                    {
                        FileName = info.Name,
                        FullPath = file,
                        LastModified = info.LastWriteTime
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error loading alarm files: {ex.Message}", Shared.Models.ConfigModels.LogType.Diagnostics);
            }
        }

        private async Task LoadRecordsAsync(string filePath)
        {
            try
            {
                SelectedAlarmRecords.Clear();
                if (!File.Exists(filePath)) return;

                // Read file asynchronously
                var lines = await File.ReadAllLinesAsync(filePath);
                var tempList = new List<AlarmHistoryItem>();

                // Skip header (Row 0)
                foreach (var line in lines.Skip(1))
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;

                    var item = ParseAlarmLine(line);
                    if (item != null)
                    {
                        tempList.Add(item);
                    }
                }

                // FIX: Sort by RaisedAt Descending (Latest first)
                var sortedList = tempList.OrderByDescending(x => x.RaisedAt).ToList();

                foreach (var item in sortedList)
                {
                    SelectedAlarmRecords.Add(item);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error parsing alarm file: {ex.Message}", Shared.Models.ConfigModels.LogType.Diagnostics);
            }
        }

        private AlarmHistoryItem ParseAlarmLine(string line)
        {
            try
            {
                // Simple CSV splitter handling quotes
                var values = SplitCsvLine(line);

                // Expected Format:
                // SerialNo,AlarmNo,Severity,AlarmText,AlarmTime,AlarmAckTime,AcknowledgedByUser,AlarmResetTime,EventType
                // 0        1       2        3         4         5            6                  7              8

                if (values.Count < 5) return null;

                return new AlarmHistoryItem
                {
                    AlarmNo = int.TryParse(values[1], out int no) ? no : 0,
                    Severity = values[2],
                    Message = values[3],
                    RaisedAt = DateTime.TryParse(values[4], out DateTime rt) ? rt : (DateTime?)null,
                    ResetAt = (values.Count > 7 && DateTime.TryParse(values[7], out DateTime rst)) ? rst : (DateTime?)null
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
            var sb = new StringBuilder();
            bool inQuotes = false;
            for (int i = 0; i < line.Length; i++)
            {
                var c = line[i];
                if (c == '"')
                {
                    if (inQuotes && i + 1 < line.Length && line[i + 1] == '"') { sb.Append('"'); i++; }
                    else inQuotes = !inQuotes;
                }
                else if (c == ',' && !inQuotes)
                {
                    values.Add(sb.ToString()); sb.Clear();
                }
                else sb.Append(c);
            }
            values.Add(sb.ToString());
            return values;
        }
    }

    // DTO for Display
    public class AlarmHistoryItem
    {
        public int AlarmNo { get; set; }
        public string Severity { get; set; }
        public string Message { get; set; }
        public DateTime? RaisedAt { get; set; }
        public DateTime? ResetAt { get; set; }
    }
}