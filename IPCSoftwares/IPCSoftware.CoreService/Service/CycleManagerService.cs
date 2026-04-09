using IPCSoftware.Core.Interfaces;
using IPCSoftware.Core.Interfaces.AppLoggerInterface;
using IPCSoftware.Core.Interfaces.CCD;
using IPCSoftware.Devices.Camera;
using IPCSoftware.Devices.PLC;
using IPCSoftware.Services;
using IPCSoftware.Services.AppLoggerServices;
using IPCSoftware.Shared;
using IPCSoftware.Shared.Models;
using IPCSoftware.Shared.Models.AeLimit;
using IPCSoftware.Shared.Models.ConfigModels;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Automation.Peers;

namespace IPCSoftware.CoreService.AOI.Service
{
    public class CycleManagerServiceAOI : CycleManagerServiceBase
    {
        public CycleManagerServiceAOI(
            IPLCTagConfigurationService tagService,
            ILogConfigurationService logConfig,
            PLCClientManager plcManager,
            IOptions<CcdSettings> appSettings,
            IServoCalibrationService servoService,
            ProductionImageService imageService,
            IExternalInterfaceService extService,   // ✅ interface
            IAeLimitService aeLimitService,
            IProductConfigurationService productService,
            IAppLogger logger) : base (tagService,logConfig,plcManager, appSettings, 
                servoService, imageService, extService, aeLimitService, productService, logger)
        {

        }

        protected override async Task LoadStationMapAsync()
        {
            try
            {
                // 1. Load Product Config to determine Limit
                var prodConfig = await _productService.LoadAsync();
                int limit = prodConfig.TotalItems;

                await SyncTotalStationsToPlc(limit);

                var positions = await _servoService.LoadPositionsAsync();
                if (positions != null && positions.Count > 0)
                {
                    // 2. Filter and Limit the Map
                    _stationMap = positions
                        .Where(p => p.PositionId != 0 && p.SequenceIndex > 0)
                        .OrderBy(p => p.SequenceIndex)
                        .Select(p => p.PositionId)
                        .Take(limit) // Limit sequence to configured count
                        .ToArray();

                    Console.WriteLine($"[CycleManager] Loaded sequence ({_stationMap.Length} items): {string.Join("->", _stationMap)}");
                }
                else
                {
                    // Default Fallback
                    _stationMap = Enumerable.Range(1, 12).ToArray();
                }
            }
            catch
            {
                _stationMap = Enumerable.Range(1, 12).ToArray();
            }
        }

        protected override async Task SyncTotalStationsToPlc(int totalItems)
        {
            try
            {
                // We write the value from JSON to PLC to enforce synchronization.
                // Reading first to compare is possible but writing ensures Source of Truth (JSON) is applied.

                int tagId = ConstantValues.NO_OF_Station;
                var allTags = await _tagService.GetAllTagsAsync();
                var tagConfig = allTags.FirstOrDefault(t => t.Id == tagId);

                if (tagConfig != null)
                {
                    var client = _plcManager.GetClient(tagConfig.PLCNo);
                    if (client != null)
                    {
                        // Optional: Read first to see if write is needed
                        // But sticking to requirement: "make sure value of plc remian in sync with value of json"
                        // Writing forcefully is the safest way to ensure this state.
                        await client.WriteAsync(tagConfig, totalItems);
                        _logger.LogInfo($"[CycleManager] Synced PLC Total Stations to {totalItems} (Tag {tagId})", LogType.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"[CycleManager] Failed to sync Total Stations to PLC: {ex.Message}", LogType.Diagnostics);
            }
        }

        /*        private async Task LoadStationMapAsync()
                {
                    try
                    {
                        var positions = await _servoService.LoadPositionsAsync();
                        if (positions != null && positions.Count > 0)
                        {
                            _stationMap = positions
                                .Where(p => p.PositionId != 0 && p.SequenceIndex > 0)
                                .OrderBy(p => p.SequenceIndex)
                                .Select(p => p.PositionId)
                                .ToArray();
                        }
                        else _stationMap = new int[] { 1, 2, 3, 6, 5, 4, 7, 8, 9, 12, 11, 10 };
                    }
                    catch { _stationMap = new int[] { 1, 2, 3, 6, 5, 4, 7, 8, 9, 12, 11, 10 }; }
                }*/

        public override async Task HandleIncomingData(string tempImagePath, Dictionary<string, object> stationData, string qrString = null)
        {
            if (_currentSequenceStep == 0 && string.IsNullOrEmpty(_activeBatchId)) _ = LoadStationMapAsync();

            if (string.IsNullOrEmpty(_activeBatchId))
            {
                if (!string.IsNullOrEmpty(qrString))
                    await StartNewCycle(tempImagePath, qrString);
            }
            else
            {
                await HandleInspectionStep(tempImagePath, stationData);
            }
        }

        protected override async Task StartNewCycle(string tempImagePath, string qrString)
        {
            try
            {
                IsCycleResetCompleted = false;
                Console.WriteLine($"--- NEW CYCLE START: {qrString} ---");
                _logger.LogInfo($"--- NEW CYCLE START: {qrString} ---", LogType.Diagnostics);

                _activeBatchId = qrString;
                _currentSequenceStep = 0;

                // 1. SYNC WITH MAC MINI
                // This fetches the JSON, maps it, writes to PLC, and updates internal flags
                // We use .Result or Wait() here cautiously because this is inside a sync void method called by TriggerService
                // Ideally trigger service awaits this.


                try
                {
                    if (File.Exists(_stateFilePath)) File.Delete(_stateFilePath);
                    var initialState = new CycleStateModel { BatchId = _activeBatchId, LastUpdated = DateTime.Now };
                    File.WriteAllText(_stateFilePath, JsonConvert.SerializeObject(initialState, Formatting.Indented));
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Failed to update initial JSON state: {ex.Message}", LogType.Diagnostics);
                }

                await _extService.SyncBatchStatusAsync(qrString);
                if (_extService.Settings.IsMacMiniEnabled)
                {
                    _aeLimitService.BeginCycle(_activeBatchId, qrString);
                }



                // 2. Initialize JSON State with Pre-Calculated NG status
                // This ensures UI shows Red immediately for NG parts
                InitializeCycleStateWithExternalStatus();

                // 3. Process QR Image
                string destPath = _imageService.ProcessAndMoveImage(tempImagePath, _imageBaseOutputPath, _activeBatchId, 0.ToString(), 0, 0, 0, true);
                // Update QR entry in JSON (Station 0)
                UpdateJsonEntry(0, destPath, "OK", 0, 0, 0);
                if (_extService.Settings.IsMacMiniEnabled)
                {

                    _aeLimitService.UpdateStation(new AeStationUpdate
                    {
                        StationId = 0,
                        SerialNumber = _activeBatchId,
                        CarrierSerial = _activeBatchId,
                        ValueX = 0,
                        ValueY = 0,
                        Angle = 0,
                        CycleTime = null
                    });
                }
            }
            catch (Exception ex) { _logger.LogError(ex.Message, LogType.Diagnostics); }
        }



        protected override void InitializeCycleStateWithExternalStatus()
        {
            base.InitializeCycleStateWithExternalStatus();
            try
            {
                var state = new CycleStateModel { BatchId = _activeBatchId, LastUpdated = DateTime.Now };

                // Populate placeholders for all 12 stations based on External Status
                // for (int i = 0; i < 12; i++)
                for (int i = 0; i < _stationMap.Length; i++)
                {
                    // Map sequence index 'i' to physical station
                    int physId = _stationMap.Length > i ? _stationMap[i] : (i + 1);

                    // Check External Status using Sequence Index (0-11)
                    bool isNg = _extService.IsSequenceRestricted(i);

                    state.Stations[physId] = new StationResult
                    {
                        StationNumber = physId,
                        Status = isNg ? "NG" : "Unchecked",
                        ImagePath = null, // No image yet
                        Timestamp = DateTime.Now
                    };
                }

                File.WriteAllText(_stateFilePath, JsonConvert.SerializeObject(state, Formatting.Indented));
            }
            catch { }
        }

        protected override async Task HandleInspectionStep(string tempImagePath, Dictionary<string, object> data)
        {
            try
            {
                if (_stationMap == null || _stationMap.Length == 0) return;
                // if (_currentSequenceStep >= _stationMap.Length) {   RequestReset(false); return; }

                int physicalStationId = _stationMap[_currentSequenceStep];
                Console.WriteLine($"--- PROCESSING STATION {physicalStationId} (Seq {_currentSequenceStep}) ---");

                double x = data.ContainsKey("X") ? Convert.ToDouble(data["X"]) : 0.0;
                double y = data.ContainsKey("Y") ? Convert.ToDouble(data["Y"]) : 0.0;
                double z = data.ContainsKey("Z") ? Convert.ToDouble(data["Z"]) : 0.0;
                string status = data.ContainsKey("Status") ? data["Status"].ToString() : "OK";

                // 2. CHECK STATUS (Using Sequence Step Index)
                bool isExternalNg = _extService.IsSequenceRestricted(_currentSequenceStep);

                // --- NEW: GET SERIAL NUMBER ---
                // Try to get Serial from External Service
                string serialNumber = _extService.GetSerialNumber(physicalStationId);

                // Fallback Logic: If serial is null (Mac Mini off/disconnected/NA), use Station ID string
                string identifierForImage = !string.IsNullOrEmpty(serialNumber)
                                            ? serialNumber
                                            : physicalStationId.ToString();

                string destUiPath;

                if (isExternalNg)
                {
                    status = "NG";
                    _logger.LogWarning($"[Cycle] Seq {_currentSequenceStep} (Stn {physicalStationId}) quarantined.", LogType.Error);

                    // Move to Quarantine
                    string fileName = Path.GetFileName(tempImagePath);
                    if (!Directory.Exists(_quarantinePath)) Directory.CreateDirectory(_quarantinePath);
                    string metaDate = DateTime.Now.ToString("yyyy_MM_dd");
                    string folderName = $"{metaDate}-{_activeBatchId}".Replace("\0", "_");
                    string destDir = Path.Combine(_quarantinePath, folderName);

                    // ✅ Ensure directory exists
                    Directory.CreateDirectory(destDir);

                    string destFile = Path.Combine(destDir, $"{identifierForImage}_{_activeBatchId}_{DateTime.Now:yyyyMMdd_HHmmss}_raw.bmp");

                    for (int i = 0; i < 3; i++)
                    {
                        try
                        {
                            File.Move(tempImagePath, destFile);
                            break;
                        }
                        catch
                        {
                            Thread.Sleep(50);
                        }
                    }

                    destUiPath = string.Empty;
                }
                else
                {
                    destUiPath = _imageService.ProcessAndMoveImage(tempImagePath, _imageBaseOutputPath, _activeBatchId, identifierForImage, x, y, z);
                }

                UpdateJsonEntry(physicalStationId, destUiPath, status, x, y, z);
                if (_extService.Settings.IsMacMiniEnabled)
                {

                    _aeLimitService.UpdateStation(new AeStationUpdate
                    {
                        StationId = physicalStationId,
                        SerialNumber = _activeBatchId,
                        CarrierSerial = _activeBatchId,
                        ValueX = x,
                        ValueY = y,
                        Angle = z,
                        CycleTime = data.TryGetValue("CycleTime", out var ctObj) ? Convert.ToDouble(ctObj) : (double?)null
                    });
                }


                _currentSequenceStep++;

                if (_currentSequenceStep >= _stationMap.Length)
                {
                    _logger.LogInfo("[CycleManager] Reset skipped - already completed.", LogType.Diagnostics);
                    if (_extService.Settings.IsMacMiniEnabled)
                    {
                        // 1. Generate the Payload (Tuple: FilePath, TcpPayload)
                        var result = await _aeLimitService.CompleteCycleAsync();

                        // 2. Send via TCP to Mac Mini
                        // The SendPdcaDataAsync method handles:
                        // - TCP connection
                        // - Sending the string
                        // - Checking response for "OK"
                        // - Raising/Clearing Alarm Bit (MACMINI_NOTCONNECTED)
                        if (!string.IsNullOrEmpty(result.TcpPayload))
                        {
                            await _extService.SendPdcaDataAsync(result.TcpPayload);
                        }
                    }
                    //Task.Run(async () => { ; RequestReset(false); });
                }
            }
            catch (Exception ex) { _logger.LogError(ex.Message, LogType.Diagnostics); }
        }

        protected override void UpdateJsonEntry(int stationNo, string imgPath, string status, double x, double y, double z)
        {
            base.UpdateJsonEntry(stationNo,imgPath, status, x, y, z);
            try
            {
                string json = File.Exists(_stateFilePath) ? File.ReadAllText(_stateFilePath) : "";
                var state = string.IsNullOrEmpty(json) ? new CycleStateModel { BatchId = _activeBatchId } : JsonConvert.DeserializeObject<CycleStateModel>(json);

                if (stationNo == 0)
                {
                    state.Stations[stationNo] = new StationResult { StationNumber = 0, ImagePath = imgPath, Timestamp = DateTime.Now };
                }
                else
                {
                    // We might update an existing placeholder created in Init
                    state.Stations[stationNo] = new StationResult
                    {
                        StationNumber = stationNo,
                        ImagePath = imgPath,
                        Status = status,
                        X = x,
                        Y = y,
                        Z = z,
                        Timestamp = DateTime.Now
                    };
                }

                state.LastUpdated = DateTime.Now;
                File.WriteAllText(_stateFilePath, JsonConvert.SerializeObject(state, Formatting.Indented));
            }
            catch (Exception ex) { _logger.LogError($"Error updating JSON: {ex.Message}", LogType.Diagnostics); }
        }

    }
}