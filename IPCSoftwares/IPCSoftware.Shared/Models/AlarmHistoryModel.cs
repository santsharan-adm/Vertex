using System;

namespace IPCSoftware.Shared.Models
{
    public class AlarmHistoryModel
    {
        public int AlarmNo { get; set; }
        public string AlarmText { get; set; }
        public string Severity { get; set; }
        public DateTime RaisedTime { get; set; }
        public DateTime ResetTime { get; set; }
        public string ResetBy { get; set; } // The user who performed the reset
    }
}