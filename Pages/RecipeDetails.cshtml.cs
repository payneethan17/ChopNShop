// Gives access to RecipeData (the shared in-memory list of recipes).
using ChopNShop.Data;
// Gives access to the Recipe class.
using ChopNShop.Models;
// Gives access to IActionResult and related MVC types.
using Microsoft.AspNetCore.Mvc;
// Gives access to PageModel, the base class for all Razor Page code-behinds.
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ChopNShop.Pages;

// Code-behind for RecipeDetails.cshtml — responsible for loading ONE
// specific recipe (based on the id in the URL) and handling its deletion.
public class RecipeDetailsModel : PageModel
{
    // Holds the single recipe being displayed. Nullable (Recipe?) because
    // there's a brief possibility it isn't found at all (bad/old link,
    // or the recipe was deleted) — the .cshtml's @if (Model.Recipe != null)
    // check exists specifically to handle that case safely.
    public Recipe? Recipe { get; set; }

    // Runs when this page is first visited. 'id' is automatically read
    // from the URL itself, thanks to @page "{id:int}" in the .cshtml.
    public IActionResult OnGet(int id)
    {
        // Searches the shared recipe list for the one whose Id matches
        // the id from the URL. FirstOrDefault returns null if nothing matches.
        Recipe = RecipeData.Recipes.FirstOrDefault(r => r.Id == id);

        // If no matching recipe was found, don't try to render the page at
        // all — just send the user back to the Recipes grid instead.
        if (Recipe == null)
        {
            return RedirectToPage("/Recipes");
        }

        // A matching recipe WAS found, so go ahead and display this page
        // normally, with Recipe now holding that recipe's full data.
        return Page();
    }

    // Runs specifically when the Delete confirmation modal's "Delete"
    // button is clicked, because that button's <form> uses
    // asp-page-handler="Delete" — routing its submission here instead of
    // to OnGet or any other handler. 'id' comes from asp-route-id on that
    // same form, NOT from the page's own URL parameter (though in this
    // case they happen to be the same recipe).
    public IActionResult OnPostDelete(int id)
    {
        // Removes the matching recipe from the shared list entirely.
        RecipeData.DeleteRecipe(id);
        // Since the recipe no longer exists, there's nothing left to show
        // on a details page for it — send the user back to the grid,
        // where that recipe will no longer appear.
        return RedirectToPage("/Recipes");
    }
}