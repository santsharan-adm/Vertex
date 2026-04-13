using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AttachMetaDataToFile.Model
{
    public class ResAttachMeta
    {
        public List<byte> output { get; set; }
        public string imgPath { get; set; }
        public int metadataSize { get; set; }
    }
}
