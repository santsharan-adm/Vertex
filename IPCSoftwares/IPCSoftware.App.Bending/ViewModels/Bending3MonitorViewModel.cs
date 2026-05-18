using System.Collections.ObjectModel;
using System.Windows.Input;
using IPCSoftware.App.Bending.Models;
using IPCSoftware.App.Bending.Views;
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

        private SafePollerEx _liveDataPoller;

        private bool _disposed;
        private readonly CoreClient _coreClient;

        public ICommand PreviousCommand { get; }
        public ICommand NextCommand { get; }

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

            PreviousCommand = new RelayCommand(ExecutePrevious);
            NextCommand = new RelayCommand(ExecuteNext);
        }

        public void Initialize()
        {
            _liveDataPoller = new SafePollerEx(
                _coreClient,
                TimeSpan.FromMilliseconds(500),
                UpdateFromService,
                _logger,
                ex => _logger.LogError(
                    $"[Bending3Monitor] Poller error: {ex.Message}",
                    LogType.Diagnostics));

            _liveDataPoller.Start();
        }

        private void ExecutePrevious()
        {
            _navigationService.NavigateToDashboard2();
        }

        private void ExecuteNext()
        {
            _navigationService.NavigateMain<PostBendingMonitor>();
        }

        private async Task UpdateFromService(Dictionary<int, object> data)
        {
            try
            {
                if (data.TryGetValue(1000, out object batchNo))
                    BatchNo = batchNo?.ToString() ?? "---";

                // PLC data structure: Each product has 10 tags (QR, LoadUpper, LoadValue, LoadLower, TempUpper, TempValue, TempLower, BendingTime, Result, padding)
                // Base offsets: Product 1 = 1001, Product 2 = 1021, Product 3 = 1041, Product 4 = 1061  
                int[] baseOffsets = { 1001, 1021, 1041, 1061 };

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
            catch (Exception ex)
            {
                _logger.LogError(
                    $"[Bending3Monitor] UpdateFromService error: {ex.Message}",
                    LogType.Diagnostics);
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