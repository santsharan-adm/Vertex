using IPCSoftware.Core.Interfaces.CCD;
using IPCSoftware.Shared;
using IPCSoftware.Shared.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IPCSoftware.CoreService.Services.CCD
{
    public class CycleManagerService : ICycleManagerService
    {
        private readonly ProductionImageService _imageService;

        // State Variables
        private string _activeBatchId = string.Empty;

        // This tracks the "Step Number" (0, 1, 2, 3...) not the Station ID
        private int _currentSequenceStep = 0;

        // Path to the JSON file shared with UI
        private readonly string _stateFilePath;

        // CONFIGURATION: Define your Snake Pattern here
        // Index 0 = First Grid Move, Index 1 = Second Grid Move, etc.
        // Based on your description: 1->2->3 (down) 6->5->4 (down) 7->8->9 (down) 12->11->10
        private readonly int[] _stationMap = new int[]
        {
            1, 2, 3,    // Row 1 (Right)
            6, 5, 4,    // Row 2 (Left)
            7, 8, 9,    // Row 3 (Right)
            12, 11, 10  // Row 4 (Left)
        };

        public CycleManagerService()
        {
            _imageService = new ProductionImageService();
            _stateFilePath = Path.Combine(ConstantValues.QrCodeImagePath, "CurrentCycleState.json");
        }

     

        public void HandleIncomingData(string tempImagePath, Dictionary<string, object> stationData, string qrString = null)
        {
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
            Console.WriteLine($"--- NEW CYCLE START: {qrString} ---");

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


        private void HandleInspectionStep(string tempImagePath, Dictionary<string, object> data)
        {
            if (_currentSequenceStep >= _stationMap.Length)
            {
                ForceResetCycle();
                return;
            }

            int physicalStationId = _stationMap[_currentSequenceStep];
            Console.WriteLine($"--- PROCESSING STATION {physicalStationId} ---");

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
                // Run background task to reset so we don't block the ACK
                Task.Run(async () =>
                {
                    await Task.Delay(1500); // Wait 1.5s (allow UI to see last result)
                    ForceResetCycle();
                });
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
            }
        }


        /*  public void ForceResetCycle()
          {
              _activeBatchId = string.Empty;
              _currentSequenceStep = 0;
              Console.WriteLine("[System] Cycle Forced Reset.");
          }*/

        public void ForceResetCycle()
        {
            _activeBatchId = string.Empty;
            _currentSequenceStep = 0;

            string folder = ConstantValues.QrCodeImagePath;

            if (Directory.Exists(folder))
            {
                // Delete all files
                foreach (var file in Directory.GetFiles(folder))
                {
                    File.Delete(file);
                }

                // (Optional) delete all subdirectories
                foreach (var dir in Directory.GetDirectories(folder))
                {
                    Directory.Delete(dir, true); // true = delete recursively
                }
            }

            Console.WriteLine("[System] Cycle Reset — Folder cleared completely.");
        }

    }
}
