using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IPCSoftware.Shared.Models
{
    public class ServoPositionModel
    {
        public int PositionId { get; set; } // 0 to 12
        public string Name { get; set; }    // e.g., "Home", "Station 1"
        public double X { get; set; }       // Saved X Coordinate
        public double Y { get; set; }       // Saved Y Coordinate
        public string Description { get; set; }
    }
}
