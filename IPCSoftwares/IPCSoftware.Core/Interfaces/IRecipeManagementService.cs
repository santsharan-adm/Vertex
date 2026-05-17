using IPCSoftware.Shared.Models;
using System.Threading.Tasks;

namespace IPCSoftware.Core.Interfaces
{
    public interface IRecipeManagementService
    {
        Task<bool> AddRecipeAsync(ServoRecipeModel newRecipe);
        Task<bool> UpdateRecipeAsync(ServoRecipeModel updatedRecipe);
        Task<bool> DeleteRecipeAsync(int programNo);
    }
}