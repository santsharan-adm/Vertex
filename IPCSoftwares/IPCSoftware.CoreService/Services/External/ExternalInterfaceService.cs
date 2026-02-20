using IPCSoftware.Core.Interfaces; // For IServoCalibrationService
using IPCSoftware.Core.Interfaces.AppLoggerInterface;
using IPCSoftware.CoreService.Services.CCD;
using IPCSoftware.CoreService.Services.PLC;
using IPCSoftware.Shared;
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
    public class ExternalInterfaceService : IDisposable
    {
        private readonly PLCClientManager _plcManager;
        private readonly IPLCTagConfigurationService _tagService;
        private readonly IAppLogger _logger;
        private readonly IProductConfigurationService _productService; // NEW Injection
        private readonly IServoCalibrationService _servoService;
        private readonly HttpClient _httpClient;
        private readonly ITcpTrafficLogger _trafficLogger;

        private readonly IOptionsMonitor<ExternalSettings> _settingsMonitor;
        private readonly MacMiniTcpClient _tcpClient;

        private Dictionary<int, string> _cachedSerials = new Dictionary<int, string>();
        public ExternalSettings Settings => _settingsMonitor.CurrentValue;

        // Connectivity State
        private bool _isMacMiniConnected = false;
        public bool IsConnected => _isMacMiniConnected;

        // Logic State
        // Dynamically sized based on ProductSettings
        private bool[] _quarantineFlagsBySequence;
        private int _totalItems = 12; // Default fallback

        public ExternalInterfaceService(
            PLCClientManager plcManager,
            IPLCTagConfigurationService tagService,
            IServoCalibrationService servoService,
            IProductConfigurationService productService, // Inject Product Service
            IAppLogger logger,ITcpTrafficLogger trafficLogger,
            IOptionsMonitor<ExternalSettings> settingsMonitor)
        {
            _plcManager = plcManager;
            _tagService = tagService;
            _servoService = servoService;
            _productService = productService;
            _logger = logger;
            _settingsMonitor = settingsMonitor;
            _trafficLogger = trafficLogger;
            _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
            _tcpClient = new MacMiniTcpClient();

            // Initialize defaults (will be resized on first sync)
            _quarantineFlagsBySequence = new bool[_totalItems];
            Array.Fill(_quarantineFlagsBySequence, true);

            _ = StartConnectionMonitor();
        }

        /// <summary>
        /// Updates the local item count from Product Settings.
        /// </summary>
        private async Task RefreshConfigurationAsync()
        {
            try
            {
                var config = await _productService.LoadAsync();
                int newCount = config.TotalItems > 0 ? config.TotalItems : 12;

                if (_totalItems != newCount || _quarantineFlagsBySequence.Length != newCount)
                {
                    _totalItems = newCount;
                    _quarantineFlagsBySequence = new bool[_totalItems];
                    Array.Fill(_quarantineFlagsBySequence, true); // Default to NG on resize
                    // _logger.LogInfo($"[ExtIf] Updated Total Items to {_totalItems}", LogType.System);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"[ExtIf] Config Load Error: {ex.Message}", LogType.Diagnostics);
            }
        }

        public string GetSerialNumber(int stationId)
        {
            if (!Settings.IsMacMiniEnabled || !IsConnected) return null;

            if (_cachedSerials.TryGetValue(stationId, out string serial))
            {
                if (string.IsNullOrWhiteSpace(serial) || serial.Equals("NA", StringComparison.OrdinalIgnoreCase))
                    return null;

                return serial;
            }
            return null;
        }

        public bool IsSequenceRestricted(int sequenceIndex)
        {
            if (!Settings.IsMacMiniEnabled) return false;

            if (sequenceIndex >= 0 && sequenceIndex < _quarantineFlagsBySequence.Length)
                return _quarantineFlagsBySequence[sequenceIndex];

            return true; // Default Restricted if out of bounds
        }

        public bool[] GetCurrentQuarantineSnapshot()
        {
            if (!Settings.IsMacMiniEnabled) return new bool[_totalItems]; // All False
            return (bool[])_quarantineFlagsBySequence.Clone();
        }
            
        public async Task SyncBatchStatusAsync(string qrCode)
        {
            // 1. Ensure we have the latest item count
            await RefreshConfigurationAsync();

            if (!Settings.IsMacMiniEnabled)
            {
                _logger.LogInfo("[ExtIf] Mac Mini Disabled. Sending ALL OK.", LogType.Error);
                await SyncAllOkToPlc();
                return;
            }

            if (!_isMacMiniConnected)
            {
                _logger.LogError("[ExtIf] Mac Mini Disconnected during Sync! Defaulting to ALL NG.", LogType.Error);
                Array.Fill(_quarantineFlagsBySequence, true); // Force NG
                return;
            }

            _cachedSerials.Clear();


            try
            {
                _logger.LogInfo($"[ExtIf] Requesting Status ({Settings.Protocol}) for: {qrCode}", LogType.Error);

                MacMiniStatusModel statusData = null;
                string rawResponse = string.Empty;
                string querySent = string.Empty;

                // --- 1. TCP/IP ---
                if (Settings.Protocol.ToUpper() == "TCP")
                {
                    string query = $"{Settings.EndPoint}@c=QUERY_4_SFC&subcmd=carrier_query&carrier_sn={qrCode}&station_code={Settings.PreviousMachineCode}&station_id={Settings.AOIMachineCode}";

                    await _trafficLogger.LogTrafficAsync("SEND", query, "SFC_QUERY");
                    if (!_tcpClient.IsConnected)
                    {
                        await _tcpClient.ConnectAsync(Settings.MacMiniIpAddress, Settings.Port);
                    }
                    rawResponse = await _tcpClient.SendAndReceiveAsync(query);
                    await _trafficLogger.LogTrafficAsync("RECV", rawResponse, "SFC_QUERY");

                    if (string.IsNullOrWhiteSpace(rawResponse))
                    {
                        _logger.LogError("[ExtIf] Received EMPTY response from Mac Mini.", LogType.Error);
                        throw new Exception("Empty Response from Mac Mini");
                    }

                    if (!rawResponse.Contains("SFC_OK", StringComparison.OrdinalIgnoreCase) &&
                        !rawResponse.Contains("OK", StringComparison.OrdinalIgnoreCase))
                    {
                        _logger.LogError($"[ExtIf] Response does not contain 'OK'. Raw: {rawResponse}", LogType.Error);
                        throw new Exception("Invalid Response Status (No OK found)");
                    }

                    statusData = await ParseRawStatusStringAsync(rawResponse);
                }
                // --- 2. HTTP/HTTPS ---
                else if (Settings.Protocol.ToUpper() == "HTTP" || Settings.Protocol.ToUpper() == "HTTPS")
                {
                    var uri = BuildApiUri(Settings.Protocol, Settings.MacMiniIpAddress, Settings.EndPoint, qrCode, Settings.PreviousMachineCode, Settings.AOIMachineCode);
                    querySent = uri.ToString();

                    await _trafficLogger.LogTrafficAsync("SEND", querySent, "SFC_QUERY_HTTP");

                    var response = await _httpClient.GetAsync(uri);
                    if (response.IsSuccessStatusCode)
                    {
                        rawResponse = await response.Content.ReadAsStringAsync();
                        await _trafficLogger.LogTrafficAsync("RECV", rawResponse, "SFC_QUERY_HTTP");

                        if (string.IsNullOrWhiteSpace(rawResponse) || !rawResponse.Contains("OK", StringComparison.OrdinalIgnoreCase))
                        {
                            throw new Exception("Invalid/Empty HTTP Response");
                        }


                        statusData = await ParseRawStatusStringAsync(rawResponse);
                    }
                    else
                    {
                        string errorMsg = $"HTTP Error: {response.StatusCode}";
                        await _trafficLogger.LogTrafficAsync("ERROR", errorMsg, "SFC_QUERY_HTTP");

                        _logger.LogError($"[ExtIf] HTTP Error: {response.StatusCode}", LogType.Error);
                        throw new Exception(errorMsg);
                    }
                }
               

                if (statusData == null || statusData.Serials.Count == 0)
                {
                    _logger.LogWarning("[ExtIf] Parsing failed or no serials found in response. Defaulting to NG.", LogType.Error);
                    Array.Fill(_quarantineFlagsBySequence, true);
                    await WriteToPlc(ConstantValues.MACMINI_NOTCONNECTED, true);
                    return;
                }

                _cachedSerials = statusData.Serials;
                await MapAndWriteToPlc(statusData);
                await WriteToPlc(ConstantValues.MACMINI_NOTCONNECTED, false);
            }
            catch (Exception ex)
            {
                await _trafficLogger.LogTrafficAsync("ERROR", ex.Message, "SFC_QUERY");

                _logger.LogError($"[ExtIf] Sync Failed: {ex.Message}", LogType.Diagnostics);
                Array.Fill(_quarantineFlagsBySequence, true);
                await WriteToPlc(ConstantValues.MACMINI_NOTCONNECTED, true);
            }
        }


        private async Task MapAndWriteToPlc(MacMiniStatusModel data)
        {
            // Get Map respecting Total Items
            int[] stationMap = await GetStationMapAsync();

            // Reset Flags
            Array.Fill(_quarantineFlagsBySequence, true);

            // Note: Status Word logic currently supports up to 16 bits (ushort). 
            // If TotalItems > 16, this single word logic needs expansion on PLC side.
            ushort statusWord = 0;

            for (int i = 0; i < _totalItems; i++)
            {
                // Safety check for map index
                if (i >= stationMap.Length) break;

                int physId = stationMap[i];

                if (data.ok.Contains(physId))
                {
                    _quarantineFlagsBySequence[i] = false; // OK
                    if (i < 16)
                    {
                        statusWord |= (ushort)(1 << i);
                    }
                }
            }

            await WriteToPlc(ConstantValues.Ext_CavityStatus, statusWord);

            // Write Sequence Registers (Dynamic Length)
          /*  for (int i = 0; i < _totalItems; i++)
            {
                if (i >= stationMap.Length) break;
                // Writing Physical Station ID into Sequence Register
                await WriteToPlc(ConstantValues.NO_OF_Station + i, stationMap[i]);
            }*/

            // Write 0 to unused registers if logic requires clearing old data (Optional)
            // for (int i = _totalItems; i < 12; i++) await WriteToPlc(ConstantValues.Ext_SeqRegStart + i, 0);

            await WriteToPlc(ConstantValues.Ext_DataReady, true);
            _logger.LogInfo($"[ExtIf] Synced. Items: {_totalItems}, Word: {statusWord:X4}", LogType.Error);
        }

        private async Task SyncAllOkToPlc()
        {
            await RefreshConfigurationAsync(); // Ensure count is correct

            Array.Fill(_quarantineFlagsBySequence, false);

            // Write 1s to status word (up to 16 items)
            ushort statusWord = 0;
            for (int i = 0; i < _totalItems && i < 16; i++) statusWord |= (ushort)(1 << i);

            await WriteToPlc(ConstantValues.Ext_CavityStatus, statusWord);

            // Write Default Sequence (or loaded map)
          /*  int[] map = await GetStationMapAsync();
            for (int i = 0; i < _totalItems; i++)
            {
                int val = (i < map.Length) ? m  ap[i] : (i + 1);
                await WriteToPlc(ConstantValues.NO_OF_Station + i, val);
            }*/

            await WriteToPlc(ConstantValues.Ext_DataReady, true);
        }


        public async Task ResetPlcInterfaceAsync()
        {
            await RefreshConfigurationAsync(); // Safety update
            await WriteToPlc(ConstantValues.Ext_DataReady, false);
            await WriteToPlc(ConstantValues.Ext_CavityStatus, 0);
            Array.Fill(_quarantineFlagsBySequence, true);
        }

        private async Task<MacMiniStatusModel> ParseRawStatusStringAsync(string rawData)
        {
            var model = new MacMiniStatusModel
            {
                ok = new List<int>(),
                // Generate default sequence list based on current TotalItems
                sequence = Enumerable.Range(1, _totalItems).ToList(),
                Serials = new Dictionary<int, string>()
            };

            if (string.IsNullOrEmpty(rawData)) return model;

            await Task.Run(() => {
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
                            int dot = idPart.IndexOf('.');
                            if (dot > 0 && int.TryParse(idPart.Substring(0, dot), out int id))
                            {
                                string serial = idPart.Substring(dot + 1);
                                string status = segments[1].Trim().ToUpper();
                                model.Serials[id] = serial;
                                if (status == "OK") model.ok.Add(id);
                            }
                        }
                    }
                }
                catch (Exception ex) 
                {
                    Console.WriteLine($"Parsing Error: {ex.Message}");
                }
            });
            return model;
        }

        private async Task<int[]> GetStationMapAsync()
        {
            try
            {
                var positions = await _servoService.LoadPositionsAsync();
                if (positions != null && positions.Count > 0)
                {
                    // Return mapped positions limited by TotalItems
                    return positions
                        .Where(p => p.PositionId != 0 && p.SequenceIndex > 0)
                        .OrderBy(p => p.SequenceIndex)
                        .Select(p => p.PositionId)
                        .Take(_totalItems)
                        .ToArray();
                }
            }
            catch { }
            // Default Linear Map (1 to TotalItems)
            return Enumerable.Range(1, _totalItems).ToArray();
        }

        private async Task StartConnectionMonitor()
        {
            // Track previous states to detect transitions (toggles)
            bool wasEnabled = Settings.IsMacMiniEnabled;
            bool firstRun = true;
            while (true)
            {
                try
                {
                    bool isEnabled = Settings.IsMacMiniEnabled;
                    bool currentConnection = false;

                    // 1. ALWAYS check physical connection so UI is always accurate
                    if (Settings.Protocol?.ToUpper() == "TCP")
                    {
                        if (!_tcpClient.IsConnected)
                        {
                            try 
                            { 
                                await _tcpClient.
                                    ConnectAsync(Settings.MacMiniIpAddress, Settings.Port); 
                                currentConnection = true; 
                            }
                            catch { currentConnection = false; }
                        }
                        else currentConnection = true;
                    }
                    else
                    {
                        currentConnection = await PingHost(Settings.MacMiniIpAddress);
                    }

                    bool connectionChanged = (_isMacMiniConnected != currentConnection);
                    _isMacMiniConnected = currentConnection; // Update UI property

                    // 2. PLC ALARM LOGIC
                    if (isEnabled)
                    {
                        // If connection drops/restores OR user just turned the toggle ON
                        if (connectionChanged || !wasEnabled || firstRun)
                        {
                            if (!currentConnection) _logger.LogError("[ExtIf] Connection Lost!", LogType.Error);
                            else _logger.LogInfo("[ExtIf] Connected.", LogType.Error);

                            // Send actual connection state to PLC
                            await UpdateMacMiniConnectionStateAsync(currentConnection);
                        }
                    }
                    else
                    {
                        // If user just toggled the button OFF on the dashboard
                        if (wasEnabled)
                        {
                            _logger.LogInfo("[ExtIf] Mac Mini Disabled on Dashboard. Clearing alarms.", LogType.Error);

                            // Passing 'true' clears the alarm because UpdateMacMiniConnectionStateAsync writes !isConnected (Writes 0)
                            await UpdateMacMiniConnectionStateAsync(true);
                        }
                    }

                    // Store current state for the next loop iteration
                    wasEnabled = isEnabled;
                    firstRun = false;
                }
                catch
                {
                    // Suppress loop errors
                }
                await Task.Delay(1000);
            }
        }




        private async Task UpdateMacMiniConnectionStateAsync(bool isConnected)
        {
            try 
            {
                await WriteToPlc(ConstantValues.MACMINI_NOTCONNECTED, !isConnected); 
            } 
            catch 
            { 

            }
        }

        private async Task<bool> PingHost(string address)
        {
            try { using var p = new Ping(); var r = await p.SendPingAsync(address, Settings.PingTimeoutMs); return r.Status == IPStatus.Success; } catch { return false; }
        }

     

        private Uri BuildApiUri(string protocol, string host, string endpoint, string sn, string prev, string curr)
        {
            var builder = new UriBuilder { Scheme = protocol, Host = host, Path = endpoint };
            var query = System.Web.HttpUtility.ParseQueryString("");
            query["c"] = "QUERY_4_SFC"; query["subcmd"] = "carrier_query";
            query["carrier_sn"] = sn; query["station_code"] = prev; query["station_id"] = curr;
            builder.Query = query.ToString();
            return builder.Uri;
        }

        private async Task WriteToPlc(int tagId, object value)
        {
            try
            {
                var allTags = await _tagService.GetAllTagsAsync();
                var tagConfig = allTags.FirstOrDefault(t => t.TagNo == tagId);
                if (tagConfig != null && tagConfig.ModbusAddress > 0)
                {
                    var client = _plcManager.GetClient(tagConfig.PLCNo);
                    if (client != null) await client.WriteAsync(tagConfig, value);
                }
            }
            catch (Exception ex) { _logger.LogError($"Ext Write Error ({tagId}): {ex.Message}", LogType.Diagnostics); }
        }

        public async Task SendPdcaDataAsync(string payload)
        {
            if (!Settings.IsMacMiniEnabled) return;

            try
            {
                _logger.LogInfo("[ExtIf] Sending PDCA Data...", LogType.Error);

                // 1. Ensure Connected
                if (!_tcpClient.IsConnected)
                {
                    await _tcpClient.ConnectAsync(Settings.MacMiniIpAddress, Settings.Port);
                }

                // 2. Send Plain Text
                await _trafficLogger.LogTrafficAsync("SEND", payload, "PDCA_DATA");
                string response = await _tcpClient.SendAndReceiveAsync(payload);
                await _trafficLogger.LogTrafficAsync("RECV", response, "PDCA_DATA");

                // 3. Validate Response
                // Requirement: "We will receive same response with ok in front of it"
                // Checking if it contains "OK" or "Success" (Case insensitive)
                bool isSuccess = !string.IsNullOrEmpty(response) &&
                                 (response.Contains("OK", StringComparison.OrdinalIgnoreCase) ||
                                  response.Contains("Success", StringComparison.OrdinalIgnoreCase));

                if (isSuccess)
                {
                    _logger.LogInfo("[ExtIf] Data Sent Successfully. Response OK.", LogType.Error);

                    // CLEAR ALARM (0)
                   // await WriteToPlc(ConstantValues.MACMINI_NOTCONNECTED, false);
                }
                else
                {
                    _logger.LogError($"[ExtIf] Data Send Failed. Response: {response}", LogType.Error);

                    // RAISE ALARM (1)
                    await WriteToPlc(ConstantValues.MACMINI_NOTCONNECTED, true);
                }
            }
            catch (Exception ex)
            {
                await _trafficLogger.LogTrafficAsync("ERROR", ex.Message, "PDCA_DATA");
                _logger.LogError($"[ExtIf] TCP Send Error: {ex.Message}", LogType.Diagnostics);

                // RAISE ALARM (1) on Exception (Connection lost, timeout, etc.)
                await WriteToPlc(ConstantValues.MACMINI_NOTCONNECTED, true);
            }
        }

        public void Dispose()
        {
            _httpClient?.Dispose();
            _tcpClient?.Dispose();
        }
    }
}
