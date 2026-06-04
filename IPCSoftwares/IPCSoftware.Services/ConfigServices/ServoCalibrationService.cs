using IPCSoftware.App.Services;
using IPCSoftware.Core.Interfaces;
using IPCSoftware.Core.Interfaces.AppLoggerInterface;
using IPCSoftware.Shared.Models;
using IPCSoftware.Shared.Models.ConfigModels;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;
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
        //private readonly IProductConfigurationService _productService;
        //private readonly IRecipeApplicationService _recipeApplicationService;
        private readonly IAppLogger _logger;

        private readonly string _RecipeFilePath;
        private readonly int _currentRunningProgram;
        private readonly string _appSettingsPath; // For current program number


        public ServoCalibrationService(IOptions<ConfigSettings> configSettings,IAppLogger logger)
        {
            //string folder = configSettings.Value.DataFolder ?? AppContext.BaseDirectory;
            _logger = logger;
            var config = configSettings.Value;
            //var sharedConfigDir = Environment.GetEnvironmentVariable("CONFIG_DIR");
            //var baseDir = !string.IsNullOrWhiteSpace(sharedConfigDir) && Directory.Exists(sharedConfigDir)
            //              ? sharedConfigDir
            //              : AppContext.BaseDirectory;

            _appSettingsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");
          
            string dataFolderPath = config.DataFolder;
            //_productService = productService;
            _dataFolder = dataFolderPath ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data");
          //  _filePath = Path.Combine(folder, "ServoCalibration.json");
            //_filePath =  Path.Combine(_dataFolder, config.ServoCalibrationFileName );
            _RecipeFilePath = Path.Combine(_dataFolder, config.ServoRecipeFileName);
            //_recipeApplicationService = recipeApplicationService;
            _currentRunningProgram = config.CurrentRunningProgram;

        }
           

        public async Task<List<ServoPositionModel>> LoadPositionsAsync()
        {
            try
            {
                var recipe = await GetRecipeByProgramNumberAsync();
                if (recipe == null) 
                {
                    _logger.LogWarning("[ServoCalibrationService] LoadPositionAsync :No recipe found , falling back to default ", LogType.Diagnostics);
                    return await CreateDefaultPositionsAsync();
                }
                int totalItems = recipe.TotalItems > 0 ? recipe.TotalItems : 12;
                var positions = new List<ServoPositionModel>
                {
                    new ServoPositionModel
                    {
                        PositionId = 0,
                        Name       = "Position 0 (Home)",
                        SequenceIndex = 0,
                        X = recipe.X0,
                        Y = recipe.Y0,
                        IsEnabled = true

                    }
                };

                // Map PositionId -> (X, Y, SequenceIndex) from recipe fields
                var coordMap = new Dictionary<int, (double X, double Y, int Seq)>
                {
                    { 1,  (recipe.X1,  recipe.Y1,  recipe.S1)  },
                    { 2,  (recipe.X2,  recipe.Y2,  recipe.S2)  },
                    { 3,  (recipe.X3,  recipe.Y3,  recipe.S3)  },
                    { 4,  (recipe.X4,  recipe.Y4,  recipe.S4)  },
                    { 5,  (recipe.X5,  recipe.Y5,  recipe.S5)  },
                    { 6,  (recipe.X6,  recipe.Y6,  recipe.S6)  },
                    { 7,  (recipe.X7,  recipe.Y7,  recipe.S7)  },
                    { 8,  (recipe.X8,  recipe.Y8,  recipe.S8)  },
                    { 9,  (recipe.X9,  recipe.Y9,  recipe.S9)  },
                    { 10, (recipe.X10, recipe.Y10, recipe.S10) },
                    { 11, (recipe.X11, recipe.Y11, recipe.S11) },
                    { 12, (recipe.X12, recipe.Y12, recipe.S12) },
                };


                for (int i = 1; i <= totalItems; i++)
                {
                    if (coordMap.TryGetValue(i, out var data))
                    {
                        positions.Add(new ServoPositionModel
                        {
                            PositionId = i,
                            Name = $"Position {i}",
                            SequenceIndex = data.Seq,
                            X = data.X,
                            Y = data.Y,
                            IsEnabled = true
                        });
                    }
                }

                _logger.LogInfo($"[ServoCalibrationService] LoadPositionsAsync: Loaded {positions.Count} positions from recipe '{recipe.ProductCode}' (ProgramNo={recipe.ProgramNo}).", LogType.Diagnostics);
                return positions;
            }

            catch (Exception ex)
            {
                _logger.LogError($"[ServoCalibrationService] LoadPositionsAsync failed: {ex.Message}", LogType.Error);
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
                            IsChecked = bool.TryParse(values[1], out bool isEn) ? isEn : true, // Added field
                            S1 = int.TryParse(values[2], out int s1) ? s1 : 0,
                            S2 = int.TryParse(values[3], out int s2) ? s2 : 0,
                            S3 = int.TryParse(values[4], out int s3) ? s3 : 0,
                            S4 = int.TryParse(values[5], out int s4) ? s4 : 0,
                            S5 = int.TryParse(values[6], out int s5) ? s5 : 0,
                            S6 = int.TryParse(values[7], out int s6) ? s6 : 0,
                            S7 = int.TryParse(values[8], out int s7) ? s7 : 0,
                            S8 = int.TryParse(values[9], out int s8) ? s8 : 0,
                            S9 = int.TryParse(values[10], out int s9) ? s9 : 0,
                            S10 = int.TryParse(values[11], out int s10) ? s10 : 0,
                            S11 = int.TryParse(values[12], out int s11) ? s11 : 0,
                            S12 = int.TryParse(values[13], out int s12) ? s12 : 0,
                            X0 = double.TryParse(values[14], out double x0) ? x0 : 0,
                            X1 = double.TryParse(values[15], out double x1) ? x1 : 0,
                            X2 = double.TryParse(values[16], out double x2) ? x2 : 0,
                            X3 = double.TryParse(values[17], out double x3) ? x3 : 0,
                            X4 = double.TryParse(values[18], out double x4) ? x4 : 0,
                            X5 = double.TryParse(values[19], out double x5) ? x5 : 0,
                            X6 = double.TryParse(values[20], out double x6) ? x6 : 0,
                            X7 = double.TryParse(values[21], out double x7) ? x7 : 0,
                            X8 = double.TryParse(values[22], out double x8) ? x8 : 0,
                            X9 = double.TryParse(values[23], out double x9) ? x9 : 0,
                            X10 = double.TryParse(values[24], out double x10) ? x10 : 0,
                            X11 = double.TryParse(values[25], out double x11) ? x11 : 0,
                            X12 = double.TryParse(values[26], out double x12) ? x12 : 0,
                            Y0 = double.TryParse(values[27], out double y0) ? y0 : 0,
                            Y1 = double.TryParse(values[28], out double y1) ? y1 : 0,
                            Y2 = double.TryParse(values[29], out double y2) ? y2 : 0,
                            Y3 = double.TryParse(values[30], out double y3) ? y3 : 0,
                            Y4 = double.TryParse(values[31], out double y4) ? y4 : 0,
                            Y5 = double.TryParse(values[32], out double y5) ? y5 : 0,
                            Y6 = double.TryParse(values[33], out double y6) ? y6 : 0,
                            Y7 = double.TryParse(values[34], out double y7) ? y7 : 0,
                            Y8 = double.TryParse(values[35], out double y8) ? y8 : 0,
                            Y9 = double.TryParse(values[36], out double y9) ? y9 : 0,
                            Y10 = double.TryParse(values[37], out double y10) ? y10 : 0,
                            Y11 = double.TryParse(values[38], out double y11) ? y11 : 0,
                            Y12 = double.TryParse(values[39], out double y12) ? y12 : 0,
                            Xmin = double.TryParse(values[40], out double xmin) ? xmin : 0,
                            Xmax = double.TryParse(values[41], out double xmax) ? xmax : 0,
                            Ymin = double.TryParse(values[42], out double ymin) ? ymin : 0,
                            Ymax = double.TryParse(values[43], out double ymax) ? ymax : 0,
                            AngleMin = double.TryParse(values[44], out double amin) ? amin : 0,
                            AngleMax = double.TryParse(values[45], out double amax) ? amax : 0,
                            ProductName = values[46],
                            ProductCode = values[47],
                            TotalItems = int.TryParse(values[48], out int totalitem) ? totalitem : 0,
                            GridRows = int.TryParse(values[49], out int gridrow) ? gridrow : 0,
                            GridColumns = int.TryParse(values[50], out int gridcols) ? gridcols : 0,
                            Position_0 = int.TryParse(values[51], out int p0) ? p0 : 0,
                            Position_1 = int.TryParse(values[52], out int p1) ? p1 : 0,
                            Position_2 = int.TryParse(values[53], out int p2) ? p2 : 0,
                            Position_3 = int.TryParse(values[54], out int p3) ? p3 : 0,
                            Position_4 = int.TryParse(values[55], out int p4) ? p4 : 0,
                            Position_5 = int.TryParse(values[56], out int p5) ? p5 : 0,
                            Position_6 = int.TryParse(values[57], out int p6) ? p6 : 0,
                            Position_7 = int.TryParse(values[58], out int p7) ? p7 : 0,
                            Position_8 = int.TryParse(values[59], out int p8) ? p8 : 0,
                            Position_9 = int.TryParse(values[60], out int p9) ? p9 : 0,
                            Position_10 = int.TryParse(values[61], out int p10) ? p10 : 0,
                            Position_11 = int.TryParse(values[62], out int p11) ? p11 : 0,
                            Position_12 = int.TryParse(values[63], out int p12) ? p12 : 0,
                            Name_0 = values[64],
                            Name_1 = values[65],
                            Name_2 = values[66],
                            Name_3 = values[67],
                            Name_4 = values[68],
                            Name_5 = values[69],
                            Name_6 = values[70],
                            Name_7 = values[71],
                            Name_8 = values[72],
                            Name_9 = values[73],
                            Name_10 = values[74],
                            Name_11 = values[75],
                            Name_12 = values[76],
                            Discription_0 = values[77],
                            Discription_1 = values[78],
                            Discription_2 = values[79],
                            Discription_3 = values[80],
                            Discription_4 = values[81],
                            Discription_5 = values[82],
                            Discription_6 = values[83],
                            Discription_7 = values[84],
                            Discription_8 = values[85],
                            Discription_9 = values[86],
                            Discription_10 = values[87],
                            Discription_11 = values[88],
                            Discription_12 = values[89],
                            Is_Enabled_0 = bool.TryParse(values[90], out bool ie0) ? ie0 : true,
                            Is_Enabled_1 = bool.TryParse(values[91], out bool ie1) ? ie1 : true,
                            Is_Enabled_2 = bool.TryParse(values[92], out bool ie2) ? ie2 : true,
                            Is_Enabled_3 = bool.TryParse(values[93], out bool ie3) ? ie3 : true,
                            Is_Enabled_4 = bool.TryParse(values[94], out bool ie4) ? ie4 : true,
                            Is_Enabled_5 = bool.TryParse(values[95], out bool ie5) ? ie5 : true,
                            Is_Enabled_6 = bool.TryParse(values[96], out bool ie6) ? ie6 : true,
                            Is_Enabled_7 = bool.TryParse(values[97], out bool ie7) ? ie7 : true,
                            Is_Enabled_8 = bool.TryParse(values[98], out bool ie8) ? ie8 : true,
                            Is_Enabled_9 = bool.TryParse(values[99], out bool ie9) ? ie9 : true,
                            Is_Enabled_10 = bool.TryParse(values[100], out bool ie10) ? ie10 : true,
                            Is_Enabled_11 = bool.TryParse(values[101], out bool ie11) ? ie11 : true,
                            Is_Enabled_12 = bool.TryParse(values[102], out bool ie12) ? ie12 : true
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
        //New method to get recipe by program number

        int targetProgramNo;
        public async Task<ServoRecipeModel> GetRecipeByProgramNumberAsync()
        {
            try
            {               

                // --- Scenario B: CoreService startup — UI hasn't provided a selection yet ---
                // If caller passes 0 or negative, fall back to appsettings.json
                //if (targetProgramNo <= 0)
                //{
                try
                    {
                        var json = File.ReadAllText(_appSettingsPath);
                        var jsonObj = JObject.Parse(json);

                        // Read from Config.CurrentRunningProgram (persisted by ModeOfOperationVM on last run)
                        var configSection = jsonObj["Config"];
                        if (configSection != null && configSection["CurrentRunningProgram"] != null)
                        {
                            targetProgramNo = configSection["CurrentRunningProgram"].Value<int>();
                            _logger.LogInfo($"[ServoCalibrationService] No UI selection — using CurrentRunningProgram={targetProgramNo} from appsettings.json.", LogType.Diagnostics);
                        }
                        else
                        {
                            _logger.LogWarning($"[ServoCalibrationService] appsettings.json Config.CurrentRunningProgram not found. Using startup default: {targetProgramNo}.", LogType.Diagnostics);
                        }
                    }
                    catch (Exception jsonEx)
                    {
                       
                        _logger.LogWarning($"[ServoCalibrationService] Failed to read appsettings.json, using startup default {targetProgramNo}: {jsonEx.Message}", LogType.Diagnostics);
                    }
                //}

                // --- Scenario A: UI selected a recipe — programNo was passed directly ---
                // Both paths now have a valid targetProgramNo, find recipe from CSV
                var recipes = await LoadRecipeAsync();
                var matched = recipes.FirstOrDefault(r => r.ProgramNo == targetProgramNo);

                if (matched == null)
                {
                    _logger.LogWarning($"[ServoCalibrationService] No recipe found for ProgramNo={targetProgramNo}. Falling back to last recipe.", LogType.Diagnostics);
                    matched = recipes.LastOrDefault(); // Safe fallback
                }

                return matched;
            }
            catch (Exception ex)
            {
                _logger.LogError($"[ServoCalibrationService] GetRecipeByProgramNumberAsync failed for ProgramNo={targetProgramNo}: {ex.Message}", LogType.Error);
                return null;
            }
        }




    }
}