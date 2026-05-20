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
    public class PostBendingMonitorViewModel : BaseViewModel, IDisposable
    {
        private readonly INavigationService _navigationService;
        private readonly CoreClient _coreClient;
        private SafePollerEx _liveDataPoller;
        private bool _disposed;

        public ICommand PreviousCommand { get; }

        #region Model

        private PostBendingMonitorModel _model = new()
        {
            Product1 = new PostBendingMonitorRow { Product = "Product 1", X = new ParameterLImitValues(), Y = new ParameterLImitValues(), Z = new ParameterLImitValues(), W = new ParameterLImitValues() },
            Product2 = new PostBendingMonitorRow { Product = "Product 2", X = new ParameterLImitValues(), Y = new ParameterLImitValues(), Z = new ParameterLImitValues(), W = new ParameterLImitValues() },
            Product3 = new PostBendingMonitorRow { Product = "Product 3", X = new ParameterLImitValues(), Y = new ParameterLImitValues(), Z = new ParameterLImitValues(), W = new ParameterLImitValues() },
            Product4 = new PostBendingMonitorRow { Product = "Product 4", X = new ParameterLImitValues(), Y = new ParameterLImitValues(), Z = new ParameterLImitValues(), W = new ParameterLImitValues() },
        };

        #endregion

        #region Properties

        // --- Batch Info ---

        public string BatchNo
        {
            get => _model.BatchNo;
            set { _model.BatchNo = value; OnPropertyChanged(); }
        }

        // --- Product 1 ---

        public PostBendingMonitorRow Product1
        {
            get => _model.Product1;
            set { _model.Product1 = value; OnPropertyChanged(); NotifyFlatProperties(1); }
        }

        public string P1_Product
        {
            get => _model.Product1?.Product;
            set { if (_model.Product1 != null) { _model.Product1.Product = value; OnPropertyChanged(); } }
        }
        public string P1_QRCode
        {
            get => _model.Product1?.QRCode;
            set { if (_model.Product1 != null) { _model.Product1.QRCode = value; OnPropertyChanged(); } }
        }
        public double P1_X_Upper
        {
            get => _model.Product1?.X?.UpperLimit ?? 0;
            set { if (_model.Product1?.X != null) { _model.Product1.X.UpperLimit = value; OnPropertyChanged(); } }
        }
        public double P1_X_Value
        {
            get => _model.Product1?.X?.PresentValue ?? 0;
            set { if (_model.Product1?.X != null) { _model.Product1.X.PresentValue = value; OnPropertyChanged(); } }
        }
        public double P1_X_Lower
        {
            get => _model.Product1?.X?.LowerLimit ?? 0;
            set { if (_model.Product1?.X != null) { _model.Product1.X.LowerLimit = value; OnPropertyChanged(); } }
        }
        public double P1_Y_Upper
        {
            get => _model.Product1?.Y?.UpperLimit ?? 0;
            set { if (_model.Product1?.Y != null) { _model.Product1.Y.UpperLimit = value; OnPropertyChanged(); } }
        }
        public double P1_Y_Value
        {
            get => _model.Product1?.Y?.PresentValue ?? 0;
            set { if (_model.Product1?.Y != null) { _model.Product1.Y.PresentValue = value; OnPropertyChanged(); } }
        }
        public double P1_Y_Lower
        {
            get => _model.Product1?.Y?.LowerLimit ?? 0;
            set { if (_model.Product1?.Y != null) { _model.Product1.Y.LowerLimit = value; OnPropertyChanged(); } }
        }
        public double P1_Z_Upper
        {
            get => _model.Product1?.Z?.UpperLimit ?? 0;
            set { if (_model.Product1?.Z != null) { _model.Product1.Z.UpperLimit = value; OnPropertyChanged(); } }
        }
        public double P1_Z_Value
        {
            get => _model.Product1?.Z?.PresentValue ?? 0;
            set { if (_model.Product1?.Z != null) { _model.Product1.Z.PresentValue = value; OnPropertyChanged(); } }
        }
        public double P1_Z_Lower
        {
            get => _model.Product1?.Z?.LowerLimit ?? 0;
            set { if (_model.Product1?.Z != null) { _model.Product1.Z.LowerLimit = value; OnPropertyChanged(); } }
        }
        public double P1_W_Upper
        {
            get => _model.Product1?.W?.UpperLimit ?? 0;
            set { if (_model.Product1?.W != null) { _model.Product1.W.UpperLimit = value; OnPropertyChanged(); } }
        }
        public double P1_W_Value
        {
            get => _model.Product1?.W?.PresentValue ?? 0;
            set { if (_model.Product1?.W != null) { _model.Product1.W.PresentValue = value; OnPropertyChanged(); } }
        }
        public double P1_W_Lower
        {
            get => _model.Product1?.W?.LowerLimit ?? 0;
            set { if (_model.Product1?.W != null) { _model.Product1.W.LowerLimit = value; OnPropertyChanged(); } }
        }
        public bool P1_Result
        {
            get => _model.Product1?.Result ?? false;
            set { if (_model.Product1 != null) { _model.Product1.Result = value; OnPropertyChanged(); } }
        }

        // --- Product 2 ---

        public PostBendingMonitorRow Product2
        {
            get => _model.Product2;
            set { _model.Product2 = value; OnPropertyChanged(); NotifyFlatProperties(2); }
        }

        public string P2_Product
        {
            get => _model.Product2?.Product;
            set { if (_model.Product2 != null) { _model.Product2.Product = value; OnPropertyChanged(); } }
        }
        public string P2_QRCode
        {
            get => _model.Product2?.QRCode;
            set { if (_model.Product2 != null) { _model.Product2.QRCode = value; OnPropertyChanged(); } }
        }
        public double P2_X_Upper
        {
            get => _model.Product2?.X?.UpperLimit ?? 0;
            set { if (_model.Product2?.X != null) { _model.Product2.X.UpperLimit = value; OnPropertyChanged(); } }
        }
        public double P2_X_Value
        {
            get => _model.Product2?.X?.PresentValue ?? 0;
            set { if (_model.Product2?.X != null) { _model.Product2.X.PresentValue = value; OnPropertyChanged(); } }
        }
        public double P2_X_Lower
        {
            get => _model.Product2?.X?.LowerLimit ?? 0;
            set { if (_model.Product2?.X != null) { _model.Product2.X.LowerLimit = value; OnPropertyChanged(); } }
        }
        public double P2_Y_Upper
        {
            get => _model.Product2?.Y?.UpperLimit ?? 0;
            set { if (_model.Product2?.Y != null) { _model.Product2.Y.UpperLimit = value; OnPropertyChanged(); } }
        }
        public double P2_Y_Value
        {
            get => _model.Product2?.Y?.PresentValue ?? 0;
            set { if (_model.Product2?.Y != null) { _model.Product2.Y.PresentValue = value; OnPropertyChanged(); } }
        }
        public double P2_Y_Lower
        {
            get => _model.Product2?.Y?.LowerLimit ?? 0;
            set { if (_model.Product2?.Y != null) { _model.Product2.Y.LowerLimit = value; OnPropertyChanged(); } }
        }
        public double P2_Z_Upper
        {
            get => _model.Product2?.Z?.UpperLimit ?? 0;
            set { if (_model.Product2?.Z != null) { _model.Product2.Z.UpperLimit = value; OnPropertyChanged(); } }
        }
        public double P2_Z_Value
        {
            get => _model.Product2?.Z?.PresentValue ?? 0;
            set { if (_model.Product2?.Z != null) { _model.Product2.Z.PresentValue = value; OnPropertyChanged(); } }
        }
        public double P2_Z_Lower
        {
            get => _model.Product2?.Z?.LowerLimit ?? 0;
            set { if (_model.Product2?.Z != null) { _model.Product2.Z.LowerLimit = value; OnPropertyChanged(); } }
        }
        public double P2_W_Upper
        {
            get => _model.Product2?.W?.UpperLimit ?? 0;
            set { if (_model.Product2?.W != null) { _model.Product2.W.UpperLimit = value; OnPropertyChanged(); } }
        }
        public double P2_W_Value
        {
            get => _model.Product2?.W?.PresentValue ?? 0;
            set { if (_model.Product2?.W != null) { _model.Product2.W.PresentValue = value; OnPropertyChanged(); } }
        }
        public double P2_W_Lower
        {
            get => _model.Product2?.W?.LowerLimit ?? 0;
            set { if (_model.Product2?.W != null) { _model.Product2.W.LowerLimit = value; OnPropertyChanged(); } }
        }
        public bool P2_Result
        {
            get => _model.Product2?.Result ?? false;
            set { if (_model.Product2 != null) { _model.Product2.Result = value; OnPropertyChanged(); } }
        }

        // --- Product 3 ---

        public PostBendingMonitorRow Product3
        {
            get => _model.Product3;
            set { _model.Product3 = value; OnPropertyChanged(); NotifyFlatProperties(3); }
        }

        public string P3_Product
        {
            get => _model.Product3?.Product;
            set { if (_model.Product3 != null) { _model.Product3.Product = value; OnPropertyChanged(); } }
        }
        public string P3_QRCode
        {
            get => _model.Product3?.QRCode;
            set { if (_model.Product3 != null) { _model.Product3.QRCode = value; OnPropertyChanged(); } }
        }
        public double P3_X_Upper
        {
            get => _model.Product3?.X?.UpperLimit ?? 0;
            set { if (_model.Product3?.X != null) { _model.Product3.X.UpperLimit = value; OnPropertyChanged(); } }
        }
        public double P3_X_Value
        {
            get => _model.Product3?.X?.PresentValue ?? 0;
            set { if (_model.Product3?.X != null) { _model.Product3.X.PresentValue = value; OnPropertyChanged(); } }
        }
        public double P3_X_Lower
        {
            get => _model.Product3?.X?.LowerLimit ?? 0;
            set { if (_model.Product3?.X != null) { _model.Product3.X.LowerLimit = value; OnPropertyChanged(); } }
        }
        public double P3_Y_Upper
        {
            get => _model.Product3?.Y?.UpperLimit ?? 0;
            set { if (_model.Product3?.Y != null) { _model.Product3.Y.UpperLimit = value; OnPropertyChanged(); } }
        }
        public double P3_Y_Value
        {
            get => _model.Product3?.Y?.PresentValue ?? 0;
            set { if (_model.Product3?.Y != null) { _model.Product3.Y.PresentValue = value; OnPropertyChanged(); } }
        }
        public double P3_Y_Lower
        {
            get => _model.Product3?.Y?.LowerLimit ?? 0;
            set { if (_model.Product3?.Y != null) { _model.Product3.Y.LowerLimit = value; OnPropertyChanged(); } }
        }
        public double P3_Z_Upper
        {
            get => _model.Product3?.Z?.UpperLimit ?? 0;
            set { if (_model.Product3?.Z != null) { _model.Product3.Z.UpperLimit = value; OnPropertyChanged(); } }
        }
        public double P3_Z_Value
        {
            get => _model.Product3?.Z?.PresentValue ?? 0;
            set { if (_model.Product3?.Z != null) { _model.Product3.Z.PresentValue = value; OnPropertyChanged(); } }
        }
        public double P3_Z_Lower
        {
            get => _model.Product3?.Z?.LowerLimit ?? 0;
            set { if (_model.Product3?.Z != null) { _model.Product3.Z.LowerLimit = value; OnPropertyChanged(); } }
        }
        public double P3_W_Upper
        {
            get => _model.Product3?.W?.UpperLimit ?? 0;
            set { if (_model.Product3?.W != null) { _model.Product3.W.UpperLimit = value; OnPropertyChanged(); } }
        }
        public double P3_W_Value
        {
            get => _model.Product3?.W?.PresentValue ?? 0;
            set { if (_model.Product3?.W != null) { _model.Product3.W.PresentValue = value; OnPropertyChanged(); } }
        }
        public double P3_W_Lower
        {
            get => _model.Product3?.W?.LowerLimit ?? 0;
            set { if (_model.Product3?.W != null) { _model.Product3.W.LowerLimit = value; OnPropertyChanged(); } }
        }
        public bool P3_Result
        {
            get => _model.Product3?.Result ?? false;
            set { if (_model.Product3 != null) { _model.Product3.Result = value; OnPropertyChanged(); } }
        }

        // --- Product 4 ---

        public PostBendingMonitorRow Product4
        {
            get => _model.Product4;
            set { _model.Product4 = value; OnPropertyChanged(); NotifyFlatProperties(4); }
        }

        public string P4_Product
        {
            get => _model.Product4?.Product;
            set { if (_model.Product4 != null) { _model.Product4.Product = value; OnPropertyChanged(); } }
        }
        public string P4_QRCode
        {
            get => _model.Product4?.QRCode;
            set { if (_model.Product4 != null) { _model.Product4.QRCode = value; OnPropertyChanged(); } }
        }
        public double P4_X_Upper
        {
            get => _model.Product4?.X?.UpperLimit ?? 0;
            set { if (_model.Product4?.X != null) { _model.Product4.X.UpperLimit = value; OnPropertyChanged(); } }
        }
        public double P4_X_Value
        {
            get => _model.Product4?.X?.PresentValue ?? 0;
            set { if (_model.Product4?.X != null) { _model.Product4.X.PresentValue = value; OnPropertyChanged(); } }
        }
        public double P4_X_Lower
        {
            get => _model.Product4?.X?.LowerLimit ?? 0;
            set { if (_model.Product4?.X != null) { _model.Product4.X.LowerLimit = value; OnPropertyChanged(); } }
        }
        public double P4_Y_Upper
        {
            get => _model.Product4?.Y?.UpperLimit ?? 0;
            set { if (_model.Product4?.Y != null) { _model.Product4.Y.UpperLimit = value; OnPropertyChanged(); } }
        }
        public double P4_Y_Value
        {
            get => _model.Product4?.Y?.PresentValue ?? 0;
            set { if (_model.Product4?.Y != null) { _model.Product4.Y.PresentValue = value; OnPropertyChanged(); } }
        }
        public double P4_Y_Lower
        {
            get => _model.Product4?.Y?.LowerLimit ?? 0;
            set { if (_model.Product4?.Y != null) { _model.Product4.Y.LowerLimit = value; OnPropertyChanged(); } }
        }
        public double P4_Z_Upper
        {
            get => _model.Product4?.Z?.UpperLimit ?? 0;
            set { if (_model.Product4?.Z != null) { _model.Product4.Z.UpperLimit = value; OnPropertyChanged(); } }
        }
        public double P4_Z_Value
        {
            get => _model.Product4?.Z?.PresentValue ?? 0;
            set { if (_model.Product4?.Z != null) { _model.Product4.Z.PresentValue = value; OnPropertyChanged(); } }
        }
        public double P4_Z_Lower
        {
            get => _model.Product4?.Z?.LowerLimit ?? 0;
            set { if (_model.Product4?.Z != null) { _model.Product4.Z.LowerLimit = value; OnPropertyChanged(); } }
        }
        public double P4_W_Upper
        {
            get => _model.Product4?.W?.UpperLimit ?? 0;
            set { if (_model.Product4?.W != null) { _model.Product4.W.UpperLimit = value; OnPropertyChanged(); } }
        }
        public double P4_W_Value
        {
            get => _model.Product4?.W?.PresentValue ?? 0;
            set { if (_model.Product4?.W != null) { _model.Product4.W.PresentValue = value; OnPropertyChanged(); } }
        }
        public double P4_W_Lower
        {
            get => _model.Product4?.W?.LowerLimit ?? 0;
            set { if (_model.Product4?.W != null) { _model.Product4.W.LowerLimit = value; OnPropertyChanged(); } }
        }
        public bool P4_Result
        {
            get => _model.Product4?.Result ?? false;
            set { if (_model.Product4 != null) { _model.Product4.Result = value; OnPropertyChanged(); } }
        }

        #endregion

        public PostBendingMonitorViewModel(
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
                var rows = new[] { _model.Product1, _model.Product2, _model.Product3, _model.Product4 };
                int[] baseOffsets = { 2001, 2021, 2041, 2061 };

                for (int i = 0; i < rows.Length && i < baseOffsets.Length; i++)
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