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
        private readonly string _dataFolder;


        public ServoCalibrationService(IOptions<ConfigSettings> configSettings)
        {
            //string folder = configSettings.Value.DataFolder ?? AppContext.BaseDirectory;

            var config = configSettings.Value;
            string dataFolderPath = config.DataFolder;

            _dataFolder = dataFolderPath ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data");
          //  _filePath = Path.Combine(folder, "ServoCalibration.json");
            _filePath =  Path.Combine(_dataFolder, config.ServoCalibrationFileName );

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

                // Integrity check: If file exists but is empty or missing sequences
                if (data == null || data.Count == 0 || data.All(p => p.SequenceIndex == 0))
                {
                    return CreateDefaultPositions();
                }
                return data;
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

            // Define Default Snake Pattern Map
            // Map: [Physical ID] -> [Sequence Index]
            var snakeMap = new Dictionary<int, int>
            {
                { 1, 1 }, { 2, 2 }, { 3, 3 },   // Row 1 (Right)
                { 6, 4 }, { 5, 5 }, { 4, 6 },   // Row 2 (Left)
                { 7, 7 }, { 8, 8 }, { 9, 9 },   // Row 3 (Right)
                { 12, 10 }, { 11, 11 }, { 10, 12 } // Row 4 (Left)
            };

            // Position 0 = Home
            list.Add(new ServoPositionModel
            {
                PositionId = 0,
                Name = "Position 0",
                SequenceIndex = 0,
                X = 0,
                Y = 0
            });

            // Positions 1-12
            for (int i = 1; i <= 12; i++)
            {
                int seq = snakeMap.ContainsKey(i) ? snakeMap[i] : i;

                list.Add(new ServoPositionModel
                {
                    PositionId = i,
                    Name = $"Position {i}",
                    SequenceIndex = seq, // Apply Default Snake Pattern
                    X = 0,
                    Y = 0
                });
            }
            return list;
        }
    }
}