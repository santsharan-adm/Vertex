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

            var fileName = string.IsNullOrWhiteSpace(config.AeLimitFileName)
                ? "AELimit.json"
                : config.AeLimitFileName;

            _configFilePath = Path.Combine(_dataFolder, fileName);

            var configuredOutput = string.IsNullOrWhiteSpace(config.AeLimitOutputFolderName)
                ? "AeLimitLogs"
                : config.AeLimitOutputFolderName;

            _defaultOutputFolder = Path.Combine(_dataFolder, configuredOutput);
            Directory.CreateDirectory(_defaultOutputFolder);

            // Synchronously load default settings so service is ready for immediate use.
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
            if (update == null)
            {
                return;
            }

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

        public async Task<string> CompleteCycleAsync(bool success = true)
        {
            AeCycleContext context;
            lock (_stateLock)
            {
                context = _currentCycle;
                _currentCycle = null;
            }

            if (context == null)
            {
                return null;
            }

            try
            {
                var payload = BuildPayload(context, success);
                var filePath = await WritePayloadAsync(context, payload);
                return filePath;
            }
            catch (Exception ex)
            {
                _logger.LogError($"[AE] Failed to write AE Limit file: {ex.Message}", LogType.Diagnostics);
                return null;
            }
        }

        public void AbortCycle()
        {
            lock (_stateLock)
            {
                _currentCycle = null;
            }
        }

        private async Task EnsureSettingsLoadedAsync()
        {
            if (_settings != null) return;

            await _fileLock.WaitAsync();
            try
            {
                if (_settings == null)
                {
                    _settings = LoadSettingsFromDisk();
                }
            }
            finally
            {
                _fileLock.Release();
            }
        }

        private void EnsureSettingsLoaded()
        {
            if (_settings != null) return;
            lock (_stateLock)
            {
                if (_settings == null)
                {
                    _settings = LoadSettingsFromDisk();
                }
            }
        }

        private AeLimitSettings LoadSettingsFromDisk()
        {
            try
            {
                if (File.Exists(_configFilePath))
                {
                    var json = File.ReadAllText(_configFilePath, Encoding.UTF8);
                    var loaded = JsonConvert.DeserializeObject<AeLimitSettings>(json);
                    if (loaded != null && loaded.Stations?.Any() == true)
                    {
                        return loaded;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"[AE] Failed to load settings: {ex.Message}", LogType.Diagnostics);
            }

            var defaults = AeLimitSettings.CreateDefault();
            try
            {
                var json = JsonConvert.SerializeObject(defaults, Formatting.Indented);
                File.WriteAllText(_configFilePath, json, Encoding.UTF8);
            }
            catch (Exception ex)
            {
                _logger.LogError($"[AE] Failed to persist default settings: {ex.Message}", LogType.Diagnostics);
            }

            return defaults;
        }

        private List<string> BuildPayload(AeCycleContext context, bool success)
        {
            var lines = new List<string>();
            var settings = _settings ?? AeLimitSettings.CreateDefault();
            var stations = settings.Stations?.OrderBy(s => s.SequenceIndex).ToList() ?? new List<AeLimitStationConfig>();
            var machineId = _externalOptions?.Value?.AOIMachineCode ?? settings.MachineId;

            foreach (var station in stations)
            {
                var stampStart = DateTime.Now;
                lines.Add($"[{stampStart:HH:mm:ss:fff}] :ok@{{success}}@");
                lines.Add($"[{stampStart:HH:mm:ss:fff}] :_{{");

                var record = context.GetRecord(station.StationId);
                var serial = record?.SerialNumber ?? context.SerialNumber ?? "NA";
                var carrier = record?.CarrierSerial ?? context.CarrierSerial ?? serial;

                var startLabel = string.IsNullOrWhiteSpace(station.StartLabel) ? settings.StartLabelDefault : station.StartLabel;
                lines.Add($"{serial}@start----> {startLabel}");

                var dutLabel = string.IsNullOrWhiteSpace(station.DutPositionLabel) ? station.Cavity.ToString(CultureInfo.InvariantCulture) : station.DutPositionLabel;
                lines.Add($"{serial}@dut_pos@{carrier}@{station.Cavity}");

                lines.Add($"{serial}@attr@MLB_AP_SN@{serial}");
                lines.Add($"{serial}@attr@Carrier_SN@{carrier}");
                lines.Add($"{serial}@pdata@Cavity@{station.Cavity}");
                lines.Add($"{serial}@pdata@ae_vendor@{settings.VendorCode}");
                lines.Add($"{serial}@pdata@Tossing@{settings.TossingDefault}");

                AddRangeLine(lines, serial, "Inspection_X", record?.ValueX, station.InspectionX);
                AddRangeLine(lines, serial, "Inspection_Y", record?.ValueY, station.InspectionY);
                AddRangeLine(lines, serial, "Inspection_A", record?.Angle, station.InspectionAngle, "degree");
                AddRangeLine(lines, serial, "Cycle_Time", record?.CycleTime, station.CycleTime ?? RangeSetting.Create(0, 0, "s", false), "s");

                lines.Add($"{serial}@attr@machine_ID@{machineId}");
                lines.Add($"{serial}@pdata@Operator_ID@{settings.OperatorIdDefault}");
                var modeValue = station.MachineModeOverride ?? settings.ModeDefault;
                lines.Add($"{serial}@pdata@Mode@{modeValue}");
                lines.Add($"{serial}@pdata@TestSeriesID@{settings.TestSeriesIdDefault}");
                lines.Add($"{serial}@pdata@Priority@{settings.PriorityDefault}");
                lines.Add($"{serial}@pdata@online@{settings.OnlineFlagDefault}");
                lines.Add($"{serial}@submit@{settings.SubmitId}");
                lines.Add("}");

                var stampClose = DateTime.Now;
                lines.Add($"[{stampClose:HH:mm:ss:fff}] :ok@{{success}}@");
            }

            return lines;
        }

        private static void AddRangeLine(List<string> lines, string serial, string label, double? value, RangeSetting range, string overrideUnit = null)
        {
            var unit = overrideUnit ?? range?.Unit ?? "mm";
            var formattedValue = value.HasValue
                ? value.Value.ToString("0.000", CultureInfo.InvariantCulture)
                : "0.000";

            var lower = range?.FormatLower() ?? "NA";
            var upper = range?.FormatUpper() ?? "NA";

            lines.Add($"{serial}@pdata@{label}@{formattedValue}@{lower}@{upper}@{unit}");
        }

        private async Task<string> WritePayloadAsync(AeCycleContext context, List<string> payload)
        {
            var settings = _settings ?? AeLimitSettings.CreateDefault();
            var folderName = settings.OutputFolderName;
            string outputFolder;
            if (string.IsNullOrWhiteSpace(folderName))
            {
                outputFolder = _defaultOutputFolder;
            }
            else if (Path.IsPathRooted(folderName))
            {
                outputFolder = folderName;
            }
            else
            {
                outputFolder = Path.Combine(_dataFolder, folderName);
            }
            Directory.CreateDirectory(outputFolder);

            var prefix = string.IsNullOrWhiteSpace(settings.FilePrefix) ? "AE" : settings.FilePrefix;
            var serialSafe = string.IsNullOrWhiteSpace(context.SerialNumber) ? "NA" : context.SerialNumber.Replace(':', '_');
            var fileName = $"{prefix}_{serialSafe}_{DateTime.Now:yyyyMMdd_HHmmss}.txt";
            var path = Path.Combine(outputFolder, fileName);

            await File.WriteAllLinesAsync(path, payload, Encoding.UTF8);
            return path;
        }

        private class AeCycleContext
        {
            private readonly Dictionary<int, AeStationUpdate> _records = new();

            public string SerialNumber { get; set; }
            public string CarrierSerial { get; set; }
            public DateTime StartedOn { get; set; }

            public void SetRecord(int stationId, AeStationUpdate update)
            {
                if (update == null) return;
                _records[stationId] = update;
            }

            public AeStationUpdate GetRecord(int stationId)
            {
                return _records.TryGetValue(stationId, out var record) ? record : null;
            }
        }
    }
}
