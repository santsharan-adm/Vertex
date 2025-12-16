using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IPCSoftware.Shared.Models.ConfigModels
{
    public class AlarmInstanceModel
    {
        public Guid InstanceId { get; set; } // Unique ID for this occurrence
        public int AlarmNo { get; set; } // Reference back to the definition
        public string AlarmText { get; set; }
        public string Severity { get; set; }

        // Runtime State Fields:
        public DateTime AlarmTime { get; set; } // When the alarm was raised
        public DateTime? AlarmResetTime { get; set; } // When the alarm cleared
        public DateTime? AlarmAckTime { get; set; } // When the alarm was acknowledged
        public string AcknowledgedByUser { get; set; } // Who acknowledged it
    }
}
