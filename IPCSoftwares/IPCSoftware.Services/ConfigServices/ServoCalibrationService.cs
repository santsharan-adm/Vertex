using IPCSoftware.App.Services;
using IPCSoftware.Core.Interfaces;
using IPCSoftware.Core.Interfaces.AppLoggerInterface;
using IPCSoftware.Shared.Models;
using IPCSoftware.Shared.Models.ConfigModels;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Numerics;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;


namespace IPCSoftware.Services.ConfigServices
{
    public class ServoCalibrationService : IServoCalibrationService
    {
        private readonly string _filePath;  
        private readonly string _dataFolder;
        private readonly IProductConfigurationService _productService;
        //private readonly IRecipeApplicationService _recipeApplicationService;
        private readonly IAppLogger _logger;

        private readonly string _RecipeFilePath;


        public ServoCalibrationService(IOptions<ConfigSettings> configSettings,
              IProductConfigurationService productService ,/*IRecipeApplicationService recipeApplicationService,*/ IAppLogger logger)
        {
            //string folder = configSettings.Value.DataFolder ?? AppContext.BaseDirectory;
            _logger = logger;
            var config = configSettings.Value;
            string dataFolderPath = config.DataFolder;
            _productService = productService;
            _dataFolder = dataFolderPath ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data");
          //  _filePath = Path.Combine(folder, "ServoCalibration.json");
            _filePath =  Path.Combine(_dataFolder, config.ServoCalibrationFileName );
            _RecipeFilePath = Path.Combine(_dataFolder, config.ServoRecipeFileName);
            //_recipeApplicationService = recipeApplicationService;

        }

        //New load from csv recipe file -
        //public async Task<List<ServoPositionModel>> LoadPositionsAsync()
        //{
        //    if (!File.Exists(_RecipeFilePath))
        //    {
        //        return await CreateDefaultPositionsAsync();
        //    }

        //    try
        //    {
        //        // Get the currently selected recipe (replaces ServoCalibration.json)
        //        var selectedRecipe = await _recipeApplicationService.GetRecipefromSelection();

        //        if (selectedRecipe == null)
        //        {
        //            _logger.LogWarning("No recipe selected. Falling back to last recipe in CSV.", LogType.Diagnostics);

        //            // Fallback: use last recipe from CSV
        //            var allRecipes = await LoadRecipeAsync();
        //            selectedRecipe = allRecipes.LastOrDefault();
        //        }

        //        if (selectedRecipe == null)
        //        {
        //            _logger.LogWarning("No recipes found in CSV. Creating default positions.", LogType.Diagnostics);
        //            return await CreateDefaultPositionsAsync();
        //        }

        //        int totalItems = selectedRecipe.TotalItems > 0 ? selectedRecipe.TotalItems : 12;

        //        var positions = new List<ServoPositionModel>
        //        {
        //            // Position 0 = Home (always present)
        //            new ServoPositionModel
        //            {
        //                PositionId    = 0,
        //                Name          = "Position 0 (Home)",
        //                SequenceIndex = 0,
        //                X             = selectedRecipe.X0,
        //                Y             = selectedRecipe.Y0,
        //                IsEnabled     = true
        //            }
        //        };

        //        // Coordinate lookup map for positions 1-12
        //        var coordMap = new Dictionary<int, (double X, double Y, int Seq)>
        //        {
        //            { 1,  (selectedRecipe.X1,  selectedRecipe.Y1,  selectedRecipe.S1)  },
        //            { 2,  (selectedRecipe.X2,  selectedRecipe.Y2,  selectedRecipe.S2)  },
        //            { 3,  (selectedRecipe.X3,  selectedRecipe.Y3,  selectedRecipe.S3)  },
        //            { 4,  (selectedRecipe.X4,  selectedRecipe.Y4,  selectedRecipe.S4)  },
        //            { 5,  (selectedRecipe.X5,  selectedRecipe.Y5,  selectedRecipe.S5)  },
        //            { 6,  (selectedRecipe.X6,  selectedRecipe.Y6,  selectedRecipe.S6)  },
        //            { 7,  (selectedRecipe.X7,  selectedRecipe.Y7,  selectedRecipe.S7)  },
        //            { 8,  (selectedRecipe.X8,  selectedRecipe.Y8,  selectedRecipe.S8)  },
        //            { 9,  (selectedRecipe.X9,  selectedRecipe.Y9,  selectedRecipe.S9)  },
        //            { 10, (selectedRecipe.X10, selectedRecipe.Y10, selectedRecipe.S10) },
        //            { 11, (selectedRecipe.X11, selectedRecipe.Y11, selectedRecipe.S11) },
        //            { 12, (selectedRecipe.X12, selectedRecipe.Y12, selectedRecipe.S12) },
        //        };

        //        for (int i = 1; i <= totalItems; i++)
        //        {
        //            if (coordMap.TryGetValue(i, out var data))
        //            {
        //                positions.Add(new ServoPositionModel
        //                {
        //                    PositionId = i,
        //                    Name = $"Position {i}",
        //                    SequenceIndex = data.Seq,
        //                    X = data.X,
        //                    Y = data.Y,
        //                    IsEnabled = true
        //                });
        //            }
        //        }

        //        _logger.LogInfo($"LoadPositionsAsync: Loaded {positions.Count} positions from recipe '{selectedRecipe.ProductCode}'.", LogType.Diagnostics);
        //        return positions;
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError($"LoadPositionsAsync failed: {ex.Message}", LogType.Error);
        //        return await CreateDefaultPositionsAsync();
        //    }
        //}
        //Old 
        public async Task<List<ServoPositionModel>> LoadPositionsAsync()
        {
            if (!File.Exists(_filePath))
            {
                return await CreateDefaultPositionsAsync();
            }

            try
            {
                string json = await File.ReadAllTextAsync(_filePath);
                var data = JsonSerializer.Deserialize<List<ServoPositionModel>>(json);

                // Integrity check: If file exists but is empty or missing sequences
                if (data == null || data.Count == 0)
                {
                    return await CreateDefaultPositionsAsync();
                }
                return data;
            }
            catch
            {
                return await CreateDefaultPositionsAsync();
            }
        }

        public async Task<List<ServoRecipeModel>> LoadRecipeAsync()                          //Modifed by Rishabh -Date 11/05/2026
        {
            var recipes = new List<ServoRecipeModel>();
            try
            {
                var lines = await File.ReadAllLinesAsync(_RecipeFilePath);               

                if (lines.Length < 2) // Need header + at least one data row
                {
                    return recipes;
                }

                var headers = lines[0].Split(',').Select(h => h.Trim()).ToArray();

                for (int i = 1; i < lines.Length; i++)
                {
                    var values = lines[i].Split(',').Select(v => v.Trim()).ToArray();

                    if (values.Length != headers.Length)
                        continue; // Skip malformed rows

                    var recipe = new ServoRecipeModel
                    {
                        ProgramNo = int.TryParse(values[0], out int pNo) ? pNo : 0,
                        S1 = int.TryParse(values[1], out int s1) ? s1 : 0,
                        S2 = int.TryParse(values[2], out int s2) ? s2 : 0,
                        S3 = int.TryParse(values[3], out int s3) ? s3 : 0,
                        S4 = int.TryParse(values[4], out int s4) ? s4 : 0,
                        S5 = int.TryParse(values[5], out int s5) ? s5 : 0,
                        S6 = int.TryParse(values[6], out int s6) ? s6 : 0,
                        S7 = int.TryParse(values[7], out int s7) ? s7 : 0,
                        S8 = int.TryParse(values[8], out int s8) ? s8 : 0,
                        S9 = int.TryParse(values[9], out int s9) ? s9 : 0,
                        S10 = int.TryParse(values[10], out int s10) ? s10 : 0,
                        S11 = int.TryParse(values[11], out int s11) ? s11 : 0,
                        S12 = int.TryParse(values[12], out int s12) ? s12 : 0,
                        X0 = double.TryParse(values[13], out double x0) ? x0 : 0,
                        X1 = double.TryParse(values[14], out double x1) ? x1 : 0,
                        X2 = double.TryParse(values[15], out double x2) ? x2 : 0,
                        X3 = double.TryParse(values[16], out double x3) ? x3 : 0,
                        X4 = double.TryParse(values[17], out double x4) ? x4 : 0,
                        X5 = double.TryParse(values[18], out double x5) ? x5 : 0,
                        X6 = double.TryParse(values[19], out double x6) ? x6 : 0,
                        X7 = double.TryParse(values[20], out double x7) ? x7 : 0,
                        X8 = double.TryParse(values[21], out double x8) ? x8 : 0,
                        X9 = double.TryParse(values[22], out double x9) ? x9 : 0,
                        X10 = double.TryParse(values[23], out double x10) ? x10 : 0,
                        X11 = double.TryParse(values[24], out double x11) ? x11 : 0,
                        X12 = double.TryParse(values[25], out double x12) ? x12 : 0,
                        Y0 = double.TryParse(values[26], out double Y0) ? Y0 : 0,
                        Y1 = double.TryParse(values[27], out double Y1) ? Y1 : 0,
                        Y2 = double.TryParse(values[28], out double Y2) ? Y2 : 0,
                        Y3 = double.TryParse(values[29], out double Y3) ? Y3 : 0,
                        Y4 = double.TryParse(values[30], out double Y4) ? Y4 : 0,
                        Y5 = double.TryParse(values[31], out double Y5) ? Y5 : 0,
                        Y6 = double.TryParse(values[32], out double Y6) ? Y6 : 0,
                        Y7 = double.TryParse(values[33], out double Y7) ? Y7 : 0,
                        Y8 = double.TryParse(values[34], out double Y8) ? Y8 : 0,
                        Y9 = double.TryParse(values[35], out double Y9) ? Y9 : 0,
                        Y10 = double.TryParse(values[36], out double Y10) ? Y10 : 0,
                        Y11 = double.TryParse(values[37], out double Y11) ? Y11 : 0,
                        Y12 = double.TryParse(values[38], out double Y12) ? Y12 : 0,
                        Xmin = double.TryParse(values[39], out double xmin) ? xmin : 0,
                        Xmax = double.TryParse(values[40], out double xmax) ? xmax : 0,
                        Ymin = double.TryParse(values[41], out double ymin) ? ymin : 0,
                        Ymax = double.TryParse(values[42], out double ymax) ? ymax : 0,
                        AngleMin = double.TryParse(values[43], out double amin) ? amin : 0,
                        AngleMax = double.TryParse(values[44], out double amax) ? amax : 0,
                        ProductName = values[45],
                        ProductCode = values[46],
                        TotalItems = int.TryParse(values[47], out int totalitem) ? totalitem : 0,
                        GridRows = int.TryParse(values[48], out int gridrow) ? gridrow : 0,
                        GridColumns = int.TryParse(values[49], out int gridcols) ? gridcols : 0,

                    };

                    recipes.Add(recipe);
                }

                
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to load recipe file: {ex.Message}",LogType.Error);
            }

            return recipes;
        }


        public async Task SavePositionsAsync(List<ServoPositionModel> positions)
        {
            var options = new JsonSerializerOptions { WriteIndented = true };
            string json = JsonSerializer.Serialize(positions, options);
            await File.WriteAllTextAsync(_filePath, json);
        }

        private async Task<List<ServoPositionModel>> CreateDefaultPositionsAsync()
        {
            var list = new List<ServoPositionModel>();
           // var productSettings = await _productService.LoadAsync();
            var savedRecipes = await LoadRecipeAsync();
            int totalItem = savedRecipes.LastOrDefault()?.TotalItems ?? 0;
            //int totalItems = productSettings.TotalItems > 0 ? productSettings.TotalItems : 12; // Default 12 if 0
            int totalItems = totalItem > 0 ? totalItem : 12; // Default 12 if 0


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
            for (int i = 1; i <= totalItems; i++)
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



        private ServoPositionModel ParseRecipeCsvLine(string line)
        {
            try
            {
                var values = SplitCsvLine(line);
                if (values.Count < 50) // Expecting at least 50 columns based on the model
                    return null;
                var model = new ServoPositionModel
                {
                    PositionId = int.TryParse(values[0], out int posId) ? posId : 0,
                    Name = values[1],
                    SequenceIndex = int.TryParse(values[2], out int seq) ? seq : 0,
                    X = double.TryParse(values[3], out double x) ? x : 0,
                    Y = double.TryParse(values[4], out double y) ? y : 0
                };
                return model;
            }
            catch(Exception ex)
            {
                _logger.LogError($"Failed to parse recipe CSV line: {ex.Message}", LogType.Error);
                return null; // Return null if parsing fails
            }

        }



        // ==================== HELPERS ====================

        private List<string> SplitCsvLine(string line)
        {
            var values = new List<string>();
            var currentValue = new StringBuilder();
            bool inQuotes = false;

            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];

                if (c == '"')
                {
                    if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                    {
                        currentValue.Append('"');
                        i++;
                    }
                    else
                    {
                        inQuotes = !inQuotes;
                    }
                }
                else if (c == ',' && !inQuotes)
                {
                    values.Add(currentValue.ToString());
                    currentValue.Clear();
                }
                else
                {
                    currentValue.Append(c);
                }
            }

            values.Add(currentValue.ToString());
            return values;
        }




    }
}