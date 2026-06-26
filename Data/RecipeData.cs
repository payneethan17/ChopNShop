// Brings in the Recipe class (and Ingredient, since it's defined in the same file)
// so this file can reference them without needing the full namespace every time.
using ChopNShop.Models;

namespace ChopNShop.Data
{
    // This class acts as a simple "fake database" for the whole app.
    // It's marked 'static', meaning there is only ever ONE copy of it in memory,
    // shared by every single request/page that uses it — no need to create an
    // instance of it anywhere (you never write "new RecipeData()").
    public static class RecipeData
    {
        // The actual in-memory list of every recipe the app currently knows about.
        // Because this is 'static', every page (Recipes, AddRecipe, EditRecipe, etc.)
        // is reading from and writing to this exact same list.
        // IMPORTANT: this list only lives in memory — it resets to empty every time
        // the app is stopped and restarted (this is the trade-off before adding a real database).
        public static List<Recipe> Recipes = new();

        // A private counter used to hand out unique IDs to new recipes.
        // 'private' means only code inside this class can touch it directly —
        // outside code must go through GetNextId() instead.
        // Starts at 1 so the first recipe created gets Id = 1.
        private static int _nextId = 1;

        // A small helper method that:
        //   1) returns the current value of _nextId
        //   2) THEN increases _nextId by 1 for next time
        // The "=>" is shorthand for a one-line method body (an "expression-bodied method").
        // Example: first call returns 1 and bumps the counter to 2.
        //          second call returns 2 and bumps it to 3, and so on.
        public static int GetNextId() => _nextId++;

        // Called whenever an existing recipe is edited and needs to be saved.
        // Takes in the *new* version of the recipe (already containing the correct Id)
        // and swaps it in to replace the old version in the list.
        public static void UpdateRecipe(Recipe updatedRecipe)
        {
            // Search through the list for a recipe whose Id matches the one we're updating.
            // FindIndex scans the list and returns the *position* (index) of the first match,
            // or -1 if nothing matches.
            int index = Recipes.FindIndex(r => r.Id == updatedRecipe.Id);

            // Only proceed if a matching recipe was actually found.
            // If index were -1 (no match), trying to use Recipes[-1] below would crash —
            // this check protects against that.
            if (index != -1)
            {
                // Replace the old recipe object at that position with the updated one.
                // This effectively "overwrites" the old data with the new edited data.
                Recipes[index] = updatedRecipe;
            }
        }

        // Called when the user wants to permanently remove a recipe (the Delete button).
        public static void DeleteRecipe(int id)
        {
            // RemoveAll goes through the entire list and removes every item that matches
            // the condition given — in this case, any recipe whose Id equals the one passed in.
            // Since Ids are unique, this will only ever remove exactly one recipe (or zero,
            // if that Id doesn't exist for some reason).
            Recipes.RemoveAll(r => r.Id == id);
        }
    }
}