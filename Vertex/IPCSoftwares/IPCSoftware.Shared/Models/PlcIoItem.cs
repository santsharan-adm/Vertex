using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IPCSoftware.Shared.Models
{
    public class PlcIoItem
    {
        public int Id { get; set; }

        public string Tag { get; set; }

        public string Value { get; set; }

        public string Description { get; set; }

    }
}
