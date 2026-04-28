using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IPCSoftware.App.Bending.Models
{
    public class ProductData
    {
        public string ProductName { get; set; }
        public string Station { get; set; }
        public double Upper { get; set; }
        public double Value { get; set; }
        public double Lower { get; set; }
        public string LoadMonitor { get; set; }
        public string TemperatureMonitor { get; set; }
        public string BendingTime { get; set; }
    }
}