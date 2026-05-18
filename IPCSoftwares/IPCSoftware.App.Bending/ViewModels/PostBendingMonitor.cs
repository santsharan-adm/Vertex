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

        // --- Product Rows ---

        private readonly PostBendingMonitorRow _row1 = new PostBendingMonitorRow { Product = "Product 1", X = new ParameterLImitValues(), Y = new ParameterLImitValues(), Z = new ParameterLImitValues(), W = new ParameterLImitValues() };
        private readonly PostBendingMonitorRow _row2 = new PostBendingMonitorRow { Product = "Product 2", X = new ParameterLImitValues(), Y = new ParameterLImitValues(), Z = new ParameterLImitValues(), W = new ParameterLImitValues() };
        private readonly PostBendingMonitorRow _row3 = new PostBendingMonitorRow { Product = "Product 3", X = new ParameterLImitValues(), Y = new ParameterLImitValues(), Z = new ParameterLImitValues(), W = new ParameterLImitValues() };
        private readonly PostBendingMonitorRow _row4 = new PostBendingMonitorRow { Product = "Product 4", X = new ParameterLImitValues(), Y = new ParameterLImitValues(), Z = new ParameterLImitValues(), W = new ParameterLImitValues() };

        // Tracks whether real PLC data has been received per product (suppresses default 0 display)
        private readonly bool[] _received = new bool[4];

        private static string Fmt(bool received, double value) =>
            received ? value.ToString("0.00") : null;

        // --- Flat Properties: Product 1 ---
        public string P1_Product => _row1.Product;
        public string P1_QRCode => _row1.QRCode;
        public string P1_X_Upper => Fmt(_received[0], _row1.X?.UpperLimit ?? 0);
        public string P1_X_Value => Fmt(_received[0], _row1.X?.PresentValue ?? 0);
        public string P1_X_Lower => Fmt(_received[0], _row1.X?.LowerLimit ?? 0);
        public string P1_Y_Upper => Fmt(_received[0], _row1.Y?.UpperLimit ?? 0);
        public string P1_Y_Value => Fmt(_received[0], _row1.Y?.PresentValue ?? 0);
        public string P1_Y_Lower => Fmt(_received[0], _row1.Y?.LowerLimit ?? 0);
        public string P1_Z_Upper => Fmt(_received[0], _row1.Z?.UpperLimit ?? 0);
        public string P1_Z_Value => Fmt(_received[0], _row1.Z?.PresentValue ?? 0);
        public string P1_Z_Lower => Fmt(_received[0], _row1.Z?.LowerLimit ?? 0);
        public string P1_W_Upper => Fmt(_received[0], _row1.W?.UpperLimit ?? 0);
        public string P1_W_Value => Fmt(_received[0], _row1.W?.PresentValue ?? 0);
        public string P1_W_Lower => Fmt(_received[0], _row1.W?.LowerLimit ?? 0);
        public bool? P1_Result => _received[0] ? _row1.Result : (bool?)null;

        // --- Flat Properties: Product 2 ---
        public string P2_Product => _row2.Product;
        public string P2_QRCode => _row2.QRCode;
        public string P2_X_Upper => Fmt(_received[1], _row2.X?.UpperLimit ?? 0);
        public string P2_X_Value => Fmt(_received[1], _row2.X?.PresentValue ?? 0);
        public string P2_X_Lower => Fmt(_received[1], _row2.X?.LowerLimit ?? 0);
        public string P2_Y_Upper => Fmt(_received[1], _row2.Y?.UpperLimit ?? 0);
        public string P2_Y_Value => Fmt(_received[1], _row2.Y?.PresentValue ?? 0);
        public string P2_Y_Lower => Fmt(_received[1], _row2.Y?.LowerLimit ?? 0);
        public string P2_Z_Upper => Fmt(_received[1], _row2.Z?.UpperLimit ?? 0);
        public string P2_Z_Value => Fmt(_received[1], _row2.Z?.PresentValue ?? 0);
        public string P2_Z_Lower => Fmt(_received[1], _row2.Z?.LowerLimit ?? 0);
        public string P2_W_Upper => Fmt(_received[1], _row2.W?.UpperLimit ?? 0);
        public string P2_W_Value => Fmt(_received[1], _row2.W?.PresentValue ?? 0);
        public string P2_W_Lower => Fmt(_received[1], _row2.W?.LowerLimit ?? 0);
        public bool? P2_Result => _received[1] ? _row2.Result : (bool?)null;

        // --- Flat Properties: Product 3 ---
        public string P3_Product => _row3.Product;
        public string P3_QRCode => _row3.QRCode;
        public string P3_X_Upper => Fmt(_received[2], _row3.X?.UpperLimit ?? 0);
        public string P3_X_Value => Fmt(_received[2], _row3.X?.PresentValue ?? 0);
        public string P3_X_Lower => Fmt(_received[2], _row3.X?.LowerLimit ?? 0);
        public string P3_Y_Upper => Fmt(_received[2], _row3.Y?.UpperLimit ?? 0);
        public string P3_Y_Value => Fmt(_received[2], _row3.Y?.PresentValue ?? 0);
        public string P3_Y_Lower => Fmt(_received[2], _row3.Y?.LowerLimit ?? 0);
        public string P3_Z_Upper => Fmt(_received[2], _row3.Z?.UpperLimit ?? 0);
        public string P3_Z_Value => Fmt(_received[2], _row3.Z?.PresentValue ?? 0);
        public string P3_Z_Lower => Fmt(_received[2], _row3.Z?.LowerLimit ?? 0);
        public string P3_W_Upper => Fmt(_received[2], _row3.W?.UpperLimit ?? 0);
        public string P3_W_Value => Fmt(_received[2], _row3.W?.PresentValue ?? 0);
        public string P3_W_Lower => Fmt(_received[2], _row3.W?.LowerLimit ?? 0);
        public bool? P3_Result => _received[2] ? _row3.Result : (bool?)null;

        // --- Flat Properties: Product 4 ---
        public string P4_Product => _row4.Product;
        public string P4_QRCode => _row4.QRCode;
        public string P4_X_Upper => Fmt(_received[3], _row4.X?.UpperLimit ?? 0);
        public string P4_X_Value => Fmt(_received[3], _row4.X?.PresentValue ?? 0);
        public string P4_X_Lower => Fmt(_received[3], _row4.X?.LowerLimit ?? 0);
        public string P4_Y_Upper => Fmt(_received[3], _row4.Y?.UpperLimit ?? 0);
        public string P4_Y_Value => Fmt(_received[3], _row4.Y?.PresentValue ?? 0);
        public string P4_Y_Lower => Fmt(_received[3], _row4.Y?.LowerLimit ?? 0);
        public string P4_Z_Upper => Fmt(_received[3], _row4.Z?.UpperLimit ?? 0);
        public string P4_Z_Value => Fmt(_received[3], _row4.Z?.PresentValue ?? 0);
        public string P4_Z_Lower => Fmt(_received[3], _row4.Z?.LowerLimit ?? 0);
        public string P4_W_Upper => Fmt(_received[3], _row4.W?.UpperLimit ?? 0);
        public string P4_W_Value => Fmt(_received[3], _row4.W?.PresentValue ?? 0);
        public string P4_W_Lower => Fmt(_received[3], _row4.W?.LowerLimit ?? 0);
        public bool? P4_Result => _received[3] ? _row4.Result : (bool?)null;

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
                var rows = new[] { _row1, _row2, _row3, _row4 };
                int[] baseOffsets = { 2001, 2021, 2041, 2061 };

                for (int i = 0; i < rows.Length; i++)
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

                    _received[i] = true;
                    NotifyFlatProperties(i + 1);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"[PostBendingMonitor] UpdateFromService error: {ex.Message}", LogType.Diagnostics);
            }
        }

        private void NotifyFlatProperties(int productIndex)
        {
            switch (productIndex)
            {
                case 1:
                    OnPropertyChanged(nameof(P1_QRCode));
                    OnPropertyChanged(nameof(P1_X_Upper)); OnPropertyChanged(nameof(P1_X_Value)); OnPropertyChanged(nameof(P1_X_Lower));
                    OnPropertyChanged(nameof(P1_Y_Upper)); OnPropertyChanged(nameof(P1_Y_Value)); OnPropertyChanged(nameof(P1_Y_Lower));
                    OnPropertyChanged(nameof(P1_Z_Upper)); OnPropertyChanged(nameof(P1_Z_Value)); OnPropertyChanged(nameof(P1_Z_Lower));
                    OnPropertyChanged(nameof(P1_W_Upper)); OnPropertyChanged(nameof(P1_W_Value)); OnPropertyChanged(nameof(P1_W_Lower));
                    OnPropertyChanged(nameof(P1_Result));
                    break;
                case 2:
                    OnPropertyChanged(nameof(P2_QRCode));
                    OnPropertyChanged(nameof(P2_X_Upper)); OnPropertyChanged(nameof(P2_X_Value)); OnPropertyChanged(nameof(P2_X_Lower));
                    OnPropertyChanged(nameof(P2_Y_Upper)); OnPropertyChanged(nameof(P2_Y_Value)); OnPropertyChanged(nameof(P2_Y_Lower));
                    OnPropertyChanged(nameof(P2_Z_Upper)); OnPropertyChanged(nameof(P2_Z_Value)); OnPropertyChanged(nameof(P2_Z_Lower));
                    OnPropertyChanged(nameof(P2_W_Upper)); OnPropertyChanged(nameof(P2_W_Value)); OnPropertyChanged(nameof(P2_W_Lower));
                    OnPropertyChanged(nameof(P2_Result));
                    break;
                case 3:
                    OnPropertyChanged(nameof(P3_QRCode));
                    OnPropertyChanged(nameof(P3_X_Upper)); OnPropertyChanged(nameof(P3_X_Value)); OnPropertyChanged(nameof(P3_X_Lower));
                    OnPropertyChanged(nameof(P3_Y_Upper)); OnPropertyChanged(nameof(P3_Y_Value)); OnPropertyChanged(nameof(P3_Y_Lower));
                    OnPropertyChanged(nameof(P3_Z_Upper)); OnPropertyChanged(nameof(P3_Z_Value)); OnPropertyChanged(nameof(P3_Z_Lower));
                    OnPropertyChanged(nameof(P3_W_Upper)); OnPropertyChanged(nameof(P3_W_Value)); OnPropertyChanged(nameof(P3_W_Lower));
                    OnPropertyChanged(nameof(P3_Result));
                    break;
                case 4:
                    OnPropertyChanged(nameof(P4_QRCode));
                    OnPropertyChanged(nameof(P4_X_Upper)); OnPropertyChanged(nameof(P4_X_Value)); OnPropertyChanged(nameof(P4_X_Lower));
                    OnPropertyChanged(nameof(P4_Y_Upper)); OnPropertyChanged(nameof(P4_Y_Value)); OnPropertyChanged(nameof(P4_Y_Lower));
                    OnPropertyChanged(nameof(P4_Z_Upper)); OnPropertyChanged(nameof(P4_Z_Value)); OnPropertyChanged(nameof(P4_Z_Lower));
                    OnPropertyChanged(nameof(P4_W_Upper)); OnPropertyChanged(nameof(P4_W_Value)); OnPropertyChanged(nameof(P4_W_Lower));
                    OnPropertyChanged(nameof(P4_Result));
                    break;
            }
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