using System.Collections.Generic;

namespace IPCSoftware.Shared.Models
{
    public class DashboardData
    {
        // SUMMARY / HEADER
        public string Availability { get; set; }
        public string Performance { get; set; }
        public string Quality { get; set; }
        public string OverallOEE { get; set; }

        public string OperatingTime { get; set; }
        public string Downtime { get; set; }

        public string GoodUnits { get; set; }
        public string TotalUnits { get; set; }

        public string Remarks { get; set; }

        // For trend graph on UI
        public List<double> CycleTrend { get; set; } = new List<double>();
        
    }
}
