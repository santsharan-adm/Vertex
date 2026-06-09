using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IPCSoftware.Shared.Models
{
    public class TaskbarItems
    {
        public bool IsPLC1Connected { get; set; }
        public bool IsPLC2Connected { get; set; }
        public bool IsMacMiniConnected { get; set; }
        public bool IsServiceConnected { get; set; }  
        public string CurrentMachineMode { get; set; }

    }
}
