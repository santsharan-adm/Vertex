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
    public class PostBendingMonitorViewModel : BaseViewModel, IDisposable
    {
        private readonly INavigationService _navigationService;
        private readonly CoreClient _coreClient;
        private SafePollerEx _liveDataPoller;
        private bool _disposed;

        public ICommand PreviousCommand { get; }

        #region Model

        private PostBendingMonitorModel _postBendingMonitorModel;

        public PostBendingMonitorModel PostBendingMonitorModel
        {
            get => _postBendingMonitorModel;
            set
            {
                _postBendingMonitorModel = value;
                OnPropertyChanged();
            }
        }

        #endregion

        #region Constructor

        public PostBendingMonitorViewModel(
            INavigationService navigationService,
            CoreClient coreClient,
            IAppLogger logger)
            : base(logger)
        {
            _navigationService = navigationService;
            _coreClient = coreClient;

            PostBendingMonitorModel = CreateDefaultModel();

            PreviousCommand = new RelayCommand(ExecutePrevious);
        }

        #endregion

        #region Initialize

        public void Initialize()
        {
            _liveDataPoller = new SafePollerEx(
                _coreClient,
                TimeSpan.FromMilliseconds(500),
                UpdateFromService,
                _logger,
                ex => _logger.LogError($"[PostBendingMonitor] Poller error: {ex.Message}", LogType.Diagnostics));

            _liveDataPoller.Start();
        }

        #endregion

        #region Navigation

        private void ExecutePrevious()
        {
            _navigationService.NavigateToDashboard3();
        }

        #endregion

        #region Service Update

        private async Task UpdateFromService(Dictionary<int, object> data)
        {
            try
            {
                if (data == null)
                    return;

                if (data.TryGetValue(2000, out object batchNo))
                    PostBendingMonitorModel.BatchNo = batchNo?.ToString() ?? "---";

                UpdateProductRow(data, PostBendingMonitorModel.Product1, 2001);
                UpdateProductRow(data, PostBendingMonitorModel.Product2, 2021);
                UpdateProductRow(data, PostBendingMonitorModel.Product3, 2041);
                UpdateProductRow(data, PostBendingMonitorModel.Product4, 2061);
            }
            catch (Exception ex)
            {
                _logger.LogError($"[PostBendingMonitor] UpdateFromService error: {ex.Message}", LogType.Diagnostics);
            }

            await Task.CompletedTask;
        }

        private void UpdateProductRow(
            Dictionary<int, object> data,
            PostBendingMonitorRow row,
            int baseAddress)
        {
            if (row == null)
                return;

            if (data.TryGetValue(baseAddress, out object qrCode))
                row.QRCode = qrCode?.ToString() ?? "---";

            EnsureParameterObjects(row);

            UpdateParameter(data, row.X, baseAddress + 1);
            UpdateParameter(data, row.Y, baseAddress + 4);
            UpdateParameter(data, row.Z, baseAddress + 7);
            UpdateParameter(data, row.W, baseAddress + 10);

            if (data.TryGetValue(baseAddress + 13, out object result))
                row.Result = ToBool(result);
        }

        private void UpdateParameter(
            Dictionary<int, object> data,
            ParameterLImitValues parameter,
            int startAddress)
        {
            if (parameter == null)
                return;

            if (data.TryGetValue(startAddress, out object upperLimit))
                parameter.UpperLimit = ToDouble(upperLimit);

            if (data.TryGetValue(startAddress + 1, out object presentValue))
                parameter.PresentValue = ToDouble(presentValue);

            if (data.TryGetValue(startAddress + 2, out object lowerLimit))
                parameter.LowerLimit = ToDouble(lowerLimit);
        }

        #endregion

        #region Helpers

        private PostBendingMonitorModel CreateDefaultModel()
        {
            return new PostBendingMonitorModel
            {
                BatchNo = "---",

                Product1 = CreateProductRow("Product 1"),
                Product2 = CreateProductRow("Product 2"),
                Product3 = CreateProductRow("Product 3"),
                Product4 = CreateProductRow("Product 4")
            };
        }

        private PostBendingMonitorRow CreateProductRow(string productName)
        {
            return new PostBendingMonitorRow
            {
                Product = productName,
                QRCode = "---",
                X = CreateParameter(),
                Y = CreateParameter(),
                Z = CreateParameter(),
                W = CreateParameter(),
                Result = false
            };
        }

        private ParameterLImitValues CreateParameter()
        {
            return new ParameterLImitValues
            {
                UpperLimit = 0,
                PresentValue = 0,
                LowerLimit = 0
            };
        }

        private void EnsureParameterObjects(PostBendingMonitorRow row)
        {
            row.X ??= CreateParameter();
            row.Y ??= CreateParameter();
            row.Z ??= CreateParameter();
            row.W ??= CreateParameter();
        }

        private double ToDouble(object value)
        {
            if (value == null)
                return 0;

            try
            {
                return Convert.ToDouble(value);
            }
            catch
            {
                return 0;
            }
        }

        private bool ToBool(object value)
        {
            if (value == null)
                return false;

            try
            {
                return Convert.ToBoolean(value);
            }
            catch
            {
                return false;
            }
        }

        #endregion

        #region Dispose

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;

            _liveDataPoller?.Stop();
            _liveDataPoller?.Dispose();
            _liveDataPoller = null;
        }

        #endregion
    }
}