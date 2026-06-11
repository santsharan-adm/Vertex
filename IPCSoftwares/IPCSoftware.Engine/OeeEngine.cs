using IPCSoftware.Core.Interfaces;
using IPCSoftware.Core.Interfaces.AppLoggerInterface;
using IPCSoftware.Datalogger;
using IPCSoftware.Devices.PLC;
using IPCSoftware.Services;
using IPCSoftware.Shared;
using IPCSoftware.Shared.Models;
using IPCSoftware.Shared.Models.ConfigModels;
using IPCSoftware.Shared.Models.Logging;
using Newtonsoft.Json.Linq;
using System.IO;
using System.Text.Json;
using Microsoft.Extensions.Configuration;

namespace IPCSoftware.Engine
{
    public class OeeEngineBase : BaseService
    {
        private readonly IDeviceConfigurationService _deviceService;
        private readonly PLCClientManager _plcManager;
        protected readonly IProductionDataLogger _prodLogger;

        protected readonly string _servoCalibrationPath;
        protected int _lastCycleTime = 0;

        // Holds all data for the current 2D code / part
        protected ProductionDataRecord? _currentCycleRecord;
        public OeeResult OeeResult = new OeeResult();

        public OeeEngineBase(
            IDeviceConfigurationService deviceService,
            PLCClientManager plcManager,
            IAppLogger logger,
            IProductionDataLogger prodLogger,
            IConfiguration configuration) : base(logger)
        {
            _deviceService = deviceService;
            _plcManager = plcManager;
            _prodLogger = prodLogger;

            var dataFolder = configuration["Config:DataFolder"];
            var servoFileName = configuration["Config:ServoCalibrationFileName"] ?? "ServoCalibration.json";

            if (string.IsNullOrWhiteSpace(dataFolder))
            {
                dataFolder = AppContext.BaseDirectory;
            }

            _servoCalibrationPath = Path.Combine(dataFolder, servoFileName);
            
            // Load the station map once at startup
            LoadStationMap();
        }

        protected virtual void LoadStationMap()
        {
            
        }

        public virtual void ProcessCycleTimeLogic(Dictionary<int, object> tagValues)
        {
            
        }

        protected virtual void HandleCcdTrigger(Dictionary<int, object> tagValues)
        {
            
        }

        /// <summary>
        /// Shared logic to calculate OEE, fill the record, save to CSV, and reset memory.
        /// </summary>
        protected virtual void FinalizeAndLogCycle(Dictionary<int, object> tagValues, bool isCycleComplete)
        {
            
        }
        
        // =========================================================
        // LIVE CALCULATION FOR UI (Called continuously)
        // =========================================================
        public virtual Dictionary<int, object> Calculate(Dictionary<int, object> values)
        {
            try
            {
                if (IsDryRunMode(values))
                {
                    return new Dictionary<int, object> { { 4, new OeeResultAOI() } };
                }

                OeeResultAOI r = new OeeResultAOI();

                // 1. Extract Raw Values
                int operatingMin = GetInt(values, ConstantValues.TAG_UpTime);
                int downTimeMin = GetInt(values, ConstantValues.TAG_DownTime);
                int totalParts = GetInt(values, ConstantValues.TAG_InFlow);
                int okParts = GetInt(values, ConstantValues.TAG_OK);
                int ngParts = GetInt(values, ConstantValues.TAG_NG);
                double idealCycle = ConstantValues.IDEAL_CYCLE_TIME;

                double x = GetDouble(values, ConstantValues.TAG_X);
                double y = GetDouble(values, ConstantValues.TAG_Y);
                double z = GetDouble(values, ConstantValues.TAG_Z);

                double minX = GetDouble(values, ConstantValues.MIN_X.Read);
                double maxX = GetDouble(values, ConstantValues.MAX_X.Read);
                double minY = GetDouble(values, ConstantValues.MIN_Y.Read);
                double maxY = GetDouble(values, ConstantValues.MAX_Y.Read);
                double minZ = GetDouble(values, ConstantValues.MIN_Z.Read);
                double maxZ = GetDouble(values, ConstantValues.MAX_Z.Read);
                


                // 2. Availability (A) Calculation
                double totalTimeMin = operatingMin + downTimeMin;
                r.Availability = 0.0;
                if (totalTimeMin > 0)
                {
                    r.Availability = (double)operatingMin / totalTimeMin;
                }

                // 3. Quality (Q) Calculation
                r.Quality = 0.0;
                if (totalParts > 0)
                {
                    r.Quality = (double)okParts / (double)totalParts;
                }

                // 4. Performance (P) Calculation
                r.Performance = 0.0;
                if (operatingMin > 0 && idealCycle > 0)
                {
                    double operatingSeconds = (double)operatingMin ;
                    if (operatingSeconds > 0)
                    {
                        r.Performance = ((double)idealCycle * totalParts) / operatingSeconds;
                    }
                }

                // 5. Overall OEE
                r.OverallOEE = r.Availability * r.Performance * r.Quality;

                // 6. Raw values pass-through for UI
                r.OKParts = okParts;
                r.NGParts = ngParts;
                r.OperatingTime = operatingMin;
                r.Downtime = downTimeMin;
                r.TotalParts = totalParts;
                r.CycleTime = _lastCycleTime;
                r.XValue = x;
                r.YValue = y;
                r.AngleValue = z;
                r.MinX = minX;
                r.MaxX = maxX;
                r.MinY = minY;
                r.MaxY = maxY;
                r.MinZ = minZ;
                r.MaxZ = maxZ;


                // Return as dictionary with ID 4 (OEE_DATA)
                return new Dictionary<int, object> { { 4, r } };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message, LogType.Diagnostics);
                throw;
            }
        }

        // =========================================================
        // DASHBOARD2 CALCULATION (RequestId = 2)
        // =========================================================
        //public Dictionary<int, object> CalculateDashboard2(Dictionary<int, object> values)
        //{
        //    try
        //    {
        //        // Do not show any values until the cycle start tag has gone HIGH at least once
        //        if (!_cycleEverStarted)
        //            return new Dictionary<int, object> { { 4, new Dashboard2Result() } };

        //        Dashboard2Result r = new Dashboard2Result();

        //        int operatingMin  = GetInt(values, ConstantValues.TAG_UpTime);
        //        int downTimeMin   = GetInt(values, ConstantValues.TAG_DownTime);
        //        int totalParts    = GetInt(values, ConstantValues.TAG_InFlow);
        //        int okParts       = GetInt(values, ConstantValues.TAG_OK);
        //        int ngParts       = GetInt(values, ConstantValues.TAG_NG);
        //        double idealCycle = ConstantValues.IDEAL_CYCLE_TIME;

        //        double totalTimeMin = operatingMin + downTimeMin;
        //        r.Availability = totalTimeMin > 0 ? (double)operatingMin / totalTimeMin : 0.0;
        //        r.Quality      = totalParts   > 0 ? (double)okParts / totalParts        : 0.0;
        //        r.Performance  = (operatingMin > 0 && idealCycle > 0)
        //            ? Math.Min(1.0, (idealCycle * totalParts) / (double)operatingMin)
        //            : 0.0;
        //        r.OverallOEE = r.Availability * r.Performance * r.Quality;

        //        r.OKParts       = okParts;
        //        r.NGParts       = ngParts;
        //        r.OperatingTime = operatingMin ;  // PLC gives minutes → convert to seconds for display
        //        r.Downtime      = downTimeMin ;   // PLC gives minutes → convert to seconds for display
        //        r.CycleTime     = _lastCycleTime;     // already in seconds
        //        r.XValue   = GetDouble(values, ConstantValues.TAG_X);
        //        r.YValue   = GetDouble(values, ConstantValues.TAG_Y);
        //        r.ZValue   = GetDouble(values, ConstantValues.TAG_Z);
        //        r.WValue   = GetDouble(values, ConstantValues.TAG_W);
        //        r.Heat     = GetDouble(values, ConstantValues.TAG_Heat);
        //        r.Punch    = GetDouble(values, ConstantValues.TAG_Punch);
        //        r.Clamp    = GetDouble(values, ConstantValues.TAG_Clamp);
        //        r.Tearing  = GetDouble(values, ConstantValues.TAG_Tearing);
        //        r.Flipping = GetDouble(values, ConstantValues.TAG_Flipping);

        //        return new Dictionary<int, object> { { 4, r } };
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex.Message, LogType.Diagnostics);
        //        throw;
        //    }
        //}

        // =========================================================
        // HELPERS
        // =========================================================



        protected int GetInt(Dictionary<int, object> values, int tagId)
        {
            if (values != null && values.TryGetValue(tagId, out object val))
            {
                try { return Convert.ToInt32(val); }
                catch (Exception ex) { _logger.LogError(ex.Message, LogType.Diagnostics); return 0; }
            }
            return 0;
        }

        protected string GetString(Dictionary<int, object> values, int tagId)
        {
            if (values != null && values.TryGetValue(tagId, out object val) && val != null)
            {
                try { return val.ToString() ?? string.Empty; }
                catch (Exception ex) { _logger.LogError(ex.Message, LogType.Diagnostics); return string.Empty; }
            }
            return string.Empty;
        }

        protected double GetDouble(Dictionary<int, object> values, int tagId)
        {
            if (values != null && values.TryGetValue(tagId, out object val) && val != null)
            {
                try { return Convert.ToDouble(val); }
                catch (Exception ex) { _logger.LogError(ex.Message, LogType.Diagnostics); return 0.0; }
            }
            return 0.0;
        }

        protected bool GetBoolState(Dictionary<int, object> tagValues, int tagId)
        {
            if (tagValues.TryGetValue(tagId, out object obj))
            {
                if (obj is bool bVal) return bVal;
                if (obj is int iVal) return iVal > 0;
            }
            return false;
        }

        protected bool IsDryRunMode(Dictionary<int, object> tagValues)
        {
            return GetBoolState(tagValues, ConstantValues.Mode_DryRun.Read);
        }

        protected virtual void ResetCycleTracking()
        {
            
        }

        protected async Task WriteTagAsync(int tagNo, object value)
        {
            try
            {
                var allTags = await _deviceService.GetAllTagsAsync();
                var tag = allTags.FirstOrDefault(t => t.Id == tagNo);
                if (tag != null)
                {
                    var client = _plcManager.GetClient(tag.PLCNo);
                    if (client != null)
                    {
                        await client.WriteAsync(tag, value);
                        _logger.LogInfo($"[CycleTime] Ack Tag {tagNo} set to {value}", LogType.Diagnostics);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"[Error] CycleTime Write Tag {tagNo}: {ex.Message}", LogType.Diagnostics);
            }
        }

       
    }
}