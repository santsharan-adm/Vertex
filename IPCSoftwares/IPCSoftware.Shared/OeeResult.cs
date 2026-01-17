using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Permissions;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;

namespace IPCSoftware.Shared
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
        public double XValue { get; set; }
        public double YValue { get; set; }
        public double AngleValue { get; set; }
        public double MinX { get; set; }
        public double MaxX { get; set; }
        public double MinY { get; set; }
        public double MaxY { get; set; }
        public double MinZ { get; set; }
        public double MaxZ { get; set; }
    }
}
