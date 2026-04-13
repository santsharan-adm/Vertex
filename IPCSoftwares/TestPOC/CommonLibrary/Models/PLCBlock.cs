using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommonLibrary.Models
{
    public class PLCBlock
    {
        public uint Id { get; set; }
        public uint[] Data { get; set; }= new uint[20];
    }

    public class PLCBlocks : Dictionary<uint, PLCBlock>
    {
    }
}
