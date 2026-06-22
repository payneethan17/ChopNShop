using ChopNShop.Models;

namespace ChopNShop.Data
{
    public static class RecipeData
    {
        public static List<Recipe> Recipes = new();
        private static int _nextId = 1;

        public static int GetNextId() => _nextId++;
    }
}