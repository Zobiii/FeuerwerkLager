using FeuerwerkLager.Data;
using FeuerwerkLager.Logs;
using FeuerwerkLager.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace FeuerwerkLager.Pages.Stock;

public class AddModel : PageModel
{
    private readonly FireworksContext _context;

    public AddModel(FireworksContext context)
    {
        _context = context;
    }

    public List<Article> Articles { get; set; } = new();
    public List<Location> Locations { get; set; } = new();

    [BindProperty]
    public int SelectedArticleId { get; set; }

    [BindProperty]
    public int? SelectedLocationId { get; set; } // null = frei

    [BindProperty]
    public int QuantityToAdd { get; set; }

    public async Task OnGetAsync()
    {
        Articles = await _context.Articles
            .OrderBy(a => a.Name)
            .ToListAsync();

        Locations = await _context.Locations
            .OrderBy(l => l.Name)
            .ToListAsync();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        Articles = await _context.Articles
            .OrderBy(a => a.Name)
            .ToListAsync();

        Locations = await _context.Locations
            .OrderBy(l => l.Name)
            .ToListAsync();

        if (QuantityToAdd <= 0)
        {
            ModelState.AddModelError(string.Empty, "Menge muss größer 0 sein.");
            return Page();
        }

        var article = await _context.Articles.FindAsync(SelectedArticleId);
        if (article == null)
        {
            ModelState.AddModelError(string.Empty, "Artikel wurde nicht gefunden.");
            return Page();
        }

        // vorhandenen Bestand an diesem Lagerplatz holen (oder frei)
        var entry = await _context.StockEntries
            .FirstOrDefaultAsync(s =>
                s.ArticleId == SelectedArticleId &&
                s.LocationId == SelectedLocationId);

        if (entry == null)
        {
            entry = new StockEntry
            {
                ArticleId = SelectedArticleId,
                LocationId = SelectedLocationId,
                Quantity = QuantityToAdd
            };
            _context.StockEntries.Add(entry);
        }
        else
        {
            entry.Quantity += QuantityToAdd;
        }

        await _context.SaveChangesAsync();

        string locationName;
        if (SelectedLocationId.HasValue)
        {
            var loc = await _context.Locations.FindAsync(SelectedLocationId.Value);
            locationName = loc?.Name ?? "?";
        }
        else
        {
            locationName = "frei";
        }

        PlainFileLogger.Log($"Bestand erhöht: +{QuantityToAdd}x {article.Name} am Lagerplatz '{locationName}'");

        return RedirectToPage("/Index");
    }
}
