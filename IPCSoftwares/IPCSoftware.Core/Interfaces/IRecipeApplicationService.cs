using IPCSoftware.Shared.Models;
using System.ComponentModel;
using System.Threading.Tasks;

namespace IPCSoftware.Core.Interfaces
{
    public interface IRecipeApplicationService : INotifyPropertyChanged
    {
        //Task<Dictionary<int, bool>> ApplyRecipeToPlcAsync(ServoRecipeModel recipe);

        Task<ServoRecipeModel> GetRecipefromSelection();
        //Task SaveAeLimitsAsync();
        //Task PulseBitAsync(int tagId, string description);
    }
}