using IPCSoftware.Core.Interfaces;
using IPCSoftware.Core.Interfaces.AppLoggerInterface;
using IPCSoftware.Core.Interfaces.CCD;
using IPCSoftware.Devices.Camera;
using IPCSoftware.Devices.PLC;
using IPCSoftware.Shared.Models;
using IPCSoftware.Shared.Models.ConfigModels;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IPCSoftware.CoreService.Bending.Service
{
    internal class CycleManagerServiceBending : CycleManagerServiceBase
    {
        public CycleManagerServiceBending(
            IPLCTagConfigurationService tagService,
            ILogConfigurationService logConfig,
            PLCClientManager plcManager,
            IOptions<CcdSettings> appSettings,
            IServoCalibrationService servoService,
            ProductionImageService imageService,
            IExternalInterfaceService extService,
            IAeLimitService aeLimitService,
            IProductConfigurationService productService,
            IAppLogger logger)
            : base(tagService, logConfig, plcManager, appSettings, servoService, imageService, extService, aeLimitService, productService, logger)
        {
            
        }
    }
}
