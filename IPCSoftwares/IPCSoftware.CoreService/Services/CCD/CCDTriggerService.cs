using IPCSoftware.Core.Interfaces;
using IPCSoftware.Core.Interfaces.AppLoggerInterface;
using IPCSoftware.Core.Interfaces.CCD;
using IPCSoftware.CoreService.Services.PLC;
using IPCSoftware.Services;
using IPCSoftware.Services.AppLoggerServices;
using IPCSoftware.Shared.Models;
using IPCSoftware.Shared.Models.ConfigModels;
using IPCSoftware.Shared.Models.Messaging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IPCSoftware.CoreService.Services.CCD
{
    public class CCDTriggerService : BaseService
    {
        private readonly ICycleManagerService _cycleManager;
        private  PLCClientManager _plcManager; // 1. Inject PLC Manager
        private readonly IPLCTagConfigurationService  _tagService; // 2. Store Tag Config
        private readonly string  _tempImageFolder; 
        // State tracking
        private bool _lastTriggerState = false;

 

        public CCDTriggerService
            (ICycleManagerService cycleManager,
            IPLCTagConfigurationService tagService,
            IOptions<CcdSettings> ccdSettings
            ,
            IAppLogger logger) : base(logger)
        {
            var ccd = ccdSettings.Value;
            _tempImageFolder = ccd.TempImgFolder;
            _tagService = tagService;
            _cycleManager = cycleManager;
        }

        /// <summary>
        /// Call this method in your Worker loop immediately after AlgorithmService.Apply returns the data.
        /// </summary>
        /// <param name="tagValues">The dictionary of processed tag values</param>
        public void ProcessTriggers(Dictionary<int, object> tagValues, PLCClientManager manager)
        {
            try
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
                    _logger.LogInfo($"[CCD] Trigger Detected on Tag {ConstantValues.TRIGGER_TAG_ID}", LogType.Diagnostics);

                    // 3. Gather Data
                    string qrCode = tagValues.ContainsKey(ConstantValues.TAG_QR_DATA) ? tagValues[ConstantValues.TAG_QR_DATA]?.ToString() : null;

                    var stationData = new Dictionary<string, object>();

                    // Fetch X, Y, Z, Status from tag dictionary
                  //  stationData["Status"] = tagValues.ContainsKey(ConstantValues.TAG_STATUS) ? tagValues[ConstantValues.TAG_STATUS].ToString() : "OK";
                    var rawStatus = tagValues.ContainsKey(ConstantValues.TAG_STATUS)
                    ? tagValues[ConstantValues.TAG_STATUS]
                    : null;
                    stationData["Status"] = MapStatus(rawStatus);


                    stationData["X"] = tagValues.ContainsKey(ConstantValues.TAG_X) ? tagValues[ConstantValues.TAG_X] : 0.0;
                    stationData["Y"] = tagValues.ContainsKey(ConstantValues.TAG_Y) ? tagValues[ConstantValues.TAG_Y] : 0.0;
                    stationData["Z"] = tagValues.ContainsKey(ConstantValues.TAG_Z) ? tagValues[ConstantValues.TAG_Z] : 0.0;

                    // 4. Execute Async Workflow
                    _ = ExecuteWorkflowAsync(qrCode, stationData);
                }

                // 4. Update State
                _lastTriggerState = currentTriggerState;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message, LogType.Diagnostics);
            }
     

        }
        private string MapStatus(object rawStatus)
        {
            if (rawStatus == null)
                return "Unchecked";

            switch (rawStatus.ToString())
            {
                case "0": return "Unchecked";
                case "1": return "OK";
                case "2": return "NG";
                default: return "Unchecked"; // fallback
            }
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
                    _logger.LogError($"[Error] Processing: {ex.Message}", LogType.Diagnostics);
                }
            }
            else
            {
                Console.WriteLine("[Error] Triggered but No Image found.");
                _logger.LogInfo("[Error] Triggered but No Image found.", LogType.Diagnostics);
            }
        }

        private async Task WriteAckToPlcAsync()
        {
            try
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
                        await client.WriteAsync(ackTag, false);
                        Console.WriteLine($"[CCD] Ack sent to Tag {ConstantValues.Return_TAG_ID}");
                        _logger.LogInfo ($"[CCD] Ack sent to Tag {ConstantValues.Return_TAG_ID}", LogType.Diagnostics);

                        // PLC Logic: When PLC sees 15=True, it will set 10=False. 
                        // Then PLC logic likely resets 15 to False later, or we toggle it.
                        // Assuming Pulse behavior here.
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message, LogType.Diagnostics);
            }
        }

        private async Task<string> WaitForImageAsync()
        {
            try
            {
                DateTime start = DateTime.Now;
                while ((DateTime.Now - start).TotalSeconds < 10)
                {
                    if (!Directory.Exists(_tempImageFolder)) return null;

                    var file = new DirectoryInfo(_tempImageFolder)
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
            catch (Exception ex)
            {
                _logger.LogError(ex.Message, LogType.Diagnostics);
                return null;
            }
        }


    }
}
