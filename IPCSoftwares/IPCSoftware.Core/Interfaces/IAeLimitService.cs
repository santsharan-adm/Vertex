using IPCSoftware.Shared.Models.AeLimit;
using System.Threading.Tasks;

namespace IPCSoftware.Core.Interfaces
{
    public interface IAeLimitService
    {
        Task InitializeAsync();
        Task<AeLimitSettings> GetSettingsAsync();
        Task SaveSettingsAsync(AeLimitSettings settings);

        void BeginCycle(string serialNumber, string carrierSerial);
        void UpdateStation(AeStationUpdate update);
        Task<string> CompleteCycleAsync(bool success = true);
        void AbortCycle();
    }
}
