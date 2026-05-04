using IPCSoftware.Shared.Models.ConfigModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IPCSoftware.Shared.Models.Messaging
{
    public class LogRequest
    {
        public string Message { get; set; }
        public string Level { get; set; } // "INFO", "WARN", "ERROR", "TRACE"
        public LogType LogType { get; set; }
        public string MemberName { get; set; }
        public string FilePath { get; set; }
        public int LineNumber { get; set; }
    }
}
