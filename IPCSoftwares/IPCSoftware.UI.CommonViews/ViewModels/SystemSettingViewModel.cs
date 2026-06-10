using IPCSoftware.Common.CommonExtensions;
using IPCSoftware.Common.UIClientComm;
using IPCSoftware.Common.WPFExtensions;
using IPCSoftware.Core.Interfaces.AppLoggerInterface;
using IPCSoftware.Services.AppLoggerServices;
using IPCSoftware.Shared;
using IPCSoftware.Shared.Models;
using IPCSoftware.Shared.Models.ConfigModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Security.Principal;
using System.ServiceProcess;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace IPCSoftware.UI.CommonViews.ViewModels
{
    public class SystemSettingViewModel : BaseViewModel, IDisposable
    {
        private readonly CoreClient _coreClient;
        private readonly ILogService _logService;

        private readonly SafePollerEx _clockPoller;
        private readonly SafePollerEx _plcPoller;
        private readonly SafePollerEx _servicePoller;

        // Use actual Windows service name
        private const string TARGET_SERVICE_NAME = "IPCSoftware.CoreService.AOI";

        private string _plcDate = "--/--/----";
        public string PlcDate
        {
            get => _plcDate;
            set => SetProperty(ref _plcDate, value);
        }

        private string _plcTime = "--:--:--";
        public string PlcTime
        {
            get => _plcTime;
            set => SetProperty(ref _plcTime, value);
        }

        // Added for second plc clock


        private string _plc2Date = "--/--/----";
        public string Plc2Date
        {
            get => _plc2Date;
            set => SetProperty(ref _plc2Date, value);
        }

        private string _plc2Time = "--:--:--";
        public string Plc2Time
        {
            get => _plc2Time;
            set => SetProperty(ref _plc2Time, value);
        }


        private string _ipcDate;
        public string IpcDate
        {
            get => _ipcDate;
            set => SetProperty(ref _ipcDate, value);
        }

        private string _ipcTime;
        public string IpcTime
        {
            get => _ipcTime;
            set => SetProperty(ref _ipcTime, value);
        }

        private string _syncState = "Idle";
        public string SyncState
        {
            get => _syncState;
            set => SetProperty(ref _syncState, value);
        }

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

                    // Re-evaluate button CanExecute
                    CommandManager.InvalidateRequerySuggested();
                }
            }
        }

        public bool IsServiceRunning => ServiceStatus == "Running";

        public string ServiceStatusColor =>
            ServiceStatus == "Running" ? "#28A745" :
            ServiceStatus == "Stopped" ? "#DC3545" :
            "#FFC107";

        public ObservableCollection<AuditLogModel> AuditLogs { get; } = new ObservableCollection<AuditLogModel>();

        public ICommand SyncCommand { get; }

        //public ICommand SyncCommand2 { get; }
        public ICommand StartServiceCommand { get; }
        public ICommand StopServiceCommand { get; }

        public SystemSettingViewModel(
            CoreClient coreClient,
            ILogService logService,
            IAppLogger logger) : base(logger)
        {
            _coreClient = coreClient;
            _logService = logService;

            SyncCommand = new RelayCommand(async () => await SyncTime());
            //SyncCommand2 = new RelayCommand(async () => await SyncTime2());
            StartServiceCommand = new RelayCommand(async () => await StartServiceAsync(), () => !IsServiceRunning);
            StopServiceCommand = new RelayCommand(async () => await StopServiceAsync(), () => IsServiceRunning);

            _clockPoller = new SafePollerEx(_coreClient,TimeSpan.FromSeconds(1), UpdateIpcTimeAsync,_logger,ex => _logger.LogError($"Clock Poll Error: {ex.Message}", LogType.Diagnostics));
            _clockPoller.Start();

            _plcPoller = new SafePollerEx(_coreClient,
                TimeSpan.FromMilliseconds(500),
                PlcPollTickAsync,_logger,
                ex => _logger.LogError($"PLC Poll Error: {ex.Message}", LogType.Diagnostics));
            _plcPoller.Start();

            _servicePoller = new SafePollerEx(_coreClient,
                TimeSpan.FromSeconds(2),
                CheckServiceStatusAsync,_logger,
                ex => _logger.LogError($"Service Poll Error: {ex.Message}", LogType.Diagnostics));
            _servicePoller.Start();

            _ = UpdateIpcTimeAsync(new Dictionary<int, object>());
            _ = CheckServiceStatusAsync(new Dictionary<int, object>());
        }

        private async Task CheckServiceStatusAsync(Dictionary<int, object> data)
        {
            try
            {
                using var sc = new ServiceController(TARGET_SERVICE_NAME);
                sc.Refresh();

                string statusText = sc.Status switch
                {
                    ServiceControllerStatus.Running => "Running",
                    ServiceControllerStatus.Stopped => "Stopped",
                    ServiceControllerStatus.StartPending => "Starting...",
                    ServiceControllerStatus.StopPending => "Stopping...",
                    ServiceControllerStatus.Paused => "Paused",
                    ServiceControllerStatus.PausePending => "Pausing...",
                    ServiceControllerStatus.ContinuePending => "Resuming...",
                    _ => sc.Status.ToString()
                };

                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    ServiceStatus = statusText;
                });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError($"Service '{TARGET_SERVICE_NAME}' not found. {ex.Message}", LogType.Diagnostics);

                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    ServiceStatus = "Service Not Installed";
                });
            }
            catch (Win32Exception ex)
            {
                _logger.LogError($"Access denied while checking service status. {ex.Message}", LogType.Diagnostics);

                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    ServiceStatus = "Access Denied";
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Unexpected error while checking service status: {ex.Message}", LogType.Diagnostics);

                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    ServiceStatus = "Unknown Error";
                });
            }
        }

        private async Task StartServiceAsync()
        {
            try
            {
                if (!IsAdministrator())
                {
                    MessageBox.Show(
                        "Please run the application as Administrator to start the service.",
                        "Admin Rights Required",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);

                    _logger.LogError("Start service failed: UI is not running as Administrator.", LogType.Diagnostics);
                    return;
                }

                using var sc = new ServiceController(TARGET_SERVICE_NAME);
                sc.Refresh();

                if (sc.Status == ServiceControllerStatus.Running)
                {
                    ServiceStatus = "Running";
                    return;
                }

                if (sc.Status == ServiceControllerStatus.StartPending)
                {
                    ServiceStatus = "Starting...";
                    return;
                }

                ServiceStatus = "Starting...";
                sc.Start();

                await Task.Run(() =>
                    sc.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromSeconds(20)));

                sc.Refresh();
                ServiceStatus = "Running";

                AddAudit("Service Started");
                _logger.LogInfo("Service Started", LogType.Audit);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError($"Service '{TARGET_SERVICE_NAME}' not found or cannot be started. {ex.Message}", LogType.Diagnostics);
                ServiceStatus = "Service Not Installed";

                MessageBox.Show(
                    $"Service '{TARGET_SERVICE_NAME}' was not found.",
                    "Service Not Found",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            catch (Win32Exception ex)
            {
                _logger.LogError($"Access denied while starting service. {ex.Message}", LogType.Diagnostics);
                ServiceStatus = "Access Denied";

                MessageBox.Show(
                    "Access denied while starting the service. Run the UI as Administrator.",
                    "Permission Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            catch (System.TimeoutException ex)
            {
                _logger.LogError($"Service start timed out. {ex.Message}", LogType.Diagnostics);
                ServiceStatus = "Start Timed Out";

                MessageBox.Show(
                    "Service start timed out. Check service logs and dependencies.",
                    "Timeout",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to start service: {ex.Message}", LogType.Diagnostics);
                ServiceStatus = "Start Failed";

                MessageBox.Show(
                    $"Could not start service.\n{ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            finally
            {
                await CheckServiceStatusAsync(new Dictionary<int, object>());
            }
        }

        private async Task StopServiceAsync()
        {
            try
            {
                if (!IsAdministrator())
                {
                    MessageBox.Show(
                        "Please run the application as Administrator to stop the service.",
                        "Admin Rights Required",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);

                    _logger.LogError("Stop service failed: UI is not running as Administrator.", LogType.Diagnostics);
                    return;
                }

                using var sc = new ServiceController(TARGET_SERVICE_NAME);
                sc.Refresh();

                if (sc.Status == ServiceControllerStatus.Stopped)
                {
                    ServiceStatus = "Stopped";
                    return;
                }

                if (sc.Status == ServiceControllerStatus.StopPending)
                {
                    ServiceStatus = "Stopping...";
                    return;
                }

                ServiceStatus = "Stopping...";
                sc.Stop();

                await Task.Run(() =>
                    sc.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(20)));

                sc.Refresh();
                ServiceStatus = "Stopped";

                AddAudit("Service Stopped");
                _logger.LogInfo("Service Stopped", LogType.Audit);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError($"Service '{TARGET_SERVICE_NAME}' not found or cannot be stopped. {ex.Message}", LogType.Diagnostics);
                ServiceStatus = "Service Not Installed";

                MessageBox.Show(
                    $"Service '{TARGET_SERVICE_NAME}' was not found.",
                    "Service Not Found",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            catch (Win32Exception ex)
            {
                _logger.LogError($"Access denied while stopping service. {ex.Message}", LogType.Diagnostics);
                ServiceStatus = "Access Denied";

                MessageBox.Show(
                    "Access denied while stopping the service. Run the UI as Administrator.",
                    "Permission Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            catch (System.TimeoutException ex)
            {
                _logger.LogError($"Service stop timed out. {ex.Message}", LogType.Diagnostics);
                ServiceStatus = "Stop Timed Out";

                MessageBox.Show(
                    "Service stop timed out. Check whether the service is hanging.",
                    "Timeout",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to stop service: {ex.Message}", LogType.Diagnostics);
                ServiceStatus = "Stop Failed";

                MessageBox.Show(
                    $"Could not stop service.\n{ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            finally
            {
                await CheckServiceStatusAsync(new Dictionary<int, object>());
            }
        }

        private bool IsAdministrator()
        {
            using var identity = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }

        private Task UpdateIpcTimeAsync(Dictionary<int, object> data)
        {
            var now = DateTime.Now;
            IpcDate = now.ToString("dd-MMM-yyyy");
            IpcTime = now.ToString("HH:mm:ss");
            OnPropertyChanged(nameof(IpcTime));
            return Task.CompletedTask;
        }

        private async Task PlcPollTickAsync(Dictionary<int, object> data)
        {
            var plcdta = await _coreClient.GetIoValuesAsync(5);

            if (plcdta.Count > 0)
            {
                //PLC1 Clock Read
                int y1 = GetInt(plcdta, ConstantValues.TAG_Time_Year.Read);
                int M1 = GetInt(plcdta, ConstantValues.TAG_Time_Month.Read);
                int d1 = GetInt(plcdta, ConstantValues.TAG_Time_Day.Read);
                int h1 = GetInt(plcdta, ConstantValues.TAG_Time_Hour.Read);
                int m1 = GetInt(plcdta, ConstantValues.TAG_Time_Minute.Read);
                int s1 = GetInt(plcdta, ConstantValues.TAG_Time_Second.Read);
                //PLC2 Clock Read

                //int y2 = GetInt(plcdta, ConstantValues.PLC2_TAG_Time_Year.Read);
                //int M2 = GetInt(plcdta, ConstantValues.PLC2_TAG_Time_Month.Read);
                //int d2 = GetInt(plcdta, ConstantValues.PLC2_TAG_Time_Day.Read);
                //int h2 = GetInt(plcdta, ConstantValues.PLC2_TAG_Time_Hour.Read);
                //int m2 = GetInt(plcdta, ConstantValues.PLC2_TAG_Time_Minute.Read);
                //int s2 = GetInt(plcdta, ConstantValues.PLC2_TAG_Time_Second.Read);

                if (y1 > 0 && M1 > 0 && d1 > 0)
                {
                    if (y1 < 100) y1 += 2000;

                    try
                    {
                        var dt = new DateTime(y1, M1, d1, h1, m1, s1);
                        PlcDate = dt.ToString("dd-MMM-yyyy");
                        PlcTime = dt.ToString("HH:mm:ss");
                    }
                    catch
                    {
                        PlcDate = "--/--/----";
                        PlcTime = "--:--:--";
                    }
                }

                //if (y2 > 0 && M2 > 0 && d2 > 0)
                //{
                //    if (y2 < 100) y2 += 2000;

                //    try
                //    {
                //        var dt = new DateTime(y2, M2, d2, h2, m2, s2);
                //        Plc2Date = dt.ToString("dd-MMM-yyyy");
                //        Plc2Time = dt.ToString("HH:mm:ss");
                //    }
                //    catch
                //    {
                //        Plc2Date = "--/--/----";
                //        Plc2Time = "--:--:--";
                //    }
                //}


            }
        }

        private async Task SyncTime()
        {
            try
            {
                SyncState = "Syncing";
                AddAudit("Sync PLC 1 triggered");

                DateTime targetTime = DateTime.Now;
                _logger.LogInfo($"Syncing PLC 1 Time to: {targetTime:yyyy-MM-dd HH:mm:ss}", LogType.Audit);

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
                AddAudit("PLC 1 time sync command sent.");
            }
            catch (Exception ex)
            {
                SyncState = "Error";
                AddAudit($"Sync PLC 1 failed: {ex.Message}");
                _logger.LogError(ex.Message, LogType.Diagnostics);
            }

            await Task.Delay(2000);
            SyncState = "Idle";
        }

        //private async Task SyncTime2()
        //{
        //    try
        //    {
        //        SyncState = "Syncing";
        //        AddAudit("Sync PLC 2 triggered");

        //        DateTime targetTime = DateTime.Now;
        //        _logger.LogInfo($"Syncing PLC 2 Time to: {targetTime:yyyy-MM-dd HH:mm:ss}", LogType.Audit);

        //        await _coreClient.WriteTagAsync(ConstantValues.PLC2_TAG_Time_Year.Write, targetTime.Year);
        //        await _coreClient.WriteTagAsync(ConstantValues.PLC2_TAG_Time_Month.Write, targetTime.Month);
        //        await _coreClient.WriteTagAsync(ConstantValues.PLC2_TAG_Time_Day.Write, targetTime.Day);
        //        await _coreClient.WriteTagAsync(ConstantValues.PLC2_TAG_Time_Hour.Write, targetTime.Hour);
        //        await _coreClient.WriteTagAsync(ConstantValues.PLC2_TAG_Time_Minute.Write, targetTime.Minute);
        //        await _coreClient.WriteTagAsync(ConstantValues.PLC2_TAG_Time_Second.Write, targetTime.Second);

        //        await _coreClient.WriteTagAsync(ConstantValues.TAG_TimeSync_Ack, 1);
        //        await Task.Delay(200);
        //        await _coreClient.WriteTagAsync(ConstantValues.TAG_TimeSync_Ack, 0);

        //        SyncState = "Synced";
        //        AddAudit("PLC 2 time sync command sent.");
        //    }
        //    catch (Exception ex)
        //    {
        //        SyncState = "Error";
        //        AddAudit($"Sync PLC 2 failed: {ex.Message}");
        //        _logger.LogError(ex.Message, LogType.Diagnostics);
        //    }

        //    await Task.Delay(2000);
        //    SyncState = "Idle";
        //}
        private int GetInt(Dictionary<int, object> data, int tagId)
        {
            if (data.TryGetValue(tagId, out object val))
            {
                try
                {
                    return Convert.ToInt32(val);
                }
                catch
                {
                }
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
            _clockPoller?.Dispose();
            _plcPoller?.Dispose();
            _servicePoller?.Dispose();
        }
    }
}