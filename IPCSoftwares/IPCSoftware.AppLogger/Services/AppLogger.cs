using IPCSoftware.AppLogger.Interfaces;
using IPCSoftware.AppLogger.Models;
using System.IO;

namespace IPCSoftware.AppLogger.Services
{
    public class AppLogger : IAppLogger
    {
        private readonly LogManager _logManager;

        public AppLogger(LogManager logManager)
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
            // resolve config
            var config = _logManager.GetConfig(type);
            if (config == null || !config.Enabled)
                return;

            // resolve file
            string filePath = _logManager.ResolveLogFile(type);
            if (filePath == null)
                return;

            // maintenance (fileSize, purge, retention)
            _logManager.ApplyMaintenance(config, filePath);

            string line = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss},{level},\"{message}\",{config.Name}";
            File.AppendAllText(filePath, line + Environment.NewLine);
        }
    }
}
