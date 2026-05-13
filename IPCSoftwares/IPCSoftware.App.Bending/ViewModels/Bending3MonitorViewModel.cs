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
    public class Bending3MonitorViewModel : BaseViewModel, IDisposable
    {
        private readonly INavigationService _navigationService;
        private readonly CoreClient _coreClient;
        private SafePoller _liveDataPoller;
        private int _liveDataRunning;
        private bool _disposed;

        public ICommand NextCommand { get; }
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

        public ObservableCollection<Bending1MonitorModel> Products { get; } = new()
        {
            new Bending1MonitorModel { Product = "Product 1" },
            new Bending1MonitorModel { Product = "Product 2" },
            new Bending1MonitorModel { Product = "Product 3" },
            new Bending1MonitorModel { Product = "Product 4" }
        };

        #endregion

        public Bending3MonitorViewModel(
            INavigationService navigationService,
            CoreClient coreClient,
            IAppLogger logger)
            : base(logger)
        {
            _navigationService = navigationService;
            _coreClient = coreClient;

            NextCommand = new RelayCommand(ExecuteNext);
            PreviousCommand = new RelayCommand(ExecutePrevious);
        }

        public void Initialize()
        {
            _liveDataPoller = new SafePoller(
                TimeSpan.FromMilliseconds(500),
                LiveDataTickAsync,
                ex => _logger.LogError($"[Bending3Monitor] Poller error: {ex.Message}", LogType.Diagnostics));

            _liveDataPoller.Start();
        }

        private void ExecuteNext()
        {
            _navigationService.NavigateToPostBendingMonitor();
        }

        private void ExecutePrevious()
        {
            _navigationService.NavigateToDashboard2();
        }

        private async Task LiveDataTickAsync()
        {
            if (Interlocked.Exchange(ref _liveDataRunning, 1) == 1)
                return;

            try
            {
                if (!_coreClient.isConnected)
                    return;

                var data = await _coreClient.GetIoValuesAsync(7);
                if (data != null && data.Count > 0)
                {
                    UpdateFromPlcData(data);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"[Bending3Monitor] LiveDataTickAsync error: {ex.Message}", LogType.Diagnostics);
            }
            finally
            {
                Interlocked.Exchange(ref _liveDataRunning, 0);
            }
        }

        private void UpdateFromPlcData(Dictionary<int, object> data)
        {
            if (data.TryGetValue(3000, out object batchNo))
                BatchNo = batchNo?.ToString() ?? "---";

            // PLC data structure: Each product has 10 tags (QR, LoadUpper, LoadValue, LoadLower, TempUpper, TempValue, TempLower, BendingTime, Result, padding)
            // Base offsets: Product 1 = 3001, Product 2 = 3011, Product 3 = 3021, Product 4 = 3031
            int[] baseOffsets = { 3001, 3011, 3021, 3031 };

            for (int i = 0; i < Products.Count && i < baseOffsets.Length; i++)
            {
                int baseOffset = baseOffsets[i];
                var product = Products[i];

                // QR Code
                if (data.TryGetValue(baseOffset, out object qrCode))
                    product.QRCode = qrCode?.ToString() ?? "---";

                // Load data
                product.Load ??= new ParameterLImitValues();
                if (data.TryGetValue(baseOffset + 1, out object loadUpper))
                    product.Load.UpperLimit = Convert.ToDouble(loadUpper);
                if (data.TryGetValue(baseOffset + 2, out object loadValue))
                    product.Load.PresentValue = Convert.ToDouble(loadValue);
                if (data.TryGetValue(baseOffset + 3, out object loadLower))
                    product.Load.LowerLimit = Convert.ToDouble(loadLower);

                // Temperature data
                product.Temprature ??= new ParameterLImitValues();
                if (data.TryGetValue(baseOffset + 4, out object tempUpper))
                    product.Temprature.UpperLimit = Convert.ToDouble(tempUpper);
                if (data.TryGetValue(baseOffset + 5, out object tempValue))
                    product.Temprature.PresentValue = Convert.ToDouble(tempValue);
                if (data.TryGetValue(baseOffset + 6, out object tempLower))
                    product.Temprature.LowerLimit = Convert.ToDouble(tempLower);

                // Bending Time and Result
                if (data.TryGetValue(baseOffset + 7, out object bendingTime))
                    product.BendingTime = Convert.ToDouble(bendingTime);
                if (data.TryGetValue(baseOffset + 8, out object result))
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
