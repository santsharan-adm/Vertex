using IPCSoftware.App.Bending.Models;
using IPCSoftware.Common.CommonExtensions;
using IPCSoftware.Common.UIClientComm;
using IPCSoftware.Core.Interfaces;
using IPCSoftware.Core.Interfaces.AppLoggerInterface;
using IPCSoftware.Shared;
using IPCSoftware.Shared.Models.ConfigModels;
using IPCSoftware.UI.CommonViews.ViewModels;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Input;

namespace IPCSoftware.App.Bending.ViewModels
{
    public class Bending1MonitorViewModel : BaseViewModel, IDisposable
    {
        private readonly INavigationService _navigationService;
        private readonly CoreClient _coreClient;
        private SafePollerEx _liveDataPoller;
        private bool _disposed;

        public ICommand NextCommand { get; }

        private BendingMonitorModel _bendingModel = new BendingMonitorModel
        {
            Product1 = new BendingMonitorProductModel
            {
                Product = "Product 1",
                Load = new ParameterLImitValues(),
                Temperature = new ParameterLImitValues()
            },
            Product2 = new BendingMonitorProductModel
            {
                Product = "Product 2",
                Load = new ParameterLImitValues(),
                Temperature = new ParameterLImitValues()
            },
            Product3 = new BendingMonitorProductModel
            {
                Product = "Product 3",
                Load = new ParameterLImitValues(),
                Temperature = new ParameterLImitValues()
            },
            Product4 = new BendingMonitorProductModel
            {
                Product = "Product 4",
                Load = new ParameterLImitValues(),
                Temperature = new ParameterLImitValues()
            }
        };

        public BendingMonitorModel BendingModel
        {
            get => _bendingModel;
            set => SetProperty(ref _bendingModel, value);
        }

        public Bending1MonitorViewModel(
            INavigationService navigationService,
            CoreClient coreClient,
            IAppLogger logger)
            : base(logger)
        {
            _navigationService = navigationService;
            _coreClient = coreClient;

            NextCommand = new RelayCommand(ExecuteNext);
        }

        public void Initialize()
        {
            _liveDataPoller = new SafePollerEx(
                _coreClient,
                TimeSpan.FromMilliseconds(500),
                UpdateFromService,
                _logger,
                ex => _logger.LogError($"[Bending1Monitor] Poller error: {ex.Message}", LogType.Diagnostics));

            _liveDataPoller.Start();
        }

        private void ExecuteNext()
        {
            _navigationService.NavigateToDashboard2();
        }

        private async Task UpdateFromService(Dictionary<int, object> data)
        {
            try
            {
                if (data == null)
                    return;

                if (data.TryGetValue(1000, out object batchNo))
                    BendingModel.BatchNo = batchNo?.ToString() ?? "---";

                // Product 1 = 1001
                UpdateProductFromData(BendingModel.Product1, data, 1001);

                // Product 2 = 1021
                UpdateProductFromData(BendingModel.Product2, data, 1021);

                // Product 3 = 1041
                UpdateProductFromData(BendingModel.Product3, data, 1041);

                // Product 4 = 1061
                UpdateProductFromData(BendingModel.Product4, data, 1061);
            }
            catch (Exception ex)
            {
                _logger.LogError($"[Bending1Monitor] UpdateFromService error: {ex.Message}", LogType.Diagnostics);
            }

            await Task.CompletedTask;
        }

        private void UpdateProductFromData(
            BendingMonitorProductModel product,
            Dictionary<int, object> data,
            int baseOffset)
        {
            if (product == null)
                return;

            if (product.Load == null)
                product.Load = new ParameterLImitValues();

            if (product.Temperature == null)
                product.Temperature = new ParameterLImitValues();

            if (data.TryGetValue(baseOffset, out object qrCode))
                product.QRCode = qrCode?.ToString() ?? "---";

            if (data.TryGetValue(baseOffset + 1, out object loadUpper))
                product.Load.UpperLimit = ToDouble(loadUpper);

            if (data.TryGetValue(baseOffset + 2, out object loadPresent))
                product.Load.PresentValue = ToDouble(loadPresent);

            if (data.TryGetValue(baseOffset + 3, out object loadLower))
                product.Load.LowerLimit = ToDouble(loadLower);

            if (data.TryGetValue(baseOffset + 4, out object tempUpper))
                product.Temperature.UpperLimit = ToDouble(tempUpper);

            if (data.TryGetValue(baseOffset + 5, out object tempPresent))
                product.Temperature.PresentValue = ToDouble(tempPresent);

            if (data.TryGetValue(baseOffset + 6, out object tempLower))
                product.Temperature.LowerLimit = ToDouble(tempLower);

            if (data.TryGetValue(baseOffset + 7, out object bendingTime))
                product.BendingTime = ToDouble(bendingTime);

            if (data.TryGetValue(baseOffset + 8, out object result))
                product.Result = ToBool(result);
        }

        private double ToDouble(object value)
        {
            if (value == null)
                return 0;

            if (double.TryParse(value.ToString(), out double result))
                return result;

            return 0;
        }

        private bool ToBool(object value)
        {
            if (value == null)
                return false;

            if (value is bool boolValue)
                return boolValue;

            if (value is int intValue)
                return intValue == 1;

            if (value is short shortValue)
                return shortValue == 1;

            if (value is double doubleValue)
                return doubleValue == 1;

            if (bool.TryParse(value.ToString(), out bool result))
                return result;

            if (int.TryParse(value.ToString(), out int number))
                return number == 1;

            return false;
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