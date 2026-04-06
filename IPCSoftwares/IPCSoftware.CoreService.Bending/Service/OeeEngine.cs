using IPCSoftware.Core.Interfaces;
using IPCSoftware.Core.Interfaces.AppLoggerInterface;
using IPCSoftware.Datalogger;
using IPCSoftware.Devices.PLC;
using IPCSoftware.Shared.Models;
using IPCSoftware.Shared.Models.ConfigModels;
using IPCSoftware.Shared.Models.Logging;
using Microsoft.Extensions.Configuration;

namespace IPCSoftware.CoreService.Bending.Service
{
    internal class OeeEngine : IPCSoftware.Engine.OeeEngine
    {
        public OeeEngine(
            IPLCTagConfigurationService tagService,
            PLCClientManager plcManager,
            IAppLogger logger,
            IProductionDataLogger prodLogger,
            IConfiguration configuration)
            : base(tagService, plcManager, logger, prodLogger, configuration)
        {

        }
    }
}