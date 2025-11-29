using FeuerwerkLager.Data;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace FeuerwerkLager.Pages.Reports;

public class NemOverviewModel : PageModel
{
    private readonly FireworksContext _context;

    public NemOverviewModel(FireworksContext context)
    {
        _context = context;
    }

    public List<NemRow> Rows { get; set; } = new();
    public double TotalNemAll { get; set; }

    public class NemRow
    {
        public string LocationName { get; set; } = string.Empty;
        public bool IsFree { get; set; }
        public int PositionCount { get; set; }
        public int TotalQuantity { get; set; } // in St\u00fccken
        public double TotalNem { get; set; }
    }

    public async Task OnGetAsync()
    {
        var entries = await _context.StockEntries
            .Include(e => e.Article)
            .Include(e => e.Location)
            .ToListAsync();

        var groups = entries
            .GroupBy(e => e.LocationId)
            .ToList();

        Rows = new List<NemRow>();

        foreach (var g in groups)
        {
            string name;
            bool isFree = !g.Key.HasValue;

            if (isFree)
            {
                name = "frei (nicht zugeordnete Best\u00e4nde)";
            }
            else
            {
                var loc = g.First().Location;
                name = loc?.Name ?? $"Lagerplatz #{g.Key}";
            }

            var posCount = g.Count();
            var totalPieces = g.Sum(e =>
            {
                var perUnit = (e.Article.IsMultiPart && e.Article.PiecesPerUnit.HasValue && e.Article.PiecesPerUnit.Value > 0)
                    ? e.Article.PiecesPerUnit.Value
                    : 1;
                return (e.FullUnits * perUnit) + e.LoosePieces;
            });
            var totalNem = g.Sum(e =>
            {
                if (!e.Article.NEM.HasValue)
                    return 0.0;

                var perUnit = (e.Article.IsMultiPart && e.Article.PiecesPerUnit.HasValue && e.Article.PiecesPerUnit.Value > 0)
                    ? e.Article.PiecesPerUnit.Value
                    : 1;

                var totalPieces = (e.FullUnits * perUnit) + e.LoosePieces;
                var nemPerPiece = e.Article.IsMultiPart && e.Article.PiecesPerUnit.HasValue && e.Article.PiecesPerUnit.Value > 0
                    ? e.Article.NEM.Value / e.Article.PiecesPerUnit.Value
                    : e.Article.NEM.Value; // f\u00fcr nicht-multipart gilt NEM pro Einheit

                return nemPerPiece * totalPieces;
            });

            Rows.Add(new NemRow
            {
                LocationName = name,
                IsFree = isFree,
                PositionCount = posCount,
                TotalQuantity = totalPieces,
                TotalNem = totalNem
            });
        }

        TotalNemAll = Rows.Sum(r => r.TotalNem);
        Rows = Rows.OrderBy(r => r.IsFree ? 1 : 0).ThenBy(r => r.LocationName).ToList();
    }
}
