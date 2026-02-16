using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Moq;
using IPCSoftware.Services;
using IPCSoftware.Shared.Models;
using IPCSoftware.Shared.Models.ApiTest;
using IPCSoftware.Shared.Models.ConfigModels;
using IPCSoftware.Core.Interfaces.AppLoggerInterface;
using Xunit;
using LogTypeEnum = IPCSoftware.Shared.Models.ConfigModels.LogType;

namespace IPCSoftware_UnitTesting.Service_Tests;


    public class ApiTestSettingsServiceTests : IDisposable
    {
        private readonly string _tempDir;

        public ApiTestSettingsServiceTests()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), "ApiTestSettingsServiceTests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_tempDir);
        }

        public void Dispose()
        {
            try
            {
                if (Directory.Exists(_tempDir))
                {
                    Directory.Delete(_tempDir, true);
                }
            }
            catch
            {
                // best-effort cleanup
            }
        }

        private ApiTestSettingsService CreateService(Mock<IAppLogger> loggerMock)
        {
            var config = new ConfigSettings { DataFolder = _tempDir };
            var options = Options.Create(config);
            return new ApiTestSettingsService(options, loggerMock?.Object);
        }

        [Fact]
        public async Task LoadAsync_ReturnsDefault_WhenFileMissing()
        {
            // arrange
            var logger = new Mock<IAppLogger>(MockBehavior.Strict);
            var svc = CreateService(logger);

            // act
            var result = await svc.LoadAsync();

            // assert - default instance should be returned
            var expected = ApiTestSettings.CreateDefault();
            Assert.NotNull(result);
            Assert.Equal(expected.Protocol, result.Protocol);
            Assert.Equal(expected.Host, result.Host);
            Assert.Equal(expected.Endpoint, result.Endpoint);
            // other properties may be null/empty in default; compare a few keys above suffice
        }

        [Fact]
        public async Task SaveAsync_WritesFile_And_LoadAsync_ReadsItBack()
        {
            // arrange
            var logger = new Mock<IAppLogger>(MockBehavior.Strict);
            var svc = CreateService(logger);

            var settings = new ApiTestSettings
            {
                Protocol = "https",
                Host = "example.com",
                Endpoint = "/api/test",
                TwoDCodeData = "QR123",
                PreviousStationCode = "PS01",
                CurrentMachineCode = "MC01"
            };

            // act
            await svc.SaveAsync(settings);

            // assert file exists and content can be read back
            var settingsFile = Path.Combine(_tempDir, "ApiTestSettings.json");
            Assert.True(File.Exists(settingsFile));

            var loaded = await svc.LoadAsync();
            Assert.NotNull(loaded);
            Assert.Equal(settings.Protocol, loaded.Protocol);
            Assert.Equal(settings.Host, loaded.Host);
            Assert.Equal(settings.Endpoint, loaded.Endpoint);
            Assert.Equal(settings.TwoDCodeData, loaded.TwoDCodeData);
            Assert.Equal(settings.PreviousStationCode, loaded.PreviousStationCode);
            Assert.Equal(settings.CurrentMachineCode, loaded.CurrentMachineCode);
        }

        [Fact]
        public async Task SaveAsync_Throws_ArgumentNullException_WhenSettingsIsNull()
        {
            // arrange
            var logger = new Mock<IAppLogger>(MockBehavior.Strict);
            var svc = CreateService(logger);

            // act & assert
            await Assert.ThrowsAsync<ArgumentNullException>(() => svc.SaveAsync(null!));
        }

        [Fact]
        public async Task LoadAsync_LogsAndReturnsDefault_WhenReadFails()
        {
            // arrange
            var logger = new Mock<IAppLogger>();
            logger.Setup(l => l.LogError(It.Is<string>(s => s.Contains("Failed to load settings")), It.IsAny<LogTypeEnum>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>()));
            var svc = CreateService(logger);

            var settingsFile = Path.Combine(_tempDir, "ApiTestSettings.json");
            File.WriteAllText(settingsFile, JsonSerializer.Serialize(new ApiTestSettings { Protocol = "will-not-read" }));

            // lock the file to force an IOException on read
            using (var fs = new FileStream(settingsFile, FileMode.Open, FileAccess.Read, FileShare.None))
            {
                // act
                var result = await svc.LoadAsync();

                // assert: should return default because of caught exception
                var expected = ApiTestSettings.CreateDefault();
                Assert.NotNull(result);
                Assert.Equal(expected.Protocol, result.Protocol);
            }

            // verify logger called (call may have occurred while file was locked)
            logger.VerifyAll();
        }

        [Fact]
        public async Task SaveAsync_LogsAndRethrows_WhenWriteFails()
        {
            // arrange
            var logger = new Mock<IAppLogger>();
            logger.Setup(l => l.LogError(It.Is<string>(s => s.Contains("Failed to save settings")), It.IsAny<LogTypeEnum>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>()));
            var svc = CreateService(logger);

            var settings = new ApiTestSettings { Protocol = "proto" };
            var settingsFile = Path.Combine(_tempDir, "ApiTestSettings.json");

            // create and lock the file so write fails
            File.WriteAllText(settingsFile, "initial");
            using var fs = new FileStream(settingsFile, FileMode.Open, FileAccess.Read, FileShare.None);

            // act & assert - SaveAsync should log and rethrow (IOException)
            await Assert.ThrowsAnyAsync<Exception>(() => svc.SaveAsync(settings));

            // verify logger was called
            logger.VerifyAll();
        }
    }
