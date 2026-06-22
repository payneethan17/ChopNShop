using ChopNShop.Data;
using ChopNShop.Models;
using ChopNShop.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ChopNShop.Pages;

public class AddRecipeModel : PageModel
{
    private readonly PexelsService _pexelsService;

    public AddRecipeModel(PexelsService pexelsService)
    {
        _pexelsService = pexelsService;
    }

    [BindProperty]
    public string Name { get; set; } = "";

    [BindProperty]
    public int? Servings { get; set; }

    [BindProperty]
    public int? PrepMinutes { get; set; }

    [BindProperty]
    public List<string> Steps { get; set; } = new();

    [BindProperty]
    public List<string> IngredientNames { get; set; } = new();

    [BindProperty]
    public List<int?> IngredientQuantities { get; set; } = new();

    [BindProperty]
    public List<string> IngredientUnits { get; set; } = new();

    [BindProperty]
    public IFormFile? Photo { get; set; }

    [BindProperty]
    public string? PhotoSearchTerm { get; set; }

    [BindProperty]
    public string? SelectedPhotoUrl { get; set; }

    public List<PhotoResult> PhotoResults { get; set; } = new();

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostSearchPhotosAsync()
    {
        if (!string.IsNullOrWhiteSpace(PhotoSearchTerm))
        {
            PhotoResults = await _pexelsService.SearchPhotosAsync(PhotoSearchTerm);
        }
        return Page();
    }

    public async Task<IActionResult> OnPostSaveAsync()
    {
        if (string.IsNullOrWhiteSpace(Name))
        {
            ModelState.AddModelError("Name", "Recipe name is required.");
            return Page();
        }

        string imagePath = "";

        if (!string.IsNullOrWhiteSpace(SelectedPhotoUrl))
        {
            imagePath = SelectedPhotoUrl;
        }
        else if (Photo != null && Photo.Length > 0)
        {
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp", ".jfif" };
            string extension = Path.GetExtension(Photo.FileName).ToLower();

            if (!allowedExtensions.Contains(extension))
            {
                ModelState.AddModelError("Photo", "Please upload a valid image file (jpg, png, gif, webp, jfif).");
                return Page();
            }

            string fileName = Guid.NewGuid() + extension;
            string savePath = Path.Combine("wwwroot/images", fileName);

            using (var stream = new FileStream(savePath, FileMode.Create))
            {
                await Photo.CopyToAsync(stream);
            }

            imagePath = "/images/" + fileName;
        }

        var recipe = new Recipe
        {
            Id = RecipeData.GetNextId(),
            Name = Name,
            Servings = Servings ?? 0,
            PrepTime = TimeSpan.FromMinutes(PrepMinutes ?? 0),
            Steps = Steps.Where(s => !string.IsNullOrWhiteSpace(s)).ToList(),
            ImagePath = imagePath,
            Ingredients = new List<Ingredient>()
        };

        for (int i = 0; i < IngredientNames.Count; i++)
        {
            if (!string.IsNullOrWhiteSpace(IngredientNames[i]))
            {
                recipe.Ingredients.Add(new Ingredient
                {
                    Name = IngredientNames[i],
                    Quantity = (i < IngredientQuantities.Count ? IngredientQuantities[i] : 0) ?? 0,
                    Unit = i < IngredientUnits.Count ? IngredientUnits[i] : ""
                });
            }
        }

        RecipeData.Recipes.Add(recipe);

        return RedirectToPage("/Recipes");
    }
}