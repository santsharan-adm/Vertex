using IPCSoftware.Core.Interfaces;
using IPCSoftware.Shared.Models;
using System;
using IPCSoftware.Core.Interfaces;

namespace IPCSoftware.Services
{
    public class ActiveRecipeProvider : IActiveRecipeProvider
    {
        private ServoRecipeModel _currentRecipe = new ServoRecipeModel();

        public ServoRecipeModel CurrentRecipe
        {
            get => _currentRecipe;
            private set
            {
                if (_currentRecipe != value)
                {
                    _currentRecipe = value;
                    RecipeChanged?.Invoke(this, _currentRecipe);
                }
            }
        }

        public event EventHandler<ServoRecipeModel> RecipeChanged;

        public void SetCurrentRecipe(ServoRecipeModel recipe)
        {
            CurrentRecipe = recipe;
        }
    }
}