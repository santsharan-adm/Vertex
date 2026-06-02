using IPCSoftware.Core.Interfaces;
using IPCSoftware.Core.Interfaces.AppLoggerInterface;
using IPCSoftware.Shared;
using IPCSoftware.Shared.Models;
using IPCSoftware.Shared.Models.ConfigModels;
using Newtonsoft.Json.Linq;
using System.ComponentModel;
using System.IO;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;



namespace IPCSoftware.Services.ConfigServices
{
    public class RecipeApplicationService : IRecipeApplicationService ,INotifyPropertyChanged
    {       
        //private readonly IAeLimitService _aeLimitService;
        //private readonly IMachineDataHandler _plcRecipeWriter;
        private readonly IAppLogger _logger;
        private ServoRecipeModel _lastSelectedRecipe;
        private readonly IServoCalibrationService _servoService;
        private readonly string _appSettingsPath; // For saving current program number

        public event PropertyChangedEventHandler? PropertyChanged;

        public RecipeApplicationService(            
            //IAeLimitService aeLimitService,
            IAppLogger logger,
            IServoCalibrationService servoService
            /*IMachineDataHandler plcRecipeWriter*/ )
        {
           
            //_aeLimitService = aeLimitService;
            _logger = logger;            
            _servoService = servoService;
            //_plcRecipeWriter = plcRecipeWriter;
            _appSettingsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");
        }





        public async Task<ServoRecipeModel> GetRecipefromSelection()
        {
            try
            {
                var lastrecipe = await _servoService.GetRecipeByProgramNumberAsync();
                return lastrecipe; 
               
            }
            catch (Exception ex) 
            { 
                _logger.LogWarning($"Unable to get recipe {_lastSelectedRecipe}: {ex}", LogType.Error);
                return new ServoRecipeModel(); 
            }
        }


    }
}