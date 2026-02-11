using IPCSoftware.Core.Interfaces;
using IPCSoftware.Core.Interfaces.AppLoggerInterface;
using IPCSoftware.Shared.Models;
using IPCSoftware.Shared.Models.ConfigModels;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Threading.Tasks;

namespace IPCSoftware.Services.ConfigServices
{
    public class ProductConfigurationService : IProductConfigurationService
    {
        private readonly string _filePath;
        private readonly IAppLogger _logger;

        public ProductConfigurationService(IOptions<ConfigSettings> config, IAppLogger logger)
        {
            _logger = logger;
            string folder = config.Value.DataFolder ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data");

            if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);

            _filePath = Path.Combine(folder, "ProductSettings.json");
        }

        public async Task<ProductSettingsModel> LoadAsync()
        {
            try
            {
                if (!File.Exists(_filePath))
                {
                    var defaultSettings = new ProductSettingsModel();
                    await SaveAsync(defaultSettings);
                    return defaultSettings;
                }

                string json = await File.ReadAllTextAsync(_filePath);
                return JsonConvert.DeserializeObject<ProductSettingsModel>(json) ?? new ProductSettingsModel();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Product Config Load Error: {ex.Message}", LogType.Diagnostics);
                return new ProductSettingsModel(); // Return default on error
            }
        }

        public async Task SaveAsync(ProductSettingsModel settings)
        {
            try
            {
                string json = JsonConvert.SerializeObject(settings, Formatting.Indented);
                await File.WriteAllTextAsync(_filePath, json);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Product Config Save Error: {ex.Message}", LogType.Diagnostics);
            }
        }   
    }
}