using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IPCSoftware.Shared.Models
{
    public class AuditLogModel
    {
        public string Time { get; set; }
        public string Message { get; set; }


    }
        

    public class LogFileInfo
    {
        public string FileName { get; set; }
        public string FullPath { get; set; }
        public DateTime LastModified { get; set; }
        public string DisplaySize { get; set; } // e.g., "12 KB"
    }
}
