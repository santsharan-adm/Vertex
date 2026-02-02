using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using IPCSoftware.Services.ConfigServices;
using IPCSoftware.Shared.Models;
using IPCSoftware.Shared.Models.ConfigModels;
using Xunit;

namespace IPCSoftware_UnitTesting.Service_Tests.ConfigServices
{
    public class LogConfigurationServiceTests : IDisposable
    {
        private readonly string _tempRoot;

        public LogConfigurationServiceTests()
        {
            _tempRoot = Path.Combine(Path.GetTempPath(), "LogConfigTests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_tempRoot);
        }

        public void Dispose()
        {
            try
            {
                if (Directory.Exists(_tempRoot))
                    Directory.Delete(_tempRoot, recursive: true);
            }
            catch
            {
                // best-effort cleanup
            }
        }

        private LogConfigurationService CreateService(string fileName = "LogConfigurations.csv")
        {
            var settings = new ConfigSettings
            {
                DataFolder = _tempRoot,
                LogConfigFileName = fileName
            };
            return new LogConfigurationService(Options.Create(settings));
        }

        private LogConfigurationModel BuildModel(LogType logType, BackupScheduleType schedule = BackupScheduleType.Daily)
        {
            var baseDir = Path.Combine(_tempRoot, "Logs", logType.ToString());
            return new LogConfigurationModel
            {
                // Id is assigned by the service on Add
                LogName = $"{logType}Test",
                LogType = logType,
                DataFolder = baseDir,
                BackupFolder = Path.Combine(_tempRoot, "Backup", logType.ToString()),
                FileName = $"{logType}_{{yyyyMMdd}}",
                LogRetentionTime = 30,
                LogRetentionFileSize = 5,
                AutoPurge = true,
                BackupSchedule = schedule,
                BackupTime = new TimeSpan(2, 0, 0),
                BackupDay = 15,
                BackupDayOfWeek = "Wednesday",
                Description = "desc",
                Remark = "remark",
                Enabled = true,
                ProductionImagePath = Path.Combine(_tempRoot, "ProdImages"),
                ProductionImageBackupPath = Path.Combine(_tempRoot, "ProdImagesBackup")
            };
        }

        [Fact]
        public async Task InitializeAsync_CreatesDefaults_WhenCsvMissing()
        {
            var svc = CreateService();
            // No CSV present initially
            Assert.False(File.Exists(Path.Combine(_tempRoot, "LogConfigurations.csv")));

            await svc.InitializeAsync();

            // After initialize, CSV should exist and defaults should be loaded
            Assert.True(File.Exists(Path.Combine(_tempRoot, "LogConfigurations.csv")));
            var all = await svc.GetAllAsync();
            Assert.NotNull(all);
            Assert.True(all.Count >= 4, "Default configurations should be created (4 entries expected).");
        }

        [Fact]
        public async Task AddAsync_AddsAndAssignsId()
        {
            var svc = CreateService("addtest.csv");
            // start with empty set
            await svc.SaveChangesAsync(new List<LogConfigurationModel>());

            var model = BuildModel(LogType.Production);
            var added = await svc.AddAsync(model);

            Assert.NotNull(added);
            Assert.True(added.Id > 0);
            var fetched = await svc.GetByIdAsync(added.Id);
            Assert.NotNull(fetched);
            Assert.Equal(added.LogType, fetched.LogType);
            Assert.Equal(added.Id, fetched.Id);
        }

        [Fact]
        public async Task AddAsync_Throws_OnDuplicateLogType()
        {
            var svc = CreateService("duplicate.csv");
            await svc.SaveChangesAsync(new List<LogConfigurationModel>());

            var m1 = BuildModel(LogType.Audit);
            var added1 = await svc.AddAsync(m1);
            Assert.NotNull(added1);

            var m2 = BuildModel(LogType.Audit);
            await Assert.ThrowsAsync<InvalidOperationException>(async () => await svc.AddAsync(m2));
        }

        [Fact]
        public async Task GetByLogTypeAsync_ReturnsExpected()
        {
            var svc = CreateService("bylog.csv");
            await svc.SaveChangesAsync(new List<LogConfigurationModel>());

            var model = BuildModel(LogType.Error);
            var added = await svc.AddAsync(model);

            var byType = await svc.GetByLogTypeAsync(LogType.Error);
            Assert.NotNull(byType);
            Assert.Equal(added.Id, byType.Id);
        }

        [Fact]
        public async Task UpdateAsync_UpdatesExisting_ReturnsTrue()
        {
            var svc = CreateService("update.csv");
            await svc.SaveChangesAsync(new List<LogConfigurationModel>());

            var model = BuildModel(LogType.Diagnostics);
            var added = await svc.AddAsync(model);

            added.Description = "UpdatedDesc";
            added.AutoPurge = false;

            var ok = await svc.UpdateAsync(added);
            Assert.True(ok);

            var fetched = await svc.GetByIdAsync(added.Id);
            Assert.Equal("UpdatedDesc", fetched.Description);
            Assert.False(fetched.AutoPurge);
        }

        [Fact]
        public async Task UpdateAsync_ReturnsFalse_WhenNotFound()
        {
            var svc = CreateService("update_notfound.csv");

            var fake = BuildModel(LogType.Production);
            fake.Id = 9999;

            var ok = await svc.UpdateAsync(fake);
            Assert.False(ok);
        }

        [Fact]
        public async Task DeleteAsync_RemovesEntry()
        {
            var svc = CreateService("delete.csv");
            await svc.SaveChangesAsync(new List<LogConfigurationModel>());

            var model = BuildModel(LogType.Audit);
            var added = await svc.AddAsync(model);

            var deleted = await svc.DeleteAsync(added.Id);
            Assert.True(deleted);

            var fetched = await svc.GetByIdAsync(added.Id);
            Assert.Null(fetched);

            // Deleting non-existent id returns false
            var deletedAgain = await svc.DeleteAsync(added.Id);
            Assert.False(deletedAgain);
        }

        [Fact]
        public async Task SaveChangesAsync_PersistsAndLoadFromCsv_RoundTrip()
        {
            var fileName = "roundtrip.csv";
            var svc1 = CreateService(fileName);
            await svc1.SaveChangesAsync(new List<LogConfigurationModel>()); // start empty

            var model = BuildModel(LogType.Production);
            var added = await svc1.AddAsync(model);

            // now create a new service pointing to the same folder/file and initialize (load)
            var svc2 = CreateService(fileName);
            await svc2.InitializeAsync(); // should load the single saved entry

            var all = await svc2.GetAllAsync();
            Assert.Single(all);
            var loaded = all.First();
            Assert.Equal(added.LogType, loaded.LogType);
            Assert.Equal(added.FileName, loaded.FileName);
        }

        [Fact]
        public async Task NormalizeBackupSettings_Behaviors_AreAppliedOnAddAndUpdate()
        {
            var svc = CreateService("normalize.csv");
            await svc.SaveChangesAsync(new List<LogConfigurationModel>());

            // Manual schedule: backup time/day/week should be reset
            var manual = BuildModel(LogType.Audit, BackupScheduleType.Manual);
            manual.BackupTime = new TimeSpan(5, 0, 0);
            manual.BackupDay = 10;
            manual.BackupDayOfWeek = "Friday";
            var addedManual = await svc.AddAsync(manual);
            var fetchedManual = await svc.GetByIdAsync(addedManual.Id);
            Assert.Equal(TimeSpan.Zero, fetchedManual.BackupTime);
            Assert.Equal(0, fetchedManual.BackupDay);
            Assert.Null(fetchedManual.BackupDayOfWeek);

            // Weekly schedule: ensure default day of week is set if empty
            var weekly = BuildModel(LogType.Error, BackupScheduleType.Weekly);
            weekly.BackupDayOfWeek = ""; // empty -> should become "Monday"
            var addedWeekly = await svc.AddAsync(weekly);
            var fetchedWeekly = await svc.GetByIdAsync(addedWeekly.Id);
            Assert.Equal("Monday", fetchedWeekly.BackupDayOfWeek);

            // Monthly schedule: day out of range should be reset to 1
            var monthly = BuildModel(LogType.Diagnostics, BackupScheduleType.Monthly);
            monthly.BackupDay = 31; // invalid (allowed 1-28)
            var addedMonthly = await svc.AddAsync(monthly);
            var fetchedMonthly = await svc.GetByIdAsync(addedMonthly.Id);
            Assert.InRange(fetchedMonthly.BackupDay, 1, 28);
            Assert.Equal(1, fetchedMonthly.BackupDay);
        }
    }
}