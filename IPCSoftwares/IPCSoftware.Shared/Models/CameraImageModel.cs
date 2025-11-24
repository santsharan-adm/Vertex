using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IPCSoftware.Shared.Models
{
    public class CameraImageModel
    {
        public string ImagePath { get; set; }
        public string Result { get; set; }  // "OK", "NG", "TOSSED"
    }
}
