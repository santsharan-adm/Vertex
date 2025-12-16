using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IPCSoftware.Shared.Models
{
    public class MetricDetailItem
    {
        public string MetricName { get; set; } // e.g., "Availability"
        public string CurrentVal { get; set; } // e.g., "85%"
        public string WeeklyVal { get; set; }  // e.g., "84%"
        public string MonthlyVal { get; set; } // e.g., "82%"
    }
}
