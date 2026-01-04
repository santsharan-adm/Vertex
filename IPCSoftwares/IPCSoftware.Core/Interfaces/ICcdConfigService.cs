using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IPCSoftware.Core.Interfaces
{
    public interface ICcdConfigService
    {
        (string ImagePath, string BackupPath) LoadCcdPaths();
        void SaveCcdPaths(string imagePath, string backupPath);
    }
}
