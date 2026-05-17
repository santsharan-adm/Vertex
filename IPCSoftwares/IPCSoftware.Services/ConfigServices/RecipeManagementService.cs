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
                var header = "ProgramNo,S1,S2,S3,S4,S5,S6,S7,S8,S9,S10,S11,S12," +
                            "X0,X1,X2,X3,X4,X5,X6,X7,X8,X9,X10,X11,X12," +
                            "Y0,Y1,Y2,Y3,Y4,Y5,Y6,Y7,Y8,Y9,Y10,Y11,Y12," +
                            "Xmin,Xmax,Ymin,Ymax,AngleMin,AngleMax";

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
            sb.AppendLine("ProgramNo,S1,S2,S3,S4,S5,S6,S7,S8,S9,S10,S11,S12," +
                         "X0,X1,X2,X3,X4,X5,X6,X7,X8,X9,X10,X11,X12," +
                         "Y0,Y1,Y2,Y3,Y4,Y5,Y6,Y7,Y8,Y9,Y10,Y11,Y12," +
                         "Xmin,Xmax,Ymin,Ymax,AngleMin,AngleMax");

            // Data rows
            foreach (var recipe in recipes)
            {
                sb.AppendLine(BuildCsvLine(recipe));
            }

            await File.WriteAllTextAsync(_recipeFilePath, sb.ToString());
        }

        private string BuildCsvLine(ServoRecipeModel recipe)
        {
            return $"{recipe.ProgramNo}," +
                   $"{recipe.S1},{recipe.S2},{recipe.S3},{recipe.S4},{recipe.S5},{recipe.S6}," +
                   $"{recipe.S7},{recipe.S8},{recipe.S9},{recipe.S10},{recipe.S11},{recipe.S12}," +
                   $"{FormatDouble(recipe.X0)},{FormatDouble(recipe.X1)},{FormatDouble(recipe.X2)},{FormatDouble(recipe.X3)}," +
                   $"{FormatDouble(recipe.X4)},{FormatDouble(recipe.X5)},{FormatDouble(recipe.X6)},{FormatDouble(recipe.X7)}," +
                   $"{FormatDouble(recipe.X8)},{FormatDouble(recipe.X9)},{FormatDouble(recipe.X10)},{FormatDouble(recipe.X11)},{FormatDouble(recipe.X12)}," +
                   $"{FormatDouble(recipe.Y0)},{FormatDouble(recipe.Y1)},{FormatDouble(recipe.Y2)},{FormatDouble(recipe.Y3)}," +
                   $"{FormatDouble(recipe.Y4)},{FormatDouble(recipe.Y5)},{FormatDouble(recipe.Y6)},{FormatDouble(recipe.Y7)}," +
                   $"{FormatDouble(recipe.Y8)},{FormatDouble(recipe.Y9)},{FormatDouble(recipe.Y10)},{FormatDouble(recipe.Y11)},{FormatDouble(recipe.Y12)}," +
                   $"{FormatDouble(recipe.Xmin)},{FormatDouble(recipe.Xmax)},{FormatDouble(recipe.Ymin)},{FormatDouble(recipe.Ymax)}," +
                   $"{FormatDouble(recipe.AngleMin)},{FormatDouble(recipe.AngleMax)}";
        }

        private string FormatDouble(double value)
        {
            return value.ToString("F2", CultureInfo.InvariantCulture);
        }
    }
}