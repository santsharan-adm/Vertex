using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Xunit;
using IPCSoftware.Services.ConfigServices;
using IPCSoftware.Shared.Models;
using IPCSoftware.Shared.Models.ConfigModels;

namespace IPCSoftware_UnitTesting.Service_Tests.ConfigServices
{
    public class ServoCalibrationServiceTests : IDisposable
    {
        private readonly string _tempFolder;

        public ServoCalibrationServiceTests()
        {
            _tempFolder = Path.Combine(Path.GetTempPath(), "ServoCalTests", Guid.NewGuid().ToString("N"));
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
                // best-effort cleanup for test isolation
            }
        }

        private ServoCalibrationService CreateService(string fileName = "ServoCalibration.json")
        {
            var cfg = new ConfigSettings
            {
                DataFolder = _tempFolder,
                ServoCalibrationFileName = fileName
            };
            return new ServoCalibrationService(Options.Create(cfg));
        }

        [Fact]
        public async Task LoadPositionsAsync_NoFile_ReturnsDefaultPositions()
        {
            // Arrange
            var svc = CreateService();

            // Ensure file does not exist
            var path = Path.Combine(_tempFolder, "ServoCalibration.json");
            if (File.Exists(path)) File.Delete(path);

            // Act
            var positions = await svc.LoadPositionsAsync();

            // Assert basic expectations
            Assert.NotNull(positions);
            Assert.Equal(13, positions.Count); // PositionId 0..12

            // Position 0 is Home with SequenceIndex 0
            var pos0 = positions.Single(p => p.PositionId == 0);
            Assert.Equal(0, pos0.SequenceIndex);

            // Validate snake pattern mapping for positions 1..12
            var expectedSnake = new Dictionary<int, int>
            {
                { 1, 1 }, { 2, 2 }, { 3, 3 },
                { 4, 6 }, { 5, 5 }, { 6, 4 },
                { 7, 7 }, { 8, 8 }, { 9, 9 },
                { 10, 12 }, { 11, 11 }, { 12, 10 }
            };

            foreach (var id in Enumerable.Range(1, 12))
            {
                var p = positions.Single(x => x.PositionId == id);
                Assert.Equal(expectedSnake[id], p.SequenceIndex);
                Assert.Equal($"Position {id}", p.Name);
            }
        }

        [Fact]
        public async Task SavePositionsAsync_WritesFileAndCanBeReadBack()
        {
            // Arrange
            var svc = CreateService("save_test.json");
            var positions = new List<ServoPositionModel>
            {
                new ServoPositionModel { PositionId = 0, Name = "Home", SequenceIndex = 0, X = 1.1, Y = 2.2 },
                new ServoPositionModel { PositionId = 1, Name = "P1", SequenceIndex = 1, X = 3.3, Y = 4.4 }
            };

            var path = Path.Combine(_tempFolder, "save_test.json");
            if (File.Exists(path)) File.Delete(path);

            // Act
            await svc.SavePositionsAsync(positions);

            // Assert file exists and content matches
            Assert.True(File.Exists(path), "Expected file to be created by SavePositionsAsync");

            var text = await File.ReadAllTextAsync(path);
            var deserialized = JsonSerializer.Deserialize<List<ServoPositionModel>>(text);

            Assert.NotNull(deserialized);
            Assert.Equal(positions.Count, deserialized.Count);

            for (int i = 0; i < positions.Count; i++)
            {
                Assert.Equal(positions[i].PositionId, deserialized[i].PositionId);
                Assert.Equal(positions[i].Name, deserialized[i].Name);
                Assert.Equal(positions[i].SequenceIndex, deserialized[i].SequenceIndex);
                Assert.Equal(positions[i].X, deserialized[i].X);
                Assert.Equal(positions[i].Y, deserialized[i].Y);
            }
        }

        [Fact]
        public async Task LoadPositionsAsync_FileExistsWithValidJson_ReturnsFileData()
        {
            // Arrange
            var fileName = "valid.json";
            var svc = CreateService(fileName);

            var filePath = Path.Combine(_tempFolder, fileName);
            var toWrite = new List<ServoPositionModel>
            {
                new ServoPositionModel { PositionId = 0, Name = "Home", SequenceIndex = 0, X = 0, Y = 0 },
                new ServoPositionModel { PositionId = 1, Name = "Custom1", SequenceIndex = 5, X = 10, Y = 20 }
            };
            var json = JsonSerializer.Serialize(toWrite, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(filePath, json);

            // Act
            var loaded = await svc.LoadPositionsAsync();

            // Assert
            Assert.NotNull(loaded);
            Assert.Equal(2, loaded.Count);
            var p1 = loaded.Single(p => p.PositionId == 1);
            Assert.Equal("Custom1", p1.Name);
            Assert.Equal(5, p1.SequenceIndex);
            Assert.Equal(10, p1.X);
            Assert.Equal(20, p1.Y);
        }

        [Fact]
        public async Task LoadPositionsAsync_FileExistsButEmptyOrInvalidJson_ReturnsDefaultPositions()
        {
            // Arrange
            var fileName = "invalid.json";
            var svc = CreateService(fileName);

            var filePath = Path.Combine(_tempFolder, fileName);

            // Case A: empty file
            await File.WriteAllTextAsync(filePath, string.Empty);
            var loadedEmpty = await svc.LoadPositionsAsync();
            Assert.NotNull(loadedEmpty);
            Assert.Equal(13, loadedEmpty.Count);

            // Case B: corrupt JSON
            await File.WriteAllTextAsync(filePath, "{ not valid json }");
            var loadedCorrupt = await svc.LoadPositionsAsync();
            Assert.NotNull(loadedCorrupt);
            Assert.Equal(13, loadedCorrupt.Count);
        }

        [Fact]
        public async Task LoadPositionsAsync_FileExists_AllSequenceZero_ReturnsDefaultPositions()
        {
            // Arrange
            var fileName = "allzero.json";
            var svc = CreateService(fileName);

            var filePath = Path.Combine(_tempFolder, fileName);
            var allZero = new List<ServoPositionModel>();
            for (int i = 0; i <= 12; i++)
            {
                allZero.Add(new ServoPositionModel
                {
                    PositionId = i,
                    Name = $"P{i}",
                    SequenceIndex = 0,
                    X = 0,
                    Y = 0
                });
            }
            await File.WriteAllTextAsync(filePath, JsonSerializer.Serialize(allZero, new JsonSerializerOptions { WriteIndented = true }));

            // Act
            var loaded = await svc.LoadPositionsAsync();

            // Assert it returned defaults instead of the all-zero sequence data
            Assert.NotNull(loaded);
            Assert.Equal(13, loaded.Count);

            // The default map should contain non-zero sequence indices for 1..12
            Assert.Contains(loaded, p => p.PositionId == 1 && p.SequenceIndex == 1);
            Assert.Contains(loaded, p => p.PositionId == 12 && p.SequenceIndex == 10);
        }
    }
}