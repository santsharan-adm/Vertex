using IPCSoftware.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

namespace IPCSoftware.Services.ConfigServices
{
    public class CcdConfigService : ICcdConfigService
    {
        private readonly string _targetFilePath;

        // Inject IHostEnvironment to detect if we are in Dev or Prod automatically
        public CcdConfigService(IHostEnvironment env)
        {
           // string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            var sharedConfigDir = Environment.GetEnvironmentVariable("CONFIG_DIR");

            // Fallback to the app's base dir if not set/invalid
            var baseDir = AppContext.BaseDirectory;
            var configDir = !string.IsNullOrWhiteSpace(sharedConfigDir) && Directory.Exists(sharedConfigDir)
                            ? sharedConfigDir
                            : baseDir;

            // LOGIC: If we are in Development, prefer the Development JSON.
            // Otherwise (Production), use the standard appsettings.json.
            if (env.IsDevelopment()) 
            {
                string devPath = Path.Combine(configDir, "appsettings.Development.json");
                // Only use Dev file if it actually exists, otherwise fall back to standard
                _targetFilePath = File.Exists(devPath) ? devPath : Path.Combine(baseDir, "appsettings.json");
            }
            else
            {
                _targetFilePath = Path.Combine(baseDir, "appsettings.json");
            }
        }

        public (string ImagePath, string BackupPath) LoadCcdPaths()
        {
            if (!File.Exists(_targetFilePath)) return (string.Empty, string.Empty);

            try
            {
                // We strictly read from the file we intend to write to
                var jsonString = File.ReadAllText(_targetFilePath);
                var root = JsonNode.Parse(jsonString);

                var ccdSection = root?["CCD"];
                return (
                    ccdSection?["BaseOutputDir"]?.ToString() ?? string.Empty,
                    ccdSection?["BaseOutputDirBackup"]?.ToString() ?? string.Empty
                );
            }
            catch
            {
                return (string.Empty, string.Empty);
            }
        }

        public void SaveCcdPaths(string imagePath, string backupPath)
        {
            if (!File.Exists(_targetFilePath)) return;

            try
            {
                var jsonString = File.ReadAllText(_targetFilePath);
                var root = JsonNode.Parse(jsonString) ?? new JsonObject();

                // Ensure "CCD" section exists
                if (root["CCD"] is not JsonObject ccdSection)
                {
                    ccdSection = new JsonObject();
                    root["CCD"] = ccdSection;
                }

                // Update values
                ccdSection["BaseOutputDir"] = imagePath;
                ccdSection["BaseOutputDirBackup"] = backupPath;

                // Save back to the SAME file we detected earlier
                File.WriteAllText(_targetFilePath, root.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
            }
            catch (Exception ex)
            {
                // Handle error
            }
        }
    }
}
