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
        
        // DI Services
        
        private readonly INavigationService _navigationService;
        private readonly CoreClient _coreClient;
        private readonly IDialogService _dialog;

        // Pollers

        private SafePollerEx _liveDataPoller;               // RequestId = 4  (OEE / Dashboard2Result)
        private SafePollerEx _ioPoller;                     // RequestId = 5  (Raw IO: mode states, ack flags)
        private SafePollerEx _resetPoller;                  // RequestId = 5  (Reset sequence ack check)

        // Per-model pollers (RequestIds 11-22)
        private SafePollerEx _inspectionTable1Poller;       // RequestId = 11
        private SafePollerEx _inspectionTable2Poller;       // RequestId = 12
        private SafePollerEx _bendingIndicatorsPoller;      // RequestId = 13
        private SafePollerEx _turnTable1Poller;             // RequestId = 14
        private SafePollerEx _turnTable2Poller;             // RequestId = 15
        private SafePollerEx _transferModulePoller;         // RequestId = 16
        private SafePollerEx _inspectionDataPoller;         // RequestId = 17
        private SafePollerEx _robotStatusPoller;            // RequestId = 18
        private SafePollerEx _inputTrayPoller;              // RequestId = 19
        private SafePollerEx _outputTrayPoller;             // RequestId = 20
        private SafePollerEx _ngBinPoller;                  // RequestId = 21
        private SafePollerEx _efficiencyBreakdownPoller;    // RequestId = 22

        private bool _disposed;

        
        // Reset sequence state machine  (mirrors AOI OEEDashboardViewModel)
        
        private enum ResetSequenceState { Idle, TriggerReset, WaitingForAck }
        private ResetSequenceState _resetState = ResetSequenceState.Idle;
        private DateTime _resetTimeoutStart;
        private int _resetPollerRunning = 0;

        
        // Commands
        
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

        // --- TurnTables ---

        private TurnTable1DataPointModel _turnTable1 = new();
        public TurnTable1DataPointModel TurnTable1
        {
            get => _turnTable1;
            set => SetProperty(ref _turnTable1, value);
        }

        private TurnTable2DataPointModel _turnTable2 = new();
        public TurnTable2DataPointModel TurnTable2
        {
            get => _turnTable2;
            set => SetProperty(ref _turnTable2, value);
        }

        // --- Transfer Module ---

        private TransferModuleModel _transferModule = new();
        public TransferModuleModel TransferModule
        {
            get => _transferModule;
            set => SetProperty(ref _transferModule, value);
        }

        // --- Inspection ---

        private InspectionDataModel _inspectionData = new();
        public InspectionDataModel InspectionData
        {
            get => _inspectionData;
            set => SetProperty(ref _inspectionData, value);
        }

        // --- Robot ---

        private RobotProcessStatus _robotStatus = new();
        public RobotProcessStatus RobotStatus
        {
            get => _robotStatus;
            set => SetProperty(ref _robotStatus, value);
        }

        // --- Efficiency Breakdown ---

        private EfficiencyBreakdown _efficiencyBreakdown = new();
        public EfficiencyBreakdown EfficiencyBreakdown
        {
            get => _efficiencyBreakdown;
            set => SetProperty(ref _efficiencyBreakdown, value);
        }

        // --- OEE / Efficiency ---

        private Dashboard2Result _dashboard2Data = new();
        public Dashboard2Result Dashboard2Data
        {
            get => _dashboard2Data;
            set => SetProperty(ref _dashboard2Data, value);
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

       //-Constructor
        
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

            // ----------------------------------------------------------------
            // Dashboard2 Model Pollers (Request IDs 11-22)
            // ----------------------------------------------------------------

            // RequestId = 11 — InspectionTable (Lot 1)
            _inspectionTable1Poller = new SafePollerEx(
                _coreClient,
                TimeSpan.FromMilliseconds(500),
                UpdateInspectionTable1,
                _logger,
                ex => _logger.LogError($"[Dashboard2] InspectionTable1 poller error: {ex.Message}", LogType.Diagnostics),
                requestId: 11);

            // RequestId = 12 — InspectionTable2 (Lot 2)
            _inspectionTable2Poller = new SafePollerEx(
                _coreClient,
                TimeSpan.FromMilliseconds(500),
                UpdateInspectionTable2,
                _logger,
                ex => _logger.LogError($"[Dashboard2] InspectionTable2 poller error: {ex.Message}", LogType.Diagnostics),
                requestId: 12);

            // RequestId = 13 — BendingIndicators
            _bendingIndicatorsPoller = new SafePollerEx(
                _coreClient,
                TimeSpan.FromMilliseconds(500),
                UpdateBendingIndicators,
                _logger,
                ex => _logger.LogError($"[Dashboard2] BendingIndicators poller error: {ex.Message}", LogType.Diagnostics),
                requestId: 13);

            // RequestId = 14 — TurnTable1
            _turnTable1Poller = new SafePollerEx(
                _coreClient,
                TimeSpan.FromMilliseconds(500),
                UpdateTurnTable1,
                _logger,
                ex => _logger.LogError($"[Dashboard2] TurnTable1 poller error: {ex.Message}", LogType.Diagnostics),
                requestId: 14);

            // RequestId = 15 — TurnTable2
            _turnTable2Poller = new SafePollerEx(
                _coreClient,
                TimeSpan.FromMilliseconds(500),
                UpdateTurnTable2,
                _logger,
                ex => _logger.LogError($"[Dashboard2] TurnTable2 poller error: {ex.Message}", LogType.Diagnostics),
                requestId: 15);

            // RequestId = 16 — TransferModule
            _transferModulePoller = new SafePollerEx(
                _coreClient,
                TimeSpan.FromMilliseconds(500),
                UpdateTransferModule,
                _logger,
                ex => _logger.LogError($"[Dashboard2] TransferModule poller error: {ex.Message}", LogType.Diagnostics),
                requestId: 16);

            // RequestId = 17 — InspectionData
            _inspectionDataPoller = new SafePollerEx(
                _coreClient,
                TimeSpan.FromMilliseconds(500),
                UpdateInspectionData,
                _logger,
                ex => _logger.LogError($"[Dashboard2] InspectionData poller error: {ex.Message}", LogType.Diagnostics),
                requestId: 17);

            // RequestId = 18 — RobotStatus
            _robotStatusPoller = new SafePollerEx(
                _coreClient,
                TimeSpan.FromMilliseconds(500),
                UpdateRobotStatus,
                _logger,
                ex => _logger.LogError($"[Dashboard2] RobotStatus poller error: {ex.Message}", LogType.Diagnostics),
                requestId: 18);

            // RequestId = 19 — InputTray
            _inputTrayPoller = new SafePollerEx(
                _coreClient,
                TimeSpan.FromMilliseconds(500),
                UpdateInputTray,
                _logger,
                ex => _logger.LogError($"[Dashboard2] InputTray poller error: {ex.Message}", LogType.Diagnostics),
                requestId: 19);

            // RequestId = 20 — OutputTray
            _outputTrayPoller = new SafePollerEx(
                _coreClient,
                TimeSpan.FromMilliseconds(500),
                UpdateOutputTray,
                _logger,
                ex => _logger.LogError($"[Dashboard2] OutputTray poller error: {ex.Message}", LogType.Diagnostics),
                requestId: 20);

            // RequestId = 21 — NGBin
            _ngBinPoller = new SafePollerEx(
                _coreClient,
                TimeSpan.FromMilliseconds(500),
                UpdateNGBin,
                _logger,
                ex => _logger.LogError($"[Dashboard2] NGBin poller error: {ex.Message}", LogType.Diagnostics),
                requestId: 21);

            // RequestId = 22 — EfficiencyBreakdown
            _efficiencyBreakdownPoller = new SafePollerEx(
                _coreClient,
                TimeSpan.FromMilliseconds(500),
                UpdateEfficiencyBreakdown,
                _logger,
                ex => _logger.LogError($"[Dashboard2] EfficiencyBreakdown poller error: {ex.Message}", LogType.Diagnostics),
                requestId: 22);

            // Start all pollers
            _liveDataPoller.Start();
            _ioPoller.Start();
            _resetPoller.Start();
            _inspectionTable1Poller.Start();
            _inspectionTable2Poller.Start();
            _bendingIndicatorsPoller.Start();
            _turnTable1Poller.Start();
            _turnTable2Poller.Start();
            _transferModulePoller.Start();
            _inspectionDataPoller.Start();
            _robotStatusPoller.Start();
            _inputTrayPoller.Start();
            _outputTrayPoller.Start();
            _ngBinPoller.Start();
            _efficiencyBreakdownPoller.Start();
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
                        Dashboard2Data = d2Result;
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
        // Dashboard2 Model Update Methods (Request IDs 11-22)
        // ----------------------------------------------------------------

        // RequestId = 11 — InspectionTable (Lot 1)
        private async Task UpdateInspectionTable1(Dictionary<int, object> data)
        {
            try
            {
                if (data.TryGetValue(11, out object modelObj))
                {
                    var model = Deserialize<DashboardInspectionModel>(modelObj);
                    if (model != null)
                    {
                        InspectionTable = model;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"[Dashboard2] UpdateInspectionTable1 error: {ex.Message}", LogType.Diagnostics);
            }

            await Task.CompletedTask;
        }

        // RequestId = 12 — InspectionTable2 (Lot 2)
        private async Task UpdateInspectionTable2(Dictionary<int, object> data)
        {
            try
            {
                if (data.TryGetValue(12, out object modelObj))
                {
                    var model = Deserialize<DashboardInspectionModel>(modelObj);
                    if (model != null)
                    {
                        InspectionTable2 = model;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"[Dashboard2] UpdateInspectionTable2 error: {ex.Message}", LogType.Diagnostics);
            }

            await Task.CompletedTask;
        }

        // RequestId = 13 — BendingIndicators
        private async Task UpdateBendingIndicators(Dictionary<int, object> data)
        {
            try
            {
                if (data.TryGetValue(13, out object modelObj))
                {
                    var model = Deserialize<BendingIndicators>(modelObj);
                    if (model != null)
                    {
                        BendingIndicators = model;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"[Dashboard2] UpdateBendingIndicators error: {ex.Message}", LogType.Diagnostics);
            }

            await Task.CompletedTask;
        }

        // RequestId = 14 — TurnTable1
        private async Task UpdateTurnTable1(Dictionary<int, object> data)
        {
            try
            {
                if (data.TryGetValue(14, out object modelObj))
                {
                    var model = Deserialize<TurnTable1DataPointModel>(modelObj);
                    if (model != null)
                    {
                        TurnTable1 = model;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"[Dashboard2] UpdateTurnTable1 error: {ex.Message}", LogType.Diagnostics);
            }

            await Task.CompletedTask;
        }

        // RequestId = 15 — TurnTable2
        private async Task UpdateTurnTable2(Dictionary<int, object> data)
        {
            try
            {
                if (data.TryGetValue(15, out object modelObj))
                {
                    var model = Deserialize<TurnTable2DataPointModel>(modelObj);
                    if (model != null)
                    {
                        TurnTable2 = model;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"[Dashboard2] UpdateTurnTable2 error: {ex.Message}", LogType.Diagnostics);
            }

            await Task.CompletedTask;
        }

        // RequestId = 16 — TransferModule
        private async Task UpdateTransferModule(Dictionary<int, object> data)
        {
            try
            {
                if (data.TryGetValue(16, out object modelObj))
                {
                    var model = Deserialize<TransferModuleModel>(modelObj);
                    if (model != null)
                    {
                        TransferModule = model;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"[Dashboard2] UpdateTransferModule error: {ex.Message}", LogType.Diagnostics);
            }

            await Task.CompletedTask;
        }

        // RequestId = 17 — InspectionData
        private async Task UpdateInspectionData(Dictionary<int, object> data)
        {
            try
            {
                if (data.TryGetValue(17, out object modelObj))
                {
                    var model = Deserialize<InspectionDataModel>(modelObj);
                    if (model != null)
                    {
                        InspectionData = model;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"[Dashboard2] UpdateInspectionData error: {ex.Message}", LogType.Diagnostics);
            }

            await Task.CompletedTask;
        }

        // RequestId = 18 — RobotStatus
        private async Task UpdateRobotStatus(Dictionary<int, object> data)
        {
            try
            {
                if (data.TryGetValue(18, out object modelObj))
                {
                    var model = Deserialize<RobotProcessStatus>(modelObj);
                    if (model != null)
                    {
                        RobotStatus = model;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"[Dashboard2] UpdateRobotStatus error: {ex.Message}", LogType.Diagnostics);
            }

            await Task.CompletedTask;
        }

        // RequestId = 19 — InputTray
        private async Task UpdateInputTray(Dictionary<int, object> data)
        {
            try
            {
                if (data.TryGetValue(19, out object modelObj))
                {
                    var model = Deserialize<InputTrayModel>(modelObj);
                    if (model != null)
                    {
                        InputTray = model;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"[Dashboard2] UpdateInputTray error: {ex.Message}", LogType.Diagnostics);
            }

            await Task.CompletedTask;
        }

        // RequestId = 20 — OutputTray
        private async Task UpdateOutputTray(Dictionary<int, object> data)
        {
            try
            {
                if (data.TryGetValue(20, out object modelObj))
                {
                    var model = Deserialize<OutputTrayModel>(modelObj);
                    if (model != null)
                    {
                        OutputTray = model;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"[Dashboard2] UpdateOutputTray error: {ex.Message}", LogType.Diagnostics);
            }

            await Task.CompletedTask;
        }

        // RequestId = 21 — NGBin
        private async Task UpdateNGBin(Dictionary<int, object> data)
        {
            try
            {
                if (data.TryGetValue(21, out object modelObj))
                {
                    var model = Deserialize<NGBinGroupModel>(modelObj);
                    if (model != null)
                    {
                        NGBin = model;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"[Dashboard2] UpdateNGBin error: {ex.Message}", LogType.Diagnostics);
            }

            await Task.CompletedTask;
        }

        // RequestId = 22 — EfficiencyBreakdown
        private async Task UpdateEfficiencyBreakdown(Dictionary<int, object> data)
        {
            try
            {
                if (data.TryGetValue(22, out object modelObj))
                {
                    var model = Deserialize<EfficiencyBreakdown>(modelObj);
                    if (model != null)
                    {
                        EfficiencyBreakdown = model;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"[Dashboard2] UpdateEfficiencyBreakdown error: {ex.Message}", LogType.Diagnostics);
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

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;

            // Dispose original pollers
            _liveDataPoller?.Stop();
            _liveDataPoller?.Dispose();
            _ioPoller?.Stop();
            _ioPoller?.Dispose();
            _resetPoller?.Stop();
            _resetPoller?.Dispose();

            // Dispose Dashboard2 model pollers
            _inspectionTable1Poller?.Stop();
            _inspectionTable1Poller?.Dispose();
            _inspectionTable2Poller?.Stop();
            _inspectionTable2Poller?.Dispose();
            _bendingIndicatorsPoller?.Stop();
            _bendingIndicatorsPoller?.Dispose();
            _turnTable1Poller?.Stop();
            _turnTable1Poller?.Dispose();
            _turnTable2Poller?.Stop();
            _turnTable2Poller?.Dispose();
            _transferModulePoller?.Stop();
            _transferModulePoller?.Dispose();
            _inspectionDataPoller?.Stop();
            _inspectionDataPoller?.Dispose();
            _robotStatusPoller?.Stop();
            _robotStatusPoller?.Dispose();
            _inputTrayPoller?.Stop();
            _inputTrayPoller?.Dispose();
            _outputTrayPoller?.Stop();
            _outputTrayPoller?.Dispose();
            _ngBinPoller?.Stop();
            _ngBinPoller?.Dispose();
            _efficiencyBreakdownPoller?.Stop();
            _efficiencyBreakdownPoller?.Dispose();
        }
    }
}
