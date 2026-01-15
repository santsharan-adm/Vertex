using IPCSoftware.Core.Interfaces; // For IServoCalibrationService
using IPCSoftware.Core.Interfaces.AppLoggerInterface;
using IPCSoftware.CoreService.Services.PLC;
using IPCSoftware.Shared.Models;
using IPCSoftware.Shared.Models.ConfigModels;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Threading.Tasks;

namespace IPCSoftware.CoreService.Services.External
{
    public class ExternalInterfaceService
    {
        private readonly PLCClientManager _plcManager;
        private readonly IPLCTagConfigurationService _tagService;
        private readonly IAppLogger _logger;
        private readonly ExternalSettings _settings;
        private readonly IServoCalibrationService _servoService; // Need this for mapping

        private readonly IOptionsMonitor<ExternalSettings> _settingsMonitor;

        private ExternalSettings Settings => _settingsMonitor.CurrentValue;

        // Connectivity State
        private bool _isMacMiniConnected = false;
        public bool IsConnected => _isMacMiniConnected;

        // Logic State
        // Index = Sequence Step (0 to 11). Value = True if Quarantined (NG).
        private bool[] _quarantineFlagsBySequence = new bool[12];

        public ExternalInterfaceService(
            PLCClientManager plcManager,
            IPLCTagConfigurationService tagService,
            IServoCalibrationService servoService,
            IAppLogger logger,
            IOptions<ExternalSettings> appSettings,
            IOptionsMonitor<ExternalSettings> settingsMonitor)
        {
            _plcManager = plcManager;
            _tagService = tagService;
            _servoService = servoService;
            _logger = logger;
            _settings = appSettings.Value;
            _settingsMonitor = settingsMonitor; // Store monitor

            // Default Safety: All NG
            Array.Fill(_quarantineFlagsBySequence, true);

            // Start Background Ping Loop (Only checks connection, doesn't process data)
            _ = StartConnectionMonitor();
        }

        // --- PUBLIC METHODS CALLED BY CYCLE MANAGER ---

        /// <summary>
        /// 1. Generates Combined String (QR + PrevCode + AOICode).
        /// 2. Fetches Data (Mocked via File Read).
        /// 3. Maps Cavity ID -> Sequence Index.
        /// 4. Writes Result to PLC.
        /// </summary>
        public async Task SyncBatchStatusAsync(string qrCode)
        {
         //   if (!_settings.IsMacMiniEnabled)
            if (!Settings.IsMacMiniEnabled)
            {
                _logger.LogInfo("[ExtIf] Mac Mini Disabled. Sending ALL OK.", LogType.Audit);
                await SyncAllOkToPlc();
                return;
            }

            if (!_isMacMiniConnected)
            {
                _logger.LogError("[ExtIf] Mac Mini Disconnected during Sync! Defaulting to ALL NG.", LogType.Diagnostics);
                // Set all to NG locally to force quarantine
                Array.Fill(_quarantineFlagsBySequence, false);
                // Do NOT write to PLC (or write all 0), let it fail/timeout or handle manually
                return;
            }

            try
            {
                // A. Generate Combined String (For API/Logic trace)
                string combinedId = $"{qrCode}_{_settings.PreviousMachineCode}_{_settings.AOIMachineCode}";
                _logger.LogInfo($"[ExtIf] Requesting Status for: {combinedId}", LogType.Production);

                // B. Get Data (Simulating API call by reading shared JSON)
                string fullPath = Path.Combine(_settings.SharedFolderPath, _settings.StatusFileName);
               // string json = await ReadFileWithRetryAsync(fullPath);
                //string json = await ReadFileWithRetryAsync(_settings.StatusFileName);
                string json = await ReadFileWithRetryAsync(Settings.StatusFileName);

                MacMiniStatusModel statusData = null;
                if (!string.IsNullOrEmpty(json))
                {
                    statusData = JsonConvert.DeserializeObject<MacMiniStatusModel>(json);
                }

                if (statusData == null)
                {
                    _logger.LogWarning("[ExtIf] No data received or invalid JSON. Defaulting to NG.", LogType.Diagnostics);
                    Array.Fill(_quarantineFlagsBySequence, true); // Fail safe
                    return;
                }

                // C. Map Data (Cavity ID -> Sequence Bit)
                await MapAndWriteToPlc(statusData);

            }
            catch (Exception ex)
            {
                _logger.LogError($"[ExtIf] Sync Failed: {ex.Message}", LogType.Diagnostics);
                Array.Fill(_quarantineFlagsBySequence, true); // Fail safe
            }
        }

        /// <summary>
        /// Returns the quarantine flag for a specific SEQUENCE STEP.
        /// </summary>
        /// <param name="sequenceIndex">0-based index of the current sequence step (0 to 11)</param>
        //public bool IsSequenceRestricted(int sequenceIndex)
        //{
        //    if (!_settings.IsMacMiniEnabled) return false;

        //    if (sequenceIndex >= 0 && sequenceIndex < 12)
        //        return _quarantineFlagsBySequence[sequenceIndex];

        //    return true; // Default to restricted if index OOB
        //}

        public bool IsSequenceRestricted(int sequenceIndex)
        {
            // Always checks the LATEST value from config
            if (!Settings.IsMacMiniEnabled) return false;

            if (sequenceIndex >= 0 && sequenceIndex < 12)
                return _quarantineFlagsBySequence[sequenceIndex];

            return true;
        }

        // --- INTERNAL LOGIC ---

        private async Task MapAndWriteToPlc(MacMiniStatusModel data)
        {
            // 1. Get the Map: [SequenceIndex] -> [PhysicalStationID]
            // We need to know: Sequence Step 0 is visiting Station X?
            int[] stationMap = await GetStationMapAsync();

            // 2. Reset Local Flags to NG (True)
            Array.Fill(_quarantineFlagsBySequence, true);

            // 3. Perform Mapping
            // apiData.ok contains Physical IDs (e.g., 1, 5, 12)
            ushort statusWord = 0;

            for (int seqIndex = 0; seqIndex < 12; seqIndex++)
            {
                // Which physical station is visited at this sequence step?
                int physicalStationId = stationMap[seqIndex];

                // Is this physical station in the "OK" list from Mac Mini?
                bool isOk = data.ok.Contains(physicalStationId);

                if (isOk)
                {
                    _quarantineFlagsBySequence[seqIndex] = false; // Not Quarantined

                    // Set PLC Bit (1 = OK)
                    // Bit 0 = Seq 0, Bit 1 = Seq 1...
                    statusWord |= (ushort)(1 << seqIndex);
                }
                else
                {
                    _quarantineFlagsBySequence[seqIndex] = true; // Quarantined
                }
            }

            // 4. Write Status Word (520)
            await WriteToPlc(ConstantValues.Ext_CavityStatus, statusWord);

            // 5. Write Sequence Order (for reference)
            // Just writing 1..12 or the Physical IDs? 
            // Usually PLC wants to know Physical ID at Sequence X.
            // Requirement says: "The IPC must send the sequence order... one value per register"
            for (int i = 0; i < 12; i++)
            {
                // Writing the Physical Station ID into the Sequence Register
                await WriteToPlc(ConstantValues.Ext_SeqRegStart + i, stationMap[i]);
            }

            // 6. Confirm Data Ready
            await WriteToPlc(ConstantValues.Ext_DataReady, true);

            _logger.LogInfo($"[ExtIf] Synced. Word: {statusWord:X4}. Ready Sent.", LogType.Production);
        }

        private async Task SyncAllOkToPlc()
        {
            Array.Fill(_quarantineFlagsBySequence, false);

            // Write All 1s (4095)
            await WriteToPlc(ConstantValues.Ext_CavityStatus, 4095);

            // Write Default Sequence (Linear 1-12)
            for (int i = 0; i < 12; i++)
            {
                await WriteToPlc(ConstantValues.Ext_SeqRegStart + i, i + 1);
            }

            await WriteToPlc(ConstantValues.Ext_DataReady, true);
        }

        // --- BACKGROUND CONNECTION MONITOR ---
        private async Task StartConnectionMonitor()
        {
            while (true)
            {
                try
                {
                   // if (_settings.IsMacMiniEnabled)
                    if (Settings.IsMacMiniEnabled)
                    {
                        bool prev = _isMacMiniConnected;
                        _isMacMiniConnected = true; // await PingHost(_settings.MacMiniIpAddress);

                        if (prev && !_isMacMiniConnected)
                            _logger.LogError("[ExtIf] Mac Mini Connection Lost!", LogType.Error);
                        else if (!prev && _isMacMiniConnected)
                            _logger.LogInfo("[ExtIf] Mac Mini Connected.", LogType.Error);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Ping Error: {ex.Message}", LogType.Diagnostics);
                }
                await Task.Delay(2000);
            }
        }

        private async Task<int[]> GetStationMapAsync()
        {
            try
            {
                var positions = await _servoService.LoadPositionsAsync();
                if (positions != null && positions.Count > 0)
                {
                    return positions
                        .Where(p => p.PositionId != 0 && p.SequenceIndex > 0)
                        .OrderBy(p => p.SequenceIndex)
                        .Select(p => p.PositionId)
                        .ToArray();
                }
            }
            catch { }
            // Default Snake
            return new int[] { 1, 2, 3, 6, 5, 4, 7, 8, 9, 12, 11, 10 };
        }

        private async Task<bool> PingHost(string address)
        {
            try { using var p = new Ping(); var r = await p.SendPingAsync(address, _settings.PingTimeoutMs); return r.Status == IPStatus.Success; } catch { return false; }
        }

        //private async Task<string> ReadFileWithRetryAsync(string filePath)
        //{
        //    for (int i = 0; i < 3; i++) { try { return await File.ReadAllTextAsync(filePath); } catch { await Task.Delay(50); } }
        //    return null;
        //}

        private async Task<string> ReadFileWithRetryAsync(/*string filePath */ string rawData)
        {
            // 1. Raw Input String
            // "0 SFC_OK 1.J85HNT00000000IS01,OK;2.J85HNT00000000IS02,OK;3.J85HNT00000000IS03,OK;4.NA,1;5.NA,1;6.NA,1;7.NA,1;8.NA,1;9.NA,1;10.NA,1;11.NA,1;12.NA,1"
           // string rawData = "0 SFC_OK 1.J85HNT00000000IS01,OK;2.J85HNT00000000IS02,OK;3.J85HNT00000000IS03,OK;4.NA,1;5.NA,1;6.NA,1;7.NA,1;8.NA,1;9.NA,1;10.NA,1;11.NA,1;12.NA,1";

            // 2. Parse String logic
            var model = new MacMiniStatusModel
            {
                ok = new List<int>(),
                sequence = new List<int> { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12 } // Default sequence
            };

            try
            {
                // Remove prefix if present (e.g., "0 SFC_OK ")
                // Find first digit for "1."
                int startIndex = rawData.IndexOf("1.");
                if (startIndex > 0) rawData = rawData.Substring(startIndex);

                // Split by ';' to get items like "1.ID,STATUS"
                var parts = rawData.Split(';', StringSplitOptions.RemoveEmptyEntries);

                foreach (var part in parts)
                {
                    // part = "1.J85HNT00000000IS01,OK"
                    var segments = part.Split(',');
                    if (segments.Length >= 2)
                    {
                        // Parse ID from "1.XXX"
                        string idPart = segments[0]; // "1.J85..."
                        int dotIndex = idPart.IndexOf('.');
                        if (dotIndex > 0)
                        {
                            if (int.TryParse(idPart.Substring(0, dotIndex), out int id))
                            {
                                // Parse Status from "OK" or "1"
                                string status = segments[1].Trim().ToUpper();

                                // Logic: "OK" = Good. "1" = NG? Wait, user said "Na also mean NG and UOP aso mean NG"
                                // "data is in this form ok mean OK, Na also mean NG and UOP aso mean NG"
                                // Usually 1 means OK in many systems but user said "4.NA,1".
                                // Let's assume ONLY "OK" string means OK based on the example provided.

                                if (status == "OK")
                                {
                                    model.ok.Add(id);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Parsing Error: {ex.Message}");
            }

            // 3. Return as JSON
            await Task.Delay(10); // Simulate IO
            return JsonConvert.SerializeObject(model);
        }



        private async Task WriteToPlc(int tagId, object value)
        {
            try
            {
                var allTags = await _tagService.GetAllTagsAsync();
                var tagConfig = allTags.FirstOrDefault(t => t.TagNo == tagId);

                if (tagConfig == null || tagConfig.ModbusAddress <= 0) return;

                var client = _plcManager.GetClient(tagConfig.PLCNo);
                if (client != null) await client.WriteAsync(tagConfig, value);
            }
            catch (Exception ex) { _logger.LogError($"Ext Write Error ({tagId}): {ex.Message}", LogType.Diagnostics); }
        }
    }
}