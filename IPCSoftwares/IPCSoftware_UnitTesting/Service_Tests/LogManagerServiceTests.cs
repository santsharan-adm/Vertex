using IPCSoftware.Services.AppLoggerServices;
using IPCSoftware.Shared.Models.ConfigModels;
using IPCSoftware.Shared.Models;
using IPCSoftware.Core.Interfaces.AppLoggerInterface;
using Moq;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using System.Collections.Generic;
using IPCSoftware.Core.Interfaces;
using Microsoft.Extensions.Options;

namespace IPCSoftware_UnitTesting.Service_Tests
{
 public class LogManagerServiceTests : IDisposable
 {
 private readonly Mock<ILogConfigurationService> _configServiceMock;
 private readonly BackupService _backupService;
 private readonly string _tempDir;

 public LogManagerServiceTests()
 {
 _configServiceMock = new Mock<ILogConfigurationService>(MockBehavior.Strict);
 _backupService = new BackupService(Options.Create(new CcdSettings()));
 _tempDir = Path.Combine(Path.GetTempPath(), "LogManagerTests", Guid.NewGuid().ToString("N"));
 Directory.CreateDirectory(_tempDir);
 }

 public void Dispose()
 {
 try
 {
 if (Directory.Exists(_tempDir))
 Directory.Delete(_tempDir, true);
 }
 catch { }
 }

 [Fact]
 public async Task InitializeAsync_LoadsConfigurations_And_GetConfigReturnsExpected()
 {
 // Arrange
 var prod = new LogConfigurationModel { Id =1, LogType = LogType.Production, Enabled = true, LogName = "P", DataFolder = _tempDir, FileName = "prod_{yyyyMMdd}" };
 var audit = new LogConfigurationModel { Id =2, LogType = LogType.Audit, Enabled = true, LogName = "A", DataFolder = _tempDir, FileName = "audit_{yyyyMMdd}" };

 _configServiceMock.Setup(s => s.GetAllAsync()).ReturnsAsync(new List<LogConfigurationModel> { prod, audit });

 var svc = new LogManagerService(_configServiceMock.Object, _backupService);

 // Act
 await svc.InitializeAsync();

 // Assert
 var cfg = svc.GetConfig(LogType.Audit);
 Assert.NotNull(cfg);
 Assert.Equal("A", cfg.LogName);

 _configServiceMock.Verify(s => s.GetAllAsync(), Times.Once);
 }

 [Fact]
 public async Task ResolveLogFile_CreatesDirectoryAndHeader_ReturnsPath()
 {
 // Arrange
 var cfg = new LogConfigurationModel
 {
 Enabled = true,
 LogType = LogType.Production,
 DataFolder = Path.Combine(_tempDir, "logs"),
 FileName = "mylog-yyyyMMdd"
 };

 _configServiceMock.Setup(s => s.GetAllAsync()).ReturnsAsync(new List<LogConfigurationModel> { cfg });
 var svc = new LogManagerService(_configServiceMock.Object, _backupService);
 await svc.InitializeAsync();

 // Act
 var path = svc.ResolveLogFile(LogType.Production);

 // Assert
 Assert.NotNull(path);
 Assert.True(Directory.Exists(cfg.DataFolder));
 Assert.True(File.Exists(path));
 var content = File.ReadAllText(path);
 Assert.Contains("Timestamp,Level,Message,Source", content);
 }

 [Fact]
 public async Task ResolveLogFile_ReturnsNull_WhenNoEnabledConfig()
 {
 // Arrange: no enabled configs
 var cfg = new LogConfigurationModel { Enabled = false, LogType = LogType.Production, DataFolder = _tempDir, FileName = "x" };
 _configServiceMock.Setup(s => s.GetAllAsync()).ReturnsAsync(new List<LogConfigurationModel> { cfg });

 var svc = new LogManagerService(_configServiceMock.Object, _backupService);
 await svc.InitializeAsync();

 // Act
 var path = svc.ResolveLogFile(LogType.Production);

 // Assert
 Assert.Null(path);
 }

 [Fact]
 public async Task ApplyMaintenance_FileSizeRotation_MovesFile_AndCreatesNew()
 {
 // Arrange
 var cfg = new LogConfigurationModel
 {
 Enabled = true,
 LogType = LogType.Audit,
 DataFolder = Path.Combine(_tempDir, "size"),
 FileName = "aud_{yyyyMMdd}",
 LogRetentionFileSize =1 // MB
 };
 Directory.CreateDirectory(cfg.DataFolder);

 string filePath = Path.Combine(cfg.DataFolder, cfg.FileName.Replace("{yyyyMMdd}", DateTime.Now.ToString("yyyyMMdd")) + ".csv");

 // create a file >1MB
 using (var fs = new FileStream(filePath, FileMode.Create, FileAccess.Write))
 {
 fs.SetLength(2 *1024 *1024); //2 MB
 }

 var svc = new LogManagerService(_configServiceMock.Object, _backupService);

 // Act
 svc.ApplyMaintenance(cfg, filePath);

 // Assert - original file should be moved (so original path now contains new header)
 Assert.True(File.Exists(filePath), "A fresh file should exist at the original path after rotation.");
 var content = File.ReadAllText(filePath);
 Assert.Contains("Timestamp,Level,Message,Source", content);

 // There should be another file in folder (the moved one)
 var files = Directory.GetFiles(cfg.DataFolder);
 Assert.True(files.Length >=1);
 Assert.Contains(files, f => Path.GetFileName(f) != Path.GetFileName(filePath));
 }

 [Fact]
 public void ApplyMaintenance_ProductionTimeRetention_DeletesOldFiles()
 {
 // Arrange
 var cfg = new LogConfigurationModel
 {
 Enabled = true,
 LogType = LogType.Production,
 DataFolder = Path.Combine(_tempDir, "time"),
 FileName = "prod_{yyyyMMdd}",
 LogRetentionTime =1 // days
 };
 Directory.CreateDirectory(cfg.DataFolder);

 var old = Path.Combine(cfg.DataFolder, "old.csv");
 var recent = Path.Combine(cfg.DataFolder, "recent.csv");

 File.WriteAllText(old, "old");
 File.WriteAllText(recent, "recent");

 File.SetLastWriteTime(old, DateTime.Now.AddDays(-2));
 File.SetLastWriteTime(recent, DateTime.Now);

 var svc = new LogManagerService(_configServiceMock.Object, _backupService);

 // Act
 svc.ApplyMaintenance(cfg, recent);

 // Assert
 Assert.False(File.Exists(old), "Old file should be deleted by time-based retention.");
 Assert.True(File.Exists(recent), "Recent file should remain.");
 }

 [Fact]
 public async Task ReadLogs_ParsesCsv_ReturnsEntries()
 {
 // Arrange
 var date = new DateTime(2026,01,01);
 string dateToken = date.ToString("yyyyMMdd");

 var cfg = new LogConfigurationModel
 {
 Enabled = true,
 LogType = LogType.Production,
 DataFolder = Path.Combine(_tempDir, "read"),
 FileName = "r_{yyyyMMdd}"
 };
 Directory.CreateDirectory(cfg.DataFolder);

 string fileName = cfg.FileName.Replace("{yyyyMMdd}", dateToken) + ".csv";
 string fullPath = Path.Combine(cfg.DataFolder, fileName);

 // write header + two lines (one with comma inside quotes)
 File.WriteAllLines(fullPath, new[] { "Timestamp,Level,Message,Source", "2026-01-01 12:00:00,INFO,\"UI Client Connected\",AOIApp","2026-01-01 13:00:00,WARN,Warining Message,AOIApp"});

 _configServiceMock.Setup(s => s.GetAllAsync()).ReturnsAsync(new List<LogConfigurationModel> { cfg });
 var svc = new LogManagerService(_configServiceMock.Object, _backupService);
 await svc.InitializeAsync();

 // Act
 var entries = svc.ReadLogs(LogType.Production, date);

 // Assert
 Assert.Equal(2, entries.Count);
 Assert.Equal("INFO", entries[0].Level);
 Assert.Equal("UI Client Connected", entries[0].Message);
 Assert.Equal("AOIApp", entries[0].Source);

 Assert.Equal("WARN", entries[1].Level);
 Assert.Equal("Warining Message", entries[1].Message);
 Assert.Equal("AOIApp" , entries[1].Source);
        }

 [Fact]
 public async Task ReadLogs_FileMissing_ReturnsEmpty()
 {
 // Arrange
 var cfg = new LogConfigurationModel
 {
 Enabled = true,
 LogType = LogType.Production,
 DataFolder = Path.Combine(_tempDir, "missing"),
 FileName = "doesnotexist_{yyyyMMdd}"
 };
 Directory.CreateDirectory(cfg.DataFolder);

 _configServiceMock.Setup(s => s.GetAllAsync()).ReturnsAsync(new List<LogConfigurationModel> { cfg });
 var svc = new LogManagerService(_configServiceMock.Object, _backupService);
 await svc.InitializeAsync();

 // Act
 var entries = svc.ReadLogs(LogType.Production, DateTime.Now);

 // Assert
 Assert.Empty(entries);
 }
 }
}
