using System;
using System.IO;
using System.Linq;
using System.Reflection;
using IPCSoftware.Services.AppLoggerServices;
using IPCSoftware.Shared.Models.ConfigModels;
using Xunit;

namespace IPCSoftware_UnitTesting.Service_Tests
{
    public class BackupServiceTests
    {
        private static BackupService CreateService() => new BackupService();

        private static LogConfigurationModel CreateDefaultConfig(string backupFolder)
            => new LogConfigurationModel
            {
                BackupFolder = backupFolder,
                FileName = "log_{yyyyMMdd}",
                BackupSchedule = BackupScheduleType.Daily,
                BackupTime = new TimeSpan(DateTime.Now.Hour, DateTime.Now.Minute, 0)
            };

        private static object InvokeIsBackupDue(BackupService svc, LogConfigurationModel config)
        {
            var method = typeof(BackupService).GetMethod("IsBackupDue", BindingFlags.Instance | BindingFlags.NonPublic)
                         ?? throw new InvalidOperationException("IsBackupDue method not found");
            return method.Invoke(svc, new object[] { config });
        }

        [Fact]
        public void IsBackupDue_Manual_ReturnsFalse()
        {
            var svc = CreateService();
            var config = CreateDefaultConfig(Path.GetTempPath());
            config.BackupSchedule = BackupScheduleType.Manual;
            config.BackupTime = new TimeSpan(0, 0, 0);

            var result = (bool)InvokeIsBackupDue(svc, config);
            Assert.False(result);
        }

        [Fact]
        public void IsBackupDue_Daily_TimeMatches_ReturnsTrue()
        {
            var svc = CreateService();
            var now = DateTime.Now;
            var config = CreateDefaultConfig(Path.GetTempPath());
            config.BackupSchedule = BackupScheduleType.Daily;
            config.BackupTime = new TimeSpan(now.Hour, now.Minute, 0);

            var result = (bool)InvokeIsBackupDue(svc, config);
            Assert.True(result);
        }

        [Fact]
        public void IsBackupDue_Schedules_ConsistentWithCurrentTime()
        {
            var svc = CreateService();
            var now = DateTime.Now;
            var config = CreateDefaultConfig(Path.GetTempPath());
            config.BackupTime = new TimeSpan(now.Hour, now.Minute, 0);

            // Weekly
            config.BackupSchedule = BackupScheduleType.Weekly;
            var expectedWeekly = now.Hour == config.BackupTime.Hours &&
                                 now.Minute == config.BackupTime.Minutes &&
                                 now.DayOfWeek == DayOfWeek.Monday;
            var resultWeekly = (bool)InvokeIsBackupDue(svc, config);
            Assert.Equal(expectedWeekly, resultWeekly);

            // Monthly
            config.BackupSchedule = BackupScheduleType.Monthly;
            var expectedMonthly = now.Hour == config.BackupTime.Hours &&
                                  now.Minute == config.BackupTime.Minutes &&
                                  now.Day == 1;
            var resultMonthly = (bool)InvokeIsBackupDue(svc, config);
            Assert.Equal(expectedMonthly, resultMonthly);
        }

        [Fact]
        public void PerformBackup_Manual_DoesNotCopy()
        {
            var svc = CreateService();
            var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);
            var source = Path.Combine(tempDir, "source.txt");
            File.WriteAllText(source, "data");

            var backupFolder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
            var config = CreateDefaultConfig(backupFolder);
            config.BackupSchedule = BackupScheduleType.Manual;

            svc.PerformBackup(config, source);

            // Backup folder should not be created (manual means no auto backup)
            Assert.False(Directory.Exists(backupFolder));

            // cleanup
            Directory.Delete(tempDir, true);
        }



        [Fact]
        public void PerformBackup_Daily_CreatesBackupFile()
        {
            var svc = CreateService();
            var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);
            var source = Path.Combine(tempDir, "source.log");
            var sourceContent = "line1\nline2";
            File.WriteAllText(source, sourceContent);

            var backupFolder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
            var now = DateTime.Now;
            var config = CreateDefaultConfig(backupFolder);
            config.BackupSchedule = BackupScheduleType.Daily;
            config.BackupTime = new TimeSpan(now.Hour, now.Minute, 0);
            config.FileName = "mylog_{yyyyMMdd}";

            // Ensure target folder does not exist so PerformBackup will create it
            if (Directory.Exists(backupFolder))
                Directory.Delete(backupFolder, true);

            svc.PerformBackup(config, source);

            try
            {
                Assert.True(Directory.Exists(backupFolder), "Backup folder was not created");

                var files = Directory.GetFiles(backupFolder);
                Assert.NotEmpty(files);

                // Expect one backup file with pattern containing "_backup_"
                var match = files.FirstOrDefault(f => Path.GetFileName(f).Contains("_backup_") && f.EndsWith(".csv", StringComparison.OrdinalIgnoreCase));
                Assert.NotNull(match);

                // The copied file should contain the same content
                var copiedContent = File.ReadAllText(match);
                Assert.Equal(sourceContent, copiedContent);
            }
            finally
            {
                // cleanup
                if (Directory.Exists(tempDir))
                    Directory.Delete(tempDir, true);
                if (Directory.Exists(backupFolder))
                    Directory.Delete(backupFolder, true);
            }
        }

        [Fact]
        public void PerformBackup_SourceMissing_ExceptionIsSwallowed()
        {
            var svc = CreateService();
            var backupFolder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
            var config = CreateDefaultConfig(backupFolder);
            config.BackupSchedule = BackupScheduleType.Daily;
            config.BackupTime = new TimeSpan(DateTime.Now.Hour, DateTime.Now.Minute, 0);

            // Source file does not exist
            var nonExistentSource = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"), "nope.txt");

            // Should not throw even though source is missing (method swallows exceptions)
            Exception ex = Record.Exception(() => svc.PerformBackup(config, nonExistentSource));
            Assert.Null(ex);

            // Backup folder should have been created before copy attempt
            Assert.True(Directory.Exists(backupFolder));

            // cleanup
            if (Directory.Exists(backupFolder))
                Directory.Delete(backupFolder, true);
        }
    }
}