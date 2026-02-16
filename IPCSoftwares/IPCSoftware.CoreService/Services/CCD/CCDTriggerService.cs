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
using System.Diagnostics;
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
        private bool _lastCycleStartState = false;

        private readonly SemaphoreSlim _triggerLock = new(1, 1);
        private volatile bool _isProcessing = false;



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
        public async Task ProcessTriggers(Dictionary<int, object> tagValues, PLCClientManager manager)
        {
            await _triggerLock.WaitAsync();
            try
            {
              
                _plcManager = manager;

                bool isCycleEnabled = false;

                if (tagValues.TryGetValue(ConstantValues.CYCLE_START_TRIGGER_TAG_ID, out object cycleStartObj))
                {
                    if (cycleStartObj is bool bVal) isCycleEnabled = bVal;
                    else if (cycleStartObj is int iVal) isCycleEnabled = iVal > 0;
                }

              
               
                if (isCycleEnabled && !_lastCycleStartState)
                {
                    if (!_cycleManager.IsCycleResetCompleted)
                    {
                        _logger.LogInfo("[CCD] Cycle Start Falling Edge -> Requesting Reset.", LogType.Error);
                        _ = Task.Run(() => _cycleManager.RequestReset(true));
                    }
                    else
                    {
                        // Add this log to see if it's being skipped intentionally
                        _logger.LogInfo("[CCD] Cycle Start Falling Edge -> Reset already done.", LogType.Error);
                    }
                }


                _lastCycleStartState = isCycleEnabled; 

                if (!isCycleEnabled)
                {
                    if (tagValues.TryGetValue(ConstantValues.Return_TAG_ID, out object returnValue))
                    {
                        if (returnValue is bool bVal)
                        {
                            if (bVal == true)
                            {
                                _ = Task.Run(() => WriteAckToPlcAsync(false));
                             
                                _logger.LogInfo($"Writing B5 to false which was not written flase in previous cycle.", LogType.Error);
                            }
                        }
                    }
                    _lastCycleStartState = false;
                    _lastTriggerState = false;
                    return;
                }

                // IF CYCLE IS NOT ENABLED, STOP HERE. DO NOT PROCESS IMAGE TRIGGERS.
                //if (!isCycleEnabled)
                //{
                //    _lastTriggerState = false;
                //    return;
                //}



                // 1. Extract Trigger State (Tag 15)
                bool currentTriggerState = false;


                if (tagValues.TryGetValue(ConstantValues.TRIGGER_TAG_ID, out object triggerObj))
                {
                    if (triggerObj is bool bVal) currentTriggerState = bVal;
                    else if (triggerObj is int iVal) currentTriggerState = iVal > 0;
                }

              //  _logger.LogInfo($"[CCD] Cycle {currentTriggerState } {_lastTriggerState}  reached at line 122.", LogType.Error);
                // 2. Rising Edge Detection (False -> True)
                if (currentTriggerState && !_lastTriggerState)
                {
                    if (!_isProcessing)
                    {

                        _isProcessing = true;

                        // e.g. 08-12-2025
                        Console.WriteLine($"[CCD] Trigger Detected on Tag {ConstantValues.TRIGGER_TAG_ID}");
                        _logger.LogInfo($"[CCD] Trigger Detected on Tag {ConstantValues.TRIGGER_TAG_ID} {DateTime.Now.ToString("HH-mm-ss-fff")} ", LogType.Error);

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
                        stationData["CycleTime"] = tagValues.ContainsKey(ConstantValues.TAG_CycleTime) ? tagValues[ConstantValues.TAG_CycleTime] : 0.0;

                        _ = Task.Run(async () =>
                        {   
                            try
                            {
                                await ExecuteWorkflowAsync(qrCode, stationData);
                            }
                            finally
                            {
                                _isProcessing = false;
                            }
                        });
                    }
                   
                 

                }
                if (!currentTriggerState && _lastTriggerState)
                {
                   
                    _ = Task.Run(() => WriteAckToPlcAsync(false));
                    //_logger.LogInfo($"[CCD] Cycle {currentTriggerState} {_lastTriggerState}  reached at line 144.", LogType.Error);
                }
               // _logger.LogInfo($"[CCD] Cycle {currentTriggerState} {_lastTriggerState}  reached at line 146.", LogType.Error);
                // 4. Update State
                _lastTriggerState = currentTriggerState;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message, LogType.Diagnostics);
            }
            finally
            {
                _triggerLock.Release();
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
            var imagePath = await WaitForImageAsync();
            if (!string.IsNullOrEmpty(imagePath))
            {
               await  _cycleManager.HandleIncomingData(imagePath, data, qrCode);
                await WriteAckToPlcAsync(true);
            }
            else
            {
                _logger.LogWarning("[CCD] No image within timeout, Wating for Cycle Timeout trigger.", LogType.Error);
               // _lastTriggerState = false;
                //await WriteAckToPlcAsync(true); // or false, depending on PLC contract
            }
        }

       // Stopwatch sp = new Stopwatch();  
        private async Task WriteAckToPlcAsync(bool writebool)
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
                        if (writebool )
                        {
                            await client.WriteAsync(ackTag, true);
                            _logger.LogInfo($"[CCD] Ack sent {1} to Tag {ConstantValues.Return_TAG_ID} {DateTime.Now.ToString("HH-mm-ss-fff")} ", LogType.Error);
                           // sp.Start();
                        }
                        else
                        {
                            await client.WriteAsync(ackTag, false);
                            _logger.LogInfo($"[CCD] Ack sent {0} by Falling Edge  to Tag {ConstantValues.Return_TAG_ID} {DateTime.Now.ToString("HH-mm-ss-fff")}", LogType.Error);
                           // sp.Stop();
                        }
                        //if (sp.Elapsed.TotalMilliseconds > 600)
                        //{
                        //    await client.WriteAsync(ackTag, false);
                        //    _logger.LogInfo($"[CCD] Ack sent {0} by timer  to Tag {ConstantValues.Return_TAG_ID}", LogType.Error);

                        //    sp.Stop();
                        //}
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
                var pollInterval = TimeSpan.FromMilliseconds(200);
                var quickWindow = TimeSpan.FromSeconds( 3);
                var maxWindow = TimeSpan.FromSeconds(5);

                DateTime start = DateTime.Now;
                while ((DateTime.Now - start) < maxWindow)
                {
                    if (!Directory.Exists(_tempImageFolder))
                    {
                        _logger.LogWarning($"[CCD] Temp image folder missing: {_tempImageFolder}", LogType.Diagnostics);
                        return null;
                    }

                    var file = new DirectoryInfo(_tempImageFolder)
                        .GetFiles("*.bmp")
                        .OrderByDescending(f => f.LastWriteTime)
                        .FirstOrDefault();

                    if (file != null)
                    {
                        try
                        {
                            using (var stream = file.Open(FileMode.Open, FileAccess.Read, FileShare.None))
                            {
                                if (stream.Length > 0)
                                    return file.FullName;
                            }
                        }
                        catch
                        {
                            // file still being written, keep polling
                        }
                    }

                    await Task.Delay(pollInterval);

                    if ((DateTime.Now - start) >= quickWindow)
                    {
                        // continue polling until maxWindow but do not block longer than requested
                    }
                }

                _logger.LogWarning($"[CCD] Image not found in {_tempImageFolder} within 5s after trigger.", LogType.Diagnostics);
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
