using IPCSoftware.Shared;

namespace IPCSoftware.Shared.Models.Bending
{
    public class BendingMonitorModel : ObservableObjectVM
    {
        private int _batchNo ;
        public int BatchNo
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

        private string _qrCode = "---";
        public string QRCode
        {
            get => _qrCode;
            set => SetProperty(ref _qrCode, value);
        }

        //-- Load (N) --//

        private ParameterLImitValues _load = new ParameterLImitValues();
        public ParameterLImitValues Load
        {
            get => _load;
            set => SetProperty(ref _load, value);
        }

        //-- Temp (°C) --//

        private ParameterLImitValues _temperature = new ParameterLImitValues();
        public ParameterLImitValues Temperature
        {
            get => _temperature;
            set => SetProperty(ref _temperature, value);
        }

        //-- Bending Time --//

        private double _bendingTime = 0;
        public double BendingTime
        {
            get => _bendingTime;
            set => SetProperty(ref _bendingTime, value);
        }

        //-- Result --//

        private bool _result = false;
        public bool Result
        {
            get => _result;
            set => SetProperty(ref _result, value);
        }
    }

    public class ParameterLImitValues : ObservableObjectVM
    {
        private double _upperLimit;
        public double UpperLimit
        {
            get => _upperLimit;
            set => SetProperty(ref _upperLimit, value);
        }

        private double _presentValue;
        public double PresentValue
        {
            get => _presentValue;
            set => SetProperty(ref _presentValue, value);
        }

        private double _lowerLimit;
        public double LowerLimit
        {
            get => _lowerLimit;
            set => SetProperty(ref _lowerLimit, value);
        }
    }
}