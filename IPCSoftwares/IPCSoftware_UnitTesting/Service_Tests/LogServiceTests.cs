using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Moq;
using IPCSoftware.Core.Interfaces;
using IPCSoftware.Core.Interfaces.AppLoggerInterface;
using IPCSoftware.Services.AppLoggerServices;
using IPCSoftware.Shared.Models;
using IPCSoftware.Shared.Models.ConfigModels;
using Xunit;

namespace IPCSoftware_UnitTesting.Service_Tests
{

    public class LogServiceTests : IDisposable
    {
        private readonly Mock<ILogConfigurationService> _mockConfigService;
        private readonly Mock<IAppLogger> _mockLogger;
        private readonly LogService _service;
        private readonly List<string> _tempPaths = new();

        public LogServiceTests()
        {
            _mockConfigService = new Mock<ILogConfigurationService>(MockBehavior.Strict);
            _mockLogger = new Mock<IAppLogger>(MockBehavior.Loose);
            _service = new LogService(_mockConfigService.Object, _mockLogger.Object);
        }

        public void Dispose()
        {
            // Clean up any temp files/directories created by tests
            foreach (var path in _tempPaths)
            {
                try
                {
                    if (Directory.Exists(path))
                        Directory.Delete(path, true);
                    else if (File.Exists(path))
                        File.Delete(path);
                }
                catch
                {
                    // ignore cleanup errors
                }
            }
        }

        private string CreateTempDirectory()
        {
            var dir = Path.Combine(Path.GetTempPath(), "LogServiceTests_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(dir);
            _tempPaths.Add(dir);
            return dir;
        }

        private string CreateTempFile(string directory, string fileName, string content, DateTime? lastWrite = null)
        {
            var filePath = Path.Combine(directory, fileName);
            File.WriteAllText(filePath, content, Encoding.UTF8);

            // optionally make file larger than 1KB for display size calculation
            if (content.Length < 2048)
            {
                var extra = new string('x', 2048 - content.Length);
                File.AppendAllText(filePath, extra);
            }

            if (lastWrite.HasValue)
                File.SetLastWriteTime(filePath, lastWrite.Value);

            _tempPaths.Add(filePath);
            return filePath;
        }

        [Fact]
        public async Task GetLogFilesAsync_ConfigNull_ReturnsEmpty_AndLogsError()
        {
            _mockConfigService
                .Setup(s => s.GetByLogTypeAsync(LogType.Production))
                .ReturnsAsync((LogConfigurationModel?)null);

            var result = await _service.GetLogFilesAsync(LogType.Production);

            Assert.Empty(result);
            _mockLogger.Verify(l => l.LogError(It.Is<string>(m => m.Contains("Log configuration")), LogType.Error, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>()), Times.Once);

            _mockConfigService.VerifyAll();
        }

        [Fact]
        public async Task GetLogFilesAsync_ConfigDisabled_ReturnsEmpty_AndLogsError()
        {
            _mockConfigService
                .Setup(s => s.GetByLogTypeAsync(LogType.Audit))
                .ReturnsAsync(new LogConfigurationModel { Enabled = false });

            var result = await _service.GetLogFilesAsync(LogType.Audit);

            Assert.Empty(result);
            _mockLogger.Verify(l => l.LogError(It.Is<string>(m => m.Contains("not found or disabled")), LogType.Error, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>()), Times.Once);

            _mockConfigService.VerifyAll();
        }

        [Fact]
        public async Task GetLogFilesAsync_FolderDoesNotExist_ReturnsEmpty_AndLogsError()
        {
            var fakeDir = Path.Combine(Path.GetTempPath(), "nonexistent_" + Guid.NewGuid().ToString("N"));

            _mockConfigService
                .Setup(s => s.GetByLogTypeAsync(LogType.Error))
                .ReturnsAsync(new LogConfigurationModel { Enabled = true, DataFolder = fakeDir });

            var result = await _service.GetLogFilesAsync(LogType.Error);

            Assert.Empty(result);
            _mockLogger.Verify(l => l.LogError(It.Is<string>(m => m.Contains("Log folder does not exist")), LogType.Error, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>()), Times.Once);

            _mockConfigService.VerifyAll();
        }

        [Fact]
        public async Task GetLogFilesAsync_ReturnsFiles_SortedAndDisplaySize()
        {
            var dir = CreateTempDirectory();

            // create 2 files with different last write times and sizes
            var older = CreateTempFile(dir, "older.csv", "header\nrow", DateTime.UtcNow.AddHours(-2));
            var newer = CreateTempFile(dir, "newer.csv", "header\nrow", DateTime.UtcNow.AddHours(-1));

            _mockConfigService
                .Setup(s => s.GetByLogTypeAsync(LogType.Diagnostics))
                .ReturnsAsync(new LogConfigurationModel { Enabled = true, DataFolder = dir });

            var result = await _service.GetLogFilesAsync(LogType.Diagnostics);

            Assert.Equal(2, result.Count);

            // newest first
            Assert.Equal("newer.csv", result[0].FileName);
            Assert.Equal("older.csv", result[1].FileName);

            // DisplaySize is computed as integer division length/1024 with " KB"
            foreach (var f in result)
            {
                Assert.EndsWith(" KB", f.DisplaySize);
                var numeric = int.Parse(f.DisplaySize.Replace(" KB", ""));
                Assert.True(numeric >= 1); // files were padded to >1KB
            }

            _mockConfigService.VerifyAll();
        }

        [Fact]
        public async Task ReadLogFileAsync_FileNotFound_ReturnsEmpty_AndLogsError()
        {
            var missing = Path.Combine(Path.GetTempPath(), "missing_" + Guid.NewGuid().ToString("N") + ".csv");

            var result = await _service.ReadLogFileAsync(missing);

            Assert.Empty(result);
            _mockLogger.Verify(l => l.LogError(It.Is<string>(m => m.Contains("Log file not found")), LogType.Error, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>()), Times.Once);
        }

        [Fact]
        public async Task ReadLogFileAsync_ParsesValidFile_ReturnsParsedEntries_SkipsInvalid()
        {
            var dir = CreateTempDirectory();
            var file = Path.Combine(dir, "testlog.csv");

            // Build CSV content
            // Header row
            // Two valid rows, one with a quoted comma in the message, one invalid timestamp row
            var validTimestamp1 = DateTime.UtcNow.AddMinutes(-1).ToString("yyyy-MM-dd HH:mm:ss:fff");
            var validTimestamp2 = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss:fff");

            var sb = new StringBuilder();
            sb.AppendLine("Timestamp,Level,Message,Source");
            sb.AppendLine($"{validTimestamp1},INFO,\"A simple message\",\"SourceA\"");
            sb.AppendLine($"{validTimestamp2},WARN,\"Message, with comma\",\"SourceB\""); // quoted comma
            sb.AppendLine($"InvalidDate,ERROR,NoParse,\"SourceC\"");
            sb.AppendLine(); // empty line should be skipped

            File.WriteAllText(file, sb.ToString());
            _tempPaths.Add(file);

            var result = await _service.ReadLogFileAsync(file);

            // Should parse the two valid entries only, and be ordered descending by timestamp
            Assert.Equal(2, result.Count);
            Assert.Equal(validTimestamp2, result[0].Timestamp.ToString("yyyy-MM-dd HH:mm:ss:fff"));
            Assert.Equal("WARN", result[0].Level);
            Assert.Equal("Message, with comma", result[0].Message);
            Assert.Equal("SourceB", result[0].Source);

            Assert.Equal(validTimestamp1, result[1].Timestamp.ToString("yyyy-MM-dd HH:mm:ss:fff"));
            Assert.Equal("INFO", result[1].Level);
            Assert.Equal("A simple message", result[1].Message);
            Assert.Equal("SourceA", result[1].Source);
        }

        [Fact]
        public async Task ReadLogFileAsync_InvalidDateIsSkipped_NoException()
        {
            var dir = CreateTempDirectory();
            var file = Path.Combine(dir, "bad.csv");

            var sb = new StringBuilder();
            sb.AppendLine("Timestamp,Level,Message,Source");
            sb.AppendLine($"2026-01-23 12:00:00:000,INFO,\"Good\",\"S\"");
            sb.AppendLine($"not-a-date,INFO,\"Bad\",\"S\"");

            File.WriteAllText(file, sb.ToString());
            _tempPaths.Add(file);

            var result = await _service.ReadLogFileAsync(file);

            Assert.Single(result);
            Assert.Equal("Good", result[0].Message);
        }
    }
}