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
        string ResolveLogFile(LogType type);
        void ApplyMaintenance(LogConfigurationModel config, string filePath);

        // NEW Methods
        void CheckAndPerformBackups(); // Called by Worker
        void PerformManualBackup(int logConfigId); // Called by UI
    }
}
