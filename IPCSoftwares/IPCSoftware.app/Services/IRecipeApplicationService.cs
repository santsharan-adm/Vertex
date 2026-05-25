using IPCSoftware.Shared.Models;
using System.Threading.Tasks;

namespace IPCSoftware.App.Services
{
    public interface IRecipeApplicationService
    {
        Task<Dictionary<int, bool>> ApplyRecipeToPlcAsync(ServoRecipeModel recipe);

        Task<ServoRecipeModel> GetRecipefromSelection();
        Task SaveAeLimitsAsync();
        Task PulseBitAsync(int tagId, string description);
    }
}