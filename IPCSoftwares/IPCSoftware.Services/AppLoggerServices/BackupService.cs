
using IPCSoftware.Shared.Models.ConfigModels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IPCSoftware.Services.AppLoggerServices
{
    public class BackupService
    {
        public void PerformBackup(LogConfigurationModel config, string filePath)
        {
            if (config.BackupSchedule == BackupScheduleType.Manual)
                return;

            // Backup folder validation
            if (!Directory.Exists(config.BackupFolder))
                Directory.CreateDirectory(config.BackupFolder);

            // Check if it's time to backup
            if (!IsBackupDue(config))
                return;

            // Backup filename
            string backupFileName = $"{config.FileName.Replace("{yyyyMMdd}",
                DateTime.Now.ToString("yyyyMMdd"))}_backup_{DateTime.Now:HHmmss}.csv";

            string backupFilePath = Path.Combine(config.BackupFolder, backupFileName);

            // Copy file
            File.Copy(filePath, backupFilePath, true);
        }

        private bool IsBackupDue(LogConfigurationModel config)
        {
            DateTime now = DateTime.Now;

            // Time check (hour/minute)
            if (now.Hour != config.BackupTime.Hours ||
                now.Minute != config.BackupTime.Minutes)
                return false;
            return config.BackupSchedule switch
            {
                BackupScheduleType.Manual => false,
                BackupScheduleType.Daily => true,
                BackupScheduleType.Weekly => now.DayOfWeek == DayOfWeek.Monday,
                BackupScheduleType.Monthly => now.Day == 1,
                _ => false
            };

        }
    }
}
