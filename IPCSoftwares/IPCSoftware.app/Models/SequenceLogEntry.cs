using System;

namespace IPCSoftware.App.Models
{
    public class SequenceLogEntry
    {
        public int Step { get; set; }
        public DateTime Timestamp { get; set; }
        public string Description { get; set; } = string.Empty;
        public string TimeDisplay => Timestamp.ToString("HH:mm:ss.fff");
    }
}
