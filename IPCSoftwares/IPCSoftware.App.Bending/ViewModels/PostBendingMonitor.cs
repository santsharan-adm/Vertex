using System.Collections.ObjectModel;
using System.Windows.Input;
using IPCSoftware.App.Bending.Models;
using IPCSoftware.Common.CommonExtensions;
using IPCSoftware.Common.UIClientComm;
using IPCSoftware.Core.Interfaces;
using IPCSoftware.Core.Interfaces.AppLoggerInterface;
using IPCSoftware.Shared;
using IPCSoftware.Shared.Models.ConfigModels;
using IPCSoftware.UI.CommonViews.ViewModels;

namespace IPCSoftware.App.Bending.ViewModels
{
    public class PostBendingViewModel : BaseViewModel, IDisposable
    {
        private readonly INavigationService _navigationService;
        private readonly CoreClient _coreClient;
        private SafePollerEx _liveDataPoller;
        private bool _disposed;

        public ICommand PreviousCommand { get; }

        #region Properties

        // --- Batch Info ---

        private string _batchNo = "Loading...";
        public string BatchNo
        {
            get => _batchNo;
            set => SetProperty(ref _batchNo, value);
        }

        // --- Product Table ---

        public ObservableCollection<PostBendingMonitorRow> Products { get; } = new()
        {
            new PostBendingMonitorRow { Product = "Product 1", X = new ParameterLImitValues(), Y = new ParameterLImitValues(), Z = new ParameterLImitValues(), W = new ParameterLImitValues() },
            new PostBendingMonitorRow { Product = "Product 2", X = new ParameterLImitValues(), Y = new ParameterLImitValues(), Z = new ParameterLImitValues(), W = new ParameterLImitValues() },
            new PostBendingMonitorRow { Product = "Product 3", X = new ParameterLImitValues(), Y = new ParameterLImitValues(), Z = new ParameterLImitValues(), W = new ParameterLImitValues() },
            new PostBendingMonitorRow { Product = "Product 4", X = new ParameterLImitValues(), Y = new ParameterLImitValues(), Z = new ParameterLImitValues(), W = new ParameterLImitValues() },
        };

        // --- Flat Properties: Product 1 ---
        public string P1_Product => Products[0].Product;
        public string P1_QRCode => Products[0].QRCode;
        public double P1_X_Upper => Products[0].X?.UpperLimit ?? 0;
        public double P1_X_Value => Products[0].X?.PresentValue ?? 0;
        public double P1_X_Lower => Products[0].X?.LowerLimit ?? 0;
        public double P1_Y_Upper => Products[0].Y?.UpperLimit ?? 0;
        public double P1_Y_Value => Products[0].Y?.PresentValue ?? 0;
        public double P1_Y_Lower => Products[0].Y?.LowerLimit ?? 0;
        public double P1_Z_Upper => Products[0].Z?.UpperLimit ?? 0;
        public double P1_Z_Value => Products[0].Z?.PresentValue ?? 0;
        public double P1_Z_Lower => Products[0].Z?.LowerLimit ?? 0;
        public double P1_W_Upper => Products[0].W?.UpperLimit ?? 0;
        public double P1_W_Value => Products[0].W?.PresentValue ?? 0;
        public double P1_W_Lower => Products[0].W?.LowerLimit ?? 0;
        public bool P1_Result => Products[0].Result;

        // --- Flat Properties: Product 2 ---
        public string P2_Product => Products[1].Product;
        public string P2_QRCode => Products[1].QRCode;
        public double P2_X_Upper => Products[1].X?.UpperLimit ?? 0;
        public double P2_X_Value => Products[1].X?.PresentValue ?? 0;
        public double P2_X_Lower => Products[1].X?.LowerLimit ?? 0;
        public double P2_Y_Upper => Products[1].Y?.UpperLimit ?? 0;
        public double P2_Y_Value => Products[1].Y?.PresentValue ?? 0;
        public double P2_Y_Lower => Products[1].Y?.LowerLimit ?? 0;
        public double P2_Z_Upper => Products[1].Z?.UpperLimit ?? 0;
        public double P2_Z_Value => Products[1].Z?.PresentValue ?? 0;
        public double P2_Z_Lower => Products[1].Z?.LowerLimit ?? 0;
        public double P2_W_Upper => Products[1].W?.UpperLimit ?? 0;
        public double P2_W_Value => Products[1].W?.PresentValue ?? 0;
        public double P2_W_Lower => Products[1].W?.LowerLimit ?? 0;
        public bool P2_Result => Products[1].Result;

        // --- Flat Properties: Product 3 ---
        public string P3_Product => Products[2].Product;
        public string P3_QRCode => Products[2].QRCode;
        public double P3_X_Upper => Products[2].X?.UpperLimit ?? 0;
        public double P3_X_Value => Products[2].X?.PresentValue ?? 0;
        public double P3_X_Lower => Products[2].X?.LowerLimit ?? 0;
        public double P3_Y_Upper => Products[2].Y?.UpperLimit ?? 0;
        public double P3_Y_Value => Products[2].Y?.PresentValue ?? 0;
        public double P3_Y_Lower => Products[2].Y?.LowerLimit ?? 0;
        public double P3_Z_Upper => Products[2].Z?.UpperLimit ?? 0;
        public double P3_Z_Value => Products[2].Z?.PresentValue ?? 0;
        public double P3_Z_Lower => Products[2].Z?.LowerLimit ?? 0;
        public double P3_W_Upper => Products[2].W?.UpperLimit ?? 0;
        public double P3_W_Value => Products[2].W?.PresentValue ?? 0;
        public double P3_W_Lower => Products[2].W?.LowerLimit ?? 0;
        public bool P3_Result => Products[2].Result;

        // --- Flat Properties: Product 4 ---
        public string P4_Product => Products[3].Product;
        public string P4_QRCode => Products[3].QRCode;
        public double P4_X_Upper => Products[3].X?.UpperLimit ?? 0;
        public double P4_X_Value => Products[3].X?.PresentValue ?? 0;
        public double P4_X_Lower => Products[3].X?.LowerLimit ?? 0;
        public double P4_Y_Upper => Products[3].Y?.UpperLimit ?? 0;
        public double P4_Y_Value => Products[3].Y?.PresentValue ?? 0;
        public double P4_Y_Lower => Products[3].Y?.LowerLimit ?? 0;
        public double P4_Z_Upper => Products[3].Z?.UpperLimit ?? 0;
        public double P4_Z_Value => Products[3].Z?.PresentValue ?? 0;
        public double P4_Z_Lower => Products[3].Z?.LowerLimit ?? 0;
        public double P4_W_Upper => Products[3].W?.UpperLimit ?? 0;
        public double P4_W_Value => Products[3].W?.PresentValue ?? 0;
        public double P4_W_Lower => Products[3].W?.LowerLimit ?? 0;
        public bool P4_Result => Products[3].Result;

        #endregion

        public PostBendingViewModel(
            INavigationService navigationService,
            CoreClient coreClient,
            IAppLogger logger)
            : base(logger)
        {
            _navigationService = navigationService;
            _coreClient = coreClient;

            PreviousCommand = new RelayCommand(ExecutePrevious);
        }

        public void Initialize()
        {
            _liveDataPoller = new SafePollerEx(_coreClient,
                TimeSpan.FromMilliseconds(500),
                UpdateFromService,
                _logger,
                ex => _logger.LogError($"[PostBendingMonitor] Poller error: {ex.Message}", LogType.Diagnostics));

            _liveDataPoller.Start();
        }

        private void ExecutePrevious()
        {
            _navigationService.NavigateToDashboard3();
        }

        private async Task UpdateFromService(Dictionary<int, object> data)
        {
            try
            {
                if (data.TryGetValue(2000, out object batchNo))
                    BatchNo = batchNo?.ToString() ?? "---";

                // PLC data structure: Each product has 13 tags (QR, X_Upper, X_Value, X_Lower, Y_Upper, Y_Value, Y_Lower, Z_Upper, Z_Value, Z_Lower, W_Upper, W_Value, W_Lower, Result, padding)
                // Base offsets: Product 1 = 2001, Product 2 = 2021, Product 3 = 2041, Product 4 = 2061
                var rows = Products;
                int[] baseOffsets = { 2001, 2021, 2041, 2061 };

                for (int i = 0; i < rows.Count && i < baseOffsets.Length; i++)
                {
                    int b = baseOffsets[i];
                    var row = rows[i];

                    if (data.TryGetValue(b, out object qrCode))
                        row.QRCode = qrCode?.ToString() ?? "---";

                    row.X ??= new ParameterLImitValues();
                    if (data.TryGetValue(b + 1, out object xUpper)) row.X.UpperLimit = Convert.ToDouble(xUpper);
                    if (data.TryGetValue(b + 2, out object xValue)) row.X.PresentValue = Convert.ToDouble(xValue);
                    if (data.TryGetValue(b + 3, out object xLower)) row.X.LowerLimit = Convert.ToDouble(xLower);

                    row.Y ??= new ParameterLImitValues();
                    if (data.TryGetValue(b + 4, out object yUpper)) row.Y.UpperLimit = Convert.ToDouble(yUpper);
                    if (data.TryGetValue(b + 5, out object yValue)) row.Y.PresentValue = Convert.ToDouble(yValue);
                    if (data.TryGetValue(b + 6, out object yLower)) row.Y.LowerLimit = Convert.ToDouble(yLower);

                    row.Z ??= new ParameterLImitValues();
                    if (data.TryGetValue(b + 7, out object zUpper)) row.Z.UpperLimit = Convert.ToDouble(zUpper);
                    if (data.TryGetValue(b + 8, out object zValue)) row.Z.PresentValue = Convert.ToDouble(zValue);
                    if (data.TryGetValue(b + 9, out object zLower)) row.Z.LowerLimit = Convert.ToDouble(zLower);

                    row.W ??= new ParameterLImitValues();
                    if (data.TryGetValue(b + 10, out object wUpper)) row.W.UpperLimit = Convert.ToDouble(wUpper);
                    if (data.TryGetValue(b + 11, out object wValue)) row.W.PresentValue = Convert.ToDouble(wValue);
                    if (data.TryGetValue(b + 12, out object wLower)) row.W.LowerLimit = Convert.ToDouble(wLower);

                    if (data.TryGetValue(b + 13, out object result)) row.Result = Convert.ToBoolean(result);

                    NotifyFlatProperties(i + 1);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"[PostBendingMonitor] UpdateFromService error: {ex.Message}", LogType.Diagnostics);
            }
        }

        private void NotifyFlatProperties(int p)
        {
            OnPropertyChanged($"P{p}_QRCode");
            OnPropertyChanged($"P{p}_X_Upper"); OnPropertyChanged($"P{p}_X_Value"); OnPropertyChanged($"P{p}_X_Lower");
            OnPropertyChanged($"P{p}_Y_Upper"); OnPropertyChanged($"P{p}_Y_Value"); OnPropertyChanged($"P{p}_Y_Lower");
            OnPropertyChanged($"P{p}_Z_Upper"); OnPropertyChanged($"P{p}_Z_Value"); OnPropertyChanged($"P{p}_Z_Lower");
            OnPropertyChanged($"P{p}_W_Upper"); OnPropertyChanged($"P{p}_W_Value"); OnPropertyChanged($"P{p}_W_Lower");
            OnPropertyChanged($"P{p}_Result");
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