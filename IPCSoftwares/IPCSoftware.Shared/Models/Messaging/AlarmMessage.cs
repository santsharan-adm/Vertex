using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IPCSoftware.Shared.Models.ConfigModels;

namespace IPCSoftware.Shared.Models.Messaging
{
    public class AlarmMessage
    {
        public AlarmInstanceModel AlarmInstance { get; set; }

        // Tells the UI what kind of action occurred
        public AlarmMessageType MessageType { get; set; }
    }

    public enum AlarmMessageType
    {
        Raised,       // New alarm condition detected
        Cleared,      // Alarm condition is no longer present
        Acknowledged, // User acknowledged the alarm via UI/API
        Updated       // General status update (less common)
    }
}
