using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommonLibrary.Models
{
    public class UIDataModel
    {
        public TimeSpan OperatingTime { get; set; }   
        public TimeSpan Downtime { get; set; }        
        public TimeSpan AverageCycleTime { get; set; }
        public TimeSpan UpTime { get; set; }
        public double Availability { get; set; }
        public double Performance { get; set; }
        public double Quality { get; set; }
    }
}
