using FeuerwerkLager.Data;
using FeuerwerkLager.Logs;
using FeuerwerkLager.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FeuerwerkLager.Pages.Locations;

public class CreateModel : PageModel
{
    private readonly FireworksContext _context;

    public CreateModel(FireworksContext context)
    {
        _context = context;
    }

    [BindProperty]
    public Location Location { get; set; } = new();

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        _context.Locations.Add(Location);
        await _context.SaveChangesAsync();

        PlainFileLogger.Log($"Lagerplatz erstellt: {Location.Name}");

        return RedirectToPage("/Index");
    }
}
