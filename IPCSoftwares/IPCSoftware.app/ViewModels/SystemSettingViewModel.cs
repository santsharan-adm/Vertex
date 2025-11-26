
using IPCSoftware.AppLogger.Interfaces;
using IPCSoftware.AppLogger.Models;
using IPCSoftware.AppLogger.Services;
using IPCSoftware.Core.Interfaces;
using IPCSoftware.Shared;
using IPCSoftware.Shared.Models;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;

public class SystemSettingViewModel : BaseViewModel
{
    private readonly IPLCService _plc;

    private readonly IAppLogger _logger;

    private readonly LogManager _logManager;

    public SystemSettingViewModel(IPLCService plcService, IAppLogger logger, LogManager logManager)
    {
        _plc = plcService;
        _logger = logger;
        _logManager = logManager;
        AuditLogs = new ObservableCollection<AuditLogModel>();

        SyncCommand = new RelayCommand(async () => await SyncTime());


        LoadTimes();
    }

    #region PROPERTIES

    public string PlcDate { get; set; }
    public string PlcTime { get; set; }

    public string IpcDate => DateTime.Now.ToString("dd-MMM-yyyy");
    public string IpcTime => DateTime.Now.ToString("HH:mm:ss");

    private string _syncState = "Idle";
    public string SyncState
    {
        get => _syncState;
        set { _syncState = value; OnPropertyChanged(); }
    }

    public ObservableCollection<AuditLogModel> AuditLogs { get; set; }

    #endregion

    public ICommand SyncCommand { get; }

    private void LoadTimes()
    {
        var plcDt = _plc.ReadPlcDateTime();

        if (plcDt != null)
        {
            PlcDate = plcDt.Value.ToString("dd-MMM-yyyy");
            PlcTime = plcDt.Value.ToString("HH:mm:ss");
            OnPropertyChanged(nameof(PlcDate));
            OnPropertyChanged(nameof(PlcTime));
        }
    }

    private async Task SyncTime()
    {
        try
        {
            SyncState = "Syncing";
            AddAudit("Sync triggered");

            await Task.Delay(1500); // simulate command sending

            var result = _plc.WritePlcDateTime(DateTime.Now);

            if (result)
            {
                SyncState = "Synced";
                AddAudit("PLC time synchronized");
                LoadTimes();
            }
            else
            {
                SyncState = "Error";
                AddAudit("PLC sync failed");
            }
        }
        catch
        {
            SyncState = "Error";
            AddAudit("PLC sync exception");
        }
    }

    private void AddAudit(string message)
    {
        _logger.LogInfo(message, LogType.Audit);

        AuditLogs.Add(new AuditLogModel
        {
            Time = DateTime.Now.ToString("HH:mm:ss"),
            Message = message
        });
    }

    private void LoadFromAuditCsv()
    {
        var todayLogs = _logManager.ReadLogs(LogType.Audit);

        foreach (var log in todayLogs)
            AuditLogs.Add(new AuditLogModel
            {
                Time = log.Timestamp.ToString("HH:mm:ss"),
                Message = log.Message
            });
    }


}


