using IPCSoftware.Core.Interfaces;
using IPCSoftware.Core.Interfaces.AppLoggerInterface;
using IPCSoftware.Core.Interfaces.CCD;
using IPCSoftware.Services;
using IPCSoftware.Shared;
using IPCSoftware.Shared.Models;
using IPCSoftware.Shared.Models.ConfigModels;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IPCSoftware.CoreService.Services.CCD
{
    public class CycleManagerService : BaseService, ICycleManagerService
    {
        private readonly ProductionImageService _imageService;
        private readonly IServoCalibrationService _servoService; // Injected
        //private readonly IConfiguration _configuration;

        // State Variables
        private string _activeBatchId = string.Empty;

        // This tracks the "Step Number" (0, 1, 2, 3...) not the Station ID
        private int _currentSequenceStep = 0;

        // Path to the JSON file shared with UI
        private readonly string _stateFilePath;

       
        private int[] _stationMap;


        public CycleManagerService(
            IOptions<CcdSettings> ccdSettng, 
            IServoCalibrationService servoService,
            ProductionImageService imageService,
            IAppLogger logger) : base(logger)
        {
            var ccd = ccdSettng.Value;
            _imageService = imageService;
            _servoService = servoService;   
            _stateFilePath = Path.Combine(ccd.QrCodeImagePath, ccd.CurrentCycleStateFileName);
            // _stateFilePath = Path.Combine(ConstantValues.QrCodeImagePath, "CurrentCycleState.json");
            _ = LoadStationMapAsync();
        }

        private async Task LoadStationMapAsync()
        {
            try
            {
                // Use the Service - Single Source of Truth
                var positions = await _servoService.LoadPositionsAsync();

                if (positions != null && positions.Count > 0)
                {
                    // Filter out Home (0) and sort by SequenceIndex (1 to 12)
                    _stationMap = positions
                        .Where(p => p.PositionId != 0 && p.SequenceIndex > 0)
                        .OrderBy(p => p.PositionId)
                        .Select(p => p.SequenceIndex)
                        .ToArray();

                    Console.WriteLine($"[CycleManager] Loaded sequence: {string.Join("->", _stationMap)}");
                }
            }
            catch (Exception ex)
            {
        
                _logger.LogError($"[CycleManager] Error loading sequence: {ex.Message}", LogType.Diagnostics);
                // Absolute fallback if Service fails entirely
                _stationMap = new int[] { 1, 2, 3, 6, 5, 4, 7, 8, 9, 12, 11, 10 };
            }
        }

        public void HandleIncomingData(string tempImagePath, Dictionary<string, object> stationData, string qrString = null)
        {
            // Reload map at start of cycle to ensure we have latest config if it changed
            if (_currentSequenceStep == 0 && string.IsNullOrEmpty(_activeBatchId))
            {
                _ = LoadStationMapAsync();
            }
            // CASE 1: Start of Cycle (QR Scan)
            if (string.IsNullOrEmpty(_activeBatchId) )
            {
                if (!string.IsNullOrEmpty(qrString))
                {
                    StartNewCycle(tempImagePath, qrString);
                }
            }
            // CASE 2: Inspection Steps
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

                // 1. Reset State
                _activeBatchId = qrString;
                _currentSequenceStep = 0;

                // 2. Clear old JSON file if exists
                if (File.Exists(_stateFilePath)) File.Delete(_stateFilePath);

                // 3. Process Station 0 Image (QR Image)
                string destPath = _imageService.ProcessAndMoveImage(tempImagePath, _activeBatchId, 0, true);

                // 4. Create Initial JSON State
                UpdateJsonState(0, destPath, "OK", 0, 0, 0);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message, LogType.Diagnostics);
            }
        }


        private void HandleInspectionStep(string tempImagePath, Dictionary<string, object> data)
        {
            try
            {
                if (_stationMap == null || _stationMap.Length == 0)
                {
                    Console.WriteLine("[Error] Station Map not loaded.");
                    return;
                }
                if (_currentSequenceStep >= _stationMap.Length)
                {
                    ForceResetCycle();
                    return;
                }

                int physicalStationId = _stationMap[_currentSequenceStep];
                Console.WriteLine($"--- PROCESSING STATION {physicalStationId} ---");
                _logger.LogInfo($"--- PROCESSING STATION {physicalStationId} ---", LogType.Diagnostics);

                // 1. Extract Data
                double x = data.ContainsKey("X") ? Convert.ToDouble(data["X"]) : 0.0;
                double y = data.ContainsKey("Y") ? Convert.ToDouble(data["Y"]) : 0.0;
                double z = data.ContainsKey("Z") ? Convert.ToDouble(data["Z"]) : 0.0;
                string status = data.ContainsKey("Status") ? data["Status"].ToString() : "OK";

                // 2. Process Image
                string destUiPath = _imageService.ProcessAndMoveImage(tempImagePath, _activeBatchId, physicalStationId);

                // 3. Update JSON State
                UpdateJsonState(physicalStationId, destUiPath, status, x, y, z);

                // 4. Advance Step
                _currentSequenceStep++;

                // 5. Check Cycle Complete
                if (_currentSequenceStep >= _stationMap.Length)
                {
                    Console.WriteLine("--- CYCLE COMPLETE. RESETTING IN 1s ---");
                    _logger.LogInfo("--- CYCLE COMPLETE. RESETTING IN 1s ---", LogType.Diagnostics);
                    // Run background task to reset so we don't block the ACK
                    Task.Run(async () =>
                    {
                        await Task.Delay(1500); // Wait 1.5s (allow UI to see last result)
                        ForceResetCycle();
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message, LogType.Diagnostics);
            }
        }


        private void UpdateJsonState(int stationNo, string imgPath, string status, double x, double y, double z)
        {
            try
            {
                CycleStateModel state;

                // Load existing or create new
                if (File.Exists(_stateFilePath))
                {
                    string json = File.ReadAllText(_stateFilePath);
                    state = JsonConvert.DeserializeObject<CycleStateModel>(json) ?? new CycleStateModel();
                }
                else
                {
                    state = new CycleStateModel { BatchId = _activeBatchId };
                }

                // Update/Add Station Data
                if (stationNo == 0)
                {
                    state.BatchId = _activeBatchId; // Ensure batch ID is set

                    state.Stations[stationNo] = new StationResult
                    {
                        StationNumber = stationNo,
                        ImagePath = imgPath,
                        Timestamp = DateTime.Now
                    };
                }
                else
                {
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

                // Write back
                string output = JsonConvert.SerializeObject(state, Formatting.Indented);
                File.WriteAllText(_stateFilePath, output);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating JSON state: {ex.Message}");
                _logger.LogError($"Error updating JSON state: {ex.Message}", LogType.Diagnostics);
            }
        }


        public void ForceResetCycle()
        {
            try
            {
                _activeBatchId = string.Empty;
                _currentSequenceStep = 0;

                string folder = Path.GetDirectoryName(_stateFilePath);  //ConstantValues.QrCodeImagePath;

                if (Directory.Exists(folder))
                {
                    // Delete all files
                    foreach (var file in Directory.GetFiles(folder))
                    {
                        File.Delete(file);
                    }
                }

                Console.WriteLine("[System] Cycle Reset — Folder cleared completely.");
                _logger.LogError("[System] Cycle Reset — Folder cleared completely." , LogType.Diagnostics  );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message, LogType.Diagnostics);
            }
        }

    }
}
