using IPCSoftware.Core.Interfaces;
using IPCSoftware.Core.Interfaces.AppLoggerInterface;
using IPCSoftware.Shared.Models;
using IPCSoftware.Shared.Models.ConfigModels;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IPCSoftware.Services.ConfigServices
{
    public class RecipeManagementService : IRecipeManagementService
    {
        private readonly string _recipeFilePath;
        private readonly IAppLogger _logger;
        private readonly IServoCalibrationService _servoService;

        public RecipeManagementService(
            IOptions<ConfigSettings> configSettings,
            IAppLogger logger,
            IServoCalibrationService servoService)
        {
            _logger = logger;
            _servoService = servoService;

            var config = configSettings.Value;
            string dataFolder = config.DataFolder ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data");
            _recipeFilePath = Path.Combine(dataFolder, config.ServoRecipeFileName);

            EnsureFileExists();
        }

        private void EnsureFileExists()
        {
            if (!File.Exists(_recipeFilePath))
            {
                CreateDefaultRecipeFile();
            }
        }

        private void CreateDefaultRecipeFile()
        {
            try
            {
                var header = "ProgramNo,IsChecked,S1,S2,S3,S4,S5,S6,S7,S8,S9,S10,S11,S12," +
                         "X0,X1,X2,X3,X4,X5,X6,X7,X8,X9,X10,X11,X12," +
                         "Y0,Y1,Y2,Y3,Y4,Y5,Y6,Y7,Y8,Y9,Y10,Y11,Y12," +
                         "Xmin,Xmax,Ymin,Ymax,AngleMin,AngleMax,ProductName,ProductCode,TotalItems,GridRows,GridColumns," +
                         "PositionID_0,PositionID_1,PositionID_2,PositionID_3,PositionID_4,PositionID_5,PositionID_6,PositionID_7,PositionID_8,PositionID_9,PositionID_10,PositionID_11,PositionID_12," +
                         "Name_0,Name_1,Name_2,Name_3,Name_4,Name_5,Name_6,Name_7,Name_8,Name_9,Name_10,Name_11,Name_12," +
                         "Description_0,Description_1,Description_2,Description_3,Description_4,Description_5,Description_6,Description_7,Description_8,Description_9,Description_10,Description_11,Description_12," +
                         "IsEnabled_0,IsEnabled_1,IsEnabled_2,IsEnabled_3,IsEnabled_4,IsEnabled_5,IsEnabled_6,IsEnabled_7,IsEnabled_8,IsEnabled_9,IsEnabled_10,IsEnabled_11,IsEnabled_12";

                File.WriteAllText(_recipeFilePath, header + Environment.NewLine);
                _logger.LogInfo("Recipe file created with default header.", LogType.Audit);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to create recipe file: {ex.Message}", LogType.Diagnostics);
            }
        }

        public async Task<bool> AddRecipeAsync(ServoRecipeModel newRecipe)
        {
            try
            {
                // 1. Check if program number already exists
                var existingRecipes = await _servoService.LoadRecipeAsync();
                if (existingRecipes.Any(r => r.ProgramNo == newRecipe.ProgramNo))
                {
                    _logger.LogWarning($"Program Number {newRecipe.ProgramNo} already exists.", LogType.Audit);
                    return false;
                }

                // 2. Append new recipe to CSV
                var csvLine = BuildCsvLine(newRecipe);
                await File.AppendAllTextAsync(_recipeFilePath, csvLine + Environment.NewLine);

                _logger.LogInfo($"Recipe added successfully: Program {newRecipe.ProgramNo}", LogType.Audit);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to add recipe: {ex.Message}", LogType.Diagnostics);
                return false;
            }
        }

        public async Task<bool> UpdateRecipeAsync(ServoRecipeModel updatedRecipe)
        {
            try
            {
                var recipes = await _servoService.LoadRecipeAsync();
                var existingIndex = recipes.FindIndex(r => r.ProgramNo == updatedRecipe.ProgramNo);

                if (existingIndex == -1)
                {
                    _logger.LogWarning($"Program Number {updatedRecipe.ProgramNo} not found for update.", LogType.Audit);
                    return false;
                }

                // Replace the recipe
                recipes[existingIndex] = updatedRecipe;

                // Rewrite entire file
                await SaveAllRecipesAsync(recipes);

                _logger.LogInfo($"Recipe updated successfully: Program {updatedRecipe.ProgramNo}", LogType.Audit);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to update recipe: {ex.Message}", LogType.Diagnostics);
                return false;
            }
        }

        public async Task<bool> DeleteRecipeAsync(int programNo)
        {
            try
            {
                var recipes = await _servoService.LoadRecipeAsync();
                var recipeToDelete = recipes.FirstOrDefault(r => r.ProgramNo == programNo);

                if (recipeToDelete == null)
                {
                    _logger.LogWarning($"Program Number {programNo} not found for deletion.", LogType.Audit);
                    return false;
                }

                recipes.Remove(recipeToDelete);
                await SaveAllRecipesAsync(recipes);

                _logger.LogInfo($"Recipe deleted successfully: Program {programNo}", LogType.Audit);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to delete recipe: {ex.Message}", LogType.Diagnostics);
                return false;
            }
        }

        private async Task SaveAllRecipesAsync(List<ServoRecipeModel> recipes)
        {
            var sb = new StringBuilder();

            // Header
            sb.AppendLine("ProgramNo,IsChecked,S1,S2,S3,S4,S5,S6,S7,S8,S9,S10,S11,S12," +
                         "X0,X1,X2,X3,X4,X5,X6,X7,X8,X9,X10,X11,X12," +
                         "Y0,Y1,Y2,Y3,Y4,Y5,Y6,Y7,Y8,Y9,Y10,Y11,Y12," +
                         "Xmin,Xmax,Ymin,Ymax,AngleMin,AngleMax,ProductName,ProductCode,TotalItems,GridRows,GridColumns,"+
                         "PositionID_0,PositionID_1,PositionID_2,PositionID_3,PositionID_4,PositionID_5,PositionID_6,PositionID_7,PositionID_8,PositionID_9,PositionID_10,PositionID_11,PositionID_12,"+
                         "Name_0,Name_1,Name_2,Name_3,Name_4,Name_5,Name_6,Name_7,Name_8,Name_9,Name_10,Name_11,Name_12,"+
                         "Description_0,Description_1,Description_2,Description_3,Description_4,Description_5,Description_6,Description_7,Description_8,Description_9,Description_10,Description_11,Description_12,"+
                         "IsEnabled_0,IsEnabled_1,IsEnabled_2,IsEnabled_3,IsEnabled_4,IsEnabled_5,IsEnabled_6,IsEnabled_7,IsEnabled_8,IsEnabled_9,IsEnabled_10,IsEnabled_11,IsEnabled_12");
                 
            // Data rows
            foreach (var recipe in recipes)
            {
                sb.AppendLine(BuildCsvLine(recipe));
            }

            await File.WriteAllTextAsync(_recipeFilePath, sb.ToString());
        }

        private string BuildCsvLine(ServoRecipeModel recipe)
        {
            return $"{recipe.ProgramNo},{recipe.IsChecked}," +
                   $"{recipe.S1},{recipe.S2},{recipe.S3},{recipe.S4},{recipe.S5},{recipe.S6}," +
                   $"{recipe.S7},{recipe.S8},{recipe.S9},{recipe.S10},{recipe.S11},{recipe.S12}," +
                   $"{FormatDouble(recipe.X0)},{FormatDouble(recipe.X1)},{FormatDouble(recipe.X2)},{FormatDouble(recipe.X3)}," +
                   $"{FormatDouble(recipe.X4)},{FormatDouble(recipe.X5)},{FormatDouble(recipe.X6)},{FormatDouble(recipe.X7)}," +
                   $"{FormatDouble(recipe.X8)},{FormatDouble(recipe.X9)},{FormatDouble(recipe.X10)},{FormatDouble(recipe.X11)},{FormatDouble(recipe.X12)}," +
                   $"{FormatDouble(recipe.Y0)},{FormatDouble(recipe.Y1)},{FormatDouble(recipe.Y2)},{FormatDouble(recipe.Y3)}," +
                   $"{FormatDouble(recipe.Y4)},{FormatDouble(recipe.Y5)},{FormatDouble(recipe.Y6)},{FormatDouble(recipe.Y7)}," +
                   $"{FormatDouble(recipe.Y8)},{FormatDouble(recipe.Y9)},{FormatDouble(recipe.Y10)},{FormatDouble(recipe.Y11)},{FormatDouble(recipe.Y12)}," +
                   $"{FormatDouble(recipe.Xmin)},{FormatDouble(recipe.Xmax)},{FormatDouble(recipe.Ymin)},{FormatDouble(recipe.Ymax)}," +
                   $"{FormatDouble(recipe.AngleMin)},{FormatDouble(recipe.AngleMax)},"+
                   $"{recipe.ProductName},{recipe.ProductCode},{recipe.TotalItems},{recipe.GridRows},{recipe.GridColumns},"+
                   $"{recipe.Position_0},{recipe.Position_1},{recipe.Position_2},{recipe.Position_3},{recipe.Position_4},{recipe.Position_5},{recipe.Position_6},{recipe.Position_7},{recipe.Position_8},{recipe.Position_9},{recipe.Position_10},{recipe.Position_11},{recipe.Position_12},"+
                   $"{recipe.Name_0},{recipe.Name_1},{recipe.Name_2},{recipe.Name_3},{recipe.Name_4},{recipe.Name_5},{recipe.Name_6},{recipe.Name_7},{recipe.Name_8},{recipe.Name_9},{recipe.Name_10},{recipe.Name_11},{recipe.Name_12},"+
                   $"{recipe.Discription_0},{recipe.Discription_1},{recipe.Discription_2},{recipe.Discription_3},{recipe.Discription_4},{recipe.Discription_5},{recipe.Discription_6},{recipe.Discription_7},{recipe.Discription_8},{recipe.Discription_9},{recipe.Discription_10},{recipe.Discription_11},{recipe.Discription_12},"+
                   $"{recipe.Is_Enabled_0},{recipe.Is_Enabled_1},{recipe.Is_Enabled_2},{recipe.Is_Enabled_3},{recipe.Is_Enabled_4},{recipe.Is_Enabled_5},{recipe.Is_Enabled_6},{recipe.Is_Enabled_7},{recipe.Is_Enabled_8},{recipe.Is_Enabled_9},{recipe.Is_Enabled_10},{recipe.Is_Enabled_11},{recipe.Is_Enabled_12}"
                   ;
        }

        private string FormatDouble(double value)
        {
            return value.ToString("F2", CultureInfo.InvariantCulture);
        }
    }
}