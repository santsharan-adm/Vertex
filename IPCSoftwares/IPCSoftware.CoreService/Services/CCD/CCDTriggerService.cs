using IPCSoftware.Core.Interfaces;
using IPCSoftware.Core.Interfaces.CCD;
using IPCSoftware.CoreService.Services.PLC;
using IPCSoftware.Services.AppLoggerServices;
using IPCSoftware.Shared.Models;
using IPCSoftware.Shared.Models.ConfigModels;
using IPCSoftware.Shared.Models.Messaging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IPCSoftware.CoreService.Services.CCD
{
    public class CCDTriggerService
    {
        private readonly ICycleManagerService _cycleManager;
        private  PLCClientManager _plcManager; // 1. Inject PLC Manager
        private readonly IPLCTagConfigurationService  _tagService; // 2. Store Tag Config


        // State tracking
        private bool _lastTriggerState = false;

        // Configuration
        private const string TEMP_IMAGE_FOLDER = ConstantValues.TempImgFolder;
        //private const int TRIGGER_TAG_ID = 15;
        //private const int QR_DATA_TAG_ID = 16;

        private const int IMAGE_WAIT_TIMEOUT_SECONDS = 10; // Wait up to 10s
        private const int POLLING_INTERVAL_MS = 500;

        public CCDTriggerService(ICycleManagerService cycleManager,  IPLCTagConfigurationService tagService)
        {
            _tagService = tagService;

            _cycleManager = cycleManager;
         //   _plcManager = plcManager;
           

            // Find the configuration for the Trigger Tag (ID 15) so we can write back to it later
        
        }

        /// <summary>
        /// Call this method in your Worker loop immediately after AlgorithmService.Apply returns the data.
        /// </summary>
        /// <param name="tagValues">The dictionary of processed tag values</param>
        public void ProcessTriggers(Dictionary<int, object> tagValues, PLCClientManager manager)
        {
            _plcManager = manager;
            // 1. Extract Trigger State (Tag 15)
            bool currentTriggerState = false;

            if (tagValues.TryGetValue(ConstantValues.TRIGGER_TAG_ID, out object triggerObj))
            {
                if (triggerObj is bool bVal) currentTriggerState = bVal;
                else if (triggerObj is int iVal) currentTriggerState = iVal > 0;
            }

            // 2. Rising Edge Detection (False -> True)
            if (currentTriggerState && !_lastTriggerState)
            {
                Console.WriteLine($"[CCD] Trigger Detected on Tag {ConstantValues.TRIGGER_TAG_ID}");

                // 3. Gather Data
                string qrCode = tagValues.ContainsKey(ConstantValues.QR_DATA_TAG_ID) ? tagValues[ConstantValues.QR_DATA_TAG_ID]?.ToString() : null;

                var stationData = new Dictionary<string, object>();

                // Fetch X, Y, Z, Status from tag dictionary
                stationData["Status"] = tagValues.ContainsKey(ConstantValues.TAG_STATUS) ? tagValues[ConstantValues.TAG_STATUS].ToString() : "OK";
                stationData["X"] = tagValues.ContainsKey(ConstantValues.TAG_X) ? tagValues[ConstantValues.TAG_X] : 0.0;
                stationData["Y"] = tagValues.ContainsKey(ConstantValues.TAG_Y) ? tagValues[ConstantValues.TAG_Y] : 0.0;
                stationData["Z"] = tagValues.ContainsKey(ConstantValues.TAG_Z) ? tagValues[ConstantValues.TAG_Z] : 0.0;

                // 4. Execute Async Workflow
                _ = ExecuteWorkflowAsync(qrCode, stationData);
            }

            // 4. Update State
            _lastTriggerState = currentTriggerState;
     

        }

        private async Task ExecuteWorkflowAsync(string qrCode, Dictionary<string, object> data)
        {
            string imagePath = await WaitForImageAsync();

            if (!string.IsNullOrEmpty(imagePath))
            {
                try
                {
                    // 1. Hand off to Cycle Manager (Updates JSON & Moves Files)
                    // Note: Update CycleManager Interface to accept Dictionary
                    _cycleManager.HandleIncomingData(imagePath, data, qrCode);

                    // 2. Write Ack (Tag 15) to PLC
                    await WriteAckToPlcAsync();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Error] Processing: {ex.Message}");
                }
            }
            else
            {
                Console.WriteLine("[Error] Triggered but No Image found.");
            }
        }

        private async Task WriteAckToPlcAsync()
        {
            var allTags = await _tagService.GetAllTagsAsync();
            var ackTag = allTags.FirstOrDefault(t => t.TagNo == ConstantValues.Return_TAG_ID); // Look for TagNo 15

            if (ackTag != null)
            {
                var client = _plcManager.GetClient(ackTag.PLCNo);
                if (client != null)
                {
                    // Write TRUE to 15 to tell PLC we are done
                    await client.WriteAsync(ackTag, true);
                    Console.WriteLine($"[CCD] Ack sent to Tag {ConstantValues.Return_TAG_ID}");

                    // PLC Logic: When PLC sees 15=True, it will set 10=False. 
                    // Then PLC logic likely resets 15 to False later, or we toggle it.
                    // Assuming Pulse behavior here.
                }
            }
        }

        private async Task<string> WaitForImageAsync()
        {
            DateTime start = DateTime.Now;
            while ((DateTime.Now - start).TotalSeconds < 10)
            {
                if (!Directory.Exists(TEMP_IMAGE_FOLDER)) return null;

                var file = new DirectoryInfo(TEMP_IMAGE_FOLDER)
                    .GetFiles("*.bmp")
                    .OrderByDescending(f => f.LastWriteTime)
                    .FirstOrDefault();

                // Simple check: Ensure file is not locked
                if (file != null)
                {
                    try
                    {
                        using (var stream = file.Open(FileMode.Open, FileAccess.Read, FileShare.None))
                        {
                            if (stream.Length > 0) return file.FullName;
                        }
                    }
                    catch { /* Locked, retry */ }
                }
                await Task.Delay(200);
            }
            return null;
        }


    }
}
