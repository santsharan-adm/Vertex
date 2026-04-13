using IPCSoftware.Core.Interfaces;
using IPCSoftware.Core.Interfaces.AppLoggerInterface;
using IPCSoftware.Core.Interfaces.CCD;
using IPCSoftware.Devices.PLC;
using IPCSoftware.Services;
using IPCSoftware.Services.AppLoggerServices;
using IPCSoftware.Services.ConfigServices; //Added Later
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

namespace IPCSoftware.Devices.Camera
{
    public class CCDTriggerServiceBase : BaseService
    {
        protected readonly ICycleManagerService _cycleManager;
        protected PLCClientManager _plcManager;
        protected readonly IPLCTagConfigurationService _tagService;
        protected readonly IObservableCcdSettingsService _observableCcdSettings; //Added by Rishabh - date - 08/04/2026//

        // State tracking
        protected bool _lastTriggerState = false;
        protected bool _lastCycleStartState = false;

        protected readonly SemaphoreSlim _triggerLock = new(1, 1);
        protected volatile bool _isProcessing = false;

        public CCDTriggerServiceBase(
            ICycleManagerService cycleManager,
            IPLCTagConfigurationService tagService,
            IOptions<CcdSettings> ccdSettings,
            IObservableCcdSettingsService observableCcdSettings,  //Added by Rishabh - date - 08/04/2026//
            IAppLogger logger) : base(logger)
        {
            _tagService = tagService;
            _cycleManager = cycleManager;
            _observableCcdSettings = observableCcdSettings;   //Added by Rishabh - date - 08/04/2026//

            if (_observableCcdSettings != null)
            {
                _observableCcdSettings.SettingsChanged += OnCcdSettingsChanged;
                _logger.LogInfo("[CCD] Observable CCD Settings service initialized and subscribed to changes.", LogType.Diagnostics);
            }
        }

        //Added by Rishabh - date - 08/04/2026//
        private void OnCcdSettingsChanged(object sender, CcdSettingsChangedEventArgs e)
        {
            _logger.LogInfo($"[CCD] Setting changed: {e.PropertyName} = {e.NewValue}", LogType.Diagnostics);
        }

        virtual public async Task ProcessTriggers(Dictionary<int, object> tagValues, PLCClientManager manager)
        {

        }

        protected async Task WriteToPlc(int tagId, object value)
        {
            try
            {
                var allTags = await _tagService.GetAllTagsAsync();
                var tagConfig = allTags.FirstOrDefault(t => t.TagNo == tagId);
                if (tagConfig != null && tagConfig.ModbusAddress > 0)
                {
                    var client = _plcManager.GetClient(tagConfig.PLCNo);
                    if (client != null) await client.WriteAsync(tagConfig, value);
                }
            }
            catch (Exception ex) { _logger.LogError($"Ext Write Error ({tagId}): {ex.Message}", LogType.Diagnostics); }
        }

        protected string MapStatus(object rawStatus)
        {
            if (rawStatus == null)
                return "Unchecked";

            switch (rawStatus.ToString())
            {
                case "0": return "Unchecked";
                case "1": return "OK";
                case "2": return "NG";
                default: return "Unchecked";
            }
        }

        protected async Task ExecuteWorkflowAsync(string qrCode, Dictionary<string, object> data)
        {
            var imagePath = await WaitForImageAsync();
            if (!string.IsNullOrEmpty(imagePath))
            {
                await _cycleManager.HandleIncomingData(imagePath, data, qrCode);
                await WriteAckToPlcAsync(true);
            }
            else
            {
                _logger.LogWarning("[CCD] No image within timeout, Waiting for Cycle Timeout trigger.", LogType.Error);
            }
        }

        virtual protected async Task WriteAckToPlcAsync(bool writebool)
        {

        }

        private async Task<string> WaitForImageAsync()
        {
            try
            {
                var pollInterval = TimeSpan.FromMilliseconds(200);
                var quickWindow = TimeSpan.FromSeconds(3);
                var maxWindow = TimeSpan.FromSeconds(5);

                string tempImageFolder = _observableCcdSettings?.TempImgFolder;  //Modfied by Rishabh - date - 08/04/2026//
                if (string.IsNullOrEmpty(tempImageFolder))
                {
                    _logger.LogWarning("[CCD] TempImgFolder is not configured.", LogType.Error);
                    return null;
                }

                DateTime start = DateTime.Now;
                while ((DateTime.Now - start) < maxWindow)
                {
                    if (!Directory.Exists(tempImageFolder))                                     //Modified by Rishabh - date - 08/04/2026//
                    {
                        _logger.LogWarning($"[CCD] Temp image folder missing: {tempImageFolder}", LogType.Diagnostics);
                        return null;
                    }

                    var file = new DirectoryInfo(tempImageFolder)                  //Modified by Rishabh - date - 08/04/2026//
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

                _logger.LogWarning($"[CCD] Image not found in {tempImageFolder} within 5s after trigger.", LogType.Diagnostics);     //Modified by Rishabh - date - 08/04/2026//
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