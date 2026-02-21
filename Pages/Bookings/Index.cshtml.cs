using FeuerwerkLager.Data;
using FeuerwerkLager.Logs;
using FeuerwerkLager.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace FeuerwerkLager.Pages.Bookings;

public class IndexModel : PageModel
{
    private readonly FireworksContext _context;

    public IndexModel(FireworksContext context)
    {
        _context = context;
    }

    public List<Article> Articles { get; set; } = new();
    public List<Location> Locations { get; set; } = new();

    // Map: "ArticleId|LocationId/free" -> verf\u00fcgbare St\u00fcckzahl (immer in St\u00fccken gerechnet)
    public Dictionary<string, int> AvailabilityPieces { get; set; } = new();

    // Map: ArticleId -> St\u00fccke pro Einheit (1, wenn kein Multipart)
    public Dictionary<int, int> PiecesPerUnitMap { get; set; } = new();

    [BindProperty]
    public int SelectedArticleId { get; set; }

    [BindProperty]
    public int? FromLocationId { get; set; }  // null = frei

    [BindProperty]
    public int? ToLocationId { get; set; }    // null = frei

    [BindProperty]
    public int FullUnitsToMove { get; set; }

    [BindProperty]
    public int LoosePiecesToMove { get; set; }

    public async Task OnGetAsync()
    {
        await LoadDataAsync();
    }

    private static string BuildKey(int articleId, int? locationId) =>
        $"{articleId}|{(locationId.HasValue ? locationId.Value.ToString() : "free")}";

    private async Task LoadDataAsync()
    {
        Articles = await _context.Articles
            .AsNoTracking()
            .OrderBy(a => a.Name)
            .ToListAsync();

        PiecesPerUnitMap = Articles.ToDictionary(
            a => a.Id,
            a => (a.IsMultiPart && a.PiecesPerUnit.HasValue && a.PiecesPerUnit.Value > 0)
                ? a.PiecesPerUnit.Value
                : 1);

        Locations = await _context.Locations
            .AsNoTracking()
            .OrderBy(l => l.Name)
            .ToListAsync();

        var entries = await _context.StockEntries
            .AsNoTracking()
            .Select(e => new { e.ArticleId, e.LocationId, e.FullUnits, e.LoosePieces })
            .ToListAsync();

        AvailabilityPieces = entries
            .GroupBy(e => new { e.ArticleId, e.LocationId })
            .ToDictionary(
                g => BuildKey(g.Key.ArticleId, g.Key.LocationId),
                g =>
                {
                    var perUnit = PiecesPerUnitMap.TryGetValue(g.Key.ArticleId, out var ppu) ? ppu : 1;
                    return g.Sum(e => (e.FullUnits * perUnit) + e.LoosePieces);
                });
    }

    private int GetAvailablePieces(int articleId, int? fromLocationId)
    {
        var key = BuildKey(articleId, fromLocationId);
        return AvailabilityPieces.TryGetValue(key, out var qty) ? qty : 0;
    }

    public async Task<IActionResult> OnPostAsync()
    {
        await LoadDataAsync();

        if (SelectedArticleId <= 0)
        {
            ModelState.AddModelError(string.Empty, "Bitte einen Artikel ausw\u00e4hlen.");
        }

        if (FromLocationId == ToLocationId)
        {
            ModelState.AddModelError(string.Empty, "Ausgang und Ziel d\u00fcrfen nicht identisch sein.");
        }

        if (FromLocationId.HasValue)
        {
            var fromExists = await _context.Locations.AnyAsync(l => l.Id == FromLocationId.Value);
            if (!fromExists)
            {
                ModelState.AddModelError(string.Empty, "Der gew\u00e4hlte Ausgang existiert nicht.");
            }
        }

        if (ToLocationId.HasValue)
        {
            var toExists = await _context.Locations.AnyAsync(l => l.Id == ToLocationId.Value);
            if (!toExists)
            {
                ModelState.AddModelError(string.Empty, "Das gew\u00e4hlte Ziel existiert nicht.");
            }
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

        if (FullUnitsToMove < 0 || LoosePiecesToMove < 0)
        {
            ModelState.AddModelError(string.Empty, "Mengen d\u00fcrfen nicht negativ sein.");
        }

        if (FullUnitsToMove == 0 && LoosePiecesToMove == 0)
        {
            ModelState.AddModelError(string.Empty, "Bitte eine Menge zum Buchen angeben.");
        }

        if (!article.IsMultiPart || !article.PiecesPerUnit.HasValue)
        {
            if (LoosePiecesToMove > 0)
            {
                ModelState.AddModelError(string.Empty, "Lose Einzelteile sind bei diesem Artikel nicht vorgesehen.");
            }
        }
        else
        {
            if (LoosePiecesToMove >= perUnit)
            {
                ModelState.AddModelError(string.Empty, $"Lose Einzelteile m\u00fcssen kleiner als {perUnit} sein.");
            }
        }

        var requestedPieces = (FullUnitsToMove * perUnit) + LoosePiecesToMove;

        var available = GetAvailablePieces(SelectedArticleId, FromLocationId);
        if (requestedPieces > available)
        {
            ModelState.AddModelError(string.Empty,
                $"Es sind nur {available} St\u00fcck am gew\u00e4hlten Ausgang verf\u00fcgbar.");
        }

        if (!ModelState.IsValid)
        {
            return Page();
        }

        // Ausgang-Bestand anpassen
        var fromEntry = await _context.StockEntries
            .FirstOrDefaultAsync(s =>
                s.ArticleId == SelectedArticleId &&
                s.LocationId == FromLocationId);

        if (fromEntry == null)
        {
            ModelState.AddModelError(string.Empty, "Fehler beim Lesen des Bestands am Ausgang.");
            return Page();
        }

        var fromPieces = (fromEntry.FullUnits * perUnit) + fromEntry.LoosePieces;
        fromPieces -= requestedPieces;

        if (fromPieces <= 0)
        {
            _context.StockEntries.Remove(fromEntry);
        }
        else
        {
            fromEntry.FullUnits = fromPieces / perUnit;
            fromEntry.LoosePieces = fromPieces % perUnit;
        }

        // Ziel-Bestand anpassen
        var toEntry = await _context.StockEntries
            .FirstOrDefaultAsync(s =>
                s.ArticleId == SelectedArticleId &&
                s.LocationId == ToLocationId);

        var toPiecesExisting = 0;

        if (toEntry == null)
        {
            toEntry = new StockEntry
            {
                ArticleId = SelectedArticleId,
                LocationId = ToLocationId
            };
            _context.StockEntries.Add(toEntry);
        }
        else
        {
            toPiecesExisting = (toEntry.FullUnits * perUnit) + toEntry.LoosePieces;
        }

        var toPieces = toPiecesExisting + requestedPieces;
        toEntry.FullUnits = toPieces / perUnit;
        toEntry.LoosePieces = toPieces % perUnit;

        await _context.SaveChangesAsync();

        string fromName = FromLocationId.HasValue
            ? (await _context.Locations.FindAsync(FromLocationId.Value))?.Name ?? "?"
            : "frei";

        string toName = ToLocationId.HasValue
            ? (await _context.Locations.FindAsync(ToLocationId.Value))?.Name ?? "?"
            : "frei";

        PlainFileLogger.Log(
            $"Buchung: {FullUnitsToMove} Einheiten, {LoosePiecesToMove} lose St\u00fcck {article.Name} von '{fromName}' nach '{toName}'");

        // Bleibt auf der Buchungsseite
        return RedirectToPage();
    }
}
