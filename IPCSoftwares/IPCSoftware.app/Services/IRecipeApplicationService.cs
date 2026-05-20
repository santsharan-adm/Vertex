using IPCSoftware.Shared.Models;
using System.Threading.Tasks;

namespace IPCSoftware.App.Services
{
    public interface IRecipeApplicationService
    {
        Task ApplyRecipeToPlcAsync(ServoRecipeModel recipe);
        Task SaveAeLimitsAsync();
        Task PulseBitAsync(int tagId, string description);
    }
}