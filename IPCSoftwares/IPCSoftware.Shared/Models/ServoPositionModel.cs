using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;

namespace IPCSoftware.Shared.Models
{
    public class ServoPositionModel : ObservableObjectVM
    {
        public int PositionId { get; set; } // 0 to 12
        public string Name { get; set; }    // e.g., "Home", "Station 1"
                                            //public double X { get; set; }       // Saved X Coordinate
                                            //public double Y { get; set; }       // Saved Y Coordinate
        private int _sequenceIndex;
        public int SequenceIndex
        {
            get => _sequenceIndex;
            set => SetProperty(ref _sequenceIndex, value);
        }

        private double _x;
        public double X
        {
            get => _x;
            set => SetProperty(ref _x, value);  
        }


        private double _y;
        public double Y
        {
            get => _y;
            set => SetProperty(ref _y, value);
        }
   

        public string Description { get; set; }
        private bool _isEnabled = true;
        public bool IsEnabled
        {
            get => _isEnabled;
            set { _isEnabled = value; OnPropertyChanged(); }
        }
    }

    public class ServoParameterItem : ObservableObjectVM
    {
        public string Name { get; set; }
        public int ReadTagId { get; set; }
        public int WriteTagId { get; set; }

        private double _currentValue;
        public double CurrentValue
        {
            get => _currentValue;
            set => SetProperty(ref _currentValue, value);
        }

        private double _newValue;
        public double NewValue
        {
            get => _newValue;
            set => SetProperty(ref _newValue, value);
        }
    }

    public class ServoRecipeModel : ObservableObjectVM
    {
        public int ProgramNo { get; set; }
        public int S1 { get; set; }
        public int S2 { get; set; }
        public int S3 { get; set; }
        public int S4 { get; set; }
        public int S5 { get; set; }
        public int S6 { get; set; }
        public int S7 { get; set; }
        public int S8 { get; set; }
        public int S9 { get; set; }
        public int S10 { get; set; }
        public int S11 { get; set; }
        public int S12 { get; set; }

        public double X0 { get; set; }
        
        public double X1 { get; set; }
        public double X2 { get; set; }
        public double X3 { get; set; }
        public double X4 { get; set; }
        public double X5 { get; set; }

        public double X6 { get; set; }
        public double X7 { get; set; }
        public double X8 { get; set; }
        public double X9 { get; set; }
        public double X10 { get; set; }
        public double X11 { get; set; }
        public double X12 { get; set; }
        public double Y0 { get; set; }
        public double Y1 { get; set; }
        public double Y2 { get; set; }
        public double Y3 { get; set; }
        public double Y4 { get; set; }
        public double Y5 { get; set; }
        public double Y6 { get; set; }
        public double Y7 { get; set; }
        public double Y8 { get; set; }
        public double Y9 { get; set; }
        public double Y10 { get; set; }
        public double Y11 { get; set; }
        public double Y12 { get; set; }

        public double Xmin { get; set; }
        public double Xmax { get; set; }
        public double Ymin { get; set; }
        public double Ymax { get; set; }

        public double AngleMin { get; set; }
        public double AngleMax { get; set; }




    }
}
