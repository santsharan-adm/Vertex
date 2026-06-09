using IPCSoftware.Core.Interfaces;
using IPCSoftware.Core.Interfaces.AppLoggerInterface;
using IPCSoftware.Communication.External;
using IPCSoftware.Devices.PLC;
using IPCSoftware.Services;
using IPCSoftware.Shared.Models;
using IPCSoftware.Shared.Models.ConfigModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IPCSoftware.Engine
{
    public class SystemMonitorServiceBase : BaseService
    {
        private readonly PLCClientManager _plcManager;
        private readonly IDeviceConfigurationService _deviceService;
        protected readonly ExternalInterfaceService _extService;

       

        // --- CONFIGURATION ---
        // Description says: "abnormal if no change for 3 s"
        protected const double HEARTBEAT_TIMEOUT_SECONDS = 3.0;
        protected const double IPC_TOGGLE_INTERVAL_SECONDS = 1.0;
        protected const double READ_TIMEOUT_SECONDS = 3.0;

        public SystemMonitorServiceBase(
            PLCClientManager plcManager,
            IDeviceConfigurationService deviceService,
            ExternalInterfaceService extService,
            IAppLogger logger) : base(logger)
        {
            _plcManager = plcManager;
            _deviceService = deviceService;
            _extService = extService;

            
        }

        /// <summary>
        /// Processes Heartbeat Logic Only.
        /// 1. Monitors PLC Pulse (PLC -> IPC)
        /// 2. Sends IPC Pulse (IPC -> PLC)
        /// </summary>
        public virtual Dictionary<int, object> Process(Dictionary<int, object> tagValues)
        {
                return new Dictionary<int, object> {  };            
        }

        protected bool GetBool(Dictionary<int, object> values, int tagId)
        {
            if (values != null && values.TryGetValue(tagId, out object val))
            {
                if (val is bool b) return b;
                if (val is int i) return i > 0;
                if (val is short s) return s > 0;
                if (val is string str)
                {
                    if (bool.TryParse(str, out bool result)) return result;
                    if (int.TryParse(str, out int intResult)) return intResult > 0;
                }
            }
            return false;
        }

        protected async Task WriteTagAsync(int tagNo, object value)
        {
            try
            {
                // Retrieve Tag Info
                var allTags = await _deviceService.GetAllTagsAsync();
                var tag = allTags.FirstOrDefault(t => t.Id == tagNo);

                if (tag == null) return;

                // Get Client
                var client = _plcManager.GetClient(tag.PLCNo);
                if (client != null && client.IsConnected)
                {
                    await client.WriteAsync(tag, value);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"[SystemMonitor] Write Error Tag {tagNo}: {ex.Message}", LogType.Diagnostics);
            }
        }

        protected virtual void ResetHeartbeat()
        {
            
        }
    }
}