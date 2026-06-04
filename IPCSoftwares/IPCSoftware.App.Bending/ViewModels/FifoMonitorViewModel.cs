using IPCSoftware.Common.CommonExtensions;
using IPCSoftware.Common.UIClientComm;
using IPCSoftware.Core.Interfaces.AppLoggerInterface;
using IPCSoftware.Shared;
using IPCSoftware.Shared.Models.Bending;
using IPCSoftware.Shared.Models.ConfigModels;
using IPCSoftware.UI.CommonViews.ViewModels;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace IPCSoftware.App.Bending.ViewModels
{
    public class FifoMonitorViewModel : BaseViewModel, IDisposable
    {
        private readonly CoreClient _coreClient;
        private SafePollerEx _fifoPoller;
        private bool _disposed;
        private int _previousBatchCount = 0;

        // LEFT table: Stage 1-2 (max 2 rows)
        private ObservableCollection<BatchDisplayRow> _leftTableRows = new();
        public ObservableCollection<BatchDisplayRow> LeftTableRows
        {
            get => _leftTableRows;
            set => SetProperty(ref _leftTableRows, value);
        }

        // TOP table: Stage 3-9 (max 7 rows)
        private ObservableCollection<BatchDisplayRow> _topTableRows = new();
        public ObservableCollection<BatchDisplayRow> TopTableRows
        {
            get => _topTableRows;
            set => SetProperty(ref _topTableRows, value);
        }

        private string _statusText = "Waiting for data...";
        public string StatusText
        {
            get => _statusText;
            set => SetProperty(ref _statusText, value);
        }

        private bool _isTriggerActive;
        public bool IsTriggerActive
        {
            get => _isTriggerActive;
            set => SetProperty(ref _isTriggerActive, value);
        }

        // Live Heater data (4 parts × 3 bends)
        private float[] _liveHeaterB1 = new float[4];
        public float[] LiveHeaterB1 { get => _liveHeaterB1; set => SetProperty(ref _liveHeaterB1, value); }
        private float[] _liveHeaterB2 = new float[4];
        public float[] LiveHeaterB2 { get => _liveHeaterB2; set => SetProperty(ref _liveHeaterB2, value); }
        private float[] _liveHeaterB3 = new float[4];
        public float[] LiveHeaterB3 { get => _liveHeaterB3; set => SetProperty(ref _liveHeaterB3, value); }

        // Live Force data (4 parts × 3 bends)
        private float[] _liveForceB1 = new float[4];
        public float[] LiveForceB1 { get => _liveForceB1; set => SetProperty(ref _liveForceB1, value); }
        private float[] _liveForceB2 = new float[4];
        public float[] LiveForceB2 { get => _liveForceB2; set => SetProperty(ref _liveForceB2, value); }
        private float[] _liveForceB3 = new float[4];
        public float[] LiveForceB3 { get => _liveForceB3; set => SetProperty(ref _liveForceB3, value); }

        public FifoMonitorViewModel(CoreClient coreClient, IAppLogger logger) : base(logger)
        {
            _coreClient = coreClient;
        }

        public void Initialize()
        {
            _fifoPoller = new SafePollerEx(
                _coreClient,
                TimeSpan.FromMilliseconds(500),
                UpdateFifoData,
                _logger,
                ex => System.Diagnostics.Debug.WriteLine($"[FifoMonitor] Poller error: {ex.Message}"),
                requestId: 30);

            _fifoPoller.Start();
        }

        private async Task UpdateFifoData(Dictionary<int, object> data)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"[FifoMonitor] UpdateFifoData called. data.Count={data?.Count}, HasKey30={data?.ContainsKey(30)}");

                if (data.TryGetValue(30, out object modelObj))
                {
                    System.Diagnostics.Debug.WriteLine($"[FifoMonitor] Raw type: {modelObj?.GetType()?.Name}, value preview: {modelObj?.ToString()?.Substring(0, Math.Min(200, modelObj?.ToString()?.Length ?? 0))}");

                    var model = Deserialize<BendingProcessDashboardModel>(modelObj);
                    System.Diagnostics.Debug.WriteLine($"[FifoMonitor] Deserialized: model={model != null}, ActiveBatches={model?.ActiveBatches?.Count}");

                    if (model?.ActiveBatches != null)
                    {
                        var allBatches = model.ActiveBatches;

                        // Split into LEFT (Stage 1-2) and TOP (Stage 3-9)
                        var leftBatches = allBatches.Where(b => b.Stage <= 2).OrderByDescending(b => b.Stage).ToList();
                        var topBatches = allBatches.Where(b => b.Stage >= 3).OrderByDescending(b => b.Stage).ToList();

                        // Update LEFT table (always 2 batches × 4 parts = up to 8 rows)
                        var newLeft = new ObservableCollection<BatchDisplayRow>();
                        var s2 = leftBatches.FirstOrDefault(b => b.Stage == 2);
                        var s1 = leftBatches.FirstOrDefault(b => b.Stage == 1);

                        if (s2 != null)
                        {
                            for (int i = 0; i < 4; i++)
                            {
                                newLeft.Add(new BatchDisplayRow
                                {
                                    BatchNo = i == 0 ? s2.BatchNumber : "",
                                    QrCode = GetQrByIndex(s2, i),
                                    Stage = s2.Stage
                                });
                            }
                        }
                        if (s1 != null)
                        {
                            for (int i = 0; i < 4; i++)
                            {
                                newLeft.Add(new BatchDisplayRow
                                {
                                    BatchNo = i == 0 ? s1.BatchNumber : "",
                                    QrCode = GetQrByIndex(s1, i),
                                    Stage = s1.Stage
                                });
                            }
                        }

                        // Update TOP table (up to 7 batches × 4 parts = 28 rows, highest stage at top)
                        var newTop = new ObservableCollection<BatchDisplayRow>();
                        foreach (var batch in topBatches.Take(7))
                        {
                            for (int i = 0; i < 4; i++)
                            {
                                newTop.Add(new BatchDisplayRow
                                {
                                    BatchNo = i == 0 ? batch.BatchNumber : "",
                                    QrCode = GetQrByIndex(batch, i),
                                    Stage = batch.Stage,
                                    B1Temp = batch.Bending1Temperatures[i],
                                    B2Temp = batch.Bending2Temperatures[i],
                                    B3Temp = batch.Bending3Temperatures[i],
                                    LoadB1 = batch.Bending1Loads[i],
                                    LoadB2 = batch.Bending2Loads[i],
                                    LoadB3 = batch.Bending3Loads[i],
                                    X = batch.Bending3X[i],
                                    Y = batch.Bending3Y[i],
                                    Z = batch.Bending3Z[i],
                                    W = batch.Bending3W[i],
                                    TearStatus = batch.TearingStatus[i] ? "OK" : "-",
                                    FlipStatus = batch.FlippingStatus[i] ? "OK" : "-",
                                    Result = batch.InspectionComplete ? (batch.InspectionResults[i] ? "OK" : "NG") : "-"
                                });
                            }
                        }

                        App.Current?.Dispatcher?.Invoke(() =>
                        {
                            // Detect if a new trigger happened (batch count changed)
                            bool triggerFired = allBatches.Count != (_previousBatchCount);
                            _previousBatchCount = allBatches.Count;

                            LeftTableRows = newLeft;
                            TopTableRows = newTop;
                            StatusText = $"Queue: {allBatches.Count} batches | Left: {leftBatches.Count} | Top: {topBatches.Count}";

                            if (triggerFired && allBatches.Count > 0)
                            {
                                IsTriggerActive = true;
                                // Auto-reset after 2 seconds
                                System.Threading.Tasks.Task.Delay(2000).ContinueWith(_ =>
                                {
                                    App.Current?.Dispatcher?.Invoke(() => IsTriggerActive = false);
                                });
                            }

                            // Update live heater/force from highest-stage batch
                            var liveBatch = topBatches.FirstOrDefault();
                            if (liveBatch != null)
                            {
                                LiveHeaterB1 = liveBatch.Bending1Temperatures;
                                LiveHeaterB2 = liveBatch.Bending2Temperatures;
                                LiveHeaterB3 = liveBatch.Bending3Temperatures;
                                LiveForceB1 = liveBatch.Bending1Loads;
                                LiveForceB2 = liveBatch.Bending2Loads;
                                LiveForceB3 = liveBatch.Bending3Loads;
                            }
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[FifoMonitor] Update error: {ex.Message}");
            }

            await Task.CompletedTask;
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

        private static string GetQrByIndex(BatchModel batch, int index)
        {
            return index switch
            {
                0 => batch.QrCode1,
                1 => batch.QrCode2,
                2 => batch.QrCode3,
                3 => batch.QrCode4,
                _ => ""
            };
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            _fifoPoller?.Stop();
            _fifoPoller?.Dispose();
        }
    }

    /// <summary>
    /// Display row for both LEFT and TOP tables.
    /// </summary>
    public class BatchDisplayRow : ObservableObjectVM
    {
        public string BatchNo { get; set; } = "";
        public string QrCode { get; set; } = "";
        public int Stage { get; set; }
        public float B1Temp { get; set; }
        public float B2Temp { get; set; }
        public float B3Temp { get; set; }
        public float Load { get; set; }
        public float LoadB1 { get; set; }
        public float LoadB2 { get; set; }
        public float LoadB3 { get; set; }
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }
        public float W { get; set; }
        public string TearStatus { get; set; } = "-";
        public string FlipStatus { get; set; } = "-";
        public string Result { get; set; } = "-";
    }
}
