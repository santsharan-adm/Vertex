using IPCSoftware.Core.Interfaces;
using IPCSoftware.Core.Interfaces.AppLoggerInterface;
using IPCSoftware.Core.Interfaces.CCD;
using IPCSoftware.Devices.Camera;
using IPCSoftware.Devices.PLC;
using IPCSoftware.Services.ConfigServices;
using IPCSoftware.Shared.Models;
using IPCSoftware.Shared.Models.ConfigModels;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IPCSoftware.CoreService.AOI.Service
{
    public class CCDTriggerServiceAOI : CCDTriggerServiceBase
    {
        public CCDTriggerServiceAOI(
            ICycleManagerService cycleManager,
            IPLCTagConfigurationService tagService,
            IOptions<CcdSettings> ccdSettings,
            IObservableCcdSettingsService observableCcdSettings,  // ✅ NEW: Added observable settings
            IAppLogger logger) : base(cycleManager, tagService, ccdSettings, observableCcdSettings, logger)
        {
        }

        override public async Task ProcessTriggers(Dictionary<int, object> tagValues, PLCClientManager manager)
        {
            base.ProcessTriggers(tagValues, manager);
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
                if (!isCycleEnabled && _lastCycleStartState)
                {
                    _logger.LogInfo("[CCD] Clearing Bits which was on during cycle.", LogType.Error);

                    await WriteToPlc(ConstantValues.Return_TAG_ID, 0);
                    await WriteToPlc(ConstantValues.Ext_DataReady, 0);
                    await WriteToPlc(ConstantValues.MACMINI_NOTCONNECTED, 0);
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
        override protected async Task WriteAckToPlcAsync(bool writebool)
        {
            base.WriteAckToPlcAsync(writebool);
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
                        if (writebool)
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
                        //    _logger.LogInfo(`[CCD] Ack sent {0} by timer  to Tag {ConstantValues.Return_TAG_ID}`, LogType.Error);

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
    }
}