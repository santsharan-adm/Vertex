using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace IPCSoftware.Shared.Models
{
    public class MacMiniStatusModel
    {
        // Property matching the JSON key "ok" which contains a list of integers
        // Example JSON: { "ok": [1, 3, 5, 12] }
        // This means cavities 1, 3, 5, and 12 are OK. Others (2, 4, 6...) are NG.
        public List<int> ok { get; set; }

        // You mentioned "list of sequence". If the JSON also contains the order (e.g. "sequence": [12, 11, ...])
        // add it here. For now, assuming static sequence or default.
        public List<int> sequence { get; set; }

        public Dictionary<int, string> Serials { get; set; } = new Dictionary<int, string>();
    }
}
