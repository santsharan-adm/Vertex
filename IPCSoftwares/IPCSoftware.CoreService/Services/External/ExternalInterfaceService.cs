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
        private readonly HttpClient _httpClient;

        private readonly IOptionsMonitor<ExternalSettings> _settingsMonitor;
        private Dictionary<int, string> _cachedSerials = new Dictionary<int, string>();
        public ExternalSettings Settings => _settingsMonitor.CurrentValue;

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
            _httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(5) // Default timeout
            };

            // Default Safety: All NG
            Array.Fill(_quarantineFlagsBySequence, true);

            // Start Background Ping Loop (Only checks connection, doesn't process data)
            _ = StartConnectionMonitor();
        }


        public string GetSerialNumber(int stationId)
        {
            if (!Settings.IsMacMiniEnabled || !IsConnected) return null;

            if (_cachedSerials.TryGetValue(stationId, out string serial))
            {
                // If serial is "NA" or empty, treat as null (fallback to station ID)
                if (string.IsNullOrWhiteSpace(serial) || serial.Equals("NA", StringComparison.OrdinalIgnoreCase))
                    return null;

                return serial;
            }
            return null;
        }

        private async Task<MacMiniStatusModel?> WaitForStatusAsync(
            string statusFileName,
            TimeSpan timeout,
            TimeSpan pollInterval)
                {
                    var deadline = DateTime.Now + timeout;

                    while (DateTime.Now < deadline)
                    {
                        try
                        {
                            var data = await ReadFileWithRetryAsync(statusFileName);
                            if (data != null && data.Serials.Count > 0)
                            return data;
                        }
                        catch
                        {
                            // ignore and retry
                        }

                        await Task.Delay(pollInterval);
                    }

                    return null; // timeout
        }

        private async Task<bool> WaitForMacMiniConnectionAsync(
                                TimeSpan timeout,
                                TimeSpan pollInterval)
        {
            var deadline = DateTime.UtcNow + timeout;
            while (DateTime.UtcNow < deadline)
            {
                if (IsConnected)
                    return true;

                await Task.Delay(pollInterval);
            }

            return false;
        }



        public async Task SyncBatchStatusAsync(string qrCode)
        {
            if (!Settings.IsMacMiniEnabled)
            {
                _logger.LogInfo("Mac Mini Disabled. Sending ALL OK.", LogType.Error);
                await SyncAllOkToPlc();
                return;
            }

            if (!IsConnected)
            {
                _logger.LogWarning(
                    "Mac Mini not connected. Waiting up to 30s before giving up.",
                    LogType.Error);

                var status= await WaitForMacMiniConnectionAsync( TimeSpan.FromSeconds(8),
                    TimeSpan.FromMilliseconds(500));
                if (!status)
                {
                   // await WriteToPlc(ConstantValues.MACMINI_NOTCONNECTED, true);
                    await Task.Delay(1000);
                    return;
                }

            }

            _cachedSerials.Clear();

            try
            {
                _logger.LogInfo($"[ExtIf] Requesting Status for: {qrCode}", LogType.Error);

                var statusData = await FetchStatusFromApiAsync(qrCode);

                // ✅ WAIT up to 30 seconds
              /*  var statusData2 = await WaitForStatusAsync(
                    Settings.StatusFileName,
                     TimeSpan.FromSeconds(3),
                    TimeSpan.FromMilliseconds(500));*/

                if (statusData == null)
                {
                    _logger.LogWarning(
                        "Query response form last mach  ne api not received within 5s.",
                        LogType.Error);
                    await WriteToPlc(ConstantValues.MACMINI_NOTCONNECTED, true);
                    await Task.Delay(1000);
                    return;
                }

                _cachedSerials = statusData.Serials;

                await MapAndWriteToPlc(statusData);
            }
            catch (Exception ex)
            {
                _logger.LogError($"[ExtIf] Sync Failed: {ex.Message}", LogType.Diagnostics);
                // ❗ Do not force NG here either
            }
        }

        private async Task<MacMiniStatusModel> FetchStatusFromApiAsync(string qrCode)
        {
            try
            {
                // Construct the URI using settings + QR code
                var uri = BuildApiUri(
                    Settings.Protocol,
                    Settings.MacMiniIpAddress,
                    Settings.EndPoint,
                    qrCode,
                    Settings.PreviousMachineCode,
                    Settings.AOIMachineCode
                );

                _logger.LogInfo($"[ExtIf] GET {uri}", LogType.Diagnostics);

                // Execute Request
                var response = await _httpClient.GetAsync(uri);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError($"[ExtIf] API Error: {response.StatusCode}", LogType.Error);
                    return null;
                }

                string responseBody = await response.Content.ReadAsStringAsync();

                // Parse response
                return await ParseRawStatusStringAsync(responseBody);
            }
            catch (Exception ex)
            {
                _logger.LogError($"[ExtIf] API Exception: {ex.Message}", LogType.Error);
                return null;
            }
        }

   
        private async Task<MacMiniStatusModel> ParseRawStatusStringAsync(string rawData)
        {
            var model = new MacMiniStatusModel
            {
                ok = new List<int>(),
                sequence = new List<int> { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12 },
                Serials = new Dictionary<int, string>()
            };

            if (string.IsNullOrEmpty(rawData)) return model;

            return await Task.Run(() =>
            {
                try
                {
                    int startIndex = rawData.IndexOf("1.");
                    if (startIndex > 0) rawData = rawData.Substring(startIndex);

                    var parts = rawData.Split(';', StringSplitOptions.RemoveEmptyEntries);

                    foreach (var part in parts)
                    {
                        var segments = part.Split(',');
                        if (segments.Length >= 2)
                        {
                            string idPart = segments[0];
                            int dotIndex = idPart.IndexOf('.');
                            if (dotIndex > 0)
                            {
                                string idString = idPart.Substring(0, dotIndex);
                                string serialString = idPart.Substring(dotIndex + 1);

                                if (int.TryParse(idString, out int id))
                                {
                                    string status = segments[1].Trim().ToUpper();
                                    if (status == "OK")
                                    {
                                        model.ok.Add(id);
                                        model.Serials[id] = serialString;
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
                return model;
            });
        }

       

        private Uri BuildApiUri(string protocol, string host, string endpoint, string carrierSn, string prevMachine, string currentMachine)
        {
            var builder = new UriBuilder
            {
                Scheme = protocol?.ToLower() == "http" ? "http" : "https",
                Host = host,
                Path = endpoint
            };

            // Use System.Web.HttpUtility if available, or this manual approach for .NET Core compatibility
            // Query: ?c=QUERY_4_SFC&subcmd=carrier_query&carrier_sn=...&station_code=...&station_id=...

            var query = System.Web.HttpUtility.ParseQueryString(string.Empty);
            query["c"] = "QUERY_4_SFC";
            query["subcmd"] = "carrier_query";
            query["carrier_sn"] = carrierSn;       // The QR Code
            query["station_code"] = prevMachine;   // Previous Machine Code
            query["station_id"] = currentMachine;  // AOI Machine Code (Current)

            builder.Query = query.ToString();

            return builder.Uri;
        }



        /*  public async Task SyncBatchStatusAsync(string qrCode)
          {
              if (!Settings.IsMacMiniEnabled)
              {
                  _logger.LogInfo("[ExtIf] Mac Mini Disabled. Sending ALL OK.", LogType.Audit);
                  await SyncAllOkToPlc();
                  return;
              }

              if (!_isMacMiniConnected)
              {
                  _logger.LogError("[ExtIf] Mac Mini Disconnected during Sync! Defaulting to ALL NG.", LogType.Error);
                  // Set all to NG locally to force quarantine
                  Array.Fill(_quarantineFlagsBySequence, true);
                  // Do NOT write to PLC (or write all 0), let it fail/timeout or handle manually
                  return;
              }

              _cachedSerials = *//*data.Serials ?? *//*new Dictionary<int, string>();

              try
              {
                  // A. Generate Combined String (For API/Logic trace)
                  string combinedId = $"{qrCode}_{Settings.PreviousMachineCode}_{Settings.AOIMachineCode}";
                  _logger.LogInfo($"[ExtIf] Requesting Status for: {combinedId}", LogType.Production);

                  // B. Get Data (Simulating API call by reading shared JSON)
                //  string fullPath = Path.Combine(_settings.SharedFolderPath, _settings.StatusFileName);
                  // string json = await ReadFileWithRetryAsync(fullPath);
                  //string json = await ReadFileWithRetryAsync(_settings.StatusFileName);
                  MacMiniStatusModel statusData = await ReadFileWithRetryAsync(Settings.StatusFileName);

                  //MacMiniStatusModel statusData = null;
                  //if (!string.IsNullOrEmpty(json))
                  //{
                  //    statusData = JsonConvert.DeserializeObject<MacMiniStatusModel>(json);
                  //}

                  if (statusData == null)
                  {
                      _logger.LogWarning("[ExtIf] No data received or invalid JSON. Defaulting to NG.", LogType.Diagnostics);
                      Array.Fill(_quarantineFlagsBySequence, true); // Fail safe
                      return;
                  }
                  _cachedSerials = statusData.Serials;
                // C. Map Data (Cavity ID -> Sequence Bit)
                await MapAndWriteToPlc(statusData);

              }
              catch (Exception ex)
              {
                  _logger.LogError($"[ExtIf] Sync Failed: {ex.Message}", LogType.Diagnostics);
                  Array.Fill(_quarantineFlagsBySequence, true); // Fail safe
              }
          }

  */
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
            await WriteToPlc(ConstantValues.Ext_DataReady, true);

            _logger.LogInfo($"[ExtIf] Synced. Word: {statusWord:X4}. Ready Sent.", LogType.Production);
        }

        private async Task SyncAllOkToPlc()
        {
            Array.Fill(_quarantineFlagsBySequence, false);

            // Write All 1s (4095)
            await WriteToPlc(ConstantValues.Ext_CavityStatus, 4095);

         
            await WriteToPlc(ConstantValues.Ext_DataReady, true);
        }

        // --- BACKGROUND CONNECTION MONITOR ---


        private async Task UpdateMacMiniConnectionStateAsync(bool isConnected)
        {
            try
            {
                await WriteToPlc(ConstantValues.MACMINI_NOTCONNECTED, !isConnected);
            }
            catch (Exception ex)
            {
                _logger.LogError($"[ExtIf] Failed to update MacMini connection state: {ex.Message}",
                                 LogType.Diagnostics);
            }
        }

        private async Task StartConnectionMonitor()
        {
            while (true)
            {
                try
                {
                    if (Settings.IsMacMiniEnabled)
                    {
                        bool prev = _isMacMiniConnected;
                        bool current = await PingHost(Settings.MacMiniIpAddress);

                        if (prev != current)
                        {
                            _isMacMiniConnected = current;

                            if (!current)
                            {
                                _logger.LogError("[ExtIf] Mac Mini DISCONNECTED", LogType.Error);
                            }
                            else
                            {
                                _logger.LogInfo("[ExtIf] Mac Mini RECONNECTED", LogType.Error);
                            }

                            // 🔥 ALWAYS update PLC on state change
                            await UpdateMacMiniConnectionStateAsync(current);
                        }
                    }

                }
                catch (Exception ex)
                {
                    _logger.LogError($"Ping Error: {ex.Message}", LogType.Diagnostics);
                }

                await Task.Delay(1000); // important: do NOT spin
            }
        }


        //private async Task StartConnectionMonitor()
        //{
        //    while (true)
        //    {
        //        try
        //        {
        //           // if (_settings.IsMacMiniEnabled)
        //            if (Settings.IsMacMiniEnabled)
        //            {
        //                bool prev = _isMacMiniConnected;
        //               // _isMacMiniConnected = true;
        //                _isMacMiniConnected = await PingHost(Settings.MacMiniIpAddress);
        //               // await PingHost(_settings.MacMiniIpAddress);

        //                if (prev && !_isMacMiniConnected)
        //                    _logger.LogError("[ExtIf] Mac Mini Connection Lost!", LogType.Error);
        //                else if (!prev && _isMacMiniConnected)
        //                    _logger.LogInfo("[ExtIf] Mac Mini Connected.", LogType.Error);
        //            }
        //        }
        //        catch (Exception ex)
        //        {
        //            _logger.LogError($"Ping Error: {ex.Message}", LogType.Diagnostics);
        //        }
             
        //    }
        //}

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
            try { using var p = new Ping(); var r = await p.SendPingAsync(address, Settings.PingTimeoutMs); return r.Status == IPStatus.Success; } catch { return false; }
        }

        //private async Task<string> ReadFileWithRetryAsync(string filePath)
        //{
        //    for (int i = 0; i < 3; i++) { try { return await File.ReadAllTextAsync(filePath); } catch { await Task.Delay(50); } }
        //    return null;
        //}

        private async Task<MacMiniStatusModel> ReadFileWithRetryAsync(/*string filePath */ string rawData)
        {
  
            // 2. Parse String logic
            var model = new MacMiniStatusModel
            {
                ok = new List<int>(),
                sequence = new List<int> { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12 }, // Default sequence
                Serials = new Dictionary<int, string>() // Init Dictionary
            };

            if (string.IsNullOrEmpty(rawData)) return model; /*JsonConvert.SerializeObject(model)*/;

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
                            string idString = idPart.Substring(0, dotIndex);
                            string serialString = idPart.Substring(dotIndex + 1); // Extract Serial "J85HNT..."

                            if (int.TryParse(idString, out int id))
                            {
                                // Store Serial
                              

                                string status = segments[1].Trim().ToUpper();
                                if (status == "OK" )
                                {
                                    model.ok.Add(id);
                                    model.Serials[id] = serialString;
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
           // await Task.Delay(10); // Simulate IO
            //return JsonConvert.SerializeObject(model);
            return model;
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