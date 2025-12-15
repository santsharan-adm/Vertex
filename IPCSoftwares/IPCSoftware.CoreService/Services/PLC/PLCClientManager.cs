using IPCSoftware.Core.Interfaces;
using IPCSoftware.Services;
using IPCSoftware.Shared.Models.ConfigModels;
using Microsoft.Extensions.Options;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace IPCSoftware.CoreService.Services.PLC
{
    public class PLCClientManager
    {
     

        private readonly IPLCTagConfigurationService _tagService;
        private readonly IDeviceConfigurationService _deviceService;
        private readonly ConfigSettings _config;


        public List<PlcClient> Clients { get; private set; } = new();

        public PLCClientManager(
            IDeviceConfigurationService deviceService,
            IPLCTagConfigurationService tagService, IOptions<ConfigSettings> config)
        {
        _config = config.Value;
            _tagService = tagService;
            _deviceService = deviceService;
           _ =  InitializeClients();
        }




        private async Task InitializeClients()
        {
            var allTags = await _tagService.GetAllTagsAsync();
            var devices = await _deviceService.GetPlcDevicesAsync();
            foreach (var dev in devices)
            {
                // Assign tags belonging to this PLCNo
                var myTags = allTags
                    .Where(t => t.PLCNo == dev.DeviceNo)
                    .ToList();

              
                var client = new PlcClient(dev, myTags, _config);

                Clients.Add(client);
            }
        }

        public List<PlcClient> GetAllClients() => Clients;


        public void UpdateTags(List<PLCTagConfigurationModel> allNewTags)
        {
            // Store the new comprehensive list (if needed)
            // Interlocked.Exchange(ref _allTags, allNewTags); 

            // Iterate through all running clients and tell them to update their subset of tags
            foreach (var client in Clients)
            {
                client.UpdateTags(allNewTags);
            }
            Console.WriteLine($"PLCClientManager notified {Clients.Count} clients of tag update.");
        }
        public PlcClient GetClient(int plcNo)
        {
            return Clients.FirstOrDefault(c => c.Device.DeviceNo == plcNo);
        }
    }

    public class CameraClientManager
    {

    }
}
