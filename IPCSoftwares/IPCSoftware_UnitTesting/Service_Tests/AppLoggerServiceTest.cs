using IPCSoftware.Core.Interfaces.AppLoggerInterface;
//using IPCSoftware.Services.AppLoggerServices.Tests;
using IPCSoftware.Shared.Models.ConfigModels;
using IPCSoftware.Shared.Models;
using IPCSoftware.Shared;
using Moq;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using IPCSoftware.Services.AppLoggerServices;
using Xunit;

namespace IPCSoftware_UnitTesting.Service_Tests
{
    public class AppLoggerServiceTests : IDisposable
    {
        private readonly Mock<ILogManagerService> _logManagerMock;
        private readonly string _tempFile;
        private readonly LogConfigurationModel _enabledConfig;

        public AppLoggerServiceTests()
        {
            _logManagerMock = new Mock<ILogManagerService>(MockBehavior.Strict);

            _tempFile = Path.Combine(Path.GetTempPath(), $"applog_test_{Guid.NewGuid():N}.log");
            // Ensure file does not exist
            if (File.Exists(_tempFile)) File.Delete(_tempFile);

            _enabledConfig = new LogConfigurationModel
            {
                Enabled = true,
                LogName = "TestLog"
            };
        }

        public void Dispose()
        {
            if (File.Exists(_tempFile))
            {
                try { File.Delete(_tempFile); } catch { /* best-effort cleanup */ }
            }
        }

        private static bool WaitForCondition(Func<bool> predicate, int timeoutMs = 2000, int pollMs = 50)
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            while (sw.ElapsedMilliseconds < timeoutMs)
            {
                if (predicate()) return true;
                Thread.Sleep(pollMs);
            }
            return predicate();
        }

        [Fact]
        public void LogInfo_WritesInfoLine_ToResolvedFile_AndCallsMaintenance()
        {
            // Arrange
            _logManagerMock.Setup(m => m.GetConfig(It.IsAny<LogType>())).Returns(_enabledConfig);
            _logManagerMock.Setup(m => m.ResolveLogFile(It.IsAny<LogType>())).Returns(_tempFile);
            _logManagerMock.Setup(m => m.ApplyMaintenance(_enabledConfig, _tempFile));

            using var svc = new AppLoggerService(_logManagerMock.Object);

            // Act
            svc.LogInfo("info-message", LogType.Production);

            // Assert - wait for background thread to write
            bool written = WaitForCondition(() => File.Exists(_tempFile) && File.ReadAllText(_tempFile).Contains("info-message"));
            Assert.True(written, "Expected log file to contain the logged info message.");
            string content = File.ReadAllText(_tempFile);
            Assert.Contains(",INFO,", content);
            Assert.Contains(_enabledConfig.LogName, content);

            _logManagerMock.Verify(m => m.ApplyMaintenance(_enabledConfig, _tempFile), Times.Once);
            _logManagerMock.VerifyAll();
        }

        [Fact]
        public void LogWarning_WritesWarnLine_ToResolvedFile()
        {
            // Arrange
            _logManagerMock.Setup(m => m.GetConfig(It.IsAny<LogType>())).Returns(_enabledConfig);
            _logManagerMock.Setup(m => m.ResolveLogFile(It.IsAny<LogType>())).Returns(_tempFile);
            _logManagerMock.Setup(m => m.ApplyMaintenance(_enabledConfig, _tempFile));

            using var svc = new AppLoggerService(_logManagerMock.Object);

            // Act
            svc.LogWarning("warn-message", LogType.Audit);

            // Assert
            bool written = WaitForCondition(() => File.Exists(_tempFile) && File.ReadAllText(_tempFile).Contains("warn-message"));
            Assert.True(written, "Expected log file to contain the logged warn message.");
            string content = File.ReadAllText(_tempFile);
            Assert.Contains(",WARN,", content);

            _logManagerMock.Verify(m => m.ApplyMaintenance(_enabledConfig, _tempFile), Times.Once);
        }

        [Fact]
        public void LogError_NonDiagnostics_WritesErrorLine_UnmodifiedMessage()
        {
            // Arrange
            _logManagerMock.Setup(m => m.GetConfig(It.IsAny<LogType>())).Returns(_enabledConfig);
            _logManagerMock.Setup(m => m.ResolveLogFile(It.IsAny<LogType>())).Returns(_tempFile);
            _logManagerMock.Setup(m => m.ApplyMaintenance(_enabledConfig, _tempFile));

            using var svc = new AppLoggerService(_logManagerMock.Object);

            // Act
            svc.LogError("error-message", LogType.Error);

            // Assert
            bool written = WaitForCondition(() => File.Exists(_tempFile) && File.ReadAllText(_tempFile).Contains("error-message"));
            Assert.True(written, "Expected log file to contain the logged error message.");
            string content = File.ReadAllText(_tempFile);
            Assert.Contains(",ERROR,", content);
            Assert.DoesNotMatch(@"\[.*\..*\(\).*Line:\d+\]", content); // no caller enrichment for non-diagnostics

            _logManagerMock.Verify(m => m.ApplyMaintenance(_enabledConfig, _tempFile), Times.Once);
        }

        [Fact]
        public void LogError_Diagnostics_PrependsCallerInfo_ToMessage()
        {
            // Arrange
            _logManagerMock.Setup(m => m.GetConfig(LogType.Diagnostics)).Returns(_enabledConfig);
            _logManagerMock.Setup(m => m.ResolveLogFile(LogType.Diagnostics)).Returns(_tempFile);
            _logManagerMock.Setup(m => m.ApplyMaintenance(_enabledConfig, _tempFile));

            using var svc = new AppLoggerService(_logManagerMock.Object);

            // Act
            svc.LogError("diag-message", LogType.Diagnostics);

            // Assert
            bool written = WaitForCondition(() => File.Exists(_tempFile) && File.ReadAllText(_tempFile).Contains("diag-message"));
            Assert.True(written, "Expected log file to contain the diagnostics message.");
            string content = File.ReadAllText(_tempFile);

            // For Diagnostics, message is prefixed with [Class.Member() Line:NN] : message
            Assert.Contains(",ERROR,", content);
            Assert.Contains("diag-message", content);
            Assert.Matches(@"\[.*\..*\(\) Line:\d+\] : .*diag-message", content);

            _logManagerMock.Verify(m => m.ApplyMaintenance(_enabledConfig, _tempFile), Times.Once);
        }

        [Fact]
        public void When_ConfigIsNull_NoWriteOccurs()
        {
            // Arrange: return null config
            _logManagerMock.Setup(m => m.GetConfig(It.IsAny<LogType>())).Returns((LogConfigurationModel)null);
            // Resolve shouldn't be called, but if it is, return a path
            _logManagerMock.Setup(m => m.ResolveLogFile(It.IsAny<LogType>())).Returns(_tempFile);

            using var svc = new AppLoggerService(_logManagerMock.Object);

            // Act
            svc.LogInfo("should-not-write", LogType.Production);

            // Assert - wait briefly and assert file doesn't contain the message
            bool existsAndContains = WaitForCondition(() => File.Exists(_tempFile) && File.ReadAllText(_tempFile).Contains("should-not-write"), 800);
            Assert.False(existsAndContains, "Expected no log to be written when config is null.");

            _logManagerMock.Verify(m => m.ResolveLogFile(It.IsAny<LogType>()), Times.Never);
        }

        [Fact]
        public void When_ConfigDisabled_NoWriteOccurs()
        {
            // Arrange: disabled config
            var disabled = new LogConfigurationModel { Enabled = false, LogName = "X" };
            _logManagerMock.Setup(m => m.GetConfig(It.IsAny<LogType>())).Returns(disabled);

            using var svc = new AppLoggerService(_logManagerMock.Object);

            // Act
            svc.LogInfo("should-not-write", LogType.Production);

            // Assert
            bool existsAndContains = WaitForCondition(() => File.Exists(_tempFile) && File.ReadAllText(_tempFile).Contains("should-not-write"), 800);
            Assert.False(existsAndContains, "Expected no log to be written when config is disabled.");
            _logManagerMock.Verify(m => m.ResolveLogFile(It.IsAny<LogType>()), Times.Never);
        }
    }
}