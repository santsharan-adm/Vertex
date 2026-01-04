using System;
using System.IO;
using System.Threading;
using Moq;
using Xunit;
using IPCSoftware.Services.AppLoggerServices;
using IPCSoftware.Core.Interfaces.AppLoggerInterface;
using IPCSoftware.Shared.Models;
using IPCSoftware.Shared.Models.ConfigModels;

namespace IPCSoftware.Services.AppLoggerServices.Tests
{
    public class AppLoggerServiceTests : IDisposable
    {
        private readonly string _tempDir;

        public AppLoggerServiceTests()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), "AppLoggerServiceTests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_tempDir);
        }

        private static string TempFilePath(string name) =>
            Path.Combine(Path.GetTempPath(), "AppLoggerServiceTests", name + ".log");

        private static LogConfigurationModel EnabledConfig(string name) =>
            new LogConfigurationModel { Enabled = true, LogName = name };

        public void Dispose()
        {
            try
            {
                if (Directory.Exists(_tempDir))
                    Directory.Delete(_tempDir, true);
            }
            catch { /* best-effort cleanup */ }
        }

        [Fact]
        public void LogInfo_WritesLine_WhenConfigEnabled()
        {
            // Arrange
            var mockLogManager = new Mock<ILogManagerService>();
            var config = EnabledConfig("ProductionLog");
            var path = Path.Combine(_tempDir, "prod.log");

            mockLogManager.Setup(m => m.GetConfig(LogType.Production)).Returns(config);
            mockLogManager.Setup(m => m.ResolveLogFile(LogType.Production)).Returns(path);
            mockLogManager.Setup(m => m.ApplyMaintenance(config, path));

            using (var svc = new AppLoggerService(mockLogManager.Object))
            {
                // Act
                svc.LogInfo("Hello World", LogType.Production);

                // Ensure background processed: Dispose will wait for drain
            }

            // Assert
            Assert.True(File.Exists(path));
            var content = File.ReadAllText(path);
            Assert.Contains("INFO", content);
            Assert.Contains("Hello World", content);
            Assert.Contains("ProductionLog", content);

            mockLogManager.Verify(m => m.ApplyMaintenance(config, path), Times.AtLeastOnce);
        }

        [Fact]
        public void LogWarning_WritesLine_WhenConfigEnabled()
        {
            // Arrange
            var mockLogManager = new Mock<ILogManagerService>();
            var config = EnabledConfig("WarnLog");
            var path = Path.Combine(_tempDir, "warn.log");

            mockLogManager.Setup(m => m.GetConfig(LogType.Audit)).Returns(config);
            mockLogManager.Setup(m => m.ResolveLogFile(LogType.Audit)).Returns(path);
            mockLogManager.Setup(m => m.ApplyMaintenance(config, path));

            using (var svc = new AppLoggerService(mockLogManager.Object))
            {
                // Act
                svc.LogWarning("Be careful", LogType.Audit);
            }

            // Assert
            Assert.True(File.Exists(path));
            var content = File.ReadAllText(path);
            Assert.Contains("WARN", content);
            Assert.Contains("Be careful", content);
            Assert.Contains("WarnLog", content);
            mockLogManager.Verify(m => m.ApplyMaintenance(config, path), Times.AtLeastOnce);
        }

        [Fact]
        public void LogError_Diagnostics_IncludesCallerInfo()
        {
            // Arrange
            var mockLogManager = new Mock<ILogManagerService>();
            var config = EnabledConfig("DiagLog");
            var path = Path.Combine(_tempDir, "diag.log");

            mockLogManager.Setup(m => m.GetConfig(LogType.Diagnostics)).Returns(config);
            mockLogManager.Setup(m => m.ResolveLogFile(LogType.Diagnostics)).Returns(path);
            mockLogManager.Setup(m => m.ApplyMaintenance(config, path));

            // Act
            using (var svc = new AppLoggerService(mockLogManager.Object))
            {
                svc.LogError("Detailed failure", LogType.Diagnostics);
            }

            // Assert
            Assert.True(File.Exists(path));
            var content = File.ReadAllText(path);

            // CallerMemberName should be this test method name
            Assert.Contains(nameof(LogError_Diagnostics_IncludesCallerInfo), content);
            // The code appends "Line:" for diagnostics formatting
            Assert.Contains("Line:", content);
            Assert.Contains("Detailed failure", content);
            Assert.Contains("DiagLog", content);
        }

        [Fact]
        public void LogError_NonDiagnostics_DoesNotAddCallerInfo()
        {
            // Arrange
            var mockLogManager = new Mock<ILogManagerService>();
            var config = EnabledConfig("ErrorLog");
            var path = Path.Combine(_tempDir, "error.log");

            mockLogManager.Setup(m => m.GetConfig(LogType.Error)).Returns(config);
            mockLogManager.Setup(m => m.ResolveLogFile(LogType.Error)).Returns(path);
            mockLogManager.Setup(m => m.ApplyMaintenance(config, path));

            using (var svc = new AppLoggerService(mockLogManager.Object))
            {
                // Act
                svc.LogError("Simple error", LogType.Error);
            }

            // Assert
            Assert.True(File.Exists(path));
            var content = File.ReadAllText(path);

            // Should contain the message but NOT the caller formatting "[Class.Member() Line:...]"
            Assert.Contains("Simple error", content);
            Assert.DoesNotContain("Line:", content); // no caller info expected for non-Diagnostics
            Assert.Contains("ErrorLog", content);
        }

        [Fact]
        public void When_ConfigIsNull_NoWriteAnd_NoMaintenance()
        {
            // Arrange
            var mockLogManager = new Mock<ILogManagerService>();
            mockLogManager.Setup(m => m.GetConfig(LogType.Production)).Returns<LogConfigurationModel>(null);

            var path = Path.Combine(_tempDir, "shouldnotexist.log");
            mockLogManager.Setup(m => m.ResolveLogFile(It.IsAny<LogType>())).Returns(path);

            using (var svc = new AppLoggerService(mockLogManager.Object))
            {
                // Act
                svc.LogInfo("Won't be written", LogType.Production);
            }

            // Assert
            Assert.False(File.Exists(path));
            mockLogManager.Verify(m => m.ApplyMaintenance(It.IsAny<LogConfigurationModel>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public void When_ResolveReturnsEmpty_NoWriteAnd_NoMaintenance()
        {
            // Arrange
            var mockLogManager = new Mock<ILogManagerService>();
            var config = EnabledConfig("EmptyPathLog");
            mockLogManager.Setup(m => m.GetConfig(LogType.Production)).Returns(config);
            mockLogManager.Setup(m => m.ResolveLogFile(LogType.Production)).Returns(string.Empty);

            using (var svc = new AppLoggerService(mockLogManager.Object))
            {
                // Act
                svc.LogInfo("Won't be written", LogType.Production);
            }

            // Assert
            // No path => nothing written, ApplyMaintenance should not be called because ResolveLogFile returned empty
            mockLogManager.Verify(m => m.ApplyMaintenance(It.IsAny<LogConfigurationModel>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public void Dispose_PreventsFurtherLogging()
        {
            // Arrange
            var mockLogManager = new Mock<ILogManagerService>();
            var config = EnabledConfig("AfterDispose");
            var path = Path.Combine(_tempDir, "afterdispose.log");

            mockLogManager.Setup(m => m.GetConfig(LogType.Production)).Returns(config);
            mockLogManager.Setup(m => m.ResolveLogFile(LogType.Production)).Returns(path);
            mockLogManager.Setup(m => m.ApplyMaintenance(config, path));

            var svc = new AppLoggerService(mockLogManager.Object);

            // Log one entry before dispose
            svc.LogInfo("First", LogType.Production);

            // Dispose to flush and stop background
            svc.Dispose();

            // Try to log after dispose - should be ignored
            svc.LogInfo("Second", LogType.Production);

            // Ensure background had time to process (Dispose already waited); read file
            Assert.True(File.Exists(path));
            var content = File.ReadAllText(path);

            Assert.Contains("First", content);
            Assert.DoesNotContain("Second", content);
        }

        [Fact]
        public void RetryPolicy_AttemptsAndDoesNotThrow_WhenFileLocked()
        {
            // Arrange
            var mockLogManager = new Mock<ILogManagerService>();
            var config = EnabledConfig("LockedLog");
            var path = Path.Combine(_tempDir, "locked.log");

            mockLogManager.Setup(m => m.GetConfig(LogType.Production)).Returns(config);
            mockLogManager.Setup(m => m.ResolveLogFile(LogType.Production)).Returns(path);
            mockLogManager.Setup(m => m.ApplyMaintenance(config, path));

            // Pre-create the file and lock it for the duration of the log call
            using (var fs = new FileStream(path, FileMode.OpenOrCreate, FileAccess.Read, FileShare.None))
            {
                using (var svc = new AppLoggerService(mockLogManager.Object))
                {
                    // Act - while file locked, logging should attempt retries and eventually fail gracefully
                    svc.LogInfo("Will hit locked file", LogType.Production);

                    // Dispose to allow worker to finish attempts
                }
                // file still locked until end of using
            }

            // Assert: no exception thrown, ApplyMaintenance was invoked at least once
            mockLogManager.Verify(m => m.ApplyMaintenance(config, path), Times.AtLeastOnce);
            // The file may or may not contain the entry depending on timing; ensure no unhandled exceptions occurred and process completed.
        }
    }
}