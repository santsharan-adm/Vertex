using IPCSoftware.Shared.Models.ConfigModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IPCSoftware.Core.Interfaces.AppLoggerInterface
{
    public interface ILogManagerService
    {
        Task InitializeAsync(); 
        LogConfigurationModel GetConfig(LogType type);

        void ApplyMaintenance(LogConfigurationModel config, string filePath);

        string ResolveLogFile(LogType type);
    }
}
