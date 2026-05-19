using IPCSoftware.App.Bending.Models;
using IPCSoftware.Shared;

namespace IPCSoftware.App.Bending.Models
{
    public class Bending1MonitorModel : ObservableObjectVM
    {
        public string BatchNo { get; set; }
        
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

        private ParameterLImitValues _temprature;
        public ParameterLImitValues Temprature
        {
            get => _temprature;
            set => SetProperty(ref _temprature, value);
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