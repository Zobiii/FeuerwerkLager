using FeuerwerkLager.Data;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace FeuerwerkLager.Pages.Locations;

public class IndexModel : PageModel
{
    private readonly FireworksContext _context;

    public IndexModel(FireworksContext context)
    {
        _context = context;
    }

    public List<LocationRow> Locations { get; set; } = new();

    public class LocationRow
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int PositionCount { get; set; }
        public int TotalQuantity { get; set; } // in St\u00fccken
        public double? TotalNEM { get; set; }
    }

    public async Task OnGetAsync()
    {
        var locations = await _context.Locations
            .OrderBy(l => l.Name)
            .ToListAsync();

        var entries = await _context.StockEntries
            .Include(s => s.Article)
            .ToListAsync();

        Locations = locations.Select(l =>
        {
            var locEntries = entries.Where(e => e.LocationId == l.Id).ToList();
            var totalPieces = locEntries.Sum(Models.StockMath.TotalPieces);
            double? totalNem = null;

            var anyNem = locEntries.Any(e => e.Article.NEM.HasValue);
            if (anyNem)
            {
                totalNem = locEntries.Sum(e => Models.StockMath.TotalNem(e) ?? 0.0);
            }

            return new LocationRow
            {
                Id = l.Id,
                Name = l.Name,
                Description = l.Description,
                PositionCount = locEntries.Count,
                TotalQuantity = totalPieces,
                TotalNEM = totalNem
            };
        }).ToList();
    }
}
