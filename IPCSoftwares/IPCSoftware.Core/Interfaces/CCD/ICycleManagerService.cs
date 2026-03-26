using IPCSoftware.Shared.Models;                    // ✅ ExternalSettings lives here
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Automation;

namespace IPCSoftware.Core.Interfaces.CCD
{
    public interface ICycleManagerService
    {
        Task HandleIncomingData(string tempImagePath, Dictionary<string, object> stationData, string qrString = null);
        bool IsCycleResetCompleted { get; }
        void RequestReset(bool fromCcd = false);
    }

    public interface IExternalInterfaceService
    {
        bool IsConnected { get; }
        ExternalSettings Settings { get; }          // ✅ added — fixes all 4 CS1061 errors
        string GetSerialNumber(int stationId);
        bool IsSequenceRestricted(int sequenceIndex);
        bool[] GetCurrentQuarantineSnapshot();
        Task SyncBatchStatusAsync(string qrCode);
        Task ResetPlcInterfaceAsync();
        Task SendPdcaDataAsync(string payload);
    }
}