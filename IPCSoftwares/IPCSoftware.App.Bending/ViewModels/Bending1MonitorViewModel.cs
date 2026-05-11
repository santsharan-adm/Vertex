using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
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
    public class Bending1MonitorViewModel : BaseViewModel, IDisposable
    {
        private readonly INavigationService _navigationService;
        private readonly CoreClient _coreClient;
        private readonly SafePoller _liveDataTimer;

        public ICommand NextCommand { get; }

        private string _batchNo = "Loading...";
        public string BatchNo
        {
            get => _batchNo;
            set => SetProperty(ref _batchNo, value);
        }

        // ==================== PRODUCT 1 DATA ====================
        private string _p1_Product = "Product 1";
        public string P1_Product
        {
            get => _p1_Product;
            set => SetProperty(ref _p1_Product, value);
        }

        private string _p1_QRCode = "---";
        public string P1_QRCode
        {
            get => _p1_QRCode;
            set => SetProperty(ref _p1_QRCode, value);
        }

        private double _p1_Load_Upper;
        public double P1_Load_Upper
        {
            get => _p1_Load_Upper;
            set => SetProperty(ref _p1_Load_Upper, value);
        }

        private double _p1_Load_Value;
        public double P1_Load_Value
        {
            get => _p1_Load_Value;
            set => SetProperty(ref _p1_Load_Value, value);
        }

        private double _p1_Load_Lower;
        public double P1_Load_Lower
        {
            get => _p1_Load_Lower;
            set => SetProperty(ref _p1_Load_Lower, value);
        }

        private double _p1_Temp_Upper;
        public double P1_Temp_Upper
        {
            get => _p1_Temp_Upper;
            set => SetProperty(ref _p1_Temp_Upper, value);
        }

        private double _p1_Temp_Value;
        public double P1_Temp_Value
        {
            get => _p1_Temp_Value;
            set => SetProperty(ref _p1_Temp_Value, value);
        }

        private double _p1_Temp_Lower;
        public double P1_Temp_Lower
        {
            get => _p1_Temp_Lower;
            set => SetProperty(ref _p1_Temp_Lower, value);
        }

        private double _p1_BendingTime;
        public double P1_BendingTime
        {
            get => _p1_BendingTime;
            set => SetProperty(ref _p1_BendingTime, value);
        }

        private bool _p1_Result;
        public bool P1_Result
        {
            get => _p1_Result;
            set => SetProperty(ref _p1_Result, value);
        }

        // ==================== PRODUCT 2 DATA ====================
        private string _p2_Product = "Product 2";
        public string P2_Product
        {
            get => _p2_Product;
            set => SetProperty(ref _p2_Product, value);
        }

        private string _p2_QRCode = "---";
        public string P2_QRCode
        {
            get => _p2_QRCode;
            set => SetProperty(ref _p2_QRCode, value);
        }

        private double _p2_Load_Upper;
        public double P2_Load_Upper
        {
            get => _p2_Load_Upper;
            set => SetProperty(ref _p2_Load_Upper, value);
        }

        private double _p2_Load_Value;
        public double P2_Load_Value
        {
            get => _p2_Load_Value;
            set => SetProperty(ref _p2_Load_Value, value);
        }

        private double _p2_Load_Lower;
        public double P2_Load_Lower
        {
            get => _p2_Load_Lower;
            set => SetProperty(ref _p2_Load_Lower, value);
        }

        private double _p2_Temp_Upper;
        public double P2_Temp_Upper
        {
            get => _p2_Temp_Upper;
            set => SetProperty(ref _p2_Temp_Upper, value);
        }

        private double _p2_Temp_Value;
        public double P2_Temp_Value
        {
            get => _p2_Temp_Value;
            set => SetProperty(ref _p2_Temp_Value, value);
        }

        private double _p2_Temp_Lower;
        public double P2_Temp_Lower
        {
            get => _p2_Temp_Lower;
            set => SetProperty(ref _p2_Temp_Lower, value);
        }

        private double _p2_BendingTime;
        public double P2_BendingTime
        {
            get => _p2_BendingTime;
            set => SetProperty(ref _p2_BendingTime, value);
        }

        private bool _p2_Result;
        public bool P2_Result
        {
            get => _p2_Result;
            set => SetProperty(ref _p2_Result, value);
        }

        // ==================== PRODUCT 3 DATA ====================
        private string _p3_Product = "Product 3";
        public string P3_Product
        {
            get => _p3_Product;
            set => SetProperty(ref _p3_Product, value);
        }

        private string _p3_QRCode = "---";
        public string P3_QRCode
        {
            get => _p3_QRCode;
            set => SetProperty(ref _p3_QRCode, value);
        }

        private double _p3_Load_Upper;
        public double P3_Load_Upper
        {
            get => _p3_Load_Upper;
            set => SetProperty(ref _p3_Load_Upper, value);
        }

        private double _p3_Load_Value;
        public double P3_Load_Value
        {
            get => _p3_Load_Value;
            set => SetProperty(ref _p3_Load_Value, value);
        }

        private double _p3_Load_Lower;
        public double P3_Load_Lower
        {
            get => _p3_Load_Lower;
            set => SetProperty(ref _p3_Load_Lower, value);
        }

        private double _p3_Temp_Upper;
        public double P3_Temp_Upper
        {
            get => _p3_Temp_Upper;
            set => SetProperty(ref _p3_Temp_Upper, value);
        }

        private double _p3_Temp_Value;
        public double P3_Temp_Value
        {
            get => _p3_Temp_Value;
            set => SetProperty(ref _p3_Temp_Value, value);
        }

        private double _p3_Temp_Lower;
        public double P3_Temp_Lower
        {
            get => _p3_Temp_Lower;
            set => SetProperty(ref _p3_Temp_Lower, value);
        }

        private double _p3_BendingTime;
        public double P3_BendingTime
        {
            get => _p3_BendingTime;
            set => SetProperty(ref _p3_BendingTime, value);
        }

        private bool _p3_Result;
        public bool P3_Result
        {
            get => _p3_Result;
            set => SetProperty(ref _p3_Result, value);
        }

        // ==================== PRODUCT 4 DATA ====================
        private string _p4_Product = "Product 4";
        public string P4_Product
        {
            get => _p4_Product;
            set => SetProperty(ref _p4_Product, value);
        }

        private string _p4_QRCode = "---";
        public string P4_QRCode
        {
            get => _p4_QRCode;
            set => SetProperty(ref _p4_QRCode, value);
        }

        private double _p4_Load_Upper;
        public double P4_Load_Upper
        {
            get => _p4_Load_Upper;
            set => SetProperty(ref _p4_Load_Upper, value);
        }

        private double _p4_Load_Value;
        public double P4_Load_Value
        {
            get => _p4_Load_Value;
            set => SetProperty(ref _p4_Load_Value, value);
        }

        private double _p4_Load_Lower;
        public double P4_Load_Lower
        {
            get => _p4_Load_Lower;
            set => SetProperty(ref _p4_Load_Lower, value);
        }

        private double _p4_Temp_Upper;
        public double P4_Temp_Upper
        {
            get => _p4_Temp_Upper;
            set => SetProperty(ref _p4_Temp_Upper, value);
        }

        private double _p4_Temp_Value;
        public double P4_Temp_Value
        {
            get => _p4_Temp_Value;
            set => SetProperty(ref _p4_Temp_Value, value);
        }

        private double _p4_Temp_Lower;
        public double P4_Temp_Lower
        {
            get => _p4_Temp_Lower;
            set => SetProperty(ref _p4_Temp_Lower, value);
        }

        private double _p4_BendingTime;
        public double P4_BendingTime
        {
            get => _p4_BendingTime;
            set => SetProperty(ref _p4_BendingTime, value);
        }

        private bool _p4_Result;
        public bool P4_Result
        {
            get => _p4_Result;
            set => SetProperty(ref _p4_Result, value);
        }

        public Bending1MonitorViewModel(INavigationService navigationService, CoreClient coreClient, IAppLogger logger)
            : base(logger)
        {
            _navigationService = navigationService;
            _coreClient = coreClient;
            NextCommand = new RelayCommand(ExecuteNext);

            _liveDataTimer = new SafePoller(TimeSpan.FromMilliseconds(500), OnLiveDataTick, OnPollingError);
            _liveDataTimer.Start();
        }

        private async Task OnLiveDataTick()
        {
            try
            {
                var data = await _coreClient.GetIoValuesAsync(5);
                if (data != null && data.Count > 0)
                {
                    UpdateFromPlcData(data);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"[Bending1Monitor] Error reading PLC data: {ex.Message}", LogType.Error);
            }
        }

        private void UpdateFromPlcData(System.Collections.Generic.Dictionary<int, object> data)
        {
            if (data.TryGetValue(1000, out object batchNo))
                BatchNo = batchNo?.ToString() ?? "---";

            if (data.TryGetValue(1001, out object p1Qr))
                P1_QRCode = p1Qr?.ToString() ?? "---";
            if (data.TryGetValue(1002, out object p1LoadUpper))
                P1_Load_Upper = Convert.ToDouble(p1LoadUpper);
            if (data.TryGetValue(1003, out object p1LoadValue))
                P1_Load_Value = Convert.ToDouble(p1LoadValue);
            if (data.TryGetValue(1004, out object p1LoadLower))
                P1_Load_Lower = Convert.ToDouble(p1LoadLower);
            if (data.TryGetValue(1005, out object p1TempUpper))
                P1_Temp_Upper = Convert.ToDouble(p1TempUpper);
            if (data.TryGetValue(1006, out object p1TempValue))
                P1_Temp_Value = Convert.ToDouble(p1TempValue);
            if (data.TryGetValue(1007, out object p1TempLower))
                P1_Temp_Lower = Convert.ToDouble(p1TempLower);
            if (data.TryGetValue(1008, out object p1BendTime))
                P1_BendingTime = Convert.ToDouble(p1BendTime);
            if (data.TryGetValue(1009, out object p1Result))
                P1_Result = Convert.ToBoolean(p1Result);

            if (data.TryGetValue(1011, out object p2Qr))
                P2_QRCode = p2Qr?.ToString() ?? "---";
            if (data.TryGetValue(1012, out object p2LoadUpper))
                P2_Load_Upper = Convert.ToDouble(p2LoadUpper);
            if (data.TryGetValue(1013, out object p2LoadValue))
                P2_Load_Value = Convert.ToDouble(p2LoadValue);
            if (data.TryGetValue(1014, out object p2LoadLower))
                P2_Load_Lower = Convert.ToDouble(p2LoadLower);
            if (data.TryGetValue(1015, out object p2TempUpper))
                P2_Temp_Upper = Convert.ToDouble(p2TempUpper);
            if (data.TryGetValue(1016, out object p2TempValue))
                P2_Temp_Value = Convert.ToDouble(p2TempValue);
            if (data.TryGetValue(1017, out object p2TempLower))
                P2_Temp_Lower = Convert.ToDouble(p2TempLower);
            if (data.TryGetValue(1018, out object p2BendTime))
                P2_BendingTime = Convert.ToDouble(p2BendTime);
            if (data.TryGetValue(1019, out object p2Result))
                P2_Result = Convert.ToBoolean(p2Result);

            if (data.TryGetValue(1021, out object p3Qr))
                P3_QRCode = p3Qr?.ToString() ?? "---";
            if (data.TryGetValue(1022, out object p3LoadUpper))
                P3_Load_Upper = Convert.ToDouble(p3LoadUpper);
            if (data.TryGetValue(1023, out object p3LoadValue))
                P3_Load_Value = Convert.ToDouble(p3LoadValue);
            if (data.TryGetValue(1024, out object p3LoadLower))
                P3_Load_Lower = Convert.ToDouble(p3LoadLower);
            if (data.TryGetValue(1025, out object p3TempUpper))
                P3_Temp_Upper = Convert.ToDouble(p3TempUpper);
            if (data.TryGetValue(1026, out object p3TempValue))
                P3_Temp_Value = Convert.ToDouble(p3TempValue);
            if (data.TryGetValue(1027, out object p3TempLower))
                P3_Temp_Lower = Convert.ToDouble(p3TempLower);
            if (data.TryGetValue(1028, out object p3BendTime))
                P3_BendingTime = Convert.ToDouble(p3BendTime);
            if (data.TryGetValue(1029, out object p3Result))
                P3_Result = Convert.ToBoolean(p3Result);

            if (data.TryGetValue(1031, out object p4Qr))
                P4_QRCode = p4Qr?.ToString() ?? "---";
            if (data.TryGetValue(1032, out object p4LoadUpper))
                P4_Load_Upper = Convert.ToDouble(p4LoadUpper);
            if (data.TryGetValue(1033, out object p4LoadValue))
                P4_Load_Value = Convert.ToDouble(p4LoadValue);
            if (data.TryGetValue(1034, out object p4LoadLower))
                P4_Load_Lower = Convert.ToDouble(p4LoadLower);
            if (data.TryGetValue(1035, out object p4TempUpper))
                P4_Temp_Upper = Convert.ToDouble(p4TempUpper);
            if (data.TryGetValue(1036, out object p4TempValue))
                P4_Temp_Value = Convert.ToDouble(p4TempValue);
            if (data.TryGetValue(1037, out object p4TempLower))
                P4_Temp_Lower = Convert.ToDouble(p4TempLower);
            if (data.TryGetValue(1038, out object p4BendTime))
                P4_BendingTime = Convert.ToDouble(p4BendTime);
            if (data.TryGetValue(1039, out object p4Result))
                P4_Result = Convert.ToBoolean(p4Result);
        }

        private void OnPollingError(Exception ex)
        {
            _logger.LogError($"[Bending1Monitor] Polling error: {ex.Message}", LogType.Error);
        }

        private void ExecuteNext()
        {
            _navigationService.NavigateToDashboard2();
        }

        public void Dispose()
        {
            _liveDataTimer?.Stop();
            _liveDataTimer?.Dispose();
        }
    }
}