using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Xunit;
using IPCSoftware.Services.ConfigServices;
using IPCSoftware.Shared.Models.ConfigModels;
using IPCSoftware.Core.Interfaces.AppLoggerInterface;
using IPCSoftware.Shared.Models;

namespace IPCSoftware_UnitTesting.Service_Tests.ConfigServices
{
    public class ShiftManagementServiceTests : IDisposable
    {
        private readonly string _tempFolder;

        public ShiftManagementServiceTests()
        {
            _tempFolder = Path.Combine(Path.GetTempPath(), "ShiftMgmtTests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_tempFolder);
        }

        public void Dispose()
        {
            try
            {
                if (Directory.Exists(_tempFolder))
                    Directory.Delete(_tempFolder, true);
            }
            catch
            {
                // best-effort cleanup
            }
        }

        private ShiftManagementService CreateService(IAppLogger logger = null)
        {
            var cfg = new ConfigSettings { DataFolder = _tempFolder };
            return new ShiftManagementService(Options.Create(cfg), logger ?? new NoOpLogger());
        }

        private class NoOpLogger : IAppLogger
        {
            public void LogInfo(string message, LogType type) { }
            public void LogWarning(string message, LogType type) { }
            public void LogError(string message, LogType type, string memberName = "", string filePath = "", int lineNumber = 0) { }
        }

        [Fact]
        public async Task InitializeAsync_NoFile_LoadsEmptyList()
        {
            // Arrange
            var svc = CreateService();
            var csvPath = Path.Combine(_tempFolder, "Shifts.csv");
            if (File.Exists(csvPath)) File.Delete(csvPath);

            // Act
            await svc.InitializeAsync();
            var shifts = await svc.GetAllShiftsAsync();

            // Assert
            Assert.NotNull(shifts);
            Assert.Empty(shifts);
        }

        [Fact]
        public async Task AddShiftAsync_AddsShift_AssignsIdAndPersists()
        {
            // Arrange
            var svc = CreateService();
            var shift = new ShiftConfigurationModel
            {
                ShiftName = "Morning",
                StartTime = TimeSpan.FromHours(6),
                EndTime = TimeSpan.FromHours(14),
                IsActive = true
            };
            var csvPath = Path.Combine(_tempFolder, "Shifts.csv");
            if (File.Exists(csvPath)) File.Delete(csvPath);

            // Act
            var added = await svc.AddShiftAsync(shift);

            // Assert
            Assert.NotNull(added);
            Assert.Equal(1, added.Id);
            var all = await svc.GetAllShiftsAsync();
            Assert.Single(all);
            Assert.Equal("Morning", all[0].ShiftName);

            // File persisted
            Assert.True(File.Exists(csvPath));
            var content = await File.ReadAllTextAsync(csvPath);
            Assert.Contains("Morning", content);
            Assert.Contains("Id,ShiftName,StartTime,EndTime,IsActive", content);
        }

        [Fact]
        public async Task AddShiftAsync_DuplicateName_Throws()
        {
            // Arrange
            var svc = CreateService();
            var s1 = new ShiftConfigurationModel { ShiftName = "Shift A", StartTime = TimeSpan.FromHours(0), EndTime = TimeSpan.FromHours(8), IsActive = true };
            var s2 = new ShiftConfigurationModel { ShiftName = "shift a", StartTime = TimeSpan.FromHours(8), EndTime = TimeSpan.FromHours(16), IsActive = true };

            // Act
            await svc.AddShiftAsync(s1);

            // Assert
            await Assert.ThrowsAsync<InvalidOperationException>(async () => await svc.AddShiftAsync(s2));
        }

        [Fact]
        public async Task AddShiftAsync_SameStartAndEnd_Throws()
        {
            // Arrange
            var svc = CreateService();
            var s = new ShiftConfigurationModel { ShiftName = "Bad", StartTime = TimeSpan.FromHours(6), EndTime = TimeSpan.FromHours(6), IsActive = true };

            // Act / Assert
            await Assert.ThrowsAsync<InvalidOperationException>(async () => await svc.AddShiftAsync(s));
        }

        [Fact]
        public async Task UpdateShiftAsync_Success_UpdatesAndPersists()
        {
            // Arrange
            var svc = CreateService();
            var a = await svc.AddShiftAsync(new ShiftConfigurationModel { ShiftName = "A", StartTime = TimeSpan.FromHours(6), EndTime = TimeSpan.FromHours(14), IsActive = true });
            var b = await svc.AddShiftAsync(new ShiftConfigurationModel { ShiftName = "B", StartTime = TimeSpan.FromHours(14), EndTime = TimeSpan.FromHours(22), IsActive = true });

            // Act
            a.ShiftName = "A-Updated";
            a.StartTime = TimeSpan.FromHours(5);
            var result = await svc.UpdateShiftAsync(a);

            // Assert
            Assert.True(result);
            var all = await svc.GetAllShiftsAsync();
            Assert.Contains(all, s => s.Id == a.Id && s.ShiftName == "A-Updated" && s.StartTime == TimeSpan.FromHours(5));

            // Persisted in file
            var csvPath = Path.Combine(_tempFolder, "Shifts.csv");
            var text = await File.ReadAllTextAsync(csvPath);
            Assert.Contains("A-Updated", text);
        }

        [Fact]
        public async Task UpdateShiftAsync_DuplicateName_Throws()
        {
            // Arrange
            var svc = CreateService();
            var a = await svc.AddShiftAsync(new ShiftConfigurationModel { ShiftName = "Alpha", StartTime = TimeSpan.FromHours(6), EndTime = TimeSpan.FromHours(14), IsActive = true });
            var b = await svc.AddShiftAsync(new ShiftConfigurationModel { ShiftName = "Beta", StartTime = TimeSpan.FromHours(14), EndTime = TimeSpan.FromHours(22), IsActive = true });

            // Act: attempt to rename A to B (duplicate)
            a.ShiftName = "Beta";

            // Assert
            await Assert.ThrowsAsync<InvalidOperationException>(async () => await svc.UpdateShiftAsync(a));
        }

        [Fact]
        public async Task UpdateShiftAsync_NotFound_ReturnsFalse()
        {
            // Arrange
            var svc = CreateService();
            var fake = new ShiftConfigurationModel { Id = 999, ShiftName = "X", StartTime = TimeSpan.FromHours(1), EndTime = TimeSpan.FromHours(2), IsActive = true };

            // Act
            var result = await svc.UpdateShiftAsync(fake);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task DeleteShiftAsync_Success_RemovesAndPersists()
        {
            // Arrange
            var svc = CreateService();
            var added = await svc.AddShiftAsync(new ShiftConfigurationModel { ShiftName = "ToDelete", StartTime = TimeSpan.FromHours(6), EndTime = TimeSpan.FromHours(14), IsActive = true });

            // Act
            var deleted = await svc.DeleteShiftAsync(added.Id);

            // Assert
            Assert.True(deleted);
            var all = await svc.GetAllShiftsAsync();
            Assert.Empty(all);

            var csvPath = Path.Combine(_tempFolder, "Shifts.csv");
            var content = await File.ReadAllTextAsync(csvPath);
            Assert.DoesNotContain("ToDelete", content);
        }

        [Fact]
        public async Task DeleteShiftAsync_NotFound_ReturnsFalse()
        {
            // Arrange
            var svc = CreateService();

            // Act
            var result = await svc.DeleteShiftAsync(12345);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task InitializeAsync_WithExistingCsv_LoadsAndSetsNextId()
        {
            // Arrange
            var csvPath = Path.Combine(_tempFolder, "Shifts.csv");
            var lines = new List<string>
            {
                "Id,ShiftName,StartTime,EndTime,IsActive",
                "5,\"Night\",22:00:00,06:00:00,True",
                "7,\"Day\",06:00:00,14:00:00,True"
            };
            await File.WriteAllTextAsync(csvPath, string.Join(Environment.NewLine, lines), Encoding.UTF8);

            var svc = CreateService();

            // Act
            await svc.InitializeAsync();
            var loaded = await svc.GetAllShiftsAsync();

            // Assert load correctness
            Assert.Equal(2, loaded.Count);
            Assert.Contains(loaded, s => s.Id == 5 && s.ShiftName == "Night");
            Assert.Contains(loaded, s => s.Id == 7 && s.ShiftName == "Day");

            // Next add should get id = max(existing)+1 => 8
            var newShift = new ShiftConfigurationModel { ShiftName = "Evening", StartTime = TimeSpan.FromHours(16), EndTime = TimeSpan.FromHours(22), IsActive = true };
            var added = await svc.AddShiftAsync(newShift);
            Assert.Equal(8, added.Id);
        }
    }
}