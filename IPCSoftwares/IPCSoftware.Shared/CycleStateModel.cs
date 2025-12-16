using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IPCSoftware.Shared
{
    public class CycleStateModel
    {
        public string BatchId { get; set; } = "Waiting for Scan...";
        public DateTime LastUpdated { get; set; }

        // Key = Station Number (e.g., 1, 2, ... 12)
        public Dictionary<int, StationResult> Stations { get; set; } = new Dictionary<int, StationResult>();
    }

    public class StationResult
    {
        public int StationNumber { get; set; }
        public string ImagePath { get; set; } // Path to the UI copy of the image
        public string Status { get; set; } // "OK" or "NG"
        public double X { get; set; }
        public double Y { get; set; }
        public double Z { get; set; }
        // Add other values as needed
        public DateTime Timestamp { get; set; }
    }
}
