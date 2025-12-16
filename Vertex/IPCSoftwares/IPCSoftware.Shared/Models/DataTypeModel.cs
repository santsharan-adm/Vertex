using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IPCSoftware.Shared.Models
{
    public class DataTypeModel
    {
        // Corresponds to the integer codes (1, 2, 3, 4, 5) used by the Core Service
        public int Id { get; set; }
        // The user-friendly name displayed in the ComboBox
        public string Name { get; set; } = string.Empty;
    }
}
