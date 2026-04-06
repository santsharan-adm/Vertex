using IPCSoftware.Core.Interfaces;
using IPCSoftware.Core.Interfaces.AppLoggerInterface;
using IPCSoftware.Datalogger;
using IPCSoftware.Devices.PLC;
using IPCSoftware.Engine;
using IPCSoftware.Shared.Models;
using IPCSoftware.Shared.Models.ConfigModels;
using IPCSoftware.Shared.Models.Logging;
using Microsoft.Extensions.Configuration;
using System.IO;
using System.Text.Json;

namespace IPCSoftware.CoreService.AOI.Service
{
    public class OeeEngineAOI : OeeEngineBase
    {
        public OeeEngineAOI(
            IPLCTagConfigurationService tagService,
            PLCClientManager plcManager,
            IAppLogger logger,
            IProductionDataLogger prodLogger,
            IConfiguration configuration)
            : base(tagService, plcManager, logger, prodLogger, configuration)
        {
        }

        protected override void LoadStationMap()
        {
            try
            {
                var jsonPath = _servoCalibrationPath;
                if (!File.Exists(jsonPath))
                {
                    _logger.LogError($"[OEE] Station positions JSON not found at: {jsonPath}", LogType.Diagnostics);
                    return;
                }
                string json = File.ReadAllText(jsonPath);
                var positions = JsonSerializer.Deserialize<List<ServoPositionModel>>(json);
                if (positions == null || positions.Count == 0)
                {
                    _logger.LogError("[OEE] Station positions JSON is empty or could not be deserialized.", LogType.Diagnostics);
                    return;
                }
                _sequenceToPositionId.Clear();
                foreach (var entry in positions.Where(p => p.SequenceIndex >= 0 && p.PositionId >= 0))
                {
                    _sequenceToPositionId[entry.SequenceIndex] = entry.PositionId;
                }

                _logger.LogInfo("[OEE] Loaded station map successfully.", LogType.Diagnostics);
            }
            catch (Exception ex)
            {
                _logger.LogError($"[OEE] Failed to load station positions JSON: {ex.Message}", LogType.Diagnostics);
            }
        }
    }
}
