using System.Collections.ObjectModel;
using System.Threading;
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
        private SafePoller _liveDataPoller;
        private int _liveDataRunning;
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
            new PostBendingMonitorRow { Product = "Product 1" },
            new PostBendingMonitorRow { Product = "Product 2" },
            new PostBendingMonitorRow { Product = "Product 3" },
            new PostBendingMonitorRow { Product = "Product 4" }
        };

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
            _liveDataPoller = new SafePoller(
                TimeSpan.FromMilliseconds(500),
                LiveDataTickAsync,
                ex => _logger.LogError($"[PostBendingMonitor] Poller error: {ex.Message}", LogType.Diagnostics));

            _liveDataPoller.Start();
        }

        private void ExecutePrevious()
        {
            _navigationService.NavigateToDashboard3();
        }

        private async Task LiveDataTickAsync()
        {
            if (Interlocked.Exchange(ref _liveDataRunning, 1) == 1)
                return;

            try
            {
                if (!_coreClient.isConnected)
                    return;

                var data = await _coreClient.GetIoValuesAsync(6);
                if (data != null && data.Count > 0)
                {
                    UpdateFromPlcData(data);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"[PostBendingMonitor] LiveDataTickAsync error: {ex.Message}", LogType.Diagnostics);
            }
            finally
            {
                Interlocked.Exchange(ref _liveDataRunning, 0);
            }
        }

        private void UpdateFromPlcData(Dictionary<int, object> data)
        {
            if (data.TryGetValue(2000, out object batchNo))
                BatchNo = batchNo?.ToString() ?? "---";

            // PLC data structure: Each product has 15 tags
            // (QR, X_Upper, X_Value, X_Lower, Y_Upper, Y_Value, Y_Lower, Z_Upper, Z_Value, Z_Lower, W_Upper, W_Value, W_Lower, Result, padding)
            // Base offsets: Product 1 = 2001, Product 2 = 2016, Product 3 = 2031, Product 4 = 2046
            int[] baseOffsets = { 2001, 2016, 2031, 2046 };

            for (int i = 0; i < Products.Count && i < baseOffsets.Length; i++)
            {
                int baseOffset = baseOffsets[i];
                var product = Products[i];

                // QR Code
                if (data.TryGetValue(baseOffset, out object qrCode))
                    product.QRCode = qrCode?.ToString() ?? "---";

                // X data
                product.X ??= new ParameterLImitValues();
                if (data.TryGetValue(baseOffset + 1, out object xUpper))
                    product.X.UpperLimit = Convert.ToDouble(xUpper);
                if (data.TryGetValue(baseOffset + 2, out object xValue))
                    product.X.PresentValue = Convert.ToDouble(xValue);
                if (data.TryGetValue(baseOffset + 3, out object xLower))
                    product.X.LowerLimit = Convert.ToDouble(xLower);

                // Y data
                product.Y ??= new ParameterLImitValues();
                if (data.TryGetValue(baseOffset + 4, out object yUpper))
                    product.Y.UpperLimit = Convert.ToDouble(yUpper);
                if (data.TryGetValue(baseOffset + 5, out object yValue))
                    product.Y.PresentValue = Convert.ToDouble(yValue);
                if (data.TryGetValue(baseOffset + 6, out object yLower))
                    product.Y.LowerLimit = Convert.ToDouble(yLower);

                // Z data
                product.Z ??= new ParameterLImitValues();
                if (data.TryGetValue(baseOffset + 7, out object zUpper))
                    product.Z.UpperLimit = Convert.ToDouble(zUpper);
                if (data.TryGetValue(baseOffset + 8, out object zValue))
                    product.Z.PresentValue = Convert.ToDouble(zValue);
                if (data.TryGetValue(baseOffset + 9, out object zLower))
                    product.Z.LowerLimit = Convert.ToDouble(zLower);

                // W data
                product.W ??= new ParameterLImitValues();
                if (data.TryGetValue(baseOffset + 10, out object wUpper))
                    product.W.UpperLimit = Convert.ToDouble(wUpper);
                if (data.TryGetValue(baseOffset + 11, out object wValue))
                    product.W.PresentValue = Convert.ToDouble(wValue);
                if (data.TryGetValue(baseOffset + 12, out object wLower))
                    product.W.LowerLimit = Convert.ToDouble(wLower);

                // Result
                if (data.TryGetValue(baseOffset + 13, out object result))
                    product.Result = Convert.ToBoolean(result);
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