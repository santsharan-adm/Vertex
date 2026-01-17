using System;
using System.Collections.Generic;
using System.Globalization;

namespace IPCSoftware.Shared.Models.AeLimit
{
    public class AeLimitSettings
    {
        public string FilePrefix { get; set; } = "AE";
        public string OutputFolderName { get; set; } = "AeLimitLogs";
        public string SubmitId { get; set; } = "BZ_UAT-AOI_V53_338_SV2.1.0.4_V2.1.0.4";
        public string MachineId { get; set; } = "BZ-2006-010627";
        public string VendorCode { get; set; } = "0";
        public string TossingDefault { get; set; } = "0";
        public string OperatorIdDefault { get; set; } = "0";
        public string ModeDefault { get; set; } = "0";
        public string TestSeriesIdDefault { get; set; } = "0";
        public string PriorityDefault { get; set; } = "0";
        public string OnlineFlagDefault { get; set; } = "1";
        public string StartLabelDefault { get; set; } = "NA";
        public List<AeLimitStationConfig> Stations { get; set; } = new();

        public static AeLimitSettings CreateDefault()
        {
            var settings = new AeLimitSettings();
            for (int i = 0; i <= 12; i++)
            {
                settings.Stations.Add(AeLimitStationConfig.CreateDefault(i));
            }
            return settings;
        }

        public AeLimitSettings Clone()
        {
            var copy = (AeLimitSettings)MemberwiseClone();
            copy.Stations = new List<AeLimitStationConfig>();
            foreach (var station in Stations)
            {
                copy.Stations.Add(station.Clone());
            }
            return copy;
        }
    }

    public class AeLimitStationConfig
    {
        public int StationId { get; set; }
        public int SequenceIndex { get; set; }
        public int Cavity { get; set; } = 0;
        public string StartLabel { get; set; } = "NA";
        public string DutPositionLabel { get; set; } = "NA";
        public string MachineModeOverride { get; set; }

        public RangeSetting InspectionX { get; set; } = RangeSetting.Create(-0.130, 0.130, "mm");
        public RangeSetting InspectionY { get; set; } = RangeSetting.Create(-0.130, 0.130, "mm");
        public RangeSetting InspectionAngle { get; set; } = RangeSetting.Create(-0.800, 0.800, "degree");
        public RangeSetting CycleTime { get; set; } = RangeSetting.Create(0, 0, "s", allowLimits: false);

        public static AeLimitStationConfig CreateDefault(int stationId)
        {
            return new AeLimitStationConfig
            {
                StationId = stationId,
                SequenceIndex = stationId,
                Cavity = stationId,
                StartLabel = stationId == 0 ? "start" : "audit",
                DutPositionLabel = $"POS{stationId:00}"
            };
        }

        public AeLimitStationConfig Clone()
        {
            return new AeLimitStationConfig
            {
                StationId = StationId,
                SequenceIndex = SequenceIndex,
                Cavity = Cavity,
                StartLabel = StartLabel,
                DutPositionLabel = DutPositionLabel,
                MachineModeOverride = MachineModeOverride,
                InspectionX = InspectionX?.Clone(),
                InspectionY = InspectionY?.Clone(),
                InspectionAngle = InspectionAngle?.Clone(),
                CycleTime = CycleTime?.Clone()
            };
        }
    }

    public class RangeSetting
    {
        public double Lower { get; set; }
        public double Upper { get; set; }
        public string Unit { get; set; } = "mm";
        public bool HasLimits { get; set; } = true;

        public static RangeSetting Create(double lower, double upper, string unit, bool allowLimits = true)
        {
            return new RangeSetting
            {
                Lower = lower,
                Upper = upper,
                Unit = unit,
                HasLimits = allowLimits
            };
        }

        public RangeSetting Clone()
        {
            return new RangeSetting
            {
                Lower = Lower,
                Upper = Upper,
                Unit = Unit,
                HasLimits = HasLimits
            };
        }

        public string FormatLower()
        {
            return HasLimits ? Lower.ToString("0.000", CultureInfo.InvariantCulture) : "NA";
        }

        public string FormatUpper()
        {
            return HasLimits ? Upper.ToString("0.000", CultureInfo.InvariantCulture) : "NA";
        }
    }

    public class AeStationUpdate
    {
        public int StationId { get; set; }
        public double? ValueX { get; set; }
        public double? ValueY { get; set; }
        public double? Angle { get; set; }
        public double? CycleTime { get; set; }
        public string SerialNumber { get; set; }
        public string CarrierSerial { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.Now;
    }
}
