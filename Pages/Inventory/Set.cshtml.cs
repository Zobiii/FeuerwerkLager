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
    public int NewFullUnits { get; set; }

    [BindProperty]
    public int NewLoosePieces { get; set; }

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

        if (SelectedLocationId.HasValue)
        {
            var locationExists = await _context.Locations.AnyAsync(l => l.Id == SelectedLocationId.Value);
            if (!locationExists)
            {
                ModelState.AddModelError(string.Empty, "Der gew\u00e4hlte Lagerplatz existiert nicht.");
                return Page();
            }
        }

        if (NewFullUnits < 0 || NewLoosePieces < 0)
        {
            ModelState.AddModelError(string.Empty, "Mengen d\u00fcrfen nicht negativ sein.");
            return Page();
        }

        var article = await _context.Articles.FindAsync(SelectedArticleId);
        if (article == null)
        {
            ModelState.AddModelError(string.Empty, "Artikel nicht gefunden.");
            return Page();
        }

        var perUnit = (article.IsMultiPart && article.PiecesPerUnit.HasValue && article.PiecesPerUnit.Value > 0)
            ? article.PiecesPerUnit.Value
            : 1;

        if (!article.IsMultiPart || !article.PiecesPerUnit.HasValue)
        {
            if (NewLoosePieces > 0)
            {
                ModelState.AddModelError(string.Empty, "Lose Einzelteile sind bei diesem Artikel nicht vorgesehen.");
                return Page();
            }
        }
        else
        {
            if (NewLoosePieces >= perUnit)
            {
                ModelState.AddModelError(string.Empty, $"Lose Einzelteile m\u00fcssen kleiner als {perUnit} sein.");
                return Page();
            }
        }

        var entry = await _context.StockEntries
            .FirstOrDefaultAsync(s =>
                s.ArticleId == SelectedArticleId &&
                s.LocationId == SelectedLocationId);

        var oldFull = entry?.FullUnits ?? 0;
        var oldLoose = entry?.LoosePieces ?? 0;

        var targetPieces = (NewFullUnits * perUnit) + NewLoosePieces;

        if (targetPieces == 0)
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
                    LocationId = SelectedLocationId
                };
                _context.StockEntries.Add(entry);
            }

            entry.FullUnits = targetPieces / perUnit;
            entry.LoosePieces = targetPieces % perUnit;
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
            $"Inventur: Artikel {article.Name}, Lagerplatz '{locName}', alt: {oldFull} Einheiten / {oldLoose} lose, neu: {NewFullUnits} Einheiten / {NewLoosePieces} lose");

        return RedirectToPage("/Index");
    }
}
