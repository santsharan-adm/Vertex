using System;
using System.Collections.Generic;
using System.Linq;
using IPCSoftware.Shared.Models.ConfigModels;

namespace IPCSoftware.CoreService.Services.Algorithm
{
    public class AlgorithmAnalysisService
    {
        private readonly List<PLCTagConfigurationModel> _tags;

        public AlgorithmAnalysisService(List<PLCTagConfigurationModel> tags)
        {
            _tags = tags;
        }

        public Dictionary<int, object> Apply(int plcNo, Dictionary<uint, object> raw)
        {
            var result = new Dictionary<int, object>();

            // Select only tags from this PLC
            var plcTags = _tags.Where(t => t.PLCNo == plcNo);

            // Group tags by ModbusAddress
            foreach (var group in plcTags.GroupBy(t => t.ModbusAddress))
            {
                uint address = (uint)group.Key;

                if (!raw.TryGetValue(address, out var rawObj))
                    continue;

                int rawValue = Convert.ToInt32(rawObj);

                foreach (var tag in group)
                {
                    object finalValue;

                    // CASE 1: DataType = Bit
                    if (tag.DataType == 3)
                    {
                        bool bit = ((rawValue >> tag.BitNo) & 1) == 1;
                        finalValue = bit;
                    }
                    // CASE 2: AlgoNo = Linear (only numeric)
                    else if (tag.AlgNo == 1 && (tag.DataType == 1 || tag.DataType == 2))
                    {
                        // (raw - offset) * span
                        double scaled = (rawValue - tag.Offset) * tag.Span;
                        finalValue = scaled;
                    }
                    // CASE 3: Raw numeric or unsupported
                    else
                    {
                        finalValue = rawValue;
                    }

                    // Add using Tag Id (not address)
                    result[tag.Id] = finalValue;
                }
            }

            return result;
        }
    }
}
