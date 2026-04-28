using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace IPCSoftware.App.Bending.Models
{
    /// <summary>
    /// Represents the current parameter values displayed for a bending stage
    /// </summary>
    public class BindingParametersModel : INotifyPropertyChanged
    {
        private string _lotId;
        private double _temp1;
        private double _temp2;
        private double _temp3;
        private double _temp4;
        private double _pressure1;
        private double _pressure2;
        private double _pressure3;
        private double _pressure4;
        private string _qrCode1;
        private string _qrCode2;
        private string _qrCode3;
        private string _qrCode4;

        public string LotId
        {
            get { return _lotId; }
            set { SetProperty(ref _lotId, value); }
        }

        public double Temp1
        {
            get { return _temp1; }
            set { SetProperty(ref _temp1, value); }
        }

        public double Temp2
        {
            get { return _temp2; }
            set { SetProperty(ref _temp2, value); }
        }

        public double Temp3
        {
            get { return _temp3; }
            set { SetProperty(ref _temp3, value); }
        }

        public double Temp4
        {
            get { return _temp4; }
            set { SetProperty(ref _temp4, value); }
        }

        public double Pressure1
        {
            get { return _pressure1; }
            set { SetProperty(ref _pressure1, value); }
        }

        public double Pressure2
        {
            get { return _pressure2; }
            set { SetProperty(ref _pressure2, value); }
        }

        public double Pressure3
        {
            get { return _pressure3; }
            set { SetProperty(ref _pressure3, value); }
        }

        public double Pressure4
        {
            get { return _pressure4; }
            set { SetProperty(ref _pressure4, value); }
        }

        public string QRCode1
        {
            get { return _qrCode1; }
            set { SetProperty(ref _qrCode1, value); }
        }

        public string QRCode2
        {
            get { return _qrCode2; }
            set { SetProperty(ref _qrCode2, value); }
        }

        public string QRCode3
        {
            get { return _qrCode3; }
            set { SetProperty(ref _qrCode3, value); }
        }

        public string QRCode4
        {
            get { return _qrCode4; }
            set { SetProperty(ref _qrCode4, value); }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string propertyName = null)
        {
            if (Equals(storage, value))
                return false;

            storage = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
