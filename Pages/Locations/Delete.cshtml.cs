using FeuerwerkLager.Data;
using FeuerwerkLager.Logs;
using FeuerwerkLager.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace FeuerwerkLager.Pages.Locations;

public class DeleteModel : PageModel
{
    private readonly FireworksContext _context;

    public DeleteModel(FireworksContext context)
    {
        _context = context;
    }

    public List<Location> Locations { get; set; } = new();

    [BindProperty]
    public int SelectedLocationId { get; set; }

    public async Task OnGetAsync()
    {
        Locations = await _context.Locations
            .Where(l => !_context.StockEntries.Any(s => s.LocationId == l.Id))
            .OrderBy(l => l.Name)
            .ToListAsync();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        Locations = await _context.Locations
            .Where(l => !_context.StockEntries.Any(s => s.LocationId == l.Id))
            .OrderBy(l => l.Name)
            .ToListAsync();

        if (SelectedLocationId <= 0)
        {
            ModelState.AddModelError(string.Empty, "Bitte einen Lagerplatz auswählen.");
            return Page();
        }

        var loc = await _context.Locations.FindAsync(SelectedLocationId);
        if (loc == null)
        {
            ModelState.AddModelError(string.Empty, "Lagerplatz nicht gefunden.");
            return Page();
        }

        var hasStock = await _context.StockEntries.AnyAsync(s => s.LocationId == loc.Id);
        if (hasStock)
        {
            ModelState.AddModelError(string.Empty, "Lagerplatz enthält noch Bestand und kann nicht gelöscht werden.");
            return Page();
        }

        _context.Locations.Remove(loc);
        await _context.SaveChangesAsync();

        PlainFileLogger.Log($"Lagerplatz gelöscht: {loc.Name}");

        return RedirectToPage("/Locations/Index");
    }
}
