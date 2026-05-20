using IPCSoftware.Shared.Models;
using System;

namespace IPCSoftware.Core.Interfaces
{

    public interface IActiveRecipeProvider
    {

        ServoRecipeModel CurrentRecipe { get; }

        event EventHandler<ServoRecipeModel> RecipeChanged;

        void SetCurrentRecipe(ServoRecipeModel recipe);
    }
}