using IPCSoftware.Core.Interfaces;
using IPCSoftware.Core.Interfaces.AppLoggerInterface;
using IPCSoftware.Datalogger;
using IPCSoftware.Devices.PLC;
using IPCSoftware.Engine;
using IPCSoftware.Shared;
using IPCSoftware.Shared.Models;
using IPCSoftware.Shared.Models.ConfigModels;
using IPCSoftware.Shared.Models.Logging;
using Microsoft.Extensions.Configuration;

namespace IPCSoftware.CoreService.Bending.Service
{
    public class OeeEngineBending : OeeEngineBase
    {
        public OeeEngineBending(
            IDeviceConfigurationService deviceService,
            PLCClientManager plcManager,
            IAppLogger logger,
            IProductionDataLogger prodLogger,
            IConfiguration configuration)
            : base(deviceService, plcManager, logger, prodLogger, configuration)
        {

        }

        // Override Calculate() so that Request ID 4 returns Bending OEE (Dashboard2Result)
        // matching the same contract as AOI — both use key 4 in the response dictionary.
        //public override Dictionary<int, object> Calculate(Dictionary<int, object> values)
        //{
        //    // CalculateDashboard2 now returns { 4, Dashboard2Result } directly.
        //    return CalculateDashboard2(values);
        //}
    }
}