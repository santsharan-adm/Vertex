using IPCSoftware.AppLogger.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace IPCSoftware.AppLogger.Services
{
    public class LogConfigService
    {
        private readonly string _configPath;

        public LogConfigService()
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string configFolder = Path.Combine(baseDir, "LogConfigs");

            if (!Directory.Exists(configFolder))
                Directory.CreateDirectory(configFolder);

            _configPath = Path.Combine(configFolder, "logconfigs.json");

            // If file does not exist → create empty list file
            if (!File.Exists(_configPath))
            {
                File.WriteAllText(_configPath, "[]");
            }
        }

        public List<LogConfig> LoadConfigs()
        {
            string json = File.ReadAllText(_configPath);
            return JsonSerializer.Deserialize<List<LogConfig>>(json)
                   ?? new List<LogConfig>();
        }

        public void SaveConfigs(List<LogConfig> configs)
        {
            string json = JsonSerializer.Serialize(configs,
                new JsonSerializerOptions { WriteIndented = true });

            File.WriteAllText(_configPath, json);
        }
    }
}
