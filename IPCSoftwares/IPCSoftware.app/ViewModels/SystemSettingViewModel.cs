using IPCSoftware.App.Helpers;
using IPCSoftware.App.Services;
using IPCSoftware.App.Services.UI;
using IPCSoftware.Core.Interfaces.AppLoggerInterface;
using IPCSoftware.Services.AppLoggerServices; // For LogService
using IPCSoftware.Shared;
using IPCSoftware.Shared.Models;
using IPCSoftware.Shared.Models.ConfigModels;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess; // Requires reference to System.ServiceProcess
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace IPCSoftware.App.ViewModels
{
    public class SystemSettingViewModel : BaseViewModel, IDisposable
    {
        private readonly CoreClient _coreClient;
        private readonly ILogService _logService; // New Injection

        private readonly SafePoller _clockPoller;
        private readonly SafePoller _plcPoller;
        private readonly SafePoller _servicePoller; // Poller for Service Status

        private const string TARGET_SERVICE_NAME = "IPCSoftware.CoreService"; // Name of your Windows Service

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

        // Service Status Properties
        private string _serviceStatus = "Checking...";
        public string ServiceStatus
        {
            get => _serviceStatus;
            set
            {
                if (SetProperty(ref _serviceStatus, value))
                {
                    OnPropertyChanged(nameof(IsServiceRunning));
                    OnPropertyChanged(nameof(ServiceStatusColor));
                }
            }
        }

        public bool IsServiceRunning => ServiceStatus == "Running";
        public string ServiceStatusColor => IsServiceRunning ? "#28A745" : (ServiceStatus == "Stopped" ? "#DC3545" : "#FFC107");

        public ObservableCollection<AuditLogModel> AuditLogs { get; set; } = new ObservableCollection<AuditLogModel>();

        public ICommand SyncCommand { get; }
        public ICommand StartServiceCommand { get; }
        public ICommand StopServiceCommand { get; }
        public ICommand RefreshLogCommand { get; }

        public SystemSettingViewModel(
            CoreClient coreClient,
            ILogService logService, // Inject LogService
            IAppLogger logger) : base(logger)
        {
            _coreClient = coreClient;
            _logService = logService;

            SyncCommand = new RelayCommand(async () => await SyncTime());
            StartServiceCommand = new RelayCommand(StartService, () => !IsServiceRunning);
            StopServiceCommand = new RelayCommand(StopService, () => IsServiceRunning);
          //  RefreshLogCommand = new RelayCommand(async () => await LoadAuditHistory());

            // 1. Clock Poller
            _clockPoller = new SafePoller(TimeSpan.FromSeconds(1), UpdateIpcTimeAsync);
            _clockPoller.Start();

            // 2. PLC Data Poller
            _plcPoller = new SafePoller(TimeSpan.FromMilliseconds(500), PlcPollTickAsync,
             ex => _logger.LogError($"PLC Poll Error: {ex.Message}", LogType.Diagnostics));
            _plcPoller.Start();

            // 3. Service Status Poller (Check every 2 seconds)
            _servicePoller = new SafePoller(TimeSpan.FromSeconds(2), CheckServiceStatus);
            _servicePoller.Start();

            UpdateIpcTimeAsync();

            // Load Historical Logs
           // _ = LoadAuditHistory();
        }

        //private async Task LoadAuditHistory()
        //{
        //    try
        //    {
        //        // Read Audit logs from the CSV file
        //        //var logs = await _logService.ReadLogs(LogType.Audit, DateTime.Now);

        //        Application.Current.Dispatcher.Invoke(() =>
        //        {
        //            AuditLogs.Clear();
        //            foreach (var log in logs)
        //            {
        //                AuditLogs.Add(new AuditLogModel
        //                {
        //                    Time = log.Timestamp.ToString("yyyy-MM-dd HH:mm:ss"),
        //                    Message = log.Message
        //                });
        //            }
        //        });
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError($"Failed to load audit history: {ex.Message}", LogType.Diagnostics);
        //    }
        //}

        // --- SERVICE CONTROL ---
        private async Task CheckServiceStatus()
        {
            try
            {
                using (ServiceController sc = new ServiceController(TARGET_SERVICE_NAME))
                {
                    ServiceControllerStatus status = sc.Status;
                    string statusStr = status.ToString();

                    Application.Current.Dispatcher.Invoke(() => ServiceStatus = statusStr);
                }
            }
            catch (Exception)
            {
                // Service might not be installed or permission denied
                Application.Current.Dispatcher.Invoke(() => ServiceStatus = "Not Found/Access Denied");
            }
            await Task.CompletedTask;
        }

        private void StartService()
        {
            RunServiceCommand("start");
        }

        private void StopService()
        {
            RunServiceCommand("stop");
        }

        private void RunServiceCommand(string action)
        {
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo("net", $"{action} {TARGET_SERVICE_NAME}");
                psi.Verb = "runas"; // Run as Administrator
                psi.UseShellExecute = true;
                psi.CreateNoWindow = true;
                Process.Start(psi);
               var auditMsg =  action == "start" ? "Started" : "Stopped";
                AddAudit($"Service {auditMsg}");

                _logger.LogInfo($"Service {auditMsg}", LogType.Audit);
                // Optimistic status update, poller will correct it
                ServiceStatus = action == "start" ? "Starting..." : "Stopping...";
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to {action} service: {ex.Message}", LogType.Diagnostics);
                MessageBox.Show($"Could not {action} service. Require Admin privileges.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // --- EXISTING LOGIC ---
        private Task UpdateIpcTimeAsync()
        {
            var now = DateTime.Now;
            IpcDate = now.ToString("dd-MMM-yyyy");
            IpcTime = now.ToString("HH:mm:ss");
            OnPropertyChanged(nameof(IpcTime));
            return Task.CompletedTask;
        }

        private async Task PlcPollTickAsync()
        {
            var data = await _coreClient.GetIoValuesAsync(5);

            if (data.Count > 0)
            {
                int y = GetInt(data, ConstantValues.TAG_Time_Year.Read);
                int M = GetInt(data, ConstantValues.TAG_Time_Month.Read);
                int d = GetInt(data, ConstantValues.TAG_Time_Day.Read);
                int h = GetInt(data, ConstantValues.TAG_Time_Hour.Read);
                int m = GetInt(data, ConstantValues.TAG_Time_Minute.Read);
                int s = GetInt(data, ConstantValues.TAG_Time_Second.Read);

                if (y > 0 && M > 0 && d > 0)
                {
                    if (y < 100) y += 2000;

                    try
                    {
                        var dt = new DateTime(y, M, d, h, m, s);
                        PlcDate = dt.ToString("dd-MMM-yyyy");
                        PlcTime = dt.ToString("HH:mm:ss");
                    }
                    catch
                    {
                        PlcDate = "--/--/----";
                        PlcTime = "--:--:--";
                    }
                }
            }
        }

        private async Task SyncTime()
        {
            try
            {
                SyncState = "Syncing";
                AddAudit("Sync triggered");

                DateTime targetTime = DateTime.Now;
                _logger.LogInfo($"Syncing PLC Time to: {targetTime:yyyy-MM-dd HH:mm:ss}", LogType.Audit);

                await _coreClient.WriteTagAsync(ConstantValues.TAG_Time_Year.Write, targetTime.Year);
                await _coreClient.WriteTagAsync(ConstantValues.TAG_Time_Month.Write, targetTime.Month);
                await _coreClient.WriteTagAsync(ConstantValues.TAG_Time_Day.Write, targetTime.Day);
                await _coreClient.WriteTagAsync(ConstantValues.TAG_Time_Hour.Write, targetTime.Hour);
                await _coreClient.WriteTagAsync(ConstantValues.TAG_Time_Minute.Write, targetTime.Minute);
                await _coreClient.WriteTagAsync(ConstantValues.TAG_Time_Second.Write, targetTime.Second);

                await _coreClient.WriteTagAsync(ConstantValues.TAG_TimeSync_Ack, 1);
                await Task.Delay(200);
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
            // Insert locally for immediate feedback, reload will sync fully later
            AuditLogs.Insert(0, new AuditLogModel
            {
                Time = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                Message = message
            });
        }

        public void Dispose()
        {
            _clockPoller.Dispose();
            _plcPoller.Dispose();
            _servicePoller.Dispose();
        }
    }
}