using IPCSoftware.Core.Interfaces;
using IPCSoftware.Core.Interfaces.AppLoggerInterface;
using IPCSoftware.Core.Interfaces.CCD;
using IPCSoftware.Devices.Camera;
using IPCSoftware.Devices.PLC;
using IPCSoftware.Shared.Models;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IPCSoftware.CoreService.AOI.Service
{
    public class CCDTriggerServiceBending : CCDTriggerServiceBase
    {
        public CCDTriggerServiceBending
           (ICycleManagerService cycleManager,
           IPLCTagConfigurationService tagService,
           IOptions<CcdSettings> ccdSettings,
           IAppLogger logger) : base(cycleManager, tagService, ccdSettings, logger)
        {
        }

        override public async Task ProcessTriggers(Dictionary<int, object> tagValues, PLCClientManager manager)
        {
            base.ProcessTriggers(tagValues, manager);
        }
        override protected async Task WriteAckToPlcAsync(bool writebool)
        {
            base.WriteAckToPlcAsync(writebool);
            
        }
    }
}
