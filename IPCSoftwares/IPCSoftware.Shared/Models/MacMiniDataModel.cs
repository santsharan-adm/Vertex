using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace IPCSoftware.Shared.Models
{
  /*  public class MacMiniDataModel
    {
        [Newtonsoft.Json.JsonProperty("ok_cavities")]
        public List<int> OkCavities { get; set; } = new List<int>();

        [Newtonsoft.Json.JsonProperty("ng_cavities")]
        public List<int> NgCavities { get; set; } = new List<int>();

        [Newtonsoft.Json.JsonProperty("timestamp")]
        public DateTime? Timestamp { get; set; }

        [Newtonsoft.Json.JsonProperty("batch_id")]
        public string BatchId { get; set; }

        [Newtonsoft.Json.JsonProperty("serial_number")]
        public string SerialNumber { get; set; }

        /// <summary>
        /// Gets boolean array where Index represents cavity (0=Cavity1, 11=Cavity12)
        /// True = OK, False = NG
        /// </summary>
        public bool[] GetStatusArray()
        {
            var result = new bool[12]; // Default all False (NG)

            if (OkCavities != null)
            {
                foreach (int cavityId in OkCavities)
                {
                    if (cavityId >= 1 && cavityId <= 12)
                    {
                        result[cavityId - 1] = true;
                    }
                }
            }

            return result;
        }
    }

*/
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
