using ChopNShop.Data;
using ChopNShop.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ChopNShop.Pages;

public class RecipeDetailsModel : PageModel
{
    public Recipe? Recipe { get; set; }

    public IActionResult OnGet(int id)
    {
        Recipe = RecipeData.Recipes.FirstOrDefault(r => r.Id == id);

        if (Recipe == null)
        {
            return RedirectToPage("/Recipes");
        }

        return Page();
    }
}