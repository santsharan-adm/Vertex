using IPCSoftware.App;
using IPCSoftware.App.Services;
using IPCSoftware.App.Services.UI;
using IPCSoftware.Core.Interfaces.AppLoggerInterface;
using IPCSoftware.Shared;
using IPCSoftware.Shared.Models;
using IPCSoftware.Shared.Models.ConfigModels;
using IPCSoftware.Shared.Models.Messaging;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;

namespace IPCSoftware.App.ViewModels
{
    public class AlarmViewModel : BaseViewModel
    {
        private readonly CoreClient _coreClient;

        private const int PageSize = 100;
        private int _historyPageIndex = 0;
        private int _totalRecords = 0;

        public string PageInfo => TotalPages == 0
            ? "No records"
            : $"Page {_historyPageIndex + 1} / {TotalPages} ({_totalRecords} records)";

        private int TotalPages => _totalRecords == 0 ? 0 : (int)Math.Ceiling(_totalRecords / (double)PageSize);
        public bool CanPrevPage => _historyPageIndex > 0;
        public bool CanNextPage => (_historyPageIndex + 1) < TotalPages;

        private bool _showActiveOnly = true;
        public bool ShowActiveOnly
        {
            get => _showActiveOnly;
            set { SetProperty(ref _showActiveOnly, value); OnPropertyChanged(nameof(FilterButtonText)); }
        }

        public string FilterButtonText => ShowActiveOnly ? "Show All" : "Show Active";

        // Collection for the DataGrid
        public ObservableCollection<AlarmInstanceModel> ActiveAlarms { get; } =
            new ObservableCollection<AlarmInstanceModel>();

        // Commands
        public RelayCommand<AlarmInstanceModel> AcknowledgeCommand { get; }
        public ICommand GlobalAcknowledgeCommand { get; }
        public ICommand GlobalResetCommand { get; }
        public ICommand NextPageCommand { get; }
        public ICommand PrevPageCommand { get; }
        public ICommand ToggleFilterCommand { get; }

        private int _serialCounter = 0;

        public AlarmViewModel(CoreClient coreClient, IAppLogger logger) : base(logger)
        {
            _coreClient = coreClient;

            // Ensure newest alarms appear at the top of the grid
            var view = CollectionViewSource.GetDefaultView(ActiveAlarms);
            view.SortDescriptions.Clear();
            view.SortDescriptions.Add(new SortDescription(nameof(AlarmInstanceModel.AlarmTime), ListSortDirection.Descending));

            // Initialize Global Commands (Tags 38 and 39)
            GlobalAcknowledgeCommand = new RelayCommand(async () => await ExecuteGlobalWrite(ConstantValues.TAG_Global_Ack, "Global Acknowledge"));
            GlobalResetCommand = new RelayCommand(async () => await ExecuteGlobalWrite(ConstantValues.TAG_Global_Reset, "Global Reset"));

            // Initialize Row-Level Command
            AcknowledgeCommand = new RelayCommand<AlarmInstanceModel>(
                     execute: async (alarm) => await AcknowledgeAlarmRequestAsync(alarm),
                     canExecute: CanExecuteAcknowledge);

            NextPageCommand = new RelayCommand(async () => await LoadPageAsync(_historyPageIndex + 1), () => CanNextPage);
            PrevPageCommand = new RelayCommand(async () => await LoadPageAsync(_historyPageIndex - 1), () => CanPrevPage);
            ToggleFilterCommand = new RelayCommand(async () =>
            {
                ShowActiveOnly = !ShowActiveOnly;
                _historyPageIndex = 0;
                await LoadPageAsync(0);
            });

            _coreClient.OnAlarmMessageReceived += HandleIncomingAlarmMessage;
            Task.Run(() => LoadPageAsync(0));
        }

        private string GetAlarmDirectory()
        {
            var dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "alarm");
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            return dir;
        }

        private string GetCurrentAlarmFilePath()
        {
            var dir = GetAlarmDirectory();
            return Path.Combine(dir, $"alarm_{DateTime.Now:yyyyMMdd}.csv");
        }

        private void AppendAlarmRecord(AlarmInstanceModel alarm, AlarmMessageType messageType)
        {
            try
            {
                var file = GetCurrentAlarmFilePath();
                var writeHeader = !File.Exists(file);
                var sb = new StringBuilder();
                if (writeHeader)
                {
                    sb.AppendLine("SerialNo,AlarmNo,Severity,AlarmText,AlarmTime,AlarmAckTime,AcknowledgedByUser,AlarmResetTime,EventType");
                }

                if (alarm.SerialNo == 0)
                {
                    _serialCounter = Math.Max(_serialCounter, _totalRecords);
                    alarm.SerialNo = ++_serialCounter;
                }

                string Escape(string value)
                {
                    if (string.IsNullOrEmpty(value)) return string.Empty;
                    var escaped = value.Replace("\"", "\"\"");
                    return $"\"{escaped}\"";
                }

                string fmt(DateTime? dt) => dt.HasValue ? dt.Value.ToString("o") : string.Empty;

                sb.AppendLine($"{alarm.SerialNo},{alarm.AlarmNo},{Escape(alarm.Severity)},{Escape(alarm.AlarmText)},{fmt(alarm.AlarmTime)},{fmt(alarm.AlarmAckTime)},{Escape(alarm.AcknowledgedByUser)},{fmt(alarm.AlarmResetTime)},{messageType}");
                File.AppendAllText(file, sb.ToString());
                _totalRecords++;
                UpdatePagingState();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to append alarm record: {ex.Message}", LogType.Diagnostics);
            }
        }

        private List<AlarmInstanceModel> ReadAlarmFile(string file)
        {
            var list = new List<AlarmInstanceModel>();
            if (!File.Exists(file)) return list;

            var lines = File.ReadAllLines(file).Skip(1);
            foreach (var line in lines)
            {
                var parsed = ParseCsvLine(line);
                if (parsed != null)
                {
                    list.Add(parsed);
                }
            }
            return list;
        }

        private void WriteAlarmFile(string file, IEnumerable<AlarmInstanceModel> records)
        {
            var sb = new StringBuilder();
            sb.AppendLine("SerialNo,AlarmNo,Severity,AlarmText,AlarmTime,AlarmAckTime,AcknowledgedByUser,AlarmResetTime,EventType");

            string Escape(string value)
            {
                if (string.IsNullOrEmpty(value)) return string.Empty;
                var escaped = value.Replace("\"", "\"\"");
                return $"\"{escaped}\"";
            }
            string fmt(DateTime? dt) => dt.HasValue ? dt.Value.ToString("o") : string.Empty;

            foreach (var record in records.OrderBy(r => r.SerialNo))
            {
                var evt = record.AlarmResetTime.HasValue ? AlarmMessageType.Cleared : AlarmMessageType.Raised;
                sb.AppendLine($"{record.SerialNo},{record.AlarmNo},{Escape(record.Severity)},{Escape(record.AlarmText)},{fmt(record.AlarmTime)},{fmt(record.AlarmAckTime)},{Escape(record.AcknowledgedByUser)},{fmt(record.AlarmResetTime)},{evt}");
            }

            File.WriteAllText(file, sb.ToString());
        }

        private void AppendRaisedRecord(AlarmInstanceModel alarm)
        {
            var file = GetCurrentAlarmFilePath();
            if (File.Exists(file))
            {
                // keep serial counter in sync
                var existing = ReadAlarmFile(file);
                if (existing.Count > 0)
                {
                    _serialCounter = Math.Max(_serialCounter, existing.Max(r => r.SerialNo));
                }
            }

            if (alarm.SerialNo == 0)
            {
                alarm.SerialNo = ++_serialCounter;
            }

            var writeHeader = !File.Exists(file);
            var sb = new StringBuilder();
            if (writeHeader)
            {
                sb.AppendLine("SerialNo,AlarmNo,Severity,AlarmText,AlarmTime,AlarmAckTime,AcknowledgedByUser,AlarmResetTime,EventType");
            }

            string Escape(string value)
            {
                if (string.IsNullOrEmpty(value)) return string.Empty;
                var escaped = value.Replace("\"", "\"\"");
                return $"\"{escaped}\"";
            }
            string fmt(DateTime? dt) => dt.HasValue ? dt.Value.ToString("o") : string.Empty;

            sb.AppendLine($"{alarm.SerialNo},{alarm.AlarmNo},{Escape(alarm.Severity)},{Escape(alarm.AlarmText)},{fmt(alarm.AlarmTime)},{fmt(alarm.AlarmAckTime)},{Escape(alarm.AcknowledgedByUser)},{fmt(alarm.AlarmResetTime)},{AlarmMessageType.Raised}");
            File.AppendAllText(file, sb.ToString());
            _totalRecords++;
            UpdatePagingState();
        }

        private AlarmInstanceModel FindExistingForUpdate(List<AlarmInstanceModel> records, AlarmInstanceModel alarm)
        {
            if (alarm.SerialNo != 0)
                return records.FirstOrDefault(r => r.SerialNo == alarm.SerialNo);

            // fallback: find the latest active (no reset) for the same AlarmNo
            return records.Where(r => r.AlarmNo == alarm.AlarmNo && r.AlarmResetTime == null)
                          .OrderByDescending(r => r.AlarmTime)
                          .FirstOrDefault();
        }

        private void UpdateRecord(AlarmInstanceModel alarm, AlarmMessageType messageType)
        {
            var file = GetCurrentAlarmFilePath();
            var records = ReadAlarmFile(file);
            if (records.Count > 0)
            {
                _serialCounter = Math.Max(_serialCounter, records.Max(r => r.SerialNo));
            }

            var existing = FindExistingForUpdate(records, alarm);
            if (existing == null)
            {
                if (alarm.SerialNo == 0) alarm.SerialNo = ++_serialCounter;
                records.Add(alarm);
                existing = alarm;
            }
            else
            {
                alarm.SerialNo = existing.SerialNo;
                existing.AlarmNo = alarm.AlarmNo;
                existing.Severity = alarm.Severity;
                existing.AlarmText = alarm.AlarmText;
                existing.AlarmTime = alarm.AlarmTime;
                existing.AlarmAckTime = alarm.AlarmAckTime;
                existing.AcknowledgedByUser = alarm.AcknowledgedByUser;
            }

            if (messageType == AlarmMessageType.Cleared && existing.AlarmResetTime == null)
            {
                existing.AlarmResetTime = alarm.AlarmResetTime ?? DateTime.Now;
            }
            if (messageType == AlarmMessageType.Acknowledged && alarm.AlarmAckTime != null)
            {
                existing.AlarmAckTime = alarm.AlarmAckTime;
                existing.AcknowledgedByUser = alarm.AcknowledgedByUser;
            }

            _totalRecords = records.Count;
            WriteAlarmFile(file, records);
            UpdatePagingState();
        }

        private List<AlarmInstanceModel> LoadAllFromLatestFile()
        {
            try
            {
                var dir = GetAlarmDirectory();
                if (!Directory.Exists(dir)) return new List<AlarmInstanceModel>();
                var latestFile = Directory.GetFiles(dir, "alarm_*.csv").OrderByDescending(f => f).FirstOrDefault();
                if (string.IsNullOrEmpty(latestFile) || !File.Exists(latestFile)) return new List<AlarmInstanceModel>();

                var records = ReadAlarmFile(latestFile);
                if (records.Count > 0)
                {
                    _serialCounter = Math.Max(_serialCounter, records.Max(r => r.SerialNo));
                }
                _totalRecords = records.Count;
                return records.OrderByDescending(a => a.AlarmTime).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to load alarm history: {ex.Message}", LogType.Diagnostics);
                return new List<AlarmInstanceModel>();
            }
        }

        private AlarmInstanceModel ParseCsvLine(string line)
        {
            try
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
                        else { inQuotes = !inQuotes; }
                    }
                    else if (c == ',' && !inQuotes)
                    {
                        values.Add(sb.ToString()); sb.Clear();
                    }
                    else sb.Append(c);
                }
                values.Add(sb.ToString());

                // Support legacy files without SerialNo (8 columns)
                int offset = values.Count == 8 ? -1 : 0;
                int serialIndex = offset == 0 ? 0 : -1;

                int idx(int i) => i + offset;

                DateTime? ParseDt(string v) => string.IsNullOrWhiteSpace(v) ? null : DateTime.Parse(v);

                var model = new AlarmInstanceModel
                {
                    SerialNo = serialIndex >= 0 ? int.Parse(values[serialIndex]) : 0,
                    AlarmNo = int.Parse(values[idx(1)]),
                    Severity = values[idx(2)],
                    AlarmText = values[idx(3)],
                    AlarmTime = DateTime.Parse(values[idx(4)]),
                    AlarmAckTime = ParseDt(values[idx(5)]),
                    AcknowledgedByUser = values[idx(6)],
                    AlarmResetTime = ParseDt(values[idx(7)])
                };

                if (model.SerialNo == 0) model.SerialNo = ++_serialCounter;
                return model;
            }
            catch
            {
                return null;
            }
        }

        private async Task LoadPageAsync(int pageIndex)
        {
            try
            {
                var records = await Task.Run(() => LoadAllFromLatestFile());
                var filtered = ShowActiveOnly ? records.Where(r => r.AlarmResetTime == null).ToList() : records;
                _totalRecords = filtered.Count;
                _serialCounter = Math.Max(_serialCounter, _totalRecords);

                var totalPages = TotalPages;
                if (totalPages == 0) pageIndex = 0;
                else
                {
                    if (pageIndex < 0) pageIndex = 0;
                    if (pageIndex > totalPages - 1) pageIndex = totalPages - 1;
                }

                var pageItems = filtered.Skip(pageIndex * PageSize).Take(PageSize).ToList();

                Application.Current.Dispatcher.Invoke(() =>
                {
                    _historyPageIndex = pageIndex;
                    ActiveAlarms.Clear();
                    foreach (var item in pageItems)
                        ActiveAlarms.Add(item);
                    UpdatePagingState();
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to load alarm page: {ex.Message}", LogType.Diagnostics);
            }
        }

        private void UpdatePagingState()
        {
            OnPropertyChanged(nameof(PageInfo));
            OnPropertyChanged(nameof(CanPrevPage));
            OnPropertyChanged(nameof(CanNextPage));
        }

        // Update these methods in AlarmViewModel.cs

        private void UpsertAlarmRecord(AlarmInstanceModel alarm, AlarmMessageType messageType)
        {
            try
            {
                var file = GetCurrentAlarmFilePath();
                var map = new Dictionary<int, AlarmInstanceModel>(); // key: AlarmNo

                if (File.Exists(file))
                {
                    var lines = File.ReadAllLines(file).Skip(1); // skip header
                    foreach (var line in lines)
                    {
                        var parsed = ParseCsvLine(line);
                        if (parsed != null)
                        {
                            if (parsed.SerialNo == 0) parsed.SerialNo = ++_serialCounter;
                            map[parsed.AlarmNo] = parsed; // latest occurrence wins by AlarmNo
                        }
                    }
                }

                if (alarm.SerialNo == 0)
                {
                    // If an entry exists for this AlarmNo, reuse its serial
                    if (map.TryGetValue(alarm.AlarmNo, out var existing))
                    {
                        alarm.SerialNo = existing.SerialNo;
                    }
                    else
                    {
                        alarm.SerialNo = ++_serialCounter;
                    }
                }

                // Apply message updates before saving
                if (messageType == AlarmMessageType.Cleared && alarm.AlarmResetTime == null)
                {
                    alarm.AlarmResetTime = DateTime.Now;
                }

                map[alarm.AlarmNo] = alarm;

                var sb = new StringBuilder();
                sb.AppendLine("SerialNo,AlarmNo,Severity,AlarmText,AlarmTime,AlarmAckTime,AcknowledgedByUser,AlarmResetTime,EventType");

                string Escape(string value)
                {
                    if (string.IsNullOrEmpty(value)) return string.Empty;
                    var escaped = value.Replace("\"", "\"\"");
                    return $"\"{escaped}\"";
                }
                string fmt(DateTime? dt) => dt.HasValue ? dt.Value.ToString("o") : string.Empty;

                foreach (var record in map.Values.OrderByDescending(x => x.AlarmTime))
                {
                    var evt = (record.AlarmNo == alarm.AlarmNo) ? messageType : AlarmMessageType.Raised;
                    sb.AppendLine($"{record.SerialNo},{record.AlarmNo},{Escape(record.Severity)},{Escape(record.AlarmText)},{fmt(record.AlarmTime)},{fmt(record.AlarmAckTime)},{Escape(record.AcknowledgedByUser)},{fmt(record.AlarmResetTime)},{evt}");
                }

                File.WriteAllText(file, sb.ToString());
                _totalRecords = map.Count;
                UpdatePagingState();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to upsert alarm record: {ex.Message}", LogType.Diagnostics);
            }
        }

        private void HandleIncomingAlarmMessage(AlarmMessage message)
        {
            try
            {
                if (message.MessageType == AlarmMessageType.Raised && message.AlarmInstance.SerialNo == 0)
                {
                    message.AlarmInstance.SerialNo = ++_serialCounter;
                }

                switch (message.MessageType)
                {
                    case AlarmMessageType.Raised:
                        AppendRaisedRecord(message.AlarmInstance);
                        break;
                    case AlarmMessageType.Acknowledged:
                    case AlarmMessageType.Cleared:
                        UpdateRecord(message.AlarmInstance, message.MessageType);
                        break;
                }

                Application.Current.Dispatcher.Invoke(async () =>
                {
                    var alarmInstance = message.AlarmInstance;
                    var existingAlarm = FindExistingActiveInGrid(alarmInstance);

                    switch (message.MessageType)
                    {
                        case AlarmMessageType.Raised:
                            if (existingAlarm == null)
                            {
                                ActiveAlarms.Insert(0, alarmInstance);
                                if (ActiveAlarms.Count > PageSize && _historyPageIndex == 0)
                                    ActiveAlarms.RemoveAt(ActiveAlarms.Count - 1);
                            }
                            else
                            {
                                existingAlarm.Severity = alarmInstance.Severity;
                                existingAlarm.AlarmText = alarmInstance.AlarmText;
                                existingAlarm.AlarmTime = alarmInstance.AlarmTime;
                                existingAlarm.AlarmAckTime = alarmInstance.AlarmAckTime;
                                existingAlarm.AcknowledgedByUser = alarmInstance.AcknowledgedByUser;
                                existingAlarm.AlarmResetTime = alarmInstance.AlarmResetTime;
                            }
                            break;

                        case AlarmMessageType.Acknowledged:
                            if (existingAlarm != null)
                            {
                                existingAlarm.AlarmAckTime = alarmInstance.AlarmAckTime;
                                existingAlarm.AcknowledgedByUser = alarmInstance.AcknowledgedByUser;
                            }
                            break;

                        case AlarmMessageType.Cleared:
                            if (existingAlarm != null)
                            {
                                existingAlarm.AlarmResetTime = alarmInstance.AlarmResetTime ?? DateTime.Now;
                                if (ShowActiveOnly)
                                {
                                    ActiveAlarms.Remove(existingAlarm);
                                }
                            }
                            break;
                    }

                    UpdatePagingState();
                    if (ShowActiveOnly)
                    {
                        await LoadPageAsync(_historyPageIndex);
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message, LogType.Diagnostics);
            }
        }

        private async Task ExecuteGlobalWrite(int tagId, string actionName)
        {
            try
            {
                await _coreClient.WriteTagAsync(tagId, true);
                _logger.LogInfo($"{actionName} (Tag {tagId}) triggered successfully.", LogType.Audit);
                var now = DateTime.Now;
                var toUpdate = ActiveAlarms.Where(a => a.AlarmResetTime == null).ToList();
                foreach (var alarm in toUpdate)
                {
                    alarm.AlarmResetTime = now;
                    UpdateRecord(alarm, AlarmMessageType.Cleared);
                }
                if (tagId == ConstantValues.TAG_Global_Reset)
                {
                    await Task.Delay(2000);
                    await _coreClient.WriteTagAsync(tagId, false);
                    _logger.LogInfo($"{actionName} (Tag {tagId}) pulsed off.", LogType.Audit);
                }
                UpdatePagingState();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error during {actionName}: {ex.Message}", LogType.Diagnostics);
            }
        }

        private bool CanExecuteAcknowledge(AlarmInstanceModel alarm)
        {
            return alarm != null && alarm.AlarmAckTime == null;
        }

        public async Task AcknowledgeAlarmRequestAsync(AlarmInstanceModel alarm)
        {
            try
            {
                if (alarm == null) return;

                bool success = await _coreClient.AcknowledgeAlarmAsync(alarm.AlarmNo, Environment.UserName);
                if (success)
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        alarm.AlarmAckTime = DateTime.Now;
                        alarm.AcknowledgedByUser = Environment.UserName;
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message, LogType.Diagnostics);
            }
        }

        private async Task LoadInitialActiveAlarms()
        {
            // Optional: Load initial data from server if supported
            await Task.Delay(100);
        }

        private AlarmInstanceModel _currentBannerAlarm;
        public AlarmInstanceModel CurrentBannerAlarm
        {
            get => _currentBannerAlarm;
            set => SetProperty(ref _currentBannerAlarm, value);
        }


        private AlarmInstanceModel FindExistingActiveInGrid(AlarmInstanceModel alarm)
        {
            if (alarm.SerialNo != 0)
                return ActiveAlarms.FirstOrDefault(a => a.SerialNo == alarm.SerialNo);

            return ActiveAlarms.Where(a => a.AlarmNo == alarm.AlarmNo && a.AlarmResetTime == null)
                                .OrderByDescending(a => a.AlarmTime)
                                .FirstOrDefault();
        }
    }
}