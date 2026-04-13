using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommonLibrary.Models
{
    /// <summary>
    /// 1. Read All Tags
    /// 2. Write Tag
    /// Parameters: int -> Parameter Index, object -> Parameter Value
    /// </summary>
    public class RequestPackage
    {
        public int RequestId { get; set; }
        public Dictionary<uint,object> Parameters { get; set; }
    }
    public class ResponsePackage
    {
        public int RequestId { get; set; }
        public Dictionary<uint, object> Parameters { get; set; }
    }
}
