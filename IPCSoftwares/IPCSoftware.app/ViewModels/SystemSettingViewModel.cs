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
                    int y = GetInt(data, ConstantValues.TAG_Time_Year.Read);
                    int M = GetInt(data, ConstantValues.TAG_Time_Month.Read);
                    int d = GetInt(data, ConstantValues.TAG_Time_Day.Read);
                    int h = GetInt(data, ConstantValues.TAG_Time_Hour.Read);
                    int m = GetInt(data, ConstantValues.TAG_Time_Minute.Read);
                    int s = GetInt(data, ConstantValues.TAG_Time_Second.Read);

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
                DateTime targetTime = DateTime.Now;

                _logger.LogInfo($"Syncing PLC Time to: {targetTime:yyyy-MM-dd HH:mm:ss}", LogType.Audit);

                // 2. Write Time Values
                await _coreClient.WriteTagAsync(ConstantValues.TAG_Time_Year.Write, targetTime.Year);
                await _coreClient.WriteTagAsync(ConstantValues.TAG_Time_Month.Write, targetTime.Month);
                await _coreClient.WriteTagAsync(ConstantValues.TAG_Time_Day.Write, targetTime.Day);
                await _coreClient.WriteTagAsync(ConstantValues.TAG_Time_Hour.Write, targetTime.Hour);
                await _coreClient.WriteTagAsync(ConstantValues.TAG_Time_Minute.Write, targetTime.Minute);
                await _coreClient.WriteTagAsync(ConstantValues.TAG_Time_Second.Write, targetTime.Second);

                // 3. Pulse Trigger (A1)
                await _coreClient.WriteTagAsync(ConstantValues.TAG_TimeSync_Ack, 1);
                await Task.Delay(200); // Hold pulse
                await _coreClient.WriteTagAsync(ConstantValues.TAG_TimeSync_Ack, 0);

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