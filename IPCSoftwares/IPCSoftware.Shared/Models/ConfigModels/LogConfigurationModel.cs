using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IPCSoftware.Shared.Models.ConfigModels
{
    public class LogConfigurationModel
    {
        public int Id { get; set; }
        public string LogName { get; set; }
        public LogType LogType { get; set; }
        public string DataFolder { get; set; }
        public string BackupFolder { get; set; }
        public string FileName { get; set; }
        public int LogRetentionTime { get; set; }
        public int LogRetentionFileSize { get; set; }
        public bool AutoPurge { get; set; }
        public BackupScheduleType BackupSchedule { get; set; }
        public TimeSpan BackupTime { get; set; }
        public int BackupDay { get; set; }  // 1-28 for monthly backup
        public string BackupDayOfWeek { get; set; }  // Monday-Sunday for weekly backup
        public string Description { get; set; }
        public string Remark { get; set; }
        public bool Enabled { get; set; }

        public LogConfigurationModel()
        {
            // Default values
            LogRetentionTime = 30;
            LogRetentionFileSize = 4;
            BackupTime = TimeSpan.Zero;
            BackupDay = 0;
            BackupDayOfWeek = null;
            Enabled = false;
        }

        public LogConfigurationModel Clone()
        {
            return new LogConfigurationModel
            {
                Id = this.Id,
                LogName = this.LogName,
                LogType = this.LogType,
                DataFolder = this.DataFolder,
                BackupFolder = this.BackupFolder,
                FileName = this.FileName,
                LogRetentionTime = this.LogRetentionTime,
                LogRetentionFileSize = this.LogRetentionFileSize,
                AutoPurge = this.AutoPurge,
                BackupSchedule = this.BackupSchedule,
                BackupTime = this.BackupTime,
                BackupDay = this.BackupDay,
                BackupDayOfWeek = this.BackupDayOfWeek,
                Description = this.Description,
                Remark = this.Remark,
                Enabled = this.Enabled
            };
        }
    }

    public enum LogType
    {
        Production = 0,
        Audit = 1,
        Error = 2,
        Diagnostics = 3
    }

    public enum BackupScheduleType  
    {
        Manual,
        Daily,
        Weekly,
        Monthly
    }



}
