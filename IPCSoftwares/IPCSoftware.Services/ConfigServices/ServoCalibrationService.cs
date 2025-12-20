using IPCSoftware.Core.Interfaces;
using IPCSoftware.Shared.Models;
using IPCSoftware.Shared.Models.ConfigModels;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace IPCSoftware.Services.ConfigServices
{
    public class ServoCalibrationService : IServoCalibrationService
    {
        private readonly string _filePath;

        public ServoCalibrationService(IOptions<ConfigSettings> config)
        {
            string folder = config.Value.DataFolder ?? AppContext.BaseDirectory;
            _filePath = Path.Combine(folder, "ServoCalibration.json");
        }

        public async Task<List<ServoPositionModel>> LoadPositionsAsync()
        {
            if (!File.Exists(_filePath))
            {
                return CreateDefaultPositions();
            }

            try
            {
                string json = await File.ReadAllTextAsync(_filePath);
                var data = JsonSerializer.Deserialize<List<ServoPositionModel>>(json);
                return data ?? CreateDefaultPositions();
            }
            catch
            {
                return CreateDefaultPositions();
            }
        }

        public async Task SavePositionsAsync(List<ServoPositionModel> positions)
        {
            var options = new JsonSerializerOptions { WriteIndented = true };
            string json = JsonSerializer.Serialize(positions, options);
            await File.WriteAllTextAsync(_filePath, json);
        }

        private List<ServoPositionModel> CreateDefaultPositions()
        {
            var list = new List<ServoPositionModel>();

            // Position 0 = Home
            list.Add(new ServoPositionModel { PositionId = 0, Name = "Home / QR", Description = "Reference Position" });

            // Positions 1-12
            for (int i = 1; i <= 12; i++)
            {
                list.Add(new ServoPositionModel { PositionId = i, Name = $"Station {i}", Description = $"Inspection Point {i}" });
            }
            return list;
        }
    }
}