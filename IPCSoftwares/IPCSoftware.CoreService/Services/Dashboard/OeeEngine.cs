// OeeEngine.cs
using IPCSoftware.Core.Interfaces;
using IPCSoftware.CoreService.Services.PLC;
using IPCSoftware.Shared.Models;
using Newtonsoft.Json.Linq;

namespace IPCSoftware.CoreService.Services.Dashboard
{
    public class OeeResult
    {
        public double Availability { get; set; }
        public double Performance { get; set; }
        public double Quality { get; set; }
        public double OverallOEE { get; set; }

        public int OperatingTime { get; set; }
        public int Downtime { get; set; }

        public int OKParts { get; set; }
        public int NGParts { get; set; }
        public int CycleTime { get; set; }
        public int TotalParts { get; set; }
    }



    public class OeeEngine
    {
        private readonly IPLCTagConfigurationService _tagService;
        private readonly PLCClientManager _plcManager;

        private bool _lastCycleTimeTriggerState = false;
        private int _lastCycleTime = 1;

        public OeeEngine(IPLCTagConfigurationService tagService, PLCClientManager plcManager)
        {
            _tagService = tagService;
            _plcManager = plcManager;
        }

        public void ProcessCycleTimeLogic(Dictionary<int, object> tagValues)
        {
            // 1. Check A1 Bit (Tag 21)
            bool currentA1State = GetBoolState(tagValues, ConstantValues.TAG_CTL_CYCLETIME_A1);

            // 2. Rising Edge Detection (0 -> 1)
            if (currentA1State && !_lastCycleTimeTriggerState)
            {
                _lastCycleTime = GetInt(tagValues, ConstantValues.TAG_CycleTime);
                Console.WriteLine($"[CycleTime] A1 Trigger Detected (Tag {ConstantValues.TAG_CTL_CYCLETIME_A1})");

                // Note: The Cycle Time Value (Tag 22) is already read in 'tagValues' 
                // because the AlgorithmService processes the whole packet.

                // 3. Send Acknowledgement B1 (Tag 23)

                _ = WriteTagAsync(ConstantValues.TAG_CTL_CYCLETIME_B1, true);
            }
           /* else if (!currentA1State && _lastCycleTimeTriggerState)
            {
                // Falling edge of A1 -> Reset B1 to ready for next
                _ = WriteTagAsync(ConstantValues.TAG_CTL_CYCLETIME_B1, false);
            }*/

            _lastCycleTimeTriggerState = currentA1State;
        }

        public Dictionary<int, object> Calculate(Dictionary<int, object> values)
        {
            OeeResult r = new OeeResult();

            // 1. Extract Raw Values using ConstantValues IDs
            int operatingMin = GetInt(values, ConstantValues.TAG_UpTime);
            int downTimeMin = GetInt(values, ConstantValues.TAG_DownTime);
            int totalParts = GetInt(values, ConstantValues.TAG_InFlow);
            int okParts = GetInt(values, ConstantValues.TAG_OK);
            int ngParts = GetInt(values, ConstantValues.TAG_NG);
            int idealCycle = GetInt(values, ConstantValues.TAG_CycleTime); // Seconds per part
            int actualCycleTime = GetInt(values, ConstantValues.TAG_CycleTime); // Seconds per part

            // 2. Availability (A) Calculation
            // Formula: Uptime / (Uptime + Alarm Stop + Downtime)
            // Code assumes downTimeMin includes Alarm Stop
            double totalTimeMin = operatingMin + downTimeMin;

            r.Availability = 0.0;
            if (totalTimeMin > 0)
            {
                r.Availability = (double)operatingMin / totalTimeMin;
            }

            // 3. Quality (Q) Calculation
            // Formula: Good Count / Total Count
            // Note: Yield = Good / Total * 100 (Code returns decimal 0-1)
            r.Quality = 0.0;
            if (totalParts > 0)
            {
                r.Quality = (double)okParts / (double)totalParts;
            }

            // 4. Performance (P) Calculation
            // Formula: (Ideal Cycle Time * Total Production) / Uptime
            // Unit Sync: CycleTime is Seconds, Uptime is Minutes -> Convert Uptime to Seconds
            r.Performance = 0.0;
            if (operatingMin > 0 && idealCycle > 0)
            {
                double operatingSeconds = (double)operatingMin * 60.0;

                if (operatingSeconds > 0)
                {
                    r.Performance = ((double)idealCycle * totalParts) / operatingSeconds;
                }
            }

            // 5. Overall OEE
            // Formula: A * P * Q
            r.OverallOEE = r.Availability * r.Performance * r.Quality;

            // 6. Raw values pass-through for UI
            r.OKParts = okParts;
            r.NGParts = ngParts;
            r.OperatingTime = operatingMin;
            r.Downtime = downTimeMin;
            r.TotalParts = totalParts;
            r.CycleTime = _lastCycleTime;

            // Return as dictionary with ID 4 (OEE_DATA)
            return new Dictionary<int, object> { { 4, r } };
        }

        // Helper to safely extract int from dictionary using Tag ID
        private int GetInt(Dictionary<int, object> values, int tagId)
        {
            if (values != null && values.TryGetValue((int)tagId, out object val))
            {
                try { return Convert.ToInt32(val); } catch { return 0; }
            }
            return 0;
        }


        private bool GetBoolState(Dictionary<int, object> tagValues, int tagId)
        {
            if (tagValues.TryGetValue(tagId, out object obj))
            {
                if (obj is bool bVal) return bVal;
                if (obj is int iVal) return iVal > 0;
            }
            return false;
        }

        private async Task WriteTagAsync(int tagNo, object value)
        {
            try
            {
                var allTags = await _tagService.GetAllTagsAsync();
                var tag = allTags.FirstOrDefault(t => t.TagNo == tagNo);
                if (tag != null)
                {
                    var client = _plcManager.GetClient(tag.PLCNo);
                    if (client != null)
                    {
                        await client.WriteAsync(tag, value);
                        Console.WriteLine($"[CycleTime] Ack Tag {tagNo} set to {value}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Error] CycleTime Write Tag {tagNo}: {ex.Message}");
            }
        }

    }
}