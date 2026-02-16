using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using IPCSoftware.Core.Interfaces.AppLoggerInterface;
using IPCSoftware.Services;
using IPCSoftware.Shared.Models;
using IPCSoftware.Shared.Models.AeLimit;
using IPCSoftware.Shared.Models.ConfigModels;
using Xunit;

namespace IPCSoftware_UnitTesting.Service_Tests;

public class AeLimitServiceTests : IDisposable
{
    private readonly string _tempFolder;

    public AeLimitServiceTests()
    {
        _tempFolder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempFolder);
    }

    private AeLimitService CreateService(string aeFileName = "AELimit.json", string aeOutputFolderName = "AeLimitLogs", ExternalSettings external = null)
    {
        var config = new ConfigSettings
        {
            DataFolder = _tempFolder,
            AeLimitFileName = aeFileName,
            AeLimitOutputFolderName = aeOutputFolderName
        };
        var configOptions = Options.Create(config);
        var externalOptions = Options.Create(external ?? new ExternalSettings());
        var logger = new TestLogger();
        return new AeLimitService(configOptions, externalOptions, logger);
    }

    [Fact]
    public async Task InitializeAsync_DoesNotThrow()
    {
        var svc = CreateService();
        await svc.InitializeAsync();
    }

    [Fact]
    public async Task GetSettingsAsync_ReturnsClone_NotSameReference()
    {
        var svc = CreateService();
        var s1 = await svc.GetSettingsAsync();
        var s2 = await svc.GetSettingsAsync();

        Assert.NotNull(s1);
        Assert.NotNull(s2);
        Assert.False(ReferenceEquals(s1, s2));
    }

    [Fact]
    public async Task SaveSettingsAsync_Null_Throws()
    {
        var svc = CreateService();
        await Assert.ThrowsAsync<ArgumentNullException>(() => svc.SaveSettingsAsync(null));
    }

    [Fact]
    public async Task SaveSettingsAsync_PersistsFile_And_UpdatesSettings()
    {
        var fileName = "mysettings.json";
        var svc = CreateService(aeFileName: fileName);

        var settings = AeLimitSettings.CreateDefault();
        settings.FilePrefix = "MYPREFIX";
        // ensure at least one station exists for roundtrip persistence
        if (settings.Stations == null || settings.Stations.Count == 0)
        {
            settings.Stations = new System.Collections.Generic.List<AeLimitStationConfig>
            {
                new AeLimitStationConfig { StationId = 1, SequenceIndex = 1, Cavity = 1 }
            };
        }

        await svc.SaveSettingsAsync(settings);

        var path = Path.Combine(_tempFolder, fileName);
        Assert.True(File.Exists(path), "Settings file should be written to disk.");
        var json = File.ReadAllText(path);
        Assert.Contains("MYPREFIX", json);
    }

    [Fact]
    public async Task BeginCycle_And_CompleteCycle_WritesPayloadFile()
    {
        var svc = CreateService(aeOutputFolderName: "OutLogs");

        // prepare settings with a single station so BuildPayload produces lines
        var settings = AeLimitSettings.CreateDefault();
        settings.FilePrefix = "AEUNIT";
        settings.OutputFolderName = ""; // force default output folder to be used
        settings.Stations = new System.Collections.Generic.List<AeLimitStationConfig>
        {
            new AeLimitStationConfig
            {
                StationId = 10,
                SequenceIndex = 1,
                Cavity = 2,
                StartLabel = "StartLabelTest",
                DutPositionLabel = "DUT-1",
                InspectionX = RangeSetting.Create(-5,5,"mm"),
                InspectionY = RangeSetting.Create(-3,3,"mm"),
                InspectionAngle = RangeSetting.Create(-10,10,"degree"),
                CycleTime = RangeSetting.Create(0, 100, "s")
            }
        };

        await svc.SaveSettingsAsync(settings);

        svc.BeginCycle("SN001", null);

        var path = await svc.CompleteCycleAsync(success: true);

        Assert.False(string.IsNullOrWhiteSpace(path));
        Assert.True(File.Exists(path), "Payload file should be created.");

        var lines = File.ReadAllLines(path);
        Assert.True(lines.Any(l => l.Contains("SN001@start---->")), "Start label should be present for serial.");
        Assert.True(lines.Any(l => l.Contains("@pdata@ae_vendor@")), "Vendor line should be present.");
    }

    [Fact]
    public async Task CompleteCycleAsync_NoCurrentCycle_ReturnsNull()
    {
        var svc = CreateService();
        var result = await svc.CompleteCycleAsync();
        Assert.Null(result);
    }

    [Fact]
    public async Task AbortCycle_ClearsCycle_ResultingCompleteReturnsNull()
    {
        var svc = CreateService();
        svc.BeginCycle("SN-ABORT", null);
        svc.AbortCycle();
        var result = await svc.CompleteCycleAsync();
        Assert.Null(result);
    }

    [Fact]
    public async Task UpdateStation_Null_DoesNotThrow()
    {
        var svc = CreateService();
        svc.UpdateStation(null);
        await Task.CompletedTask; // just ensure no exceptions
    }

    [Fact]
    public async Task UpdateStation_CreatesCycleAndRecords_WrittenToPayload()
    {
        var svc = CreateService();

        var settings = AeLimitSettings.CreateDefault();
        settings.FilePrefix = "AEUPD";
        settings.OutputFolderName = ""; // use default
        settings.Stations = new System.Collections.Generic.List<AeLimitStationConfig>
        {
            new AeLimitStationConfig
            {
                StationId = 5,
                SequenceIndex = 1,
                Cavity = 1,
                InspectionX = RangeSetting.Create(-1,1,"mm"),
                InspectionY = RangeSetting.Create(-1,1,"mm"),
                InspectionAngle = RangeSetting.Create(-180,180,"degree"),
                CycleTime = RangeSetting.Create(0, 60, "s")
            }
        };

        await svc.SaveSettingsAsync(settings);

        var update = new AeStationUpdate
        {
            StationId = 5,
            SerialNumber = "UPDSN1",
            CarrierSerial = "UPDCR1",
            ValueX = 1.234,
            ValueY = 2.345,
            Angle = 3.456,
            CycleTime = 12.5,
            Timestamp = DateTime.UtcNow
        };

        svc.UpdateStation(update);

        var path = await svc.CompleteCycleAsync(true);
        Assert.False(string.IsNullOrWhiteSpace(path));
        Assert.True(File.Exists(path));

        var lines = File.ReadAllLines(path);
        // formatted value has 3 decimals per service implementation
        Assert.Contains(lines, l => l.Contains("1.234") && l.Contains("@pdata@Inspection_X@"));
        Assert.Contains(lines, l => l.Contains("3.456") && l.Contains("@pdata@Inspection_A@"));
        Assert.Contains(lines, l => l.Contains("12.500") && l.Contains("@pdata@Cycle_Time@"));
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
            // best effort cleanup for test temp directory
        }
    }

    private class TestLogger : IAppLogger
    {
        public void LogInfo(string message, LogType type) { /* no-op for tests */ }
        public void LogWarning(string message, LogType type) { /* no-op for tests */ }
        public void LogError(string message, LogType type, string memberName = "", string filePath = "", int lineNumber = 0) { /* no-op for tests */ }
    }
}