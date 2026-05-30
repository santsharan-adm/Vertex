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
        private readonly IRecipeApplicationService _recipeService;
        private AeLimitSettings _settings;
        private AeCycleContext _currentCycle;

        public AeLimitService(IOptions<ConfigSettings> configOptions,
                              IOptions<ExternalSettings> externalOptions,
                              IRecipeApplicationService recipeService,
                              IAppLogger logger) : base(logger)
        {
            var config = configOptions.Value ?? new ConfigSettings();
            _externalOptions = externalOptions;
            _recipeService = recipeService;
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
                // Load the currently active recipe from Recipe.csv
                var recipe = await _recipeService.GetRecipefromSelection();

                // 1. Build station configs from recipe + settings
                var stations = BuildStationsFromRecipe(recipe, _settings);

                // 2. Generate content lines
                var contentLines = BuildCycleContent(context, stations);

                // 3. Generate TCP Payload — Format: _{ ...data... }
                var tcpSb = new StringBuilder();
                tcpSb.AppendLine("_{");
                foreach (var line in contentLines)
                    tcpSb.AppendLine(line);
                tcpSb.Append("}");
                string tcpPayload = tcpSb.ToString();

                // 4. Generate File Payload — Format: [Time] :_{ ...data... }
                var fileLines = new List<string>();
                var stamp = DateTime.Now.ToString("HH:mm:ss:fff");
                fileLines.Add("");
                fileLines.Add($"[{stamp}] :_{{");
                fileLines.AddRange(contentLines);
                fileLines.Add("}");

                // 5. Append to daily file
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

        // --- Station Config Builder (from Recipe.csv via IRecipeApplicationService) ---


        private List<AeStationRecipeConfig> BuildStationsFromRecipe(ServoRecipeModel recipe, AeLimitSettings settings)
        {
            var result = new List<AeStationRecipeConfig>();

            if (recipe == null || settings == null)
                return result;

            for (int i = 0; i <= 12; i++)
            {
                bool isEnabled = GetIsEnabled(recipe, i);
                if (!isEnabled) continue;

                result.Add(new AeStationRecipeConfig
                {
                    StationIndex = i,
                    StationId = GetPositionId(recipe, i),     // PositionID_N → maps StationId
                    Cavity = GetCavity(recipe, i),         // S0..S12 → Cavity
                    InspectionXLower = recipe.Xmin,
                    InspectionXUpper = recipe.Xmax,
                    InspectionYLower = recipe.Ymin,
                    InspectionYUpper = recipe.Ymax,
                    InspectionAngleLower = recipe.AngleMin,
                    InspectionAngleUpper = recipe.AngleMax,
                    Name = GetName(recipe, i),
                    IsEnabled = true,
                    MachineModeOverride = settings.MachineModeOverride
                });
            }

            return result;
        }

        // --- Cycle Content Builder ---

        private List<string> BuildCycleContent( AeCycleContext context, List<AeStationRecipeConfig> stations)
        {
            var lines = new List<string>();
            var settings = _settings ?? AeLimitSettings.CreateDefault();
            var machineId = _externalOptions?.Value?.AOIMachineCode ?? settings.MachineId;

            foreach (var station in stations)
            {
                // Only include stations that have actual measurement data for this cycle
                var record = context.GetRecord(station.StationId);
                if (record == null) continue;

                var serial = record.SerialNumber ?? context.SerialNumber ?? "NA";
                var carrier = record.CarrierSerial ?? context.CarrierSerial ?? serial;

                // StartLabel: use global default from AELimit.json
                var startLabel = string.IsNullOrWhiteSpace(settings.StartLabel)
                    ? settings.StartLabelDefault
                    : settings.StartLabel;

                lines.Add($"{serial}@start----> {startLabel}");
                lines.Add($"{serial}@dut_pos@{carrier}@{station.Cavity}");
                lines.Add($"{serial}@attr@MLB_AP_SN@{serial}");
                lines.Add($"{serial}@attr@Carrier_SN@{carrier}");
                lines.Add($"{serial}@pdata@Cavity@{station.Cavity}");
                lines.Add($"{serial}@pdata@ae_vendor@{settings.VendorCode}");
                lines.Add($"{serial}@pdata@Tossing@{settings.TossingDefault}");

                // Inspection X — limits from Recipe.csv, unit/hasLimits from AELimit.json
                var rangeX = RangeSetting.Create(
                    station.InspectionXLower, station.InspectionXUpper,
                    settings.InspectionXUnit, settings.InspectionXHasLimits);

                var rangeY = RangeSetting.Create(
                    station.InspectionYLower, station.InspectionYUpper,
                    settings.InspectionYUnit, settings.InspectionYHasLimits);

                var rangeAngle = RangeSetting.Create(
                    station.InspectionAngleLower, station.InspectionAngleUpper,
                    settings.InspectionAngleUnit, settings.InspectionAngleHasLimits);

                AddRangeLine(lines, serial, "Inspection_X", record.ValueX, rangeX);
                AddRangeLine(lines, serial, "Inspection_Y", record.ValueY, rangeY);
                AddRangeLine(lines, serial, "Inspection_A", record.Angle, rangeAngle);
                AddRangeLine(lines, serial, "Cycle_Time", record.CycleTime, settings.CycleTime);

                lines.Add($"{serial}@attr@machine_ID@{machineId}");
                lines.Add($"{serial}@pdata@Operator_ID@{settings.OperatorIdDefault}");

                var modeValue = !string.IsNullOrWhiteSpace(station.MachineModeOverride)
                    ? station.MachineModeOverride
                    : settings.ModeDefault;

                lines.Add($"{serial}@pdata@Mode@{modeValue}");
                lines.Add($"{serial}@pdata@TestSeriesID@{settings.TestSeriesIdDefault}");
                lines.Add($"{serial}@pdata@Priority@{settings.PriorityDefault}");
                lines.Add($"{serial}@pdata@online@{settings.OnlineFlagDefault}");
                lines.Add($"{serial}@submit@{settings.SubmitId}");
            }

            return lines;
        }

        private static void AddRangeLine(List<string> lines, string serial, string label,
            double? value, RangeSetting range, string overrideUnit = null)
        {
            var unit = overrideUnit ?? range?.Unit ?? "mm";
            var formattedValue = value.HasValue
                ? value.Value.ToString("0.000", CultureInfo.InvariantCulture)
                : "0.000";
            var lower = range?.FormatLower() ?? "NA";
            var upper = range?.FormatUpper() ?? "NA";
            lines.Add($"{serial}@pdata@{label}@{formattedValue}@{lower}@{upper}@{unit}");
        }

        private async Task<string> WritePayloadAsync(AeCycleContext context, List<string> lines)
        {
            var settings = _settings ?? AeLimitSettings.CreateDefault();
            var folderName = settings.OutputFolderName;
            string outputFolder = string.IsNullOrWhiteSpace(folderName)
                ? _defaultOutputFolder
                : (Path.IsPathRooted(folderName) ? folderName : Path.Combine(_dataFolder, folderName));

            if (!Directory.Exists(outputFolder)) Directory.CreateDirectory(outputFolder);

            var prefix = string.IsNullOrWhiteSpace(settings.FilePrefix) ? "AE" : settings.FilePrefix;
            var fileName = $"{prefix}_Log_{DateTime.Now:yyyyMMdd}.txt";
            var path = Path.Combine(outputFolder, fileName);

            await File.AppendAllLinesAsync(path, lines, Encoding.UTF8);
            return path;
        }

        // --- Settings Load Helpers ---

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
                    return JsonConvert.DeserializeObject<AeLimitSettings>(json)
                           ?? AeLimitSettings.CreateDefault();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"[AE] Config Load Error: {ex.Message}", LogType.Diagnostics);
            }

            var defaults = AeLimitSettings.CreateDefault();
            try { File.WriteAllText(_configFilePath, JsonConvert.SerializeObject(defaults, Formatting.Indented), Encoding.UTF8); }
            catch { /* best-effort write */ }
            return defaults;
        }

        // --- Recipe.csv field accessors (index 0..12) ---

        /// <summary>Cavity = S0..S12. S0 is implicitly 0; S1..S12 come from recipe.</summary>
        private static int GetCavity(IPCSoftware.Shared.Models.ServoRecipeModel r, int i) => i switch
        {
            0 => 0,
            1 => r.S1,
            2 => r.S2,
            3 => r.S3,
            4 => r.S4,
            5 => r.S5,
            6 => r.S6,
            7 => r.S7,
            8 => r.S8,
            9 => r.S9,
            10 => r.S10,
            11 => r.S11,
            12 => r.S12,
            _ => 0
        };

        /// <summary>StationId = PositionID_N (Position_N property in model).</summary>
        private static int GetPositionId(IPCSoftware.Shared.Models.ServoRecipeModel r, int i) => i switch
        {
            0 => r.Position_0,
            1 => r.Position_1,
            2 => r.Position_2,
            3 => r.Position_3,
            4 => r.Position_4,
            5 => r.Position_5,
            6 => r.Position_6,
            7 => r.Position_7,
            8 => r.Position_8,
            9 => r.Position_9,
            10 => r.Position_10,
            11 => r.Position_11,
            12 => r.Position_12,
            _ => i
        };

        private static string GetName(IPCSoftware.Shared.Models.ServoRecipeModel r, int i) => i switch
        {
            0 => r.Name_0,
            1 => r.Name_1,
            2 => r.Name_2,
            3 => r.Name_3,
            4 => r.Name_4,
            5 => r.Name_5,
            6 => r.Name_6,
            7 => r.Name_7,
            8 => r.Name_8,
            9 => r.Name_9,
            10 => r.Name_10,
            11 => r.Name_11,
            12 => r.Name_12,
            _ => $"Station {i}"
        };

        private static bool GetIsEnabled(IPCSoftware.Shared.Models.ServoRecipeModel r, int i) => i switch
        {
            0 => r.Is_Enabled_0,
            1 => r.Is_Enabled_1,
            2 => r.Is_Enabled_2,
            3 => r.Is_Enabled_3,
            4 => r.Is_Enabled_4,
            5 => r.Is_Enabled_5,
            6 => r.Is_Enabled_6,
            7 => r.Is_Enabled_7,
            8 => r.Is_Enabled_8,
            9 => r.Is_Enabled_9,
            10 => r.Is_Enabled_10,
            11 => r.Is_Enabled_11,
            12 => r.Is_Enabled_12,
            _ => false
        };

        // --- Cycle Context ---

        private class AeCycleContext
        {
            private readonly Dictionary<int, AeStationUpdate> _records = new();
            public string SerialNumber { get; set; }
            public string CarrierSerial { get; set; }
            public DateTime StartedOn { get; set; }

            public void SetRecord(int stationId, AeStationUpdate update)
            {
                if (update != null) _records[stationId] = update;
            }

            public AeStationUpdate GetRecord(int stationId) =>
                _records.TryGetValue(stationId, out var record) ? record : null;
        }
    }
}