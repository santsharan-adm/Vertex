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
    public class Bending2MonitorViewModel : BaseViewModel, IDisposable
    {
        private readonly INavigationService _navigationService;

        private SafePollerEx _liveDataPoller;

        private bool _disposed;
        private readonly CoreClient _coreClient;

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

        // --- Product ---

        private string _product;
        public string Product
        {
            get => _product;
            set => SetProperty(ref _product, value);
        }

        // --- QR Code ---

        private string _qrCode;
        public string QRCode
        {
            get => _qrCode;
            set => SetProperty(ref _qrCode, value);
        }

        // --- Load (N) ---

        private ParameterLImitValues _load = new();
        public ParameterLImitValues Load
        {
            get => _load;
            set => SetProperty(ref _load, value);
        }

        // --- Temperature (°C) ---

        private ParameterLImitValues _temprature = new();
        public ParameterLImitValues Temprature
        {
            get => _temprature;
            set => SetProperty(ref _temprature, value);
        }

        // --- Bending Time ---

        private double _bendingTime;
        public double BendingTime
        {
            get => _bendingTime;
            set => SetProperty(ref _bendingTime, value);
        }

        // --- Result ---

        private bool _result;
        public bool Result
        {
            get => _result;
            set => SetProperty(ref _result, value);
        }

        // --- Product Table ---

        public ObservableCollection<BendingMonitorProductModel> Products { get; } = new()
        {
            new BendingMonitorProductModel { Product = "Product 1" },
            new BendingMonitorProductModel { Product = "Product 2" },
            new BendingMonitorProductModel { Product = "Product 3" },
            new BendingMonitorProductModel { Product = "Product 4" }
        };

        #endregion

        public Bending2MonitorViewModel(
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
            _liveDataPoller = new SafePollerEx(_coreClient,
                TimeSpan.FromMilliseconds(500),
                UpdateFromService,
                _logger,
                ex => _logger.LogError($"[Bending2Monitor] Poller error: {ex.Message}", LogType.Diagnostics));

            _liveDataPoller.Start();
        }

        private void ExecuteNext()
        {
            _navigationService.NavigateMain<Bending3MonitorView>();
        }

        private void ExecutePrevious()
        {
            _navigationService.NavigateMain<Bending1MonitorView>();
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
                    product.Temperature ??= new ParameterLImitValues();
                    if (data.TryGetValue(baseOffset + 4, out object tempUpper))
                        product.Temperature.UpperLimit = Convert.ToDouble(tempUpper);
                    if (data.TryGetValue(baseOffset + 5, out object tempValue))
                        product.Temperature.PresentValue = Convert.ToDouble(tempValue);
                    if (data.TryGetValue(baseOffset + 6, out object tempLower))
                        product.Temperature.LowerLimit = Convert.ToDouble(tempLower);

                    // Bending Time and Result
                    if (data.TryGetValue(baseOffset + 7, out object bendingTime))
                        product.BendingTime = Convert.ToDouble(bendingTime);
                    if (data.TryGetValue(baseOffset + 8, out object result))
                        product.Result = Convert.ToBoolean(result);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"[Bending2Monitor] UpdateFromService error: {ex.Message}", LogType.Diagnostics);
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