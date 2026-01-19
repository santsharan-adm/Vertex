using System;
using System.IO;
using System.Linq;
using System.Reflection;
using IPCSoftware.Services.AppLoggerServices;
using IPCSoftware.Shared.Models;
using IPCSoftware.Shared.Models.ConfigModels;
using Microsoft.Extensions.Options;
using Xunit;

namespace IPCSoftware_UnitTesting.Service_Tests
{
    public class BackupServiceTests
    {
        private static BackupService CreateService() => new BackupService(Options.Create(new CcdSettings()));

        private static LogConfigurationModel CreateDefaultConfig(string backupFolder)
            => new LogConfigurationModel
            {
                BackupFolder = backupFolder,
                FileName = "log_{yyyyMMdd}",
                BackupSchedule = BackupScheduleType.Daily,
                BackupTime = new TimeSpan(DateTime.Now.Hour, DateTime.Now.Minute, 0),
                Enabled = true
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
            var sourceFile = Path.Combine(tempDir, "source.txt");
            File.WriteAllText(sourceFile, "data");

            var backupFolder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
            var config = CreateDefaultConfig(backupFolder);
            config.BackupSchedule = BackupScheduleType.Manual;
            config.Enabled = false; // when manual and not enabled, PerformBackup returns immediately
            config.DataFolder = tempDir;

            svc.PerformBackup(config);

            // Backup folder should not be created (manual & disabled means no auto backup)
            Assert.False(Directory.Exists(backupFolder));

            // cleanup
            Directory.Delete(tempDir, true);
        }

        [Fact]
        public void PerformBackup_CopiesDirectoryContents()
        {
            var svc = CreateService();
            var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);
            var source = Path.Combine(tempDir, "source.log");
            var sourceContent = "line1\nline2";
            File.WriteAllText(source, sourceContent);

            var subDir = Path.Combine(tempDir, "sub");
            Directory.CreateDirectory(subDir);
            var subFile = Path.Combine(subDir, "inner.txt");
            File.WriteAllText(subFile, "inner");

            var backupFolder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
            var config = CreateDefaultConfig(backupFolder);
            config.BackupSchedule = BackupScheduleType.Daily;
            config.BackupTime = new TimeSpan(DateTime.Now.Hour, DateTime.Now.Minute, 0);
            config.FileName = "mylog_{yyyyMMdd}";
            config.DataFolder = tempDir;

            // Ensure target folder does not exist so PerformBackup will create it
            if (Directory.Exists(backupFolder))
                Directory.Delete(backupFolder, true);

            svc.PerformBackup(config);

            try
            {
                Assert.True(Directory.Exists(backupFolder), "Backup folder was not created");

                var files = Directory.GetFiles(backupFolder, "*", SearchOption.AllDirectories);
                Assert.NotEmpty(files);

                // Expect copied files to include source.log and sub/inner.txt
                var copiedSource = Path.Combine(backupFolder, Path.GetFileName(source));
                Assert.True(File.Exists(copiedSource));
                Assert.Equal(sourceContent, File.ReadAllText(copiedSource));

                var copiedInner = Path.Combine(backupFolder, "sub", Path.GetFileName(subFile));
                Assert.True(File.Exists(copiedInner));
                Assert.Equal("inner", File.ReadAllText(copiedInner));
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
        public void PerformBackup_SourceMissing_NoExceptionAndNoBackupCreated()
        {
            var svc = CreateService();
            var backupFolder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
            var config = CreateDefaultConfig(backupFolder);
            config.BackupSchedule = BackupScheduleType.Daily;
            config.BackupTime = new TimeSpan(DateTime.Now.Hour, DateTime.Now.Minute, 0);

            // DataFolder does not exist
            config.DataFolder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));

            // Should not throw even though source folder is missing (method swallows exceptions)
            Exception ex = Record.Exception(() => svc.PerformBackup(config));
            Assert.Null(ex);

            // Backup folder should NOT have been created because data folder missing
            Assert.False(Directory.Exists(backupFolder));
        }

        [Fact]
        public void PerformRestore_RestoresDirectoryContents()
        {
            var svc = CreateService();
            var backupFolder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(backupFolder);
            var backupFile = Path.Combine(backupFolder, "b.txt");
            File.WriteAllText(backupFile, "backupdata");

            var destFolder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
            var config = CreateDefaultConfig(backupFolder);
            config.BackupFolder = backupFolder;
            config.DataFolder = destFolder;

            svc.PerformRestore(config);

            try
            {
                Assert.True(Directory.Exists(destFolder));
                var restored = Path.Combine(destFolder, Path.GetFileName(backupFile));
                Assert.True(File.Exists(restored));
                Assert.Equal("backupdata", File.ReadAllText(restored));
            }
            finally
            {
                if (Directory.Exists(backupFolder))
                    Directory.Delete(backupFolder, true);
                if (Directory.Exists(destFolder))
                    Directory.Delete(destFolder, true);
            }
        }

        [Fact]
        public void PerformBackupAndRestore_ProductionImages_AreHandled()
        {
            var svc = CreateService();

            // Setup production images source and backup
            var prodSource = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
            var prodBackup = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(prodSource);
            var img = Path.Combine(prodSource, "img.png");
            File.WriteAllText(img, "imgdata");

            var config = CreateDefaultConfig(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N")));
            config.DataFolder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
            config.ProductionImagePath = prodSource;
            config.ProductionImageBackupPath = prodBackup;
            config.LogType = LogType.Production;

            // Perform backup should copy production images to backup path
            svc.PerformBackup(config);
            try
            {
                Assert.True(Directory.Exists(prodBackup));
                var backedImg = Path.Combine(prodBackup, Path.GetFileName(img));
                Assert.True(File.Exists(backedImg));
                Assert.Equal("imgdata", File.ReadAllText(backedImg));

                // Now clear original and restore from backup
                Directory.Delete(prodSource, true);
                svc.PerformRestore(config);

                Assert.True(Directory.Exists(prodSource));
                var restoredImg = Path.Combine(prodSource, Path.GetFileName(img));
                Assert.True(File.Exists(restoredImg));
                Assert.Equal("imgdata", File.ReadAllText(restoredImg));
            }
            finally
            {
                if (Directory.Exists(prodSource)) Directory.Delete(prodSource, true);
                if (Directory.Exists(prodBackup)) Directory.Delete(prodBackup, true);
                if (Directory.Exists(config.BackupFolder)) Directory.Delete(config.BackupFolder, true);
                if (Directory.Exists(config.DataFolder)) Directory.Delete(config.DataFolder, true);
            }
        }
    }
}