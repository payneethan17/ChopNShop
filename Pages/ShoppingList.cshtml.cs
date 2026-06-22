using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ChopNShop.Pages;

public class ShoppingListModel : PageModel
{
    // Stores items for the current session only (resets on restart)
    private static List<ShoppingItem> Items = new();

    public List<ShoppingItem> ShoppingItems => Items;

    [BindProperty]
    public string? NewItemName { get; set; }

    public void OnGet()
    {
        // Runs when the page first loads — nothing needed here yet
    }

    // Runs when the "Add" form is submitted
    public IActionResult OnPostAdd()
    {
        if (!string.IsNullOrWhiteSpace(NewItemName))
        {
            Items.Add(new ShoppingItem { Name = NewItemName });
        }
        return RedirectToPage();
    }

    // Runs when a "Remove" button is clicked
    public IActionResult OnPostRemove(int id)
    {
        Items.RemoveAll(i => i.Id == id);
        return RedirectToPage();
    }

    // Runs when a checkbox is toggled
    public IActionResult OnPostToggle(int id)
    {
        var item = Items.FirstOrDefault(i => i.Id == id);
        if (item != null)
        {
            item.IsChecked = !item.IsChecked;
        }
        return RedirectToPage();
    }
}

public class ShoppingItem
{
    private static int _nextId = 1;
    public int Id { get; set; } = _nextId++;
    public string Name { get; set; } = "";
    public bool IsChecked { get; set; }
}
