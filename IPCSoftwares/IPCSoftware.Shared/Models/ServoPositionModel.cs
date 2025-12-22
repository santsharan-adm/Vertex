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

        private double _y;
        public double Y
        {
            get => _y;
            set => SetProperty(ref _y, value);
        }

        private double _x;
        public double X
        {
            get => _x;
            set => SetProperty(ref _x, value);
        }
        public string Description { get; set; }
    }
}
