using System;

namespace IPCSoftware.Shared.Models.ConfigModels
{
    public class ShiftConfigurationModel
    {
        public int Id { get; set; }
        public string ShiftName { get; set; } // e.g. "Shift 1"
        public TimeSpan StartTime { get; set; } // e.g. 06:00:00
        public TimeSpan EndTime { get; set; }   // e.g. 14:00:00
        public bool IsActive { get; set; } = true;

        // Helper for UI display (e.g., "06:00 - 14:00")
        public string TimeRange => $"{StartTime:hh\\:mm} - {EndTime:hh\\:mm}";

        // Helper to check if "Now" is in this shift (handles overnight shifts like 22:00-06:00)
        public bool IsCurrent(TimeSpan now)
        {
            if (!IsActive) return false;
            if (StartTime <= EndTime)
                return now >= StartTime && now < EndTime;
            else // Overnight
                return now >= StartTime || now < EndTime;
        }

        public ShiftConfigurationModel Clone()
        {
            return (ShiftConfigurationModel)this.MemberwiseClone();
        }
    }
}