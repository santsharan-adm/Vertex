using System.Windows.Input;
using IPCSoftware.App.Bending.Models;
using IPCSoftware.Common.CommonExtensions;
using IPCSoftware.Common.UIClientComm;
using IPCSoftware.Core.Interfaces;
using IPCSoftware.Core.Interfaces.AppLoggerInterface;
using IPCSoftware.Services;
using IPCSoftware.Shared;
using IPCSoftware.Shared.Models;
using IPCSoftware.Shared.Models.ConfigModels;
using IPCSoftware.UI.CommonViews.ViewModels;
using Newtonsoft.Json;

namespace IPCSoftware.App.Bending.ViewModels
{
    public class Dashboard2ViewModel : BaseViewModel, IDisposable
    {
        // ----------------------------------------------------------------
        // DI Services
        // ----------------------------------------------------------------
        private readonly INavigationService _navigationService;
        private readonly CoreClient _coreClient;
        private readonly IDialogService _dialog;

        // ----------------------------------------------------------------
        // Pollers
        // ----------------------------------------------------------------
        private SafePollerEx _liveDataPoller;     // RequestId = 4  (OEE / Dashboard2Result)
        private SafePollerEx _ioPoller;           // RequestId = 5  (Raw IO: mode states, ack flags)
        private SafePollerEx _resetPoller;        // RequestId = 5  (Reset sequence ack check)

        private bool _disposed;

        // ----------------------------------------------------------------
        // Reset sequence state machine  (mirrors AOI OEEDashboardViewModel)
        // ----------------------------------------------------------------
        private enum ResetSequenceState { Idle, TriggerReset, WaitingForAck }
        private ResetSequenceState _resetState = ResetSequenceState.Idle;
        private DateTime _resetTimeoutStart;
        private int _resetPollerRunning = 0;

        // ----------------------------------------------------------------
        // Commands
        // ----------------------------------------------------------------
        public ICommand ToggleSidebarCommand { get; }
        public ICommand AutoRunCommand { get; }
        public ICommand DryRunCommand { get; }
        public ICommand CycleStartStopCommand { get; }
        public ICommand AcknowledgeAlarmCommand { get; }

        #region Properties

        // --- Inspection Tables (Lot 1 & Lot 2) ---

        private DashboardInspectionModel _inspectionTable = new();
        public DashboardInspectionModel InspectionTable
        {
            get => _inspectionTable;
            set => SetProperty(ref _inspectionTable, value);
        }

        private DashboardInspectionModel _inspectionTable2 = new();
        public DashboardInspectionModel InspectionTable2
        {
            get => _inspectionTable2;
            set => SetProperty(ref _inspectionTable2, value);
        }

        // --- Bending Station Indicators (Temperature, Force, Status) ---

        private BendingIndicators _bendingIndicators = new();
        public BendingIndicators BendingIndicators
        {
            get => _bendingIndicators;
            set => SetProperty(ref _bendingIndicators, value);
        }

        // --- Tray & NG Bin ---

        private InputTrayModel _inputTray = new();
        public InputTrayModel InputTray
        {
            get => _inputTray;
            set => SetProperty(ref _inputTray, value);
        }

        private OutputTrayModel _outputTray = new();
        public OutputTrayModel OutputTray
        {
            get => _outputTray;
            set => SetProperty(ref _outputTray, value);
        }

        private NGBinGroupModel _ngBin = new();
        public NGBinGroupModel NGBin
        {
            get => _ngBin;
            set => SetProperty(ref _ngBin, value);
        }

        // --- OEE / Efficiency ---

        private EfficiencyBreakdown _efficiency = new();
        public EfficiencyBreakdown Efficiency
        {
            get => _efficiency;
            set => SetProperty(ref _efficiency, value);
        }

        // --- Machine Mode States (from RequestId = 5) ---

        private bool _isAutoRunActive;
        public bool IsAutoRunActive
        {
            get => _isAutoRunActive;
            set => SetProperty(ref _isAutoRunActive, value);
        }

        private bool _isDryRunActive;
        public bool IsDryRunActive
        {
            get => _isDryRunActive;
            set => SetProperty(ref _isDryRunActive, value);
        }

        private bool _isCycleRunning;
        public bool IsCycleRunning
        {
            get => _isCycleRunning;
            set => SetProperty(ref _isCycleRunning, value);
        }

        #endregion

        // ----------------------------------------------------------------
        // Constructor
        // ----------------------------------------------------------------
        public Dashboard2ViewModel(
            INavigationService navigationService,
            CoreClient coreClient,
            IDialogService dialog,
            IAppLogger logger) : base(logger)
        {
            _navigationService = navigationService;
            _coreClient = coreClient;
            _dialog = dialog;

            // --- Commands ---
            ToggleSidebarCommand    = new RelayCommand(ExecuteGoToMenu);
            AutoRunCommand          = new RelayCommand(async () => await ExecuteAutoRunAsync());
            DryRunCommand           = new RelayCommand(async () => await ExecuteDryRunAsync());
            CycleStartStopCommand   = new RelayCommand(StartResetSequence);
            AcknowledgeAlarmCommand = new RelayCommand<int>(async alarmNo => await ExecuteAcknowledgeAlarmAsync(alarmNo));
        }

        // ----------------------------------------------------------------
        // Initialize  (called from code-behind after DataContext is set)
        // ----------------------------------------------------------------
        public void Initialize()
        {
            // RequestId = 4 — OEE / Dashboard2Result data
            _liveDataPoller = new SafePollerEx(
                _coreClient,
                TimeSpan.FromMilliseconds(500),
                UpdateOeeFromService,
                _logger,
                ex => _logger.LogError($"[Dashboard2] OEE poller error: {ex.Message}", LogType.Diagnostics),
                requestId: 4);

            // RequestId = 5 — Raw IO values (mode state, ack flags)
            _ioPoller = new SafePollerEx(
                _coreClient,
                TimeSpan.FromMilliseconds(500),
                UpdateIoFromService,
                _logger,
                ex => _logger.LogError($"[Dashboard2] IO poller error: {ex.Message}", LogType.Diagnostics),
                requestId: 5);

            // Reset-sequence poller — also reads RequestId = 5 but only acts when reset is in progress
            _resetPoller = new SafePollerEx(
                _coreClient,
                TimeSpan.FromMilliseconds(200),
                ResetSequenceTickAsync,
                _logger,
                ex => _logger.LogError($"[Dashboard2] Reset poller error: {ex.Message}", LogType.Diagnostics),
                requestId: 5);

            _liveDataPoller.Start();
            _ioPoller.Start();
            _resetPoller.Start();
        }

        // ----------------------------------------------------------------
        // RequestId = 4  —  OEE / Dashboard2Result
        // ----------------------------------------------------------------
        private async Task UpdateOeeFromService(Dictionary<int, object> data)
        {
            try
            {
                if (data.TryGetValue(4, out object d2Obj))
                {
                    var d2Result = Deserialize<Dashboard2Result>(d2Obj);
                    if (d2Result != null)
                    {
                        Efficiency = new EfficiencyBreakdown
                        {
                            Availability  = (int)Math.Round(d2Result.Availability * 100),
                            Performance   = (int)Math.Round(d2Result.Performance * 100),
                            Quality       = (int)Math.Round(d2Result.Quality * 100),
                            OEEDetails    = (int)Math.Round(d2Result.OverallOEE * 100),
                            OKCount       = d2Result.OKParts,
                            NGCount       = d2Result.NGParts,
                            OperatingTime = FormatDuration(d2Result.OperatingTime),
                            Downtime      = FormatDuration(d2Result.Downtime),
                            CycleTime     = $"{d2Result.CycleTime / 1000.0:F2} s",
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"[Dashboard2] UpdateOeeFromService error: {ex.Message}", LogType.Diagnostics);
            }

            await Task.CompletedTask;
        }

        // ----------------------------------------------------------------
        // RequestId = 5  —  Raw IO values (machine mode states)
        // ----------------------------------------------------------------
        private async Task UpdateIoFromService(Dictionary<int, object> data)
        {
            try
            {
                bool autoRun  = GetBoolState(data, ConstantValues.Mode_Auto.Read);
                bool dryRun   = GetBoolState(data, ConstantValues.Mode_DryRun.Read);
                bool cycleRun = GetBoolState(data, ConstantValues.CYCLE_START_TRIGGER_TAG_ID);

                if (IsAutoRunActive != autoRun)   IsAutoRunActive  = autoRun;
                if (IsDryRunActive  != dryRun)    IsDryRunActive   = dryRun;
                if (IsCycleRunning  != cycleRun)  IsCycleRunning   = cycleRun;
            }
            catch (Exception ex)
            {
                _logger.LogError($"[Dashboard2] UpdateIoFromService error: {ex.Message}", LogType.Diagnostics);
            }

            await Task.CompletedTask;
        }

        // ----------------------------------------------------------------
        // Commands  —  RequestId = 6 (WriteTag)
        // ----------------------------------------------------------------
        private void ExecuteGoToMenu()
        {
            _navigationService.NavigateToBendingLandingPage();
        }

        private async Task ExecuteAutoRunAsync()
        {
            try
            {
                bool newState = !IsAutoRunActive;
                await _coreClient.WriteTagAsync(ConstantValues.Mode_Auto.Write, newState);
                _logger.LogInfo($"[Dashboard2] AUTO RUN → {newState}", LogType.Diagnostics);
            }
            catch (Exception ex)
            {
                _logger.LogError($"[Dashboard2] ExecuteAutoRunAsync error: {ex.Message}", LogType.Diagnostics);
            }
        }

        private async Task ExecuteDryRunAsync()
        {
            try
            {
                bool newState = !IsDryRunActive;
                await _coreClient.WriteTagAsync(ConstantValues.Mode_DryRun.Write, newState);
                _logger.LogInfo($"[Dashboard2] DRY RUN → {newState}", LogType.Diagnostics);
            }
            catch (Exception ex)
            {
                _logger.LogError($"[Dashboard2] ExecuteDryRunAsync error: {ex.Message}", LogType.Diagnostics);
            }
        }

        // ----------------------------------------------------------------
        // Reset Sequence  (CYCLE START / STOP)  —  RequestId = 5 + 6
        // ----------------------------------------------------------------
        private void StartResetSequence()
        {
            if (_resetState != ResetSequenceState.Idle)
            {
                _logger.LogWarning("[Dashboard2] Reset already in progress.", LogType.Diagnostics);
                return;
            }

            _resetState = ResetSequenceState.TriggerReset;
            _resetTimeoutStart = DateTime.Now;
            _logger.LogInfo("[Dashboard2] Reset sequence started.", LogType.Diagnostics);
        }

        private async Task ResetSequenceTickAsync(Dictionary<int, object> data)
        {
            if (System.Threading.Interlocked.Exchange(ref _resetPollerRunning, 1) == 1)
                return;

            try
            {
                switch (_resetState)
                {
                    case ResetSequenceState.Idle:
                        return;

                    case ResetSequenceState.TriggerReset:
                        await _coreClient.WriteTagAsync(ConstantValues.RESET_TAG_ID, true);
                        _resetState = ResetSequenceState.WaitingForAck;
                        _logger.LogInfo("[Dashboard2] Reset trigger sent, waiting for ack...", LogType.Diagnostics);
                        break;

                    case ResetSequenceState.WaitingForAck:
                        bool ackReceived = GetBoolState(data, ConstantValues.RESET_ACK_TAG_ID);
                        if (ackReceived)
                        {
                            _logger.LogInfo("[Dashboard2] Reset acknowledged by PLC.", LogType.Diagnostics);
                            await _coreClient.WriteTagAsync(ConstantValues.RESET_TAG_ID, false);
                            _resetState = ResetSequenceState.Idle;
                        }
                        else if ((DateTime.Now - _resetTimeoutStart).TotalSeconds > 5)
                        {
                            _logger.LogWarning("[Dashboard2] Reset ack timeout (5s). Resetting state.", LogType.Diagnostics);
                            _resetState = ResetSequenceState.Idle;
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"[Dashboard2] ResetSequenceTickAsync error: {ex.Message}", LogType.Diagnostics);
                _resetState = ResetSequenceState.Idle;
            }
            finally
            {
                System.Threading.Interlocked.Exchange(ref _resetPollerRunning, 0);
            }

            await Task.CompletedTask;
        }

        // ----------------------------------------------------------------
        // RequestId = 7  —  Alarm Acknowledgement
        // ----------------------------------------------------------------
        private async Task ExecuteAcknowledgeAlarmAsync(int alarmNo)
        {
            try
            {
                await _coreClient.AcknowledgeAlarmAsync(alarmNo, "Dashboard");
                _logger.LogInfo($"[Dashboard2] Alarm {alarmNo} acknowledged.", LogType.Diagnostics);
            }
            catch (Exception ex)
            {
                _logger.LogError($"[Dashboard2] ExecuteAcknowledgeAlarmAsync error: {ex.Message}", LogType.Diagnostics);
            }
        }

        // ----------------------------------------------------------------
        // Helpers
        // ----------------------------------------------------------------
        private bool GetBoolState(Dictionary<int, object> data, int tagId)
        {
            if (data != null && data.TryGetValue(tagId, out object val))
            {
                if (val is bool b) return b;
                if (bool.TryParse(val?.ToString(), out bool parsed)) return parsed;
            }
            return false;
        }

        private static T Deserialize<T>(object raw) where T : class
        {
            try
            {
                var json = JsonConvert.SerializeObject(raw);
                return JsonConvert.DeserializeObject<T>(json);
            }
            catch
            {
                return null;
            }
        }

        private static string FormatDuration(double totalSeconds)
        {
            TimeSpan t = TimeSpan.FromSeconds(totalSeconds);
            return t.TotalDays >= 1 ? $"{t.Days}d {t.Hours}h {t.Minutes}m" : t.ToString(@"hh\:mm\:ss");
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            _liveDataPoller?.Stop();
            _liveDataPoller?.Dispose();
            _ioPoller?.Stop();
            _ioPoller?.Dispose();
            _resetPoller?.Stop();
            _resetPoller?.Dispose();
        }
    }
}
