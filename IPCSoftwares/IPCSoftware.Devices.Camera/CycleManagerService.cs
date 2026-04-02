using IPCSoftware.Core.Interfaces;
using IPCSoftware.Core.Interfaces.AppLoggerInterface;
using IPCSoftware.Core.Interfaces.CCD;              // ✅ IExternalInterfaceService is here now
using IPCSoftware.Devices.PLC;
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
using System.Security.Cryptography;
using System.Threading.Tasks;
// ✅ REMOVED: using IPCSoftware.Communication.External

namespace IPCSoftware.Devices.Camera
{
    public class CycleManagerServiceBase : BaseService, ICycleManagerService
    {
        protected readonly IPLCTagConfigurationService _tagService;
        protected readonly PLCClientManager _plcManager;
        protected readonly ProductionImageService _imageService;
        protected readonly IServoCalibrationService _servoService;
        protected readonly IExternalInterfaceService _extService;  // ✅ interface
        protected readonly IAeLimitService _aeLimitService;
        protected readonly IProductConfigurationService _productService;

        protected string _activeBatchId = string.Empty;
        protected int _currentSequenceStep = 0;
        protected readonly string _stateFilePath;
        protected readonly string _quarantinePath;
        protected readonly string _imageBaseOutputPath;
        protected int[] _stationMap;
        public bool IsCycleResetCompleted { get; protected set; }
        private string _tempImageFolderPath;



        public CycleManagerServiceBase(
            IPLCTagConfigurationService tagService,
            ILogConfigurationService logConfig,
            PLCClientManager plcManager,
            IOptions<CcdSettings> appSettings,
            IServoCalibrationService servoService,
            ProductionImageService imageService,
            IExternalInterfaceService extService,   // ✅ interface
            IAeLimitService aeLimitService,
            IProductConfigurationService productService,
            IAppLogger logger) : base(logger)
        {
            var ccd = appSettings.Value;
            _tempImageFolderPath = ccd.TempImgFolder;
            _tagService = tagService;
            _plcManager = plcManager;
            _imageService = imageService;
            _servoService = servoService;
            _extService = extService;
            _aeLimitService = aeLimitService;
            _productService = productService;
            _stateFilePath = Path.Combine(ccd.QrCodeImagePath, ccd.CurrentCycleStateFileName);
            var logs =  logConfig.GetAllAsync();
            var allLogs = logConfig.GetAllAsync().GetAwaiter().GetResult();
            var config = allLogs.FirstOrDefault(c => c.LogType == LogType.Production);
            var basePath = config.ProductionImagePath;

            _imageBaseOutputPath = basePath;
            _quarantinePath = Path.Combine(basePath, "Quarantine");
            if (!Directory.Exists(_quarantinePath)) Directory.CreateDirectory(_quarantinePath);

            _ = LoadStationMapAsync();
        }

        virtual protected async Task LoadStationMapAsync()
        {
           
        }

        virtual protected async Task SyncTotalStationsToPlc(int totalItems)
        {
            
        }

        /*        private async Task LoadStationMapAsync()
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
                }*/

        public virtual async Task HandleIncomingData(string tempImagePath, Dictionary<string, object> stationData, string qrString = null)
        {
            
        }

        virtual protected async Task StartNewCycle(string tempImagePath, string qrString)
        {
            
        }



        virtual protected void InitializeCycleStateWithExternalStatus()
        {
            
        }

        virtual protected async Task HandleInspectionStep(string tempImagePath, Dictionary<string, object> data)
        {
            
        }

        virtual protected void UpdateJsonEntry(int stationNo, string imgPath, string status, double x, double y, double z)
        {
            
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


        private int _resetInProgress = 0;

       


        public void RequestReset(bool fromCcd = false)
        {
            // 1. Log who is requesting
            _logger.LogInfo($"[CycleManager] Reset Requested. Source: {(fromCcd ? "CCD Trigger" : "Cycle Complete")}", LogType.Error);

            // 2. If already reset, we can exit, but LOG IT so we know why.
            if (IsCycleResetCompleted)
            {
                _logger.LogInfo("[CycleManager] Reset skipped - already completed.", LogType.Error);
                return;
            }

            // 3. Atomic Lock
            if (Interlocked.Exchange(ref _resetInProgress, 1) == 1)
            {
                _logger.LogInfo("[CycleManager] Reset skipped - another reset is currently in progress.", LogType.Error);
                return;
            }

            try
            {
                ForceResetCycle(fromCcd);
                IsCycleResetCompleted = true; // Set AFTER successful reset
            }
            finally
            {
                Interlocked.Exchange(ref _resetInProgress, 0);
            }
        }

       
        private void ForceResetCycle(bool ccdReset = false)
        {
            try
            {
                _logger.LogInfo("[CycleManager] Executing ForceResetCycle...", LogType.Diagnostics);

                // 1. CRITICAL: Clear State Variables IMMEDIATELY
                _activeBatchId = string.Empty;
                _currentSequenceStep = 0;
                // 2. Reset PLC bits (Fire and Forget to avoid blocking)
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await WriteToPlc(ConstantValues.Return_TAG_ID, 0);
                        await WriteToPlc(ConstantValues.Ext_DataReady, 0);
                        await WriteToPlc(ConstantValues.MACMINI_NOTCONNECTED, 0);
                        await WriteTagAsync(); // Reset Ack
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"[CycleManager] PLC Reset Error: {ex.Message}", LogType.Diagnostics);
                    }
                });

                // 3. Abort AE Limit
                //if ( _extService.Settings.IsMacMiniEnabled)
                //{
                //}
                    _aeLimitService.AbortCycle();

                // 4. File Cleanup (Can be slow, do last)
                string folder = Path.GetDirectoryName(_stateFilePath);
                if (Directory.Exists(_tempImageFolderPath))
                {
                    foreach (var file in Directory.GetFiles(_tempImageFolderPath))
                    {
                        try { File.Delete(file); } catch { }
                    }
                }
                if (Directory.Exists(folder))
                {
                    // Delete all files
                    foreach (var file in Directory.GetFiles(folder))
                    {
                        try { File.Delete(file); } catch { }
                    }
                }
                    Console.WriteLine("[System] Cycle Reset.");
                _logger.LogError("[System] Cycle Reset — Folder cleared completely.", LogType.Error);
            }
            catch (Exception ex)
            {
                _logger.LogError($"[CycleManager] Reset Error: {ex.Message}", LogType.Diagnostics);
            }
        }


    }
}