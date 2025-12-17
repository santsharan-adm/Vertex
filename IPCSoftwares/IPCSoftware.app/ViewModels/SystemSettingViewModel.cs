
using IPCSoftware.App.ViewModels;
using IPCSoftware.Core.Interfaces;
using IPCSoftware.Core.Interfaces.AppLoggerInterface;
using IPCSoftware.Shared;
using IPCSoftware.Shared.Models;
using IPCSoftware.Shared.Models.ConfigModels;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Threading; // Required for Timer

public class SystemSettingViewModel : BaseViewModel
{
    private readonly IPLCService _plc;
    private readonly DispatcherTimer _clockTimer; // Timer for Live Clock

    public SystemSettingViewModel(
        IPLCService plcService,
        IAppLogger logger) : base(logger)
    {
        _plc = plcService;
        AuditLogs = new ObservableCollection<AuditLogModel>();

        SyncCommand = new RelayCommand(async () => await SyncTime());

        // 1. Initialize and Start Live Clock Timer
        _clockTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1)
        };
        _clockTimer.Tick += (s, e) => UpdateIpcTime();
        _clockTimer.Start();

        // Initial Load
        UpdateIpcTime();
        LoadPlcTime();
    }

    #region PROPERTIES

    // PLC Time
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

    // IPC Time (Live)
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

    public ObservableCollection<AuditLogModel> AuditLogs { get; set; }

    #endregion

    public ICommand SyncCommand { get; }

    // 2. Method to update IPC UI Time
    private void UpdateIpcTime()
    {
        var now = DateTime.Now;
        IpcDate = now.ToString("dd-MMM-yyyy");
        IpcTime = now.ToString("HH:mm:ss");
    }

    private void LoadPlcTime()
    {
        try
        {
            var plcDt = _plc.ReadPlcDateTime();
            if (plcDt != null)
            {
                PlcDate = plcDt.Value.ToString("dd-MMM-yyyy");
                PlcTime = plcDt.Value.ToString("HH:mm:ss");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.Message, LogType.Diagnostics);
        }
    }

    private async Task SyncTime()
    {
        try
        {
            SyncState = "Syncing";
            AddAudit("Sync triggered");

            await Task.Delay(1000); // Visual delay for effect

            // 3. Get Exact System Time
            DateTime timeToSync = DateTime.Now;

            var result = _plc.WritePlcDateTime(timeToSync);

            if (result)
            {
                SyncState = "Synced";
                AddAudit("PLC time synchronized successfully");

                // 4. Immediately update UI to reflect the synced time
                PlcDate = timeToSync.ToString("dd-MMM-yyyy");
                PlcTime = timeToSync.ToString("HH:mm:ss");
            }
            else
            {
                SyncState = "Error";
                AddAudit("PLC sync failed");
            }
        }
        catch (Exception ex)
        {
            SyncState = "Error";
            //AddAudit($"PLC sync exception: {ex.Message}");
           _logger.LogError(ex.Message, LogType.Diagnostics);
            
        }

        // Reset button state after a few seconds
        await Task.Delay(2000);
        SyncState = "Idle";
    }

    private void AddAudit(string message)
    {
        // Insert at top so newest is first
        AuditLogs.Insert(0, new AuditLogModel
        {
            Time = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
            Message = message
        });
    }
}