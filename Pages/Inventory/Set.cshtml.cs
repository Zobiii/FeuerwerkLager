using FeuerwerkLager.Data;
using FeuerwerkLager.Logs;
using FeuerwerkLager.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace FeuerwerkLager.Pages.Inventory;

public class SetModel : PageModel
{
    private readonly FireworksContext _context;

    public SetModel(FireworksContext context)
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
    public int NewQuantity { get; set; }

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

        if (SelectedArticleId <= 0)
        {
            ModelState.AddModelError(string.Empty, "Bitte einen Artikel auswählen.");
            return Page();
        }

        if (SelectedLocationId.HasValue)
        {
            var locationExists = await _context.Locations.AnyAsync(l => l.Id == SelectedLocationId.Value);
            if (!locationExists)
            {
                ModelState.AddModelError(string.Empty, "Der gewählte Lagerplatz existiert nicht.");
                return Page();
            }
        }

        if (NewQuantity < 0)
        {
            ModelState.AddModelError(string.Empty, "Menge darf nicht negativ sein.");
            return Page();
        }

        var article = await _context.Articles.FindAsync(SelectedArticleId);
        if (article == null)
        {
            ModelState.AddModelError(string.Empty, "Artikel nicht gefunden.");
            return Page();
        }

        var entry = await _context.StockEntries
            .FirstOrDefaultAsync(s =>
                s.ArticleId == SelectedArticleId &&
                s.LocationId == SelectedLocationId);

        int oldQuantity = entry?.Quantity ?? 0;

        if (NewQuantity == 0)
        {
            if (entry != null)
            {
                _context.StockEntries.Remove(entry);
            }
        }
        else
        {
            if (entry == null)
            {
                entry = new StockEntry
                {
                    ArticleId = SelectedArticleId,
                    LocationId = SelectedLocationId,
                    Quantity = NewQuantity
                };
                _context.StockEntries.Add(entry);
            }
            else
            {
                entry.Quantity = NewQuantity;
            }
        }

        await _context.SaveChangesAsync();

        string locName;
        if (SelectedLocationId.HasValue)
        {
            var loc = await _context.Locations.FindAsync(SelectedLocationId.Value);
            locName = loc?.Name ?? "?";
        }
        else
        {
            locName = "frei";
        }

        PlainFileLogger.Log(
            $"Inventur: Artikel {article.Name}, Lagerplatz '{locName}', Menge alt: {oldQuantity}, neu: {NewQuantity}");

        return RedirectToPage("/Index");
    }
}
