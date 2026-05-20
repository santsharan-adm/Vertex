using IPCSoftware.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IPCSoftware.App.Bending.Models
{
    public class PostBendingMonitorModel : ObservableObjectVM
    {
        private string _batchNo;
        public string BatchNo
        {
            get => _batchNo;
            set => SetProperty(ref _batchNo, value);
        }

        private PostBendingMonitorRow _product1;
        public PostBendingMonitorRow Product1
        {
            get => _product1;
            set => SetProperty(ref _product1, value);
        }

        private PostBendingMonitorRow _product2;
        public PostBendingMonitorRow Product2
        {
            get => _product2;
            set => SetProperty(ref _product2, value);
        }

        private PostBendingMonitorRow _product3;
        public PostBendingMonitorRow Product3
        {
            get => _product3;
            set => SetProperty(ref _product3, value);
        }

        private PostBendingMonitorRow _product4;
        public PostBendingMonitorRow Product4
        {
            get => _product4;
            set => SetProperty(ref _product4, value);
        }
    }


    public class PostBendingMonitorRow : ObservableObjectVM
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

        //-- X --//

        private ParameterLImitValues _x;
        public ParameterLImitValues X
        {
            get => _x;
            set => SetProperty(ref _x, value);
        }
        //-- Y --//

        private ParameterLImitValues _y;
        public ParameterLImitValues Y
        {
            get => _y;
            set => SetProperty(ref _y, value);
        }

        //-- Z --//

        private ParameterLImitValues _z;
        public ParameterLImitValues Z
        {
            get => _z;
            set => SetProperty(ref _z, value);
        }
        //-- W --//

        private ParameterLImitValues _w;
        public ParameterLImitValues W
        {
            get => _w;
            set => SetProperty(ref _w, value);
        }

        //-- Result --//

        private bool _result;
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
