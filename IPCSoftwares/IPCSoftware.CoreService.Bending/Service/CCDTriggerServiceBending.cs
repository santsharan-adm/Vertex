using IPCSoftware.Core.Interfaces;
using IPCSoftware.Core.Interfaces.AppLoggerInterface;
using IPCSoftware.Core.Interfaces.CCD;
using IPCSoftware.Devices.Camera;
using IPCSoftware.Devices.PLC;
using IPCSoftware.Services.ConfigServices; //Added Later
using IPCSoftware.Shared.Models;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IPCSoftware.CoreService.Bending.Service
{
    public class CCDTriggerServiceBending : CCDTriggerServiceBase
    {
        public CCDTriggerServiceBending(
            ICycleManagerService cycleManager,
            IPLCTagConfigurationService tagService,
            IOptions<CcdSettings> ccdSettings,
            IObservableCcdSettingsService observableCcdSettings,  // //Added by Rishabh - date - 08/04/2026//
            IAppLogger logger) : base(cycleManager, tagService, ccdSettings, observableCcdSettings, logger)
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