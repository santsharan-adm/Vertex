using IPCSoftware.Core.Interfaces.CCD;
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
        }

        public void HandleIncomingImage(string tempImagePath, string plcProvidedQrString = null)
        {
            // LOGIC: If BatchID is empty, it means we are at the very start (Home Pos 0)
            if (string.IsNullOrEmpty(_activeBatchId))
            {
                // CASE 1: Station 0 (QR Code Reading)
                HandleQrCodeStep(tempImagePath, plcProvidedQrString);
            }
            else
            {
                // CASE 2: Inspection Grid (Station 1 to 12)
                HandleInspectionStep(tempImagePath);
            }
        }

        private void HandleQrCodeStep(string tempImagePath, string qrString)
        {
            Console.WriteLine("--- NEW CYCLE START (Pos 0 - QR Scan) ---");

            if (string.IsNullOrEmpty(qrString))
            {
                Console.WriteLine("[Error] Image received for Pos 0, but QR String is missing!");
                // Optionally: process as "UNKNOWN_BATCH" or return
                return;
            }

            // 1. Lock in the Batch ID
            _activeBatchId = qrString;

            // 2. Reset Sequence Counter
            _currentSequenceStep = 0;

            // 3. Process Station 0 Image
            Console.WriteLine($"[Logic] Processing QR Image. BatchID: {_activeBatchId}");

            // We pass '0' as station number for the QR code image
            _imageService.ProcessAndMoveImage(tempImagePath, _activeBatchId, 0, true);
        }

        private void HandleInspectionStep(string tempImagePath)
        {
            // 1. Check if we have exceeded the expected number of images
            if (_currentSequenceStep >= _stationMap.Length)
            {
                Console.WriteLine("[Error] Received more images than defined in Station Map! resetting...");
                ForceResetCycle();
                return;
            }

            // 2. Get the ACTUAL Physical Station ID from our Snake Map
            int physicalStationId = _stationMap[_currentSequenceStep];

            Console.WriteLine($"--- INSPECTION: Step {_currentSequenceStep + 1}/12 -> Physical St: {physicalStationId} ---");

            // 3. Process Image
            // We pass the Mapped ID (e.g., 6) not the counter (e.g., 4)
            _imageService.ProcessAndMoveImage(tempImagePath, _activeBatchId, physicalStationId);

            // 4. Advance Sequence
            _currentSequenceStep++;

            // 5. Check for Cycle Completion
            if (_currentSequenceStep >= _stationMap.Length)
            {
                Console.WriteLine("--- CYCLE COMPLETE (Last Station 10 Processed) ---");
                Console.WriteLine("--- Ready for next QR Code ---");

                // Reset for next product
                _activeBatchId = string.Empty;
                _currentSequenceStep = 0;
            }
        }

        public void ForceResetCycle()
        {
            _activeBatchId = string.Empty;
            _currentSequenceStep = 0;
            Console.WriteLine("[System] Cycle Forced Reset.");
        }
    }
}
