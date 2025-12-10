
using IPCSoftware.Core.Interfaces.AppLoggerInterface;
using IPCSoftware.Shared.Models.ConfigModels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace IPCSoftware.Services.AppLoggerServices
{
    public class AppLoggerService : IAppLogger
    {
        private readonly ILogManagerService _logManager;

        public AppLoggerService(ILogManagerService logManager)
        {
            _logManager = logManager;
        }

        // Public APIs
        public void LogInfo(string message, LogType type)
        {
            WriteLog("INFO", message, type);
        }

        public void LogWarning(string message, LogType type)
        {
            WriteLog("WARN", message, type);
        }

        public void LogError(string message, LogType type)
        {
            WriteLog("ERROR", message, type);
        }

        private void WriteLog(string level, string message, LogType type)
        {
            try
            {
                var config = _logManager.GetConfig(type);
                if (config == null || !config.Enabled)
                    return;

                // resolve file
                string filePath = _logManager.ResolveLogFile(type);
                if (filePath == null)
                    return;

                // maintenance (fileSize, purge, retention)
                _logManager.ApplyMaintenance(config, filePath);

                string line = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss},{level},\"{message}\",{config.LogName}";
                File.AppendAllText(filePath, line + Environment.NewLine);
            }
            catch (Exception ex)
            {

            }
            // resolve config
            
        }

     
    }


}
