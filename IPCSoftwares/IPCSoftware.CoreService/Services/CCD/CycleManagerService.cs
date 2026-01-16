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

        private string _activeBatchId = string.Empty;
        private int _currentSequenceStep = 0;
        private readonly string _stateFilePath;
        private readonly string _quarantinePath;
        private readonly string _imageBaseOutputPath;
        private int[] _stationMap;
        public bool IsCycleResetCompleted { get; private set; }


        public CycleManagerService(
            IPLCTagConfigurationService tagService,
            ILogConfigurationService logConfig,
            PLCClientManager plcManager,
            IOptions<CcdSettings> appSettings,
            IServoCalibrationService servoService,
            ProductionImageService imageService,
            ExternalInterfaceService extService,
            IAeLimitService aeLimitService,
            IAppLogger logger) : base(logger)
        {
            var ccd = appSettings.Value;
            _tagService = tagService;
            _plcManager = plcManager;
            _imageService = imageService;
            _servoService = servoService;
            _extService = extService;
            _aeLimitService = aeLimitService;

            _stateFilePath = Path.Combine(ccd.QrCodeImagePath, ccd.CurrentCycleStateFileName);
            var logs =  logConfig.GetAllAsync();
            var allLogs = logConfig.GetAllAsync().GetAwaiter().GetResult();
            var config = allLogs.FirstOrDefault(c => c.LogType == LogType.Production);
            var baseProductionPath = config.DataFolder;
            string baseOut = ccd.ImageFolderName;
            var basePath = Path.Combine(baseProductionPath, baseOut);
            _imageBaseOutputPath = basePath;
            _quarantinePath = Path.Combine(basePath, "Quarantine");
            if (!Directory.Exists(_quarantinePath)) Directory.CreateDirectory(_quarantinePath);

            _ = LoadStationMapAsync();
        }

        private async Task LoadStationMapAsync()
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
        }

        public void HandleIncomingData(string tempImagePath, Dictionary<string, object> stationData, string qrString = null)
        {
            if (_currentSequenceStep == 0 && string.IsNullOrEmpty(_activeBatchId)) _ = LoadStationMapAsync();

            if (string.IsNullOrEmpty(_activeBatchId))
            {
                if (!string.IsNullOrEmpty(qrString)) StartNewCycle(tempImagePath, qrString);
                IsCycleResetCompleted = false;
            }
            else
            {
                HandleInspectionStep(tempImagePath, stationData);
            }
        }

        private void StartNewCycle(string tempImagePath, string qrString)
        {
            try
            {
                Console.WriteLine($"--- NEW CYCLE START: {qrString} ---");
                _logger.LogInfo($"--- NEW CYCLE START: {qrString} ---", LogType.Diagnostics);

                _activeBatchId = qrString;
                _currentSequenceStep = 0;
                _aeLimitService.BeginCycle(_activeBatchId, qrString);

                // 1. SYNC WITH MAC MINI
                // This fetches the JSON, maps it, writes to PLC, and updates internal flags
                // We use .Result or Wait() here cautiously because this is inside a sync void method called by TriggerService
                // Ideally trigger service awaits this.
                _extService.SyncBatchStatusAsync(qrString).Wait();

                if (File.Exists(_stateFilePath)) File.Delete(_stateFilePath);

                // 2. Initialize JSON State with Pre-Calculated NG status
                // This ensures UI shows Red immediately for NG parts
                InitializeCycleStateWithExternalStatus();

                // 3. Process QR Image
                string destPath = _imageService.ProcessAndMoveImage(tempImagePath, _imageBaseOutputPath, _activeBatchId, 0, 0, 0, 0, true);
                // Update QR entry in JSON (Station 0)
                UpdateJsonEntry(0, destPath, "OK", 0, 0, 0);
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
            catch (Exception ex) { _logger.LogError(ex.Message, LogType.Diagnostics); }
        }



        private void InitializeCycleStateWithExternalStatus()
        {
            try
            {
                var state = new CycleStateModel { BatchId = _activeBatchId, LastUpdated = DateTime.Now };

                // Populate placeholders for all 12 stations based on External Status
                for (int i = 0; i < 12; i++)
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

        private void HandleInspectionStep(string tempImagePath, Dictionary<string, object> data)
        {
            try
            {
                if (_stationMap == null || _stationMap.Length == 0) return;
                if (_currentSequenceStep >= _stationMap.Length) { IsCycleResetCompleted = true; ForceResetCycle(); return; }

                int physicalStationId = _stationMap[_currentSequenceStep];
                Console.WriteLine($"--- PROCESSING STATION {physicalStationId} (Seq {_currentSequenceStep}) ---");

                double x = data.ContainsKey("X") ? Convert.ToDouble(data["X"]) : 0.0;
                double y = data.ContainsKey("Y") ? Convert.ToDouble(data["Y"]) : 0.0;
                double z = data.ContainsKey("Z") ? Convert.ToDouble(data["Z"]) : 0.0;
                string status = data.ContainsKey("Status") ? data["Status"].ToString() : "OK";

                // 2. CHECK STATUS (Using Sequence Step Index)
                bool isExternalNg = _extService.IsSequenceRestricted(_currentSequenceStep);
                string destUiPath;

                if (isExternalNg)
                {
                    status = "NG";
                    _logger.LogWarning($"[Cycle] Seq {_currentSequenceStep} (Stn {physicalStationId}) quarantined.", LogType.Production);

                    // Move to Quarantine
                    string fileName = Path.GetFileName(tempImagePath);
                    if (!Directory.Exists(_quarantinePath)) Directory.CreateDirectory(_quarantinePath);
                    string destFile = Path.Combine(_quarantinePath, $"{DateTime.Now:yyyyMMdd_HHmmss}_{fileName}");
                    File.Move(tempImagePath, destFile);
                    destUiPath = string.Empty;
                }
                else
                {
                    destUiPath = _imageService.ProcessAndMoveImage(tempImagePath, _imageBaseOutputPath, _activeBatchId, physicalStationId, x, y, z);
                }

                UpdateJsonEntry(physicalStationId, destUiPath, status, x, y, z);
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

                _currentSequenceStep++;

                if (_currentSequenceStep >= _stationMap.Length)
                {
                    Console.WriteLine("--- CYCLE COMPLETE ---");
                    _ = _aeLimitService.CompleteCycleAsync();
                    Task.Run(async () => { await Task.Delay(100); IsCycleResetCompleted = true; ForceResetCycle(); });
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

        public void ForceResetCycle(bool ccdReset = false)
        {
            try
            {
                _activeBatchId = string.Empty;
                _currentSequenceStep = 0;
                _aeLimitService.AbortCycle();

             
                string folder = Path.GetDirectoryName(_stateFilePath);


                if (Directory.Exists(folder))
                {
                    // Delete all files
                    foreach (var file in Directory.GetFiles(folder))
                    {
                        File.Delete(file);
                    }
                    WriteToPlc(ConstantValues.Return_TAG_ID, 0);
                     WriteToPlc(ConstantValues.Ext_DataReady, 0);
                      
                    WriteTagAsync(); // Reset Ack
                    Console.WriteLine("[System] Cycle Reset.");

                    _logger.LogError("[System] Cycle Reset — Folder cleared completely.", LogType.Error);
                    if (ccdReset)
                    {

                    _logger.LogError("[CycleManager] Cycle Reset — By CCD Service .", LogType.Error);
                    }
                }
            }
            catch (Exception ex) { _logger.LogError(ex.Message, LogType.Diagnostics); }
        }
    }
}