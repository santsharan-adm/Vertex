using IPCSoftware.Core.Interfaces;
using IPCSoftware.Core.Interfaces.AppLoggerInterface;
using IPCSoftware.Datalogger;
using IPCSoftware.Devices.PLC;
using IPCSoftware.Engine;
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
    }
}