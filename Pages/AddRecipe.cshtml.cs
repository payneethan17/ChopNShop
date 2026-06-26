// Gives access to RecipeData (the shared in-memory list of recipes).
using ChopNShop.Data;
// Gives access to the Recipe and Ingredient classes.
using ChopNShop.Models;
// Gives access to PexelsService and PhotoResult (the Pexels search feature).
using ChopNShop.Services;
// Gives access to IActionResult, IFormFile, and other MVC/web-related types.
using Microsoft.AspNetCore.Mvc;
// Gives access to PageModel — the base class every Razor Page code-behind inherits from.
using Microsoft.AspNetCore.Mvc.RazorPages;
// Gives access to Regex, used here to validate ingredient quantity formatting.
using System.Text.RegularExpressions;

namespace ChopNShop.Pages;

// The code-behind class for AddRecipe.cshtml. Everything in here runs on
// the SERVER — none of this is visible to the user directly, but it's what
// powers everything the .cshtml file displays and reacts to.
public class AddRecipeModel : PageModel
{
    // A reference to the Pexels search service. This is "injected" automatically
    // by ASP.NET (see the constructor below) rather than being created manually
    // with "new PexelsService()" — this pattern is called Dependency Injection.
    private readonly PexelsService _pexelsService;

    // The constructor runs once whenever ASP.NET creates a new instance of this
    // page model to handle a request. ASP.NET automatically supplies a working
    // PexelsService instance here, because it was registered in Program.cs.
    public AddRecipeModel(PexelsService pexelsService)
    {
        _pexelsService = pexelsService;
    }

    // ----------------------------------------------------------------
    // FORM-BOUND PROPERTIES
    // Every property marked [BindProperty] is automatically filled in
    // by ASP.NET from matching form fields whenever the form is submitted
    // (matched by the HTML input's name="..." attribute).
    // ----------------------------------------------------------------

    // Holds the recipe's name as typed into the Name text box.
    // Defaults to "" so it's never null before the form is submitted.
    [BindProperty]
    public string Name { get; set; } = "";

    // Nullable (int?) on purpose: if the Servings box is left blank when
    // ANY form on this page is submitted (including just clicking "Search"),
    // binding an empty string into a plain "int" would crash. Making it
    // nullable lets an empty box bind safely to "null" instead of erroring.
    [BindProperty]
    public int? Servings { get; set; }

    // Same reasoning as Servings — stays nullable to safely handle a blank box.
    [BindProperty]
    public int? PrepMinutes { get; set; }

    // Holds every step the user typed in. Because multiple <input name="Steps">
    // boxes exist on the page (one per step row, including dynamically added
    // ones), ASP.NET automatically collects all of their values, in order,
    // into this single list.
    [BindProperty]
    public List<string> Steps { get; set; } = new();

    // Same "multiple inputs sharing one name" pattern as Steps, just for the
    // ingredient name column specifically.
    [BindProperty]
    public List<string> IngredientNames { get; set; } = new();

    // Stored as strings (not numbers) specifically so values like "1/2" or a
    // completely blank entry can be kept exactly as typed, with no forced
    // conversion to a number.
    [BindProperty]
    public List<string> IngredientQuantities { get; set; } = new();

    // Same pattern again, for the unit column (cup, lb, tbsp, etc.).
    [BindProperty]
    public List<string> IngredientUnits { get; set; } = new();

    // Represents an uploaded file from the <input type="file" name="Photo">
    // box. Nullable because the user might not upload a file at all (e.g.
    // if they're using the Pexels search instead).
    [BindProperty]
    public IFormFile? Photo { get; set; }

    // Whatever the user typed into the "search for a photo online" box.
    [BindProperty]
    public string? PhotoSearchTerm { get; set; }

    // Filled in by JavaScript (the selectPhoto() function in the .cshtml)
    // whenever the user clicks one of the Pexels search result thumbnails.
    // This is how the chosen photo's URL actually reaches the server.
    [BindProperty]
    public string? SelectedPhotoUrl { get; set; }

    // NOT marked [BindProperty] — this property is only ever set BY this
    // class (after a search runs), never filled in directly from form input.
    // Holds whatever photos came back from the most recent Pexels search,
    // so the .cshtml can loop through and display them as thumbnails.
    public List<PhotoResult> PhotoResults { get; set; } = new();

    // Runs automatically whenever someone simply VISITS this page (a normal
    // GET request, like clicking "+ Add Recipe"). Nothing special needs to
    // happen here since the Add Recipe form always starts out blank.
    public void OnGet()
    {
    }

    // Runs specifically when the "Search" button is clicked, because that
    // button has asp-page-handler="SearchPhotos" in the .cshtml, which tells
    // ASP.NET to route its submission here instead of the default handler.
    public async Task<IActionResult> OnPostSearchPhotosAsync()
    {
        // Only actually search if the user typed something — avoids making
        // a pointless API call with an empty search term.
        if (!string.IsNullOrWhiteSpace(PhotoSearchTerm))
        {
            // Calls out to the Pexels API (via PexelsService) and waits for
            // the results before continuing. 'await' pauses this method
            // here without freezing the whole server while the network
            // request to Pexels is in progress.
            PhotoResults = await _pexelsService.SearchPhotosAsync(PhotoSearchTerm);
        }
        // Re-displays this same page, now with PhotoResults filled in,
        // so the .cshtml's @if (Model.PhotoResults.Any()) block can show
        // the new thumbnails.
        return Page();
    }

    // Runs specifically when the "Save Recipe" button is clicked, because
    // that button has asp-page-handler="Save" in the .cshtml.
    // This is the method that actually creates and stores the new recipe.
    public async Task<IActionResult> OnPostSaveAsync()
    {
        // --- VALIDATION STEP 1: Name is required ---
        if (string.IsNullOrWhiteSpace(Name))
        {
            // Attaches an error message tied to the "Name" field. The
            // .cshtml's error summary box will display this text.
            ModelState.AddModelError("Name", "Recipe name is required.");
            // Stops here and re-shows the form (with whatever the user
            // already typed still filled in, thanks to asp-for bindings)
            // instead of continuing on to save anything.
            return Page();
        }

        // --- VALIDATION STEP 2: Ingredient quantities must be blank,
        // a whole number, or a simple fraction like "1/2" ---
        // This regex pattern means:
        //   ^            start of the string
        //   \d*          zero or more digits (so "" or "3" or "12" all match)
        //   (/\d+)?      OPTIONALLY a slash followed by one-or-more digits (for fractions)
        //   $            end of the string
        var quantityPattern = new Regex(@"^\d*(/\d+)?$");

        // Check every quantity the user entered, across every ingredient row.
        foreach (var qty in IngredientQuantities)
        {
            // Only flag it as a problem if it's NOT blank AND doesn't match
            // the allowed pattern — blank values are intentionally allowed
            // (per the requirement that quantity can be left empty).
            if (!string.IsNullOrWhiteSpace(qty) && !quantityPattern.IsMatch(qty.Trim()))
            {
                ModelState.AddModelError("IngredientQuantities", "Quantities can only be whole numbers or fractions (e.g. 1/2).");
                return Page();
            }
        }

        // Will end up holding wherever this recipe's photo actually lives —
        // either a Pexels URL, or a path to a file saved in wwwroot/images.
        string imagePath = "";

        // --- PHOTO HANDLING: check Pexels selection first ---
        if (!string.IsNullOrWhiteSpace(SelectedPhotoUrl))
        {
            // The user picked a photo from the Pexels search results —
            // just use that URL directly, no file saving needed.
            imagePath = SelectedPhotoUrl;
        }
        // --- PHOTO HANDLING: otherwise, check for an uploaded file ---
        else if (Photo != null && Photo.Length > 0)
        {
            // Only these file extensions are allowed to be uploaded.
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp", ".jfif" };
            // Pulls just the extension off the uploaded file's original name
            // (e.g. "dinner.JPG" -> ".JPG"), then lowercases it so the
            // comparison above works regardless of how it was typed.
            string extension = Path.GetExtension(Photo.FileName).ToLower();

            // If the extension isn't one of the allowed ones, reject the
            // upload entirely — nothing gets saved to disk.
            if (!allowedExtensions.Contains(extension))
            {
                ModelState.AddModelError("Photo", "Please upload a valid image file (jpg, png, gif, webp, jfif).");
                return Page();
            }

            // Generates a random, unique file name (instead of reusing the
            // user's original file name) so two different uploads can never
            // accidentally overwrite each other.
            string fileName = Guid.NewGuid() + extension;
            // Builds the full path on disk where this file will be saved.
            string savePath = Path.Combine("wwwroot/images", fileName);

            // Opens a new file on disk at savePath and copies the uploaded
            // file's actual bytes into it. The 'using' block makes sure the
            // file stream is properly closed afterward, even if something
            // goes wrong while copying.
            using (var stream = new FileStream(savePath, FileMode.Create))
            {
                await Photo.CopyToAsync(stream);
            }

            // Stores the WEB-ACCESSIBLE path (not the disk path) so it can
            // later be used directly inside an <img src="..."> tag.
            imagePath = "/images/" + fileName;
        }
        // (If neither a Pexels photo nor a file upload was provided,
        // imagePath simply stays as "" — the recipe will just have no photo.)

        // Builds the actual new Recipe object from everything gathered so far.
        var recipe = new Recipe
        {
            // Hands out a fresh, never-used-before Id from RecipeData.
            Id = RecipeData.GetNextId(),
            Name = Name,
            // "?? 0" means: if Servings ended up null (left blank), use 0 instead.
            Servings = Servings ?? 0,
            // Converts the entered number of minutes into a proper TimeSpan,
            // again defaulting to 0 minutes if PrepMinutes was left blank.
            PrepTime = TimeSpan.FromMinutes(PrepMinutes ?? 0),
            // Filters OUT any completely blank step entries (e.g. if the user
            // added an extra step row but never typed anything into it)
            // before storing the final list.
            Steps = Steps.Where(s => !string.IsNullOrWhiteSpace(s)).ToList(),
            ImagePath = imagePath,
            // Starts as an empty list — ingredients get added one by one below.
            Ingredients = new List<Ingredient>()
        };

        // Walks through every ingredient row by INDEX (0, 1, 2, ...) so that
        // the matching Name/Quantity/Unit from each of the three separate
        // lists can be combined back together into one Ingredient object,
        // since they all share the same position/order.
        for (int i = 0; i < IngredientNames.Count; i++)
        {
            // Skip this row entirely if no ingredient name was typed —
            // treats it as an empty/unused row rather than saving a blank ingredient.
            if (!string.IsNullOrWhiteSpace(IngredientNames[i]))
            {
                recipe.Ingredients.Add(new Ingredient
                {
                    Name = IngredientNames[i],
                    // Safely grabs the matching quantity by the same index,
                    // but only if that list actually has an entry at this
                    // position (guards against the three lists ever ending
                    // up different lengths). Falls back to "" otherwise.
                    Quantity = i < IngredientQuantities.Count ? IngredientQuantities[i].Trim() : "",
                    Unit = i < IngredientUnits.Count ? IngredientUnits[i] : ""
                });
            }
        }

        // Adds the fully built recipe into the shared in-memory list, making
        // it immediately visible to every other page that reads from RecipeData.
        RecipeData.Recipes.Add(recipe);

        // Sends the browser to the Recipes grid page, where the newly
        // added recipe should now appear as a card.
        return RedirectToPage("/Recipes");
    }
}