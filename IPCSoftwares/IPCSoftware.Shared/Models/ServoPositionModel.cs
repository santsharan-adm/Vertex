using System;
using System.Collections.Generic;
using System.Linq;
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
}
