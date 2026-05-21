using IPCSoftware.Shared;

namespace IPCSoftware.Shared.Models.Bending
{
    namespace IPCSoftware.App.Bending.Models
    {
        public class PostBendingMonitorModel : ObservableObjectVM
        {
            private string _batchNo = "Loading...";
            public string BatchNo
            {
                get => _batchNo;
                set => SetProperty(ref _batchNo, value);
            }

            private PostBendingMonitorRow _product1 = new();
            public PostBendingMonitorRow Product1
            {
                get => _product1;
                set => SetProperty(ref _product1, value);
            }

            private PostBendingMonitorRow _product2 = new();
            public PostBendingMonitorRow Product2
            {
                get => _product2;
                set => SetProperty(ref _product2, value);
            }

            private PostBendingMonitorRow _product3 = new();
            public PostBendingMonitorRow Product3
            {
                get => _product3;
                set => SetProperty(ref _product3, value);
            }

            private PostBendingMonitorRow _product4 = new();
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

            private string _qrCode = "---";
            public string QRCode
            {
                get => _qrCode;
                set => SetProperty(ref _qrCode, value);
            }

            // ----------------------------------------------------------------
            // X
            // ----------------------------------------------------------------

            private PostParameterLImitValues _x = new PostParameterLImitValues();
            public PostParameterLImitValues X
            {
                get => _x;
                set => SetProperty(ref _x, value);
            }

            // ----------------------------------------------------------------
            // Y
            // ----------------------------------------------------------------

            private PostParameterLImitValues _y = new PostParameterLImitValues();
            public PostParameterLImitValues Y
            {
                get => _y;
                set => SetProperty(ref _y, value);
            }

            // ----------------------------------------------------------------
            // Z
            // ----------------------------------------------------------------

            private PostParameterLImitValues _z = new PostParameterLImitValues();
            public PostParameterLImitValues Z
            {
                get => _z;
                set => SetProperty(ref _z, value);
            }

            // ----------------------------------------------------------------
            // W
            // ----------------------------------------------------------------

            private PostParameterLImitValues _w = new PostParameterLImitValues();
            public PostParameterLImitValues W
            {
                get => _w;
                set => SetProperty(ref _w, value);
            }

            // ----------------------------------------------------------------
            // Result
            // ----------------------------------------------------------------

            private bool _result = false;
            public bool Result
            {
                get => _result;
                set => SetProperty(ref _result, value);
            }
        }

        public class PostParameterLImitValues : ObservableObjectVM
        {
            private double _upperLimit = 0;
            public double UpperLimit
            {
                get => _upperLimit;
                set => SetProperty(ref _upperLimit, value);
            }

            private double _presentValue = 0;
            public double PresentValue
            {
                get => _presentValue;
                set => SetProperty(ref _presentValue, value);
            }

            private double _lowerLimit = 0;
            public double LowerLimit
            {
                get => _lowerLimit;
                set => SetProperty(ref _lowerLimit, value);
            }
        }
    }
}