using IPCSoftware.App.Services;
using IPCSoftware.App.Services.UI;
using IPCSoftware.Core.Interfaces.AppLoggerInterface;
using IPCSoftware.Shared;
using IPCSoftware.Shared.Models;
using IPCSoftware.Shared.Models.ConfigModels;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Threading;

namespace IPCSoftware.App.ViewModels
{
    public class SystemSettingViewModel : BaseViewModel, IDisposable
    {
        private readonly CoreClient _coreClient;
        private readonly DispatcherTimer _clockTimer;
        private readonly DispatcherTimer _plcPollTimer;

        // --- TAG CONFIGURATION ---
        // READ Tags (PLC -> IPC): DM10019...DM10024 (Example 511-516)
        private const int TAG_READ_YEAR = 4;//511;
        private const int TAG_READ_MONTH = 5; //12;
        private const int TAG_READ_DAY = 6; //;
        private const int TAG_READ_HOUR = 7 ;
        private const int TAG_READ_MIN = 8;
        private const int TAG_READ_SEC = 9;

        // WRITE Tags (IPC -> PLC): DM10019...DM10024 (Example 4-9)
        // Usually Write and Read addresses are the same for Time Sync registers, 
        // but based on your request:
        private const int TAG_WRITE_YEAR = 4;
        private const int TAG_WRITE_MONTH = 5;
        private const int TAG_WRITE_DAY = 6;
        private const int TAG_WRITE_HOUR = 7;
        private const int TAG_WRITE_MIN = 8;
        private const int TAG_WRITE_SEC = 9;

        // SYNC TRIGGER (A1)
        private const int TAG_SYNC_TRIGGER = 3;

        // --- Properties ---
        private string _plcDate = "--/--/----";
        public string PlcDate { get => _plcDate; set => SetProperty(ref _plcDate, value); }

        private string _plcTime = "--:--:--";
        public string PlcTime { get => _plcTime; set => SetProperty(ref _plcTime, value); }

        private string _ipcDate;
        public string IpcDate { get => _ipcDate; set => SetProperty(ref _ipcDate, value); }

        private string _ipcTime;
        public string IpcTime { get => _ipcTime; set => SetProperty(ref _ipcTime, value); }

        private string _syncState = "Idle";
        public string SyncState { get => _syncState; set => SetProperty(ref _syncState, value); }

        public ObservableCollection<AuditLogModel> AuditLogs { get; set; } = new ObservableCollection<AuditLogModel>();

        public ICommand SyncCommand { get; }

        public SystemSettingViewModel(CoreClient coreClient, IAppLogger logger) : base(logger)
        {
            _coreClient = coreClient;

            SyncCommand = new RelayCommand(async () => await SyncTime());

            // 1. IPC Clock (1s Tick)
            _clockTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _clockTimer.Tick += (s, e) => UpdateIpcTime();
            _clockTimer.Start();

            // 2. PLC Polling (500ms Tick)
            _plcPollTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
            _plcPollTimer.Tick += PlcPollTick;
            _plcPollTimer.Start();

            UpdateIpcTime();
        }

        private void UpdateIpcTime()
        {
            var now = DateTime.Now;
            IpcDate = now.ToString("dd-MMM-yyyy");
            IpcTime = now.ToString("HH:mm:ss");
        }

        private async void PlcPollTick(object sender, EventArgs e)
        {
            try
            {
                // Request IO Packet (ID 5 assumed)
                var data = await _coreClient.GetIoValuesAsync(5);

                if (data != null)
                {
                    // Read Time Parts
                    int y = GetInt(data, TAG_READ_YEAR);
                    int M = GetInt(data, TAG_READ_MONTH);
                    int d = GetInt(data, TAG_READ_DAY);
                    int h = GetInt(data, TAG_READ_HOUR);
                    int m = GetInt(data, TAG_READ_MIN);
                    int s = GetInt(data, TAG_READ_SEC);

                    // Validate & Format
                    if (y > 0 && M > 0 && d > 0)
                    {
                        // Handle 2-digit vs 4-digit year if needed
                        if (y < 100) y += 2000;

                        try
                        {
                            var dt = new DateTime(y, M, d, h, m, s);
                            PlcDate = dt.ToString("dd-MMM-yyyy");
                            PlcTime = dt.ToString("HH:mm:ss");
                        }
                        catch(Exception ex)
                        {
                            // Invalid date from PLC (e.g. 0/0/0)
                            PlcDate = "--/--/----";
                            PlcTime = "--:--:--";
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Silent fail for polling
                // System.Diagnostics.Debug.WriteLine($"PLC Time Read Error: {ex.Message}");
            }
        }

        private async Task SyncTime()
        {
            try
            {
                SyncState = "Syncing";
                AddAudit("Sync triggered");

                // 1. Calculate Target Time (+2 Seconds Margin)
                DateTime targetTime = DateTime.Now.AddSeconds(2);

                _logger.LogInfo($"Syncing PLC Time to: {targetTime:yyyy-MM-dd HH:mm:ss}", LogType.Audit);

                // 2. Write Time Values
                await _coreClient.WriteTagAsync(TAG_WRITE_YEAR, targetTime.Year);
                await _coreClient.WriteTagAsync(TAG_WRITE_MONTH, targetTime.Month);
                await _coreClient.WriteTagAsync(TAG_WRITE_DAY, targetTime.Day);
                await _coreClient.WriteTagAsync(TAG_WRITE_HOUR, targetTime.Hour);
                await _coreClient.WriteTagAsync(TAG_WRITE_MIN, targetTime.Minute);
                await _coreClient.WriteTagAsync(TAG_WRITE_SEC, targetTime.Second);

                // 3. Pulse Trigger (A1)
                await _coreClient.WriteTagAsync(TAG_SYNC_TRIGGER, 1);
                await Task.Delay(500); // Hold pulse
                await _coreClient.WriteTagAsync(TAG_SYNC_TRIGGER, 0);

                SyncState = "Synced";
                AddAudit("PLC time sync command sent.");
            }
            catch (Exception ex)
            {
                SyncState = "Error";
                AddAudit($"Sync failed: {ex.Message}");
                _logger.LogError(ex.Message, LogType.Diagnostics);
            }

            await Task.Delay(2000);
            SyncState = "Idle";
        }

        private int GetInt(Dictionary<int, object> data, int tagId)
        {
            if (data.TryGetValue(tagId, out object val))
            {
                try { return Convert.ToInt32(val); } catch { }
            }
            return 0;
        }

        private void AddAudit(string message)
        {
            AuditLogs.Insert(0, new AuditLogModel
            {
                Time = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                Message = message
            });
        }

        public void Dispose()
        {
            _clockTimer.Stop();
            _plcPollTimer.Stop();
        }
    }
}