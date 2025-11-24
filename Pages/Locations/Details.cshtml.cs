using FeuerwerkLager.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace FeuerwerkLager.Pages.Locations;

public class DetailsModel : PageModel
{
    private readonly FireworksContext _context;

    public DetailsModel(FireworksContext context)
    {
        _context = context;
    }

    public LocationView? Location { get; set; }
    public List<LocationItemRow> Items { get; set; } = new();
    public int TotalQuantity { get; set; }
    public double? TotalNEM { get; set; }

    public class LocationView
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
    }

    public class LocationItemRow
    {
        public string ArticleName { get; set; } = string.Empty;
        public string Company { get; set; } = string.Empty;
        public string? ProductNumber { get; set; }
        public int Quantity { get; set; }
        public double? NemPerPiece { get; set; }
        public double? TotalNem { get; set; }
    }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var loc = await _context.Locations.FindAsync(id);
        if (loc == null)
        {
            return NotFound();
        }

        Location = new LocationView
        {
            Id = loc.Id,
            Name = loc.Name,
            Description = loc.Description
        };

        var entries = await _context.StockEntries
            .Include(e => e.Article)
            .Where(e => e.LocationId == id)
            .OrderBy(e => e.Article.Name)
            .ToListAsync();

        Items = entries.Select(e =>
        {
            double? totalNem = null;
            if (e.Article.NEM.HasValue)
            {
                totalNem = e.Article.NEM.Value * e.Quantity;
            }

            return new LocationItemRow
            {
                ArticleName = e.Article.Name,
                Company = e.Article.Company,
                ProductNumber = e.Article.ProductNumber,
                Quantity = e.Quantity,
                NemPerPiece = e.Article.NEM,
                TotalNem = totalNem
            };
        }).ToList();

        TotalQuantity = Items.Sum(i => i.Quantity);
        if (Items.Any(i => i.TotalNem.HasValue))
        {
            TotalNEM = Items.Sum(i => i.TotalNem ?? 0.0);
        }

        return Page();
    }
}
