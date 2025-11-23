using IPCSoftware.AppLogger.Models;
using System;
using System.IO;

namespace IPCSoftware.AppLogger.Services
{
    public class BackupService
    {
        public void PerformBackup(LogConfig config, string filePath)
        {
            if (config.BackupSchedule == BackupScheduleType.Never)
                return;

            // Backup folder validation
            if (!Directory.Exists(config.BackupFolder))
                Directory.CreateDirectory(config.BackupFolder);

            // Check if it's time to backup
            if (!IsBackupDue(config))
                return;

            // Backup filename
            string backupFileName = $"{config.FileNamePattern.Replace("{yyyyMMdd}",
                DateTime.Now.ToString("yyyyMMdd"))}_backup_{DateTime.Now:HHmmss}.csv";

            string backupFilePath = Path.Combine(config.BackupFolder, backupFileName);

            // Copy file
            File.Copy(filePath, backupFilePath, true);
        }

        private bool IsBackupDue(LogConfig config)
        {
            DateTime now = DateTime.Now;

            // Time check (hour/minute)
            if (now.Hour != config.BackupTime.Hours ||
                now.Minute != config.BackupTime.Minutes)
                return false;

            switch (config.BackupSchedule)
            {
                case BackupScheduleType.Manual:
                    return false;

                case BackupScheduleType.Daily:
                    return true;

                case BackupScheduleType.Weekly:
                    return now.DayOfWeek == DayOfWeek.Monday;

                case BackupScheduleType.Monthly:
                    return now.Day == 1;

                default:
                    return false;
            }
        }
    }
}
