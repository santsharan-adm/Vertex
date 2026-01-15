using IPCSoftware.Shared.Models.ConfigModels;
using System.Collections.Generic;
using System.Linq;

namespace IPCSoftware.CoreService.Services.PLC
{
    public class PLCClientManager
    {
        private readonly List<DeviceInterfaceModel> _devices;
        private readonly List<PLCTagConfigurationModel> _allTags;

        public List<PlcClient> Clients { get; private set; } = new();

        public PLCClientManager(
            List<DeviceInterfaceModel> devices,
            List<PLCTagConfigurationModel> tags)
        {
            _devices = devices;
            _allTags = tags;

            InitializeClients();
        }

        private void InitializeClients()
        {
            foreach (var dev in _devices)
            {
                // Assign tags belonging to this PLCNo
                var myTags = _allTags
                    .Where(t => t.PLCNo == dev.DeviceNo)
                    .ToList();

                var client = new PlcClient(dev, myTags);

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
}
