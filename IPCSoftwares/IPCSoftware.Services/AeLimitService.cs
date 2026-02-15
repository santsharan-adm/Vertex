using IPCSoftware.Core.Interfaces;
using IPCSoftware.Core.Interfaces.AppLoggerInterface;
using IPCSoftware.Shared.Models;
using IPCSoftware.Shared.Models.AeLimit;
using IPCSoftware.Shared.Models.ConfigModels;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace IPCSoftware.Services
{
    public class AeLimitService : BaseService, IAeLimitService
    {
        private readonly string _configFilePath;
        private readonly string _dataFolder;
        private readonly string _defaultOutputFolder;
        private readonly IOptions<ExternalSettings> _externalOptions;
        private readonly SemaphoreSlim _fileLock = new(1, 1);
        private readonly object _stateLock = new();

        private AeLimitSettings _settings;
        private AeCycleContext _currentCycle;

        public AeLimitService(IOptions<ConfigSettings> configOptions,
                              IOptions<ExternalSettings> externalOptions,
                              IAppLogger logger) : base(logger)
        {
            var config = configOptions.Value ?? new ConfigSettings();
            _externalOptions = externalOptions;

            _dataFolder = config.DataFolder ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data");
            Directory.CreateDirectory(_dataFolder);

            var fileName = string.IsNullOrWhiteSpace(config.AeLimitFileName) ? "AELimit.json" : config.AeLimitFileName;
            _configFilePath = Path.Combine(_dataFolder, fileName);

            var configuredOutput = string.IsNullOrWhiteSpace(config.AeLimitOutputFolderName) ? "AeLimitLogs" : config.AeLimitOutputFolderName;
            _defaultOutputFolder = Path.Combine(_dataFolder, configuredOutput);
            Directory.CreateDirectory(_defaultOutputFolder);

            _settings = LoadSettingsFromDisk();
        }

        public async Task InitializeAsync()
        {
            await Task.CompletedTask;
        }

        public async Task<AeLimitSettings> GetSettingsAsync()
        {
            await EnsureSettingsLoadedAsync();
            return _settings.Clone();
        }

        public async Task SaveSettingsAsync(AeLimitSettings settings)
        {
            if (settings == null) throw new ArgumentNullException(nameof(settings));
            await _fileLock.WaitAsync();
            try
            {
                var json = JsonConvert.SerializeObject(settings, Formatting.Indented);
                await File.WriteAllTextAsync(_configFilePath, json, Encoding.UTF8);
                _settings = settings.Clone();
            }
            finally
            {
                _fileLock.Release();
            }
        }

        public void BeginCycle(string serialNumber, string carrierSerial)
        {
            EnsureSettingsLoaded();
            lock (_stateLock)
            {
                _currentCycle = new AeCycleContext
                {
                    SerialNumber = serialNumber,
                    CarrierSerial = string.IsNullOrWhiteSpace(carrierSerial) ? serialNumber : carrierSerial,
                    StartedOn = DateTime.Now
                };
            }
        }

        public void UpdateStation(AeStationUpdate update)
        {
            if (update == null) return;
            EnsureSettingsLoaded();
            lock (_stateLock)
            {
                if (_currentCycle == null)
                {
                    _currentCycle = new AeCycleContext
                    {
                        SerialNumber = update.SerialNumber,
                        CarrierSerial = update.CarrierSerial,
                        StartedOn = DateTime.Now
                    };
                }
                _currentCycle.SetRecord(update.StationId, update);
            }
        }

        public async Task<(string FilePath, string TcpPayload)> CompleteCycleAsync(bool success = true)
        {
            AeCycleContext context;
            lock (_stateLock)
            {
                context = _currentCycle;
                _currentCycle = null;
            }

            if (context == null) return (null, null);

            try
            {
                // 1. Generate Content (Combines all processed items into one list)
                var contentLines = BuildCycleContent(context);

                // 2. Generate TCP Payload (Plain Text)
                // Format: _{ ...data... }
                var tcpSb = new StringBuilder();
                tcpSb.AppendLine("_{");
                foreach (var line in contentLines)
                {
                    tcpSb.AppendLine(line);
                }
                tcpSb.Append("}");
                string tcpPayload = tcpSb.ToString();

                // 3. Generate File Payload (With Headers)
                // Format: [Time] :_{ ...data... }
                var fileLines = new List<string>();
                var stamp = DateTime.Now.ToString("HH:mm:ss:fff");

                fileLines.Add(""); // Empty line separator
                //fileLines.Add($"[{stamp}] :ok@{(success ? "true" : "false")}@");
                fileLines.Add($"[{stamp}] :_{{");
                fileLines.AddRange(contentLines);
                fileLines.Add("}");

                // 4. Append to Daily File
                var filePath = await WritePayloadAsync(context, fileLines);

                return (filePath, tcpPayload);
            }
            catch (Exception ex)
            {
                _logger.LogError($"[AE] Failed to generate log: {ex.Message}", LogType.Diagnostics);
                return (null, null);
            }
        }

        public void AbortCycle()
        {
            lock (_stateLock) { _currentCycle = null; }
        }

        // --- Helper Methods ---

        private List<string> BuildCycleContent(AeCycleContext context)
        {
            var lines = new List<string>();
            var settings = _settings ?? AeLimitSettings.CreateDefault();
            var stations = settings.Stations?.OrderBy(s => s.SequenceIndex).ToList() ?? new List<AeLimitStationConfig>();
            var machineId = _externalOptions?.Value?.AOIMachineCode ?? settings.MachineId;

            foreach (var station in stations)
            {
                // FILTER: Only include stations that actually have data
                // This ensures if Product Setting is 6, we only log 6 items (+ QR station 0)
                var record = context.GetRecord(station.StationId);
                if (record == null) continue;

                var serial = record.SerialNumber ?? context.SerialNumber ?? "NA";
                var carrier = record.CarrierSerial ?? context.CarrierSerial ?? serial;

                var startLabel = string.IsNullOrWhiteSpace(station.StartLabel) ? settings.StartLabelDefault : station.StartLabel;
                lines.Add($"{serial}@start----> {startLabel}");

                lines.Add($"{serial}@dut_pos@{carrier}@{station.Cavity}");
                lines.Add($"{serial}@attr@MLB_AP_SN@{serial}");
                lines.Add($"{serial}@attr@Carrier_SN@{carrier}");
                lines.Add($"{serial}@pdata@Cavity@{station.Cavity}");
                lines.Add($"{serial}@pdata@ae_vendor@{settings.VendorCode}");
                lines.Add($"{serial}@pdata@Tossing@{settings.TossingDefault}");

                AddRangeLine(lines, serial, "Inspection_X", record.ValueX, station.InspectionX);
                AddRangeLine(lines, serial, "Inspection_Y", record.ValueY, station.InspectionY);
                AddRangeLine(lines, serial, "Inspection_A", record.Angle, station.InspectionAngle, "degree");
                AddRangeLine(lines, serial, "Cycle_Time", record.CycleTime, station.CycleTime ?? RangeSetting.Create(0, 0, "s", false), "s");

                lines.Add($"{serial}@attr@machine_ID@{machineId}");
                lines.Add($"{serial}@pdata@Operator_ID@{settings.OperatorIdDefault}");
                var modeValue = !string.IsNullOrWhiteSpace(station.MachineModeOverride) ? station.MachineModeOverride : settings.ModeDefault;
                lines.Add($"{serial}@pdata@Mode@{modeValue}");
                lines.Add($"{serial}@pdata@TestSeriesID@{settings.TestSeriesIdDefault}");
                lines.Add($"{serial}@pdata@Priority@{settings.PriorityDefault}");
                lines.Add($"{serial}@pdata@online@{settings.OnlineFlagDefault}");
                lines.Add($"{serial}@submit@{settings.SubmitId}");
            }
            return lines;
        }

        private static void AddRangeLine(List<string> lines, string serial, string label, double? value, RangeSetting range, string overrideUnit = null)
        {
            var unit = overrideUnit ?? range?.Unit ?? "mm";
            var formattedValue = value.HasValue ? value.Value.ToString("0.000", CultureInfo.InvariantCulture) : "0.000";
            var lower = range?.FormatLower() ?? "NA";
            var upper = range?.FormatUpper() ?? "NA";
            lines.Add($"{serial}@pdata@{label}@{formattedValue}@{lower}@{upper}@{unit}");
        }

        private async Task<string> WritePayloadAsync(AeCycleContext context, List<string> lines)
        {
            var settings = _settings ?? AeLimitSettings.CreateDefault();
            var folderName = settings.OutputFolderName;
            string outputFolder = string.IsNullOrWhiteSpace(folderName) ? _defaultOutputFolder :
                                  (Path.IsPathRooted(folderName) ? folderName : Path.Combine(_dataFolder, folderName));

            if (!Directory.Exists(outputFolder)) Directory.CreateDirectory(outputFolder);

            var prefix = string.IsNullOrWhiteSpace(settings.FilePrefix) ? "AE" : settings.FilePrefix;

            // LOGIC: One file per day (Append Mode)
            // Filename: AE_Log_20260215.txt
            var fileName = $"{prefix}_Log_{DateTime.Now:yyyyMMdd}.txt";
            var path = Path.Combine(outputFolder, fileName);

            // AppendAllLines creates the file if it doesn't exist, or appends if it does.
            await File.AppendAllLinesAsync(path, lines, Encoding.UTF8);

            return path;
        }

        private async Task EnsureSettingsLoadedAsync()
        {
            if (_settings != null) return;
            await _fileLock.WaitAsync();
            try { if (_settings == null) _settings = LoadSettingsFromDisk(); }
            finally { _fileLock.Release(); }
        }

        private void EnsureSettingsLoaded()
        {
            if (_settings != null) return;
            lock (_stateLock) { if (_settings == null) _settings = LoadSettingsFromDisk(); }
        }

        private AeLimitSettings LoadSettingsFromDisk()
        {
            try
            {
                if (File.Exists(_configFilePath))
                {
                    var json = File.ReadAllText(_configFilePath, Encoding.UTF8);
                    return JsonConvert.DeserializeObject<AeLimitSettings>(json) ?? AeLimitSettings.CreateDefault();
                }
            }
            catch (Exception ex) { _logger.LogError($"[AE] Config Load Error: {ex.Message}", LogType.Diagnostics); }

            var defaults = AeLimitSettings.CreateDefault();
            try { File.WriteAllText(_configFilePath, JsonConvert.SerializeObject(defaults, Formatting.Indented), Encoding.UTF8); } catch { }
            return defaults;
        }

        private class AeCycleContext
        {
            private readonly Dictionary<int, AeStationUpdate> _records = new();
            public string SerialNumber { get; set; }
            public string CarrierSerial { get; set; }
            public DateTime StartedOn { get; set; }

            public void SetRecord(int stationId, AeStationUpdate update) { if (update != null) _records[stationId] = update; }
            public AeStationUpdate GetRecord(int stationId) => _records.TryGetValue(stationId, out var record) ? record : null;
        }
    }
}