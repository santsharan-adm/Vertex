using IPCSoftware.Core.Interfaces;
using IPCSoftware.Core.Interfaces.AppLoggerInterface;
using IPCSoftware.Shared;
using IPCSoftware.Shared.Models;
using IPCSoftware.Shared.Models.ConfigModels;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using IPCSoftware.App.ViewModels;
using System.IO;

namespace IPCSoftware.App.Services
{

    public class MachineDataHandler : IMachineDataHandler
    {
        private readonly IAppLogger _logger;
        private readonly IPLCTagConfigurationService _tagService;
        private ServoRecipeModel _lastSelectedRecipe;
        private readonly string _appSettingsPath; // For saving current program number
        private readonly ServoCalibrationViewModel _calibrationViewModel;
        public MachineDataHandler(
            IAppLogger logger,
            IPLCTagConfigurationService tagService , ServoCalibrationViewModel calibrationViewModel)
        {
            _logger = logger;
            _tagService = tagService;
            _calibrationViewModel = calibrationViewModel;
            _appSettingsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");
        }

        //public async Task<Dictionary<int, bool>> WriteSelectedRecipeAsync(ServoRecipeModel recipe)
        //{
        //    // NOTE: In CoreService context this is called via RecipeApplicationService.
        //    // Actual PLC writes happen inside CoreService through its own services.
        //    // This handler acts as a pass-through/coordinator for the service layer.
        //    var result = new Dictionary<int, bool>();

        //    try
        //    {
        //        _logger.LogInfo($"[MachineDataHandler] Writing recipe '{recipe.ProductCode}' (ProgramNo={recipe.ProgramNo})", LogType.Audit);

        //        // Coordinates and AE limits are written by CoreService directly.
        //        // This layer just signals success so RecipeApplicationService can proceed.
        //        result[1] = true; // Sequence/Coordinate write delegated to CoreService
        //        result[2] = true; // TotalItems sync delegated to CoreService
        //        result[3] = true; // AE Limits delegated to CoreService
        //        result[4] = true; // Handshake delegated to CoreService
        //    }
        //    catch (System.Exception ex)
        //    {
        //        _logger.LogError($"[MachineDataHandler] Error: {ex.Message}", LogType.Error);
        //        result[1] = false;
        //    }

        //    return result;
        //}


        public async Task<Dictionary<int, bool>> ApplyRecipeToPlcAsync(ServoRecipeModel recipe)
        {
            var dict = new Dictionary<int, bool>();   // here dictionary dict will have ( "1": Confirmation of Seq+X|Y Coord write to plc , "2": No_Of_Station to plc , "3" : AE Limits write to plc , "4" : Confirmation from plc that AE limits is write successfully - as bool formate) after returning from ServoCalib VM

            try
            {


                //dict = await _plcRecipeWriter.WriteSelectedRecipeAsync(recipe);
                //if (dict.ContainsKey(1)) { bool result1 = dict[1]; }
                _lastSelectedRecipe = recipe;
                dict = await _calibrationViewModel.WriteSelectedRecipeAsync(recipe);
                // Persist CurrentRunningProgram into Config section of appsettings.json
                try
                {
                    var json = File.ReadAllText(_appSettingsPath);
                    var jsonObj = JObject.Parse(json);

                    // Navigate to Config section (where CurrentRunningProgram lives)
                    if (jsonObj["Config"] == null)
                        jsonObj["Config"] = new JObject();

                    jsonObj["Config"]["CurrentRunningProgram"] = _lastSelectedRecipe.ProgramNo;

                    File.WriteAllText(_appSettingsPath, jsonObj.ToString());
                    _logger.LogInfo($"[ModeOfOperation] CurrentRunningProgram updated to {_lastSelectedRecipe.ProgramNo} in appsettings.json.", LogType.Audit);
                }
                catch (Exception jsonEx)
                {
                    // Non-critical: log but don't block the user
                    _logger.LogError($"Failed to persist CurrentRunningProgram: {jsonEx.Message}", LogType.Diagnostics);
                }

                return dict;
            }

            catch (Exception ex)
            {
                _logger.LogError($"Error writing sequence indexes to PLC: {ex.Message}", LogType.Error);
                return dict = new Dictionary<int, bool>();

            }

        }
    }
}