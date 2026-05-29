using IPCSoftware.Core.Interfaces;
using IPCSoftware.Core.Interfaces.AppLoggerInterface;
using IPCSoftware.Shared;
using IPCSoftware.Shared.Models;
using IPCSoftware.Shared.Models.ConfigModels;
using System.Threading.Tasks;

namespace IPCSoftware.Services.ConfigServices
{
    public class RecipeApplicationService : IRecipeApplicationService
    {       
        private readonly IAeLimitService _aeLimitService;
        private readonly IMachineDataHandler _plcRecipeWriter;
        private readonly IAppLogger _logger;
        private ServoRecipeModel _lastSelectedRecipe;

        public RecipeApplicationService(            
            IAeLimitService aeLimitService,
            IAppLogger logger,
            IMachineDataHandler plcRecipeWriter)
        {
           
            _aeLimitService = aeLimitService;
            _logger = logger;
            _plcRecipeWriter = plcRecipeWriter;
        }

        public async Task<Dictionary<int,bool>> ApplyRecipeToPlcAsync(ServoRecipeModel recipe)
        {
            var dict = new Dictionary<int, bool>();   // here dictionary dict will have ( "1": Confirmation of Seq+X|Y Coord write to plc , "2": No_Of_Station to plc , "3" : AE Limits write to plc , "4" : Confirmation from plc that AE limits is write successfully - as bool formate) after returning from ServoCalib VM

            try
            {
                dict = await _plcRecipeWriter.WriteSelectedRecipeAsync(recipe);
                //if (dict.ContainsKey(1)) { bool result1 = dict[1]; }
                _lastSelectedRecipe = recipe;
                return dict;
            }

            catch (Exception ex)
            {
                _logger.LogError($"Error writing sequence indexes to PLC: {ex.Message}", LogType.Error);
                return dict = new Dictionary<int,bool>();
                
            }

        }


        public async Task<ServoRecipeModel> GetRecipefromSelection()
        {
            try
            {
                if (_lastSelectedRecipe == null) { return new ServoRecipeModel(); }
                return _lastSelectedRecipe;
            }
            catch (Exception ex) { _logger.LogWarning($"Unable to get recipe {_lastSelectedRecipe}: {ex}", LogType.Error); return new ServoRecipeModel(); }
        }

        public async Task SaveAeLimitsAsync()
        {
            var settings = await _aeLimitService.GetSettingsAsync();
         
        }

        //public async Task PulseBitAsync(int tagId, string description)
        //{
        //    _logger.LogInfo($"Pulsing bit {tagId}: {description}", LogType.Audit);
            
        //    await _coreClient.WriteTagAsync(tagId, 1);
        //    await Task.Delay(200);
        //    await _coreClient.WriteTagAsync(tagId, 0);
        //}

        //private async Task WriteSequenceIndexesToPlc()
        //{

        //}

    }
}