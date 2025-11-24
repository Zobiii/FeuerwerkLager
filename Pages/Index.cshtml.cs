using FeuerwerkLager.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace FeuerwerkLager.Pages;

public class IndexModel : PageModel
{
    private readonly FireworksContext _context;

    public IndexModel(FireworksContext context)
    {
        _context = context;
    }

    public IList<StockRow> Stock { get; set; } = new List<StockRow>();
    public IList<LocationOption> Locations { get; set; } = new List<LocationOption>();

    // Dashboard-Kennzahlen (gesamtes System, nicht gefiltert)
    public int TotalArticleCount { get; set; }
    public int TotalLocationCount { get; set; }
    public int TotalStockPositionsAll { get; set; }
    public double TotalNemAll { get; set; }
    public int TotalPiecesAll { get; set; }

    public class StockRow
    {
        public int ArticleId { get; set; }
        public string ArticleName { get; set; } = string.Empty;
        public string? ProductNumber { get; set; }
        public string Company { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;

        public string LocationName { get; set; } = "frei";
        public bool IsFree => LocationName == "frei";

        public int Quantity { get; set; }
        public double? TotalNEM { get; set; }

        // Multi-Part Infos
        public bool IsMultiPart { get; set; }
        public int? PiecesPerUnit { get; set; }
        public int? TotalPieces { get; set; }
    }

    public class LocationOption
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    [BindProperty(SupportsGet = true)]
    public string? SearchTerm { get; set; }

    [BindProperty(SupportsGet = true)]
    public int? FilterLocationId { get; set; }

    [BindProperty(SupportsGet = true)]
    public bool OnlyFree { get; set; }

    public async Task OnGetAsync()
    {
        // Kennzahlen (ohne Filter)
        TotalArticleCount = await _context.Articles.CountAsync();
        TotalLocationCount = await _context.Locations.CountAsync();

        var allEntries = await _context.StockEntries
            .Include(e => e.Article)
            .ToListAsync();

        TotalStockPositionsAll = allEntries.Count;
        TotalNemAll = allEntries.Sum(e => (e.Article.NEM ?? 0.0) * e.Quantity);
        TotalPiecesAll = allEntries
            .Where(e => e.Article.IsMultiPart && e.Article.PiecesPerUnit.HasValue)
            .Sum(e => e.Quantity * e.Article.PiecesPerUnit!.Value);

        // Filter-Lookup
        Locations = await _context.Locations
            .OrderBy(l => l.Name)
            .Select(l => new LocationOption
            {
                Id = l.Id,
                Name = l.Name
            })
            .ToListAsync();

        // Bestandsliste (mit Filter)
        var query = _context.StockEntries
            .Include(s => s.Article)
            .Include(s => s.Location)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(SearchTerm))
        {
            var st = SearchTerm.Trim();
            query = query.Where(e =>
                EF.Functions.Like(e.Article.Name, $"%{st}%") ||
                EF.Functions.Like(e.Article.Company, $"%{st}%") ||
                (e.Article.ProductNumber != null &&
                 EF.Functions.Like(e.Article.ProductNumber, $"%{st}%")) ||
                (e.Article.Category != null &&
                 EF.Functions.Like(e.Article.Category, $"%{st}%")));
        }

        if (OnlyFree)
        {
            query = query.Where(e => e.LocationId == null);
        }
        else if (FilterLocationId.HasValue)
        {
            query = query.Where(e => e.LocationId == FilterLocationId.Value);
        }

        var entries = await query
            .OrderBy(e => e.LocationId.HasValue ? e.Location!.Name : "zzz_frei")
            .ThenBy(e => e.Article.Name)
            .ToListAsync();

        Stock = entries.Select(e =>
        {
            int? totalPieces = null;
            if (e.Article.IsMultiPart && e.Article.PiecesPerUnit.HasValue)
            {
                totalPieces = e.Article.PiecesPerUnit.Value * e.Quantity;
            }

            return new StockRow
            {
                ArticleId = e.ArticleId,
                ArticleName = e.Article.Name,
                ProductNumber = e.Article.ProductNumber,
                Company = e.Article.Company,
                Category = e.Article.Category,
                LocationName = e.Location != null ? e.Location.Name : "frei",
                Quantity = e.Quantity,
                TotalNEM = e.Article.NEM.HasValue ? e.Article.NEM.Value * e.Quantity : null,
                IsMultiPart = e.Article.IsMultiPart,
                PiecesPerUnit = e.Article.PiecesPerUnit,
                TotalPieces = totalPieces
            };
        }).ToList();
    }
}
