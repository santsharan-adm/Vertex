using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using IPCSoftware.Services.ConfigServices;
using Xunit;

namespace IPCSoftware_UnitTesting.Service_Tests.ConfigServices
{
    // Minimal test implementation of IHostEnvironment so we don't need any mocking library.
    internal class TestHostEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Production;
        public string ApplicationName { get; set; } = "TestApp";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public IFileProvider ContentRootFileProvider { get; set; } = new PhysicalFileProvider(AppContext.BaseDirectory);
    }

    public class CcdConfigServiceTests : IDisposable
    {
        private readonly string _originalConfigDir;
        private readonly string _tempDir;

        public CcdConfigServiceTests()
        {
            // preserve original env var
            _originalConfigDir = Environment.GetEnvironmentVariable("CONFIG_DIR");
            _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_tempDir);
        }

        public void Dispose()
        {
            // restore env var
            Environment.SetEnvironmentVariable("CONFIG_DIR", _originalConfigDir);

            // cleanup temp dir
            try
            {
                if (Directory.Exists(_tempDir))
                    Directory.Delete(_tempDir, true);
            }
            catch { /* best-effort cleanup */ }
        }

        private static JsonNode ReadJsonFromFile(string path)
        {
            var s = File.ReadAllText(path);
            return JsonNode.Parse(s);
        }

        [Fact]
        public void LoadCcdPaths_ReturnsEmpty_When_TargetFileMissing()
        {
            // Arrange
            var env = new TestHostEnvironment { EnvironmentName = Environments.Production };
            // Ensure no appsettings.json exists in AppContext.BaseDirectory for this test name collision
            var baseAppSettings = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
            var backupExists = File.Exists(baseAppSettings);
            string backupContent = null;
            if (backupExists)
            {
                backupContent = File.ReadAllText(baseAppSettings);
                File.Delete(baseAppSettings);
            }

            try
            {
                var svc = new CcdConfigService(env);

                // Act
                var (image, backup) = svc.LoadCcdPaths();

                // Assert
                Assert.Equal(string.Empty, image);
                Assert.Equal(string.Empty, backup);
            }
            finally
            {
                if (backupExists)
                    File.WriteAllText(baseAppSettings, backupContent);
            }
        }

        [Fact]
        public void LoadCcdPaths_ReadsValues_FromDevelopmentFile_When_ConfigDirSet()
        {
            // Arrange
            Environment.SetEnvironmentVariable("CONFIG_DIR", _tempDir);
            var devFile = Path.Combine(_tempDir, "appsettings.Development.json");
            var doc = new JsonObject
            {
                ["CCD"] = new JsonObject
                {
                    ["BaseOutputDir"] = "C:\\Images",
                    ["BaseOutputDirBackup"] = "D:\\Backup"
                }
            };
            File.WriteAllText(devFile, doc.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));

            var env = new TestHostEnvironment { EnvironmentName = Environments.Development };

            var svc = new CcdConfigService(env);

            // Act
            var (image, backup) = svc.LoadCcdPaths();

            // Assert
            Assert.Equal("C:\\Images", image);
            Assert.Equal("D:\\Backup", backup);
        }

        [Fact]
        public void LoadCcdPaths_ReadsValues_From_AppSettings_In_BaseDir_When_NotDevelopment()
        {
            // Arrange
            var appSettings = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
            var backupExists = File.Exists(appSettings);
            string backupContent = null;
            if (backupExists)
            {
                backupContent = File.ReadAllText(appSettings);
                File.Delete(appSettings);
            }

            try
            {
                var doc = new JsonObject
                {
                    ["CCD"] = new JsonObject
                    {
                        ["BaseOutputDir"] = "/var/images",
                        ["BaseOutputDirBackup"] = "/var/backup"
                    }
                };
                File.WriteAllText(appSettings, doc.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));

                var env = new TestHostEnvironment { EnvironmentName = Environments.Production };

                var svc = new CcdConfigService(env);

                // Act
                var (image, backup) = svc.LoadCcdPaths();

                // Assert
                Assert.Equal("/var/images", image);
                Assert.Equal("/var/backup", backup);
            }
            finally
            {
                if (backupExists)
                    File.WriteAllText(appSettings, backupContent);
                else if (File.Exists(appSettings))
                    File.Delete(appSettings);
            }
        }

        [Fact]
        public void LoadCcdPaths_ReturnsEmpty_OnMalformedJson()
        {
            // Arrange
            Environment.SetEnvironmentVariable("CONFIG_DIR", _tempDir);
            var devFile = Path.Combine(_tempDir, "appsettings.Development.json");
            File.WriteAllText(devFile, "{ not a valid json ");

            var env = new TestHostEnvironment { EnvironmentName = Environments.Development };
            var svc = new CcdConfigService(env);

            // Act
            var (image, backup) = svc.LoadCcdPaths();

            // Assert
            Assert.Equal(string.Empty, image);
            Assert.Equal(string.Empty, backup);
        }

        [Fact]
        public void SaveCcdPaths_CreatesOrUpdates_CcdSection()
        {
            // Arrange
            var appSettings = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
            var backupExists = File.Exists(appSettings);
            string backupContent = null;
            if (backupExists)
            {
                backupContent = File.ReadAllText(appSettings);
                File.Delete(appSettings);
            }

            try
            {
                // start with empty object (no CCD section)
                File.WriteAllText(appSettings, "{}");

                var env = new TestHostEnvironment { EnvironmentName = Environments.Production };
                var svc = new CcdConfigService(env);

                // Act
                svc.SaveCcdPaths(@"C:\NewImages", @"E:\NewBackup");

                // Assert - file should exist and contain CCD with values
                Assert.True(File.Exists(appSettings));
                var root = ReadJsonFromFile(appSettings);
                Assert.Equal("C:\\NewImages", root["CCD"]["BaseOutputDir"]?.ToString());
                Assert.Equal("E:\\NewBackup", root["CCD"]["BaseOutputDirBackup"]?.ToString());
            }
            finally
            {
                if (backupExists)
                    File.WriteAllText(appSettings, backupContent);
                else if (File.Exists(appSettings))
                    File.Delete(appSettings);
            }
        }

        [Fact]
        public void SaveCcdPaths_DoesNothing_When_TargetFileMissing()
        {
            // Arrange: ensure CONFIG_DIR points to a dir with no files and development environment
            Environment.SetEnvironmentVariable("CONFIG_DIR", _tempDir);
            var env = new TestHostEnvironment { EnvironmentName = Environments.Development };

            var svc = new CcdConfigService(env);

            // Ensure the target file does not exist
            var devPath = Path.Combine(_tempDir, "appsettings.Development.json");
            if (File.Exists(devPath)) File.Delete(devPath);

            // Act - should not throw
            Exception ex = Record.Exception(() => svc.SaveCcdPaths("X", "Y"));

            // Assert
            Assert.Null(ex);
            Assert.False(File.Exists(devPath));
        }
    }
}