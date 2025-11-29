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
    public int FullUnitsToAdd { get; set; }

    [BindProperty]
    public int LoosePiecesToAdd { get; set; }

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
            ModelState.AddModelError(string.Empty, "Bitte einen Artikel ausw\u00e4hlen.");
            return Page();
        }

        if (FullUnitsToAdd < 0 || LoosePiecesToAdd < 0)
        {
            ModelState.AddModelError(string.Empty, "Mengen d\u00fcrfen nicht negativ sein.");
            return Page();
        }

        if (FullUnitsToAdd == 0 && LoosePiecesToAdd == 0)
        {
            ModelState.AddModelError(string.Empty, "Bitte mindestens eine Menge angeben.");
            return Page();
        }

        if (SelectedLocationId.HasValue)
        {
            var locationExists = await _context.Locations.AnyAsync(l => l.Id == SelectedLocationId.Value);
            if (!locationExists)
            {
                ModelState.AddModelError(string.Empty, "Der gew\u00e4hlte Lagerplatz existiert nicht.");
                return Page();
            }
        }

        var article = await _context.Articles.FindAsync(SelectedArticleId);
        if (article == null)
        {
            ModelState.AddModelError(string.Empty, "Artikel wurde nicht gefunden.");
            return Page();
        }

        if (!article.IsMultiPart || !article.PiecesPerUnit.HasValue)
        {
            if (LoosePiecesToAdd > 0)
            {
                ModelState.AddModelError(string.Empty, "Lose Einzelteile sind bei diesem Artikel nicht vorgesehen.");
                return Page();
            }
        }
        else
        {
            var perUnit = article.PiecesPerUnit.Value;
            if (LoosePiecesToAdd >= perUnit)
            {
                ModelState.AddModelError(string.Empty, $"Lose Einzelteile m\u00fcssen kleiner als {perUnit} sein.");
                return Page();
            }
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
                FullUnits = FullUnitsToAdd,
                LoosePieces = LoosePiecesToAdd
            };
            _context.StockEntries.Add(entry);
        }
        else
        {
            entry.FullUnits += FullUnitsToAdd;
            entry.LoosePieces += LoosePiecesToAdd;
        }

        if (article.IsMultiPart && article.PiecesPerUnit.HasValue && article.PiecesPerUnit.Value > 0)
        {
            var perUnit = article.PiecesPerUnit.Value;
            entry.FullUnits += entry.LoosePieces / perUnit;
            entry.LoosePieces = entry.LoosePieces % perUnit;
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

        PlainFileLogger.Log($"Bestand erh\u00f6ht: +{FullUnitsToAdd} Einheiten, +{LoosePiecesToAdd} lose St\u00fcck von {article.Name} am Lagerplatz '{locationName}'");

        return RedirectToPage("/Index");
    }
}
