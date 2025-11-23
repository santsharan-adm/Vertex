

namespace IPCSoftware.AppLogger.Models
{
    public enum LogType
    {
        Production = 1,
        Audit = 2,
        Error = 3
    }

    public enum BackupScheduleType
    {
        Never,
        Manual,
        Daily,
        Weekly,
        Monthly
    }

    public class LogConfig
    {
        public string Name { get; set; }
        public LogType Type { get; set; }

        public string DataFolder { get; set; }
        public string BackupFolder { get; set; }

        public string FileNamePattern { get; set; }   // e.g. Audit_{yyyyMMdd}

        public int LogRetentionDays { get; set; }     // Production only
        public int LogRetentionFileSizeMB { get; set; } // Audit, Error only

        public bool AutoPurge { get; set; }

        public BackupScheduleType BackupSchedule { get; set; }
        public TimeSpan BackupTime { get; set; }

        public string Description { get; set; }
        public string Remark { get; set; }

        public bool Enabled { get; set; }
    }
}
