using System.Threading;
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
        private readonly INavigationService _navigationService;
        private readonly CoreClient _coreClient;
        private SafePoller _liveDataPoller;
        private int _liveDataRunning;
        private bool _disposed;

        public ICommand ToggleSidebarCommand { get; }

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

        #endregion

        public Dashboard2ViewModel(
            INavigationService navigationService,
            CoreClient coreClient,
            IAppLogger logger): base(logger)
        {
            _navigationService = navigationService;
            _coreClient = coreClient;

            ToggleSidebarCommand = new RelayCommand(ExecuteGoToMenu);
        }

        public void Initialize()
        {
            _liveDataPoller = new SafePoller(
                TimeSpan.FromMilliseconds(500),
                LiveDataTickAsync,
                ex => _logger.LogError($"[Dashboard2] Poller error: {ex.Message}", LogType.Diagnostics));

            _liveDataPoller.Start();
        }

        private void ExecuteGoToMenu()
        {
            _navigationService.NavigateToBendingLandingPage();
        }

        private async Task LiveDataTickAsync()
        {
            if (Interlocked.Exchange(ref _liveDataRunning, 1) == 1)
                return;

            try
            {
                if (!_coreClient.isConnected)
                    return;

                // --- OEE / Efficiency data (RequestId = 2, dedicated to Dashboard2) ---
                var oeeTask = _coreClient.GetIoValuesAsync(2);
                var oeeCompleted = await Task.WhenAny(oeeTask, Task.Delay(2000));

                if (oeeCompleted != oeeTask)
                {
                    _logger.LogWarning("[Dashboard2] OEE request timed out.", LogType.Diagnostics);
                }
                else
                {
                    var oeeData = await oeeTask;
                    if (oeeData != null && oeeData.TryGetValue(2, out object d2Obj))
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
            }
            catch (Exception ex)
            {
                _logger.LogError($"[Dashboard2] LiveDataTickAsync error: {ex.Message}", LogType.Diagnostics);
            }
            finally
            {
                Interlocked.Exchange(ref _liveDataRunning, 0);
            }
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
        }
    }
}
