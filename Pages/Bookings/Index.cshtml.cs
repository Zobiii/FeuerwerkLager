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

    // Map: "ArticleId|LocationId/free" -> verfügbare Menge
    public Dictionary<string, int> Availability { get; set; } = new();

    [BindProperty]
    public int SelectedArticleId { get; set; }

    [BindProperty]
    public int? FromLocationId { get; set; }  // null = frei

    [BindProperty]
    public int? ToLocationId { get; set; }    // null = frei

    [BindProperty]
    public int Quantity { get; set; }

    public async Task OnGetAsync()
    {
        await LoadDataAsync();
    }

    private static string BuildKey(int articleId, int? locationId) =>
        $"{articleId}|{(locationId.HasValue ? locationId.Value.ToString() : "free")}";

    private async Task LoadDataAsync()
    {
        Articles = await _context.Articles
            .OrderBy(a => a.Name)
            .ToListAsync();

        Locations = await _context.Locations
            .OrderBy(l => l.Name)
            .ToListAsync();

        var entries = await _context.StockEntries.ToListAsync();

        Availability = entries
            .GroupBy(e => new { e.ArticleId, e.LocationId })
            .ToDictionary(
                g => BuildKey(g.Key.ArticleId, g.Key.LocationId),
                g => g.Sum(e => e.Quantity)
            );
    }

    private int GetAvailableQuantity(int articleId, int? fromLocationId)
    {
        var key = BuildKey(articleId, fromLocationId);
        return Availability.TryGetValue(key, out var qty) ? qty : 0;
    }

    public async Task<IActionResult> OnPostAsync()
    {
        await LoadDataAsync();

        if (SelectedArticleId <= 0)
        {
            ModelState.AddModelError(string.Empty, "Bitte einen Artikel auswählen.");
        }

        if (Quantity <= 0)
        {
            ModelState.AddModelError(string.Empty, "Menge muss größer 0 sein.");
        }

        if (FromLocationId == ToLocationId)
        {
            ModelState.AddModelError(string.Empty, "Ausgang und Ziel dürfen nicht identisch sein.");
        }

        if (FromLocationId.HasValue)
        {
            var fromExists = await _context.Locations.AnyAsync(l => l.Id == FromLocationId.Value);
            if (!fromExists)
            {
                ModelState.AddModelError(string.Empty, "Der gewählte Ausgang existiert nicht.");
            }
        }

        if (ToLocationId.HasValue)
        {
            var toExists = await _context.Locations.AnyAsync(l => l.Id == ToLocationId.Value);
            if (!toExists)
            {
                ModelState.AddModelError(string.Empty, "Das gewählte Ziel existiert nicht.");
            }
        }

        var available = GetAvailableQuantity(SelectedArticleId, FromLocationId);
        if (Quantity > available)
        {
            ModelState.AddModelError(string.Empty,
                $"Es sind nur {available} Stück am gewählten Ausgang verfügbar.");
        }

        if (!ModelState.IsValid)
        {
            return Page();
        }

        var article = await _context.Articles.FindAsync(SelectedArticleId);
        if (article == null)
        {
            ModelState.AddModelError(string.Empty, "Artikel nicht gefunden.");
            return Page();
        }

        // Ausgang-Bestand anpassen
        var fromEntry = await _context.StockEntries
            .FirstOrDefaultAsync(s =>
                s.ArticleId == SelectedArticleId &&
                s.LocationId == FromLocationId);

        if (fromEntry == null || fromEntry.Quantity < Quantity)
        {
            ModelState.AddModelError(string.Empty, "Fehler beim Lesen des Bestands am Ausgang.");
            return Page();
        }

        fromEntry.Quantity -= Quantity;
        if (fromEntry.Quantity == 0)
        {
            _context.StockEntries.Remove(fromEntry);
        }

        // Ziel-Bestand anpassen
        var toEntry = await _context.StockEntries
            .FirstOrDefaultAsync(s =>
                s.ArticleId == SelectedArticleId &&
                s.LocationId == ToLocationId);

        if (toEntry == null)
        {
            toEntry = new StockEntry
            {
                ArticleId = SelectedArticleId,
                LocationId = ToLocationId,
                Quantity = Quantity
            };
            _context.StockEntries.Add(toEntry);
        }
        else
        {
            toEntry.Quantity += Quantity;
        }

        await _context.SaveChangesAsync();

        string fromName = FromLocationId.HasValue
            ? (await _context.Locations.FindAsync(FromLocationId.Value))?.Name ?? "?"
            : "frei";

        string toName = ToLocationId.HasValue
            ? (await _context.Locations.FindAsync(ToLocationId.Value))?.Name ?? "?"
            : "frei";

        PlainFileLogger.Log(
            $"Buchung: {Quantity}x {article.Name} von '{fromName}' nach '{toName}'");

        // Bleibt auf der Buchungsseite
        return RedirectToPage();
    }
}
