namespace IPCSoftware.Shared.Models.Logging
{
    /// <summary>
    /// Holds result and coordinates for one station.
    /// Station index (0..12) will be handled by the array position.
    /// </summary>
    public class StationMeasurement
    {
        public string Result { get; set; } = string.Empty;
        public double? X { get; set; }
        public double? Y { get; set; }
        public double? Z { get; set; }
    }

    /// <summary>
    /// One complete production cycle row for the ProductionData CSV.
    /// This will map 1:1 to one CSV line.
    /// </summary>
    public class ProductionDataRecord
    {
        public string TwoDCode { get; set; } = string.Empty;

        // We have stations 0..12 (13 stations total).
        // Index 0 = St0, index 1 = St1, ... index 12 = St12.
        public StationMeasurement[] Stations { get; } = new StationMeasurement[13]
        {
            new(), new(), new(), new(), new(), new(), new(),
            new(), new(), new(), new(), new(), new()
        };

        // OEE KPIs
        public double? OEE { get; set; }
        public double? Availability { get; set; }
        public double? Performance { get; set; }
        public double? Quality { get; set; }

        // Counters
        public int? Total_IN { get; set; }
        public int? OK { get; set; }
        public int? NG { get; set; }

        // Time metrics (use same unit everywhere – seconds or ms, your choice)
        public double? Uptime { get; set; }
        public double? Downtime { get; set; }
        public double? TotalTime { get; set; }
        public double? CT { get; set; } // Cycle Time

        public DateTime Timestamp { get; set; }
    }
}
