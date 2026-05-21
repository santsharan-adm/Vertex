using IPCSoftware.App.ViewModels;
using IPCSoftware.Core.Interfaces;
using IPCSoftware.Core.Interfaces.AppLoggerInterface;
using IPCSoftware.Shared;
using IPCSoftware.Shared.Models;
using IPCSoftware.Shared.Models.ConfigModels;
using System.Threading.Tasks;

namespace IPCSoftware.App.Services
{
    public class RecipeApplicationService : IRecipeApplicationService
    {
        private readonly CoreClient _coreClient;
        private readonly IAeLimitService _aeLimitService;
        private readonly ServoCalibrationViewModel _servoViewModel;
        private readonly IAppLogger _logger;

        public RecipeApplicationService(
            CoreClient coreClient,
            IAeLimitService aeLimitService,
            IAppLogger logger,
            ServoCalibrationViewModel servoViewModel)
        {
            _coreClient = coreClient;
            _aeLimitService = aeLimitService;
            _logger = logger;
            _servoViewModel = servoViewModel;
        }

        public async Task ApplyRecipeToPlcAsync(ServoRecipeModel recipe)
        {

            try
            {
                _servoViewModel.WriteSelectedRecipeAsync(recipe);
            }

            catch (Exception ex)
            {
                _logger.LogError($"Error writing sequence indexes to PLC: {ex.Message}", LogType.Error);
            }

        }

        public async Task SaveAeLimitsAsync()
        {
            var settings = await _aeLimitService.GetSettingsAsync();
         
        }

        public async Task PulseBitAsync(int tagId, string description)
        {
            _logger.LogInfo($"Pulsing bit {tagId}: {description}", LogType.Audit);
            
            await _coreClient.WriteTagAsync(tagId, 1);
            await Task.Delay(200);
            await _coreClient.WriteTagAsync(tagId, 0);
        }

        //private async Task WriteSequenceIndexesToPlc()
        //{

        //}

    }
}