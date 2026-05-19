using IPCSoftware.App.Bending.Models;
using IPCSoftware.Shared;

namespace IPCSoftware.App.Bending.Models
{
    public class BendingMonitorModel : ObservableObjectVM
    {
        private string _batchNo = "Loading...";
        public string BatchNo
        {
            get => _batchNo;
            set => SetProperty(ref _batchNo, value);
        }

        public BendingMonitorProductModel Product1 { get; set; } = new();
        public BendingMonitorProductModel Product2 { get; set; } = new();
        public BendingMonitorProductModel Product3 { get; set; } = new();
        public BendingMonitorProductModel Product4 { get; set; } = new();
}

    public class BendingMonitorProductModel : ObservableObjectVM
    {
        private string _product;
        public string Product
        {
            get => _product;
            set => SetProperty(ref _product, value);
        }

        private string _qrCode;
        public string QRCode
        {
            get => _qrCode;
            set => SetProperty(ref _qrCode, value);
        }

        //-- Load (N) --//

        private ParameterLImitValues _load;
        public ParameterLImitValues Load
        {
            get => _load;
            set => SetProperty(ref _load, value);
        }

        //-- Temp (°C) --//

        private ParameterLImitValues _temperature;
        public ParameterLImitValues Temperature
        {
            get => _temperature;
            set => SetProperty(ref _temperature, value);
        }

        //-- Bending Time --//

        private double _bendingTime;
        public double BendingTime
        {
            get => _bendingTime;
            set => SetProperty(ref _bendingTime, value);
        }

        //-- Result --//

        private bool _result;
        public bool Result
        {
            get => _result;
            set => SetProperty(ref _result, value);
        }
    }
}