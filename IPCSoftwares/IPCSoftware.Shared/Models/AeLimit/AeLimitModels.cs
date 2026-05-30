using System;
using System.Collections.Generic;
using System.Globalization;

namespace IPCSoftware.Shared.Models.AeLimit
{
    /// <summary>
    /// Maps 1:1 to AELimit.json.
    /// Station-specific data (Cavity, StationId, X/Y/Angle ranges) is now sourced from Recipe.csv.
    /// </summary>
    public class AeLimitSettings
    {
        // --- File / Output ---
        public string FilePrefix { get; set; } = "AE";
        public string OutputFolderName { get; set; } = "AeLimitLogs";

        // --- Submit / Machine Identity ---
        public string SubmitId { get; set; } = "BZ_UAT-AOI_V53_338_SV2.1.0.4_V2.1.0.4";
        public string MachineId { get; set; } = "BZ-2006-010627";
        public string VendorCode { get; set; } = "0";

        // --- Payload Defaults ---
        public string TossingDefault { get; set; } = "0";
        public string OperatorIdDefault { get; set; } = "0";
        public string ModeDefault { get; set; } = "0";
        public string TestSeriesIdDefault { get; set; } = "0";
        public string PriorityDefault { get; set; } = "0";
        public string OnlineFlagDefault { get; set; } = "1";
        public string StartLabelDefault { get; set; } = "NA";

        // --- Common Inspection Units (previously per-station, now shared) ---
        public string InspectionXUnit { get; set; } = "mm";
        public string InspectionYUnit { get; set; } = "mm";
        public string InspectionAngleUnit { get; set; } = "degree";

        // --- Common Inspection Limit Flags ---
        public bool InspectionXHasLimits { get; set; } = true;
        public bool InspectionYHasLimits { get; set; } = true;
        public bool InspectionAngleHasLimits { get; set; } = true;

        // --- Common Station Defaults (previously per-station, now shared) ---
        public string StartLabel { get; set; } = "start";
        public string DutPositionLabel { get; set; } = "POS00";
        public string MachineModeOverride { get; set; } = null;

        // --- Common Cycle Time (shared across all stations) ---
        public RangeSetting CycleTime { get; set; } = RangeSetting.Create(5.0, 25.0, "s", allowLimits: false);

        public AeLimitSettings Clone()
        {
            var copy = (AeLimitSettings)MemberwiseClone();
            copy.CycleTime = CycleTime?.Clone();
            return copy;
        }

        public static AeLimitSettings CreateDefault() => new();
    }

    /// <summary>
    /// Runtime station configuration built from Recipe.csv.
    /// NOT serialized to AELimit.json.
    /// </summary>
    public class AeStationRecipeConfig
    {
        /// <summary>0-based index (column position in Recipe.csv: S0, S1, ...S12)</summary>
        public int StationIndex { get; set; }

        /// <summary>From PositionID_N column in Recipe.csv.</summary>
        public int StationId { get; set; }

        /// <summary>From S0..S12 columns in Recipe.csv.</summary>
        public int Cavity { get; set; }

        /// <summary>From Xmin / Xmax columns in Recipe.csv (shared per recipe row).</summary>
        public double InspectionXLower { get; set; }
        public double InspectionXUpper { get; set; }

        /// <summary>From Ymin / Ymax columns in Recipe.csv.</summary>
        public double InspectionYLower { get; set; }
        public double InspectionYUpper { get; set; }

        /// <summary>From AngleMin / AngleMax columns in Recipe.csv.</summary>
        public double InspectionAngleLower { get; set; }
        public double InspectionAngleUpper { get; set; }

        /// <summary>From Name_N column in Recipe.csv.</summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>From Description_N column in Recipe.csv.</summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>From IsEnabled_N column in Recipe.csv.</summary>
        public bool IsEnabled { get; set; } = true;

        // --- Resolved at runtime from AeLimitSettings (not from CSV) ---
        public string MachineModeOverride { get; set; }
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

        public string FormatLower() =>
            HasLimits ? Lower.ToString("0.000", CultureInfo.InvariantCulture) : "NA";

        public string FormatUpper() =>
            HasLimits ? Upper.ToString("0.000", CultureInfo.InvariantCulture) : "NA";
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