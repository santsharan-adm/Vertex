using IPCSoftware.Core.Interfaces;
using IPCSoftware.Core.Interfaces.CCD;
using IPCSoftware.CoreService.Services.PLC;
using IPCSoftware.Services.AppLoggerServices;
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
        private const string TEMP_IMAGE_FOLDER = @"D:\Repos\Vertex\IPCSoftwares\CCD";
        private const int TRIGGER_TAG_ID = 15;
        private const int QR_DATA_TAG_ID = 16;

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

            if (tagValues.TryGetValue(TRIGGER_TAG_ID, out object triggerObj))
            {
                if (triggerObj is bool bVal) currentTriggerState = bVal;
                else if (triggerObj is int iVal) currentTriggerState = iVal > 0;
            }

            // 2. Rising Edge Detection (False -> True)
            if (currentTriggerState && !_lastTriggerState)
            {
                // 3. Extract QR Code (Tag 16) - needed if this is the start of a cycle
                string currentQrCode = string.Empty;
                if (tagValues.TryGetValue(QR_DATA_TAG_ID, out object qrObj))
                {
                    currentQrCode = qrObj?.ToString() ?? string.Empty;
                }

                Console.WriteLine($"[CCD Monitor] Rising Edge Detected on Tag {TRIGGER_TAG_ID}.");
                _ = ExecuteWorkflowAsync(currentQrCode);
            }

            // 4. Update State
            _lastTriggerState = currentTriggerState;
     

        }

        private async Task ExecuteWorkflowAsync(string qrCode)
        {
            string? imagePath = null;
            DateTime startTime = DateTime.Now;

            // 1. WAIT FOR IMAGE
            while ((DateTime.Now - startTime).TotalSeconds < IMAGE_WAIT_TIMEOUT_SECONDS)
            {
                imagePath = GetLatestImageFromTemp();

                if (!string.IsNullOrEmpty(imagePath) && IsFileReady(imagePath))
                {
                    Console.WriteLine($"[CCD Monitor] Image found: {Path.GetFileName(imagePath)}");
                    break;
                }
                await Task.Delay(POLLING_INTERVAL_MS);
            }

            if (!string.IsNullOrEmpty(imagePath))
            {
                // 2. PROCESS IMAGE
                try
                {
                    _cycleManager.HandleIncomingImage(imagePath, qrCode);
                    Console.WriteLine("[CCD Monitor] Image Processed Successfully.");

                    // 3. WRITE BACK TO PLC (Reset Bit to 0)
                    // This happens ONLY after processing is done.
                    var allTags = _tagService.GetAllTagsAsync().GetAwaiter().GetResult();
                    var _triggerTag = allTags.FirstOrDefault(t => t.Id == TRIGGER_TAG_ID);
                    if (_triggerTag != null)
                    {
                        var client = _plcManager.GetClient(_triggerTag.PLCNo);
                        if (client != null)
                        {
                            // Write FALSE (0) to Tag 15
                            await client.WriteAsync(_triggerTag, false);
                            Console.WriteLine("[CCD Monitor] PLC Trigger Bit Reset to 0.");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[CCD Monitor] Processing Error: {ex.Message}");
                }
            }
            else
            {
                Console.WriteLine($"[Error] Trigger received, but NO IMAGE appeared in folder.");
            }
        }



        private string GetLatestImageFromTemp()
        {
            try
            {
                if (!Directory.Exists(TEMP_IMAGE_FOLDER)) return null;

                var directory = new DirectoryInfo(TEMP_IMAGE_FOLDER);

                // Get the most recent BMP file
                var latestFile = directory.GetFiles("*.bmp")
                                          .OrderByDescending(f => f.LastWriteTime)
                                          .FirstOrDefault();

                return latestFile?.FullName;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Error] Failed to find image: {ex.Message}");
                return null;
            }
        }


        // Helper to ensure the camera is finished writing the file
        private bool IsFileReady(string filename)
        {
            try
            {
                using (FileStream inputStream = File.Open(filename, FileMode.Open, FileAccess.Read, FileShare.None))
                {
                    if (inputStream.Length > 0)
                    {
                        return true;
                    }
                }
                return false;
            }
            catch (Exception)
            {
                return false; // File is locked by another process (Camera)
            }
        }


    }
}
