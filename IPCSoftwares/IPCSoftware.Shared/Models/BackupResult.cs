using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IPCSoftware.Shared.Models
{
    public class BackupResult
    {
        public int TotalFiles { get; set; }
        public int CopiedFiles { get; set; }
        public int FailedFiles { get; set; }
        public bool IsSuccess => FailedFiles == 0;
    }
}
