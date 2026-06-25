using ChopNShop.Models;

namespace ChopNShop.Data
{
    public static class RecipeData
    {
        public static List<Recipe> Recipes = new();
        private static int _nextId = 1;

        public static int GetNextId() => _nextId++;

        public static void UpdateRecipe(Recipe updatedRecipe)
        {
            int index = Recipes.FindIndex(r => r.Id == updatedRecipe.Id);
            if (index != -1)
            {
                Recipes[index] = updatedRecipe;
            }
        }

        public static void DeleteRecipe(int id)
        {
            Recipes.RemoveAll(r => r.Id == id);
        }
    }
}