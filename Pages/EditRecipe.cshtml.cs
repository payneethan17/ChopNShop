using ChopNShop.Data;
using ChopNShop.Models;
using ChopNShop.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Text.RegularExpressions;

namespace ChopNShop.Pages;

// Code-behind for EditRecipe.cshtml. Structurally very similar to
// AddRecipeModel — the big difference is that this page needs to LOAD
// an existing recipe's data first (OnGet), and UPDATE it instead of
// creating a brand new one (OnPostSaveAsync).
public class EditRecipeModel : PageModel
{
    // Same dependency-injection pattern as AddRecipeModel — ASP.NET
    // automatically supplies a working PexelsService instance here.
    private readonly PexelsService _pexelsService;

    public EditRecipeModel(PexelsService pexelsService)
    {
        _pexelsService = pexelsService;
    }

    // Holds which specific recipe is being edited. This travels through
    // the form as a hidden field (<input type="hidden" asp-for="Id" />)
    // so OnPostSaveAsync() still knows which recipe to update, even
    // though the URL itself isn't re-submitted with the form.
    [BindProperty]
    public int Id { get; set; }

    // Validation for this is handled manually inside OnPostSaveAsync(),
    // so no [Required] attribute is used here — having both a manual
    // check AND a [Required] attribute would produce two duplicate
    // "Recipe name is required" messages on the same submission.
    // Marked nullable (string?) to match AddRecipeModel's pattern, even
    // though OnGet() always assigns it a real value before the form is shown.
    [BindProperty]
    public string? Name { get; set; } = "";

    // Nullable for the same reason as in AddRecipeModel — protects
    // against a blank box failing to bind when, say, just the Search
    // button is clicked instead of Save.
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

    // Holds the recipe's CURRENT photo path, loaded in OnGet() and then
    // carried through the form as a hidden field. This is what lets the
    // recipe keep its existing photo if the user doesn't upload a new
    // file or pick a new Pexels photo while editing.
    [BindProperty]
    public string? ExistingImagePath { get; set; }

    public List<PhotoResult> PhotoResults { get; set; } = new();

    // Runs when the Edit page is first visited (e.g. clicking "Edit" on
    // the Recipe Details page). 'id' is automatically pulled from the
    // URL itself (because of @page "{id:int}" in the .cshtml).
    public IActionResult OnGet(int id)
    {
        // Look through the shared recipe list for the one matching this id.
        var recipe = RecipeData.Recipes.FirstOrDefault(r => r.Id == id);

        // Safety check: if somehow no recipe with this id exists (bad/old
        // link, recipe was deleted, etc.), bail out to the Recipes grid
        // instead of trying to display a form for a recipe that isn't there.
        if (recipe == null)
        {
            return RedirectToPage("/Recipes");
        }

        // Copy every piece of the found recipe's data into this page's
        // bound properties. Because these properties use asp-for in the
        // .cshtml, doing this here is what makes all the form fields show
        // up ALREADY FILLED IN with the recipe's existing information.
        Id = recipe.Id;
        Name = recipe.Name;
        Servings = recipe.Servings;
        // TimeSpan doesn't store "PrepMinutes" directly — this converts
        // the stored TimeSpan back into a plain number of minutes for
        // display in the Prep Time text box.
        PrepMinutes = recipe.PrepTime.HasValue ? (int)recipe.PrepTime.Value.TotalMinutes : null;
        // If the recipe has actual steps, copy them as-is. If it somehow
        // has ZERO steps (e.g. a very bare-bones recipe), fall back to a
        // list containing one empty string — this guarantees the .cshtml's
        // @for loop always has at least one row to render, instead of
        // showing a completely empty/missing Steps section.
        Steps = recipe.Steps.Count > 0 ? recipe.Steps.ToList() : new List<string> { "" };
        ExistingImagePath = recipe.ImagePath;

        // Same "at least one row" safety net as above, but for ingredients.
        // Since ingredients are stored as Ingredient objects (with Name,
        // Quantity, and Unit bundled together), they need to be split back
        // out into three separate lists here — matching how the form fields
        // and AddRecipe's binding pattern expect them.
        if (recipe.Ingredients.Count > 0)
        {
            // .Select(...) transforms each Ingredient object into just one
            // piece of it (e.g. just the Name), producing three separate
            // "parallel" lists that all line up by position/index.
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

        // Renders EditRecipe.cshtml now that every property above has
        // been filled in with the recipe's existing data.
        return Page();
    }

    // Identical in behavior to AddRecipeModel's version — runs the Pexels
    // search and redisplays the page with PhotoResults filled in.
    public async Task<IActionResult> OnPostSearchPhotosAsync()
    {
        if (!string.IsNullOrWhiteSpace(PhotoSearchTerm))
        {
            PhotoResults = await _pexelsService.SearchPhotosAsync(PhotoSearchTerm);
        }
        return Page();
    }

    // Runs when "Save Changes" is clicked (asp-page-handler="Save" on that button).
    // This builds an UPDATED version of the recipe and replaces the old one,
    // rather than creating a brand new recipe like AddRecipe does.
    public async Task<IActionResult> OnPostSaveAsync()
    {
        // --- VALIDATION STEP 1: Name is required ---
        // Does NOT immediately "return Page()" here — instead, it just
        // records the error and lets the method keep running, so every
        // other validation check below also gets a chance to run on this
        // same submission. That way, if there were multiple problems at
        // once, the user sees ALL of them together, not one at a time.
        if (string.IsNullOrWhiteSpace(Name))
        {
            ModelState.AddModelError("Name", "Recipe name is required.");
        }

        // --- VALIDATION STEP 2: Ingredient quantities must be blank,
        // a whole number, or a simple fraction like "1/2" ---
        // Pattern breakdown:
        //   ^            start of the string
        //   \d*          zero or more digits (so "" or "3" or "12" all match)
        //   (/\d+)?      OPTIONALLY a slash followed by one-or-more digits (for fractions)
        //   $            end of the string
        var quantityPattern = new Regex(@"^\d*(/\d+)?$");

        // Loop by INDEX (rather than foreach) so each error message can
        // reference exactly which row it's complaining about.
        for (int i = 0; i < IngredientQuantities.Count; i++)
        {
            var qty = IngredientQuantities[i];
            // Blank values are intentionally allowed — only flag it if
            // it's NOT blank AND doesn't match the allowed pattern.
            if (!string.IsNullOrWhiteSpace(qty) && !quantityPattern.IsMatch(qty.Trim()))
            {
                ModelState.AddModelError($"IngredientQuantities[{i}]", "Quantities can only be whole numbers or fractions (e.g. 1/2).");
            }
        }

        // --- VALIDATION STEP 3: Flag rows where a quantity or unit was
        // entered, but the ingredient name itself was left blank ---
        // Without this check, a row like Quantity="2", Unit="cups", Name=""
        // would silently get skipped later on (since the ingredient-saving
        // loop ignores rows with no name), quietly losing data the user
        // clearly meant to enter.
        for (int i = 0; i < IngredientNames.Count; i++)
        {
            bool hasName = !string.IsNullOrWhiteSpace(IngredientNames[i]);
            bool hasQuantity = i < IngredientQuantities.Count && !string.IsNullOrWhiteSpace(IngredientQuantities[i]);
            bool hasUnit = i < IngredientUnits.Count && !string.IsNullOrWhiteSpace(IngredientUnits[i]);

            if (!hasName && (hasQuantity || hasUnit))
            {
                ModelState.AddModelError($"IngredientNames[{i}]", $"Ingredient row {i + 1} has a quantity or unit but is missing an ingredient name.");
            }
        }

        // Now that all three validation checks above have had a chance to
        // run (none of them stopped early), check ModelState as a whole.
        // If anything failed, stop here and re-show the form — correctly
        // pre-filled with whatever the user already had typed/edited,
        // thanks to the asp-for bindings and the @for loops in the .cshtml.
        if (!ModelState.IsValid)
        {
            return Page();
        }

        // --- Everything below only runs once ALL validation has passed. ---

        // KEY DIFFERENCE FROM AddRecipe: instead of starting from a blank
        // string, this starts from whatever photo path the recipe ALREADY
        // had — so if the user doesn't upload a new file or pick a new
        // Pexels photo, the original photo is preserved automatically.
        string imagePath = ExistingImagePath ?? "";

        // If the user picked a new Pexels photo during this edit, that
        // takes priority and overwrites the existing photo path.
        if (!string.IsNullOrWhiteSpace(SelectedPhotoUrl))
        {
            imagePath = SelectedPhotoUrl;
        }
        // Otherwise, if they uploaded a brand new file, that also
        // overwrites the existing photo path (same validation/saving
        // logic as Add Recipe).
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
        // (If neither condition above is true, imagePath simply stays as
        // whatever ExistingImagePath already was — nothing changes.)

        // Builds a brand new Recipe object representing the EDITED version.
        // Note this re-uses the SAME Id as before (not a new one from
        // RecipeData.GetNextId()), since this is meant to replace the
        // existing recipe, not create an additional one.
        var updatedRecipe = new Recipe
        {
            Id = Id,
            // The "!" tells the compiler "trust me, this isn't null" —
            // safe because the validation above already stopped execution
            // if Name were null/blank.
            Name = Name!,
            Servings = Servings,
            PrepTime = PrepMinutes.HasValue ? TimeSpan.FromMinutes(PrepMinutes.Value) : null,
            Steps = Steps.Where(s => !string.IsNullOrWhiteSpace(s)).ToList(),
            ImagePath = imagePath,
            Ingredients = new List<Ingredient>()
        };

        // Same ingredient-rebuilding loop as Add Recipe — walks through
        // all three parallel lists by index and combines matching entries
        // back into proper Ingredient objects.
        for (int i = 0; i < IngredientNames.Count; i++)
        {
            // Thanks to VALIDATION STEP 3 above, any row reaching this
            // point with a blank name is also guaranteed to have a blank
            // quantity and unit — so skipping it here is safe and doesn't
            // lose any data the user actually intended to enter.
            if (!string.IsNullOrWhiteSpace(IngredientNames[i]))
            {
                updatedRecipe.Ingredients.Add(new Ingredient
                {
                    Name = IngredientNames[i],
                    // "?? """ guards against this specific value ever being
                    // null (rather than just an empty string) before calling
                    // .Trim() on it — calling .Trim() directly on null would
                    // crash with a NullReferenceException.
                    Quantity = i < IngredientQuantities.Count ? (IngredientQuantities[i] ?? "").Trim() : "",
                    Unit = i < IngredientUnits.Count ? IngredientUnits[i] : ""
                });
            }
        }

        // Instead of RecipeData.Recipes.Add(...) (which would create a
        // SECOND recipe), this calls the UpdateRecipe helper, which finds
        // the existing recipe by Id and replaces it with this new version.
        RecipeData.UpdateRecipe(updatedRecipe);

        // After saving changes, send the user back to that specific
        // recipe's details page (passing its Id along in the route),
        // rather than the whole Recipes grid — since that's the most
        // useful place to land after finishing an edit.
        return RedirectToPage("/RecipeDetails", new { id = Id });
    }
}