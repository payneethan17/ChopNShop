using ChopNShop.Data;
using ChopNShop.Models;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ChopNShop.Pages;

public class RecipesModel : PageModel
{
    public List<Recipe> Recipes => RecipeData.Recipes;

    public void OnGet()
    {
    }
}