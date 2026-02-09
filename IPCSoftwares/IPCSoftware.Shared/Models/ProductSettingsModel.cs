using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IPCSoftware.Shared.Models
{
    public class ProductSettingsModel
    {
        public string ProductName { get; set; } = "Default Product";
        public string ProductCode { get; set; } = "DEF-001";

        // Total active items to process
        public int TotalItems { get; set; } = 12;

        // Grid Layout Configuration
        public int GridRows { get; set; } = 4;
        public int GridColumns { get; set; } = 3;
    }
}
