using IPCSoftware.Core.Interfaces;
using IPCSoftware.Core.Interfaces.AppLoggerInterface;
using IPCSoftware.Core.Interfaces.CCD;
using IPCSoftware.CoreService.Services.External;
using IPCSoftware.CoreService.Services.PLC;
using IPCSoftware.Services;
using IPCSoftware.Shared;
using IPCSoftware.Shared.Models;
using IPCSoftware.Shared.Models.AeLimit;
using IPCSoftware.Shared.Models.ConfigModels;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace IPCSoftware.CoreService.Services.CCD
{
    public class CycleManagerService : BaseService, ICycleManagerService
    {
        // ... (Fields) ...
        private readonly IPLCTagConfigurationService _tagService;
        private readonly PLCClientManager _plcManager;
        private readonly ProductionImageService _imageService;
        private readonly IServoCalibrationService _servoService;
        private readonly ExternalInterfaceService _extService;
        private readonly IAeLimitService _aeLimitService;
        private readonly IProductConfigurationService _productService;

        private string _activeBatchId = string.Empty;
        private int _currentSequenceStep = 0;
        private readonly string _stateFilePath;
        private readonly string _quarantinePath;
        private readonly string _imageBaseOutputPath;
        private int[] _stationMap;
        public bool IsCycleResetCompleted { get; private set; }
        private string _tempImageFolderPath;



        public CycleManagerService(
            IPLCTagConfigurationService tagService,
            ILogConfigurationService logConfig,
            PLCClientManager plcManager,
            IOptions<CcdSettings> appSettings,
            IServoCalibrationService servoService,
            ProductionImageService imageService,
            ExternalInterfaceService extService,
            IAeLimitService aeLimitService,
             IProductConfigurationService productService,
            IAppLogger logger) : base(logger)
        {
            var ccd = appSettings.Value;
            _tempImageFolderPath = ccd.TempImgFolder;
            _tagService = tagService;
            _plcManager = plcManager;
            _imageService = imageService;
            _servoService = servoService;
            _extService = extService;
            _aeLimitService = aeLimitService;
            _productService = productService;
            _stateFilePath = Path.Combine(ccd.QrCodeImagePath, ccd.CurrentCycleStateFileName);
            var logs =  logConfig.GetAllAsync();
            var allLogs = logConfig.GetAllAsync().GetAwaiter().GetResult();
            var config = allLogs.FirstOrDefault(c => c.LogType == LogType.Production);
            var basePath = config.ProductionImagePath;

            _imageBaseOutputPath = basePath;
            _quarantinePath = Path.Combine(basePath, "Quarantine");
            if (!Directory.Exists(_quarantinePath)) Directory.CreateDirectory(_quarantinePath);

            _ = LoadStationMapAsync();
        }

        private async Task LoadStationMapAsync()
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

        private async Task SyncTotalStationsToPlc(int totalItems)
        {
            try
            {
                // We write the value from JSON to PLC to enforce synchronization.
                // Reading first to compare is possible but writing ensures Source of Truth (JSON) is applied.

                int tagId = ConstantValues.NO_OF_Station;
                var allTags = await _tagService.GetAllTagsAsync();
                var tagConfig = allTags.FirstOrDefault(t => t.TagNo == tagId);

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

        public async Task HandleIncomingData(string tempImagePath, Dictionary<string, object> stationData, string qrString = null)
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

        private async Task StartNewCycle(string tempImagePath, string qrString)
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


               await  _extService.SyncBatchStatusAsync(qrString);
                if (_extService.Settings.IsMacMiniEnabled)
                {
                    _aeLimitService.BeginCycle(_activeBatchId, qrString);
                }
           

                if (File.Exists(_stateFilePath)) File.Delete(_stateFilePath);

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



        private void InitializeCycleStateWithExternalStatus()
        {
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

        private async Task HandleInspectionStep(string tempImagePath, Dictionary<string, object> data)
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
                    _logger.LogWarning($"[Cycle] Seq {_currentSequenceStep} (Stn {physicalStationId}) quarantined.", LogType.Production);

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

        private void UpdateJsonEntry(int stationNo, string imgPath, string status, double x, double y, double z)
        {
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

   
        private async Task WriteTagAsync()
        {
            // Use ConstantValues for Tag ID (initialized from appsettings via Program.cs)
            int tagNo = ConstantValues.Return_TAG_ID;
            try
            {
                var allTags = await _tagService.GetAllTagsAsync();
                var tag = allTags.FirstOrDefault(t => t.TagNo == tagNo);

                if (tag != null)
                {
                    var client = _plcManager.GetClient(tag.PLCNo);
                    if (client != null) await client.WriteAsync(tag, 0);
                }
            }
            catch (Exception ex) { _logger.LogError($"[Error] Ack Tag {tagNo}: {ex.Message}", LogType.Diagnostics); }
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


        private int _resetInProgress = 0;

       


        public void RequestReset(bool fromCcd = false)
        {
            // 1. Log who is requesting
            _logger.LogInfo($"[CycleManager] Reset Requested. Source: {(fromCcd ? "CCD Trigger" : "Cycle Complete")}", LogType.Error);

            // 2. If already reset, we can exit, but LOG IT so we know why.
            if (IsCycleResetCompleted)
            {
                _logger.LogInfo("[CycleManager] Reset skipped - already completed.", LogType.Error);
                return;
            }

            // 3. Atomic Lock
            if (Interlocked.Exchange(ref _resetInProgress, 1) == 1)
            {
                _logger.LogInfo("[CycleManager] Reset skipped - another reset is currently in progress.", LogType.Error);
                return;
            }

            try
            {
                ForceResetCycle(fromCcd);
                IsCycleResetCompleted = true; // Set AFTER successful reset
            }
            finally
            {
                Interlocked.Exchange(ref _resetInProgress, 0);
            }
        }

       
        private void ForceResetCycle(bool ccdReset = false)
        {
            try
            {
                _logger.LogInfo("[CycleManager] Executing ForceResetCycle...", LogType.Diagnostics);

                // 1. CRITICAL: Clear State Variables IMMEDIATELY
                _activeBatchId = string.Empty;
                _currentSequenceStep = 0;
                // 2. Reset PLC bits (Fire and Forget to avoid blocking)
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await WriteToPlc(ConstantValues.Return_TAG_ID, 0);
                        await WriteToPlc(ConstantValues.Ext_DataReady, 0);
                        await WriteToPlc(ConstantValues.MACMINI_NOTCONNECTED, 0);
                        await WriteTagAsync(); // Reset Ack
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"[CycleManager] PLC Reset Error: {ex.Message}", LogType.Diagnostics);
                    }
                });

                // 3. Abort AE Limit
                //if ( _extService.Settings.IsMacMiniEnabled)
                //{
                //}
                    _aeLimitService.AbortCycle();

                // 4. File Cleanup (Can be slow, do last)
                string folder = Path.GetDirectoryName(_stateFilePath);
                if (Directory.Exists(_tempImageFolderPath))
                {
                    foreach (var file in Directory.GetFiles(_tempImageFolderPath))
                    {
                        try { File.Delete(file); } catch { }
                    }
                }
                if (Directory.Exists(folder))
                {
                    // Delete all files
                    foreach (var file in Directory.GetFiles(folder))
                    {
                        try { File.Delete(file); } catch { }
                    }
                }
                    Console.WriteLine("[System] Cycle Reset.");
                _logger.LogError("[System] Cycle Reset — Folder cleared completely.", LogType.Error);
            }
            catch (Exception ex)
            {
                _logger.LogError($"[CycleManager] Reset Error: {ex.Message}", LogType.Diagnostics);
            }
        }


    }
}