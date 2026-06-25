using ChopNShop.Data;
using ChopNShop.Models;
using ChopNShop.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Text.RegularExpressions;

namespace ChopNShop.Pages;

public class EditRecipeModel : PageModel
{
    private readonly PexelsService _pexelsService;

    public EditRecipeModel(PexelsService pexelsService)
    {
        _pexelsService = pexelsService;
    }

    [BindProperty]
    public int Id { get; set; }

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
    public List<string> IngredientQuantities { get; set; } = new();

    [BindProperty]
    public List<string> IngredientUnits { get; set; } = new();

    [BindProperty]
    public IFormFile? Photo { get; set; }

    [BindProperty]
    public string? PhotoSearchTerm { get; set; }

    [BindProperty]
    public string? SelectedPhotoUrl { get; set; }

    [BindProperty]
    public string? ExistingImagePath { get; set; }

    public List<PhotoResult> PhotoResults { get; set; } = new();

    public IActionResult OnGet(int id)
    {
        var recipe = RecipeData.Recipes.FirstOrDefault(r => r.Id == id);
        if (recipe == null)
        {
            return RedirectToPage("/Recipes");
        }

        Id = recipe.Id;
        Name = recipe.Name;
        Servings = recipe.Servings;
        PrepMinutes = (int)recipe.PrepTime.TotalMinutes;
        Steps = recipe.Steps.Count > 0 ? recipe.Steps.ToList() : new List<string> { "" };
        ExistingImagePath = recipe.ImagePath;

        if (recipe.Ingredients.Count > 0)
        {
            IngredientNames = recipe.Ingredients.Select(i => i.Name).ToList();
            IngredientQuantities = recipe.Ingredients.Select(i => i.Quantity).ToList();
            IngredientUnits = recipe.Ingredients.Select(i => i.Unit).ToList();
        }
        else
        {
            IngredientNames = new List<string> { "" };
            IngredientQuantities = new List<string> { "" };
            IngredientUnits = new List<string> { "" };
        }

        return Page();
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

        var quantityPattern = new Regex(@"^\d*(/\d+)?$");
        foreach (var qty in IngredientQuantities)
        {
            if (!string.IsNullOrWhiteSpace(qty) && !quantityPattern.IsMatch(qty.Trim()))
            {
                ModelState.AddModelError("IngredientQuantities", "Quantities can only be whole numbers or fractions (e.g. 1/2).");
                return Page();
            }
        }

        string imagePath = ExistingImagePath ?? "";

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

        var updatedRecipe = new Recipe
        {
            Id = Id,
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
                updatedRecipe.Ingredients.Add(new Ingredient
                {
                    Name = IngredientNames[i],
                    Quantity = i < IngredientQuantities.Count ? IngredientQuantities[i].Trim() : "",
                    Unit = i < IngredientUnits.Count ? IngredientUnits[i] : ""
                });
            }
        }

        RecipeData.UpdateRecipe(updatedRecipe);

        return RedirectToPage("/RecipeDetails", new { id = Id });
    }
}