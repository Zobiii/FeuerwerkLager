using FeuerwerkLager.Data;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ZXing;
using ZXing.Rendering;

namespace FeuerwerkLager.Pages.Locations;

public class BarcodesModel : PageModel
{
    private readonly FireworksContext _context;

    public BarcodesModel(FireworksContext context)
    {
        _context = context;
    }

    public class BarcodeRow
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string SvgContent { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
    }

    public List<BarcodeRow> Barcodes { get; set; } = new();

    public async Task OnGetAsync()
    {
        var locations = await _context.Locations
            .OrderBy(l => l.Name)
            .ToListAsync();

        if (!locations.Any())
        {
            Barcodes = new List<BarcodeRow>();
            return;
        }

        // Barcode-Writer für CODE_128 als SVG
        var writer = new BarcodeWriterSvg
        {
            Format = BarcodeFormat.CODE_128,
            Options = new ZXing.Common.EncodingOptions
            {
                Height = 60,
                Width = 220,
                Margin = 2,
                PureBarcode = true
            },
            Renderer = new SvgRenderer()
        };

        foreach (var loc in locations)
        {
            // Was im Barcode steht - hier einfach der Lagerplatzname, z.B. "KT-01"
            var value = loc.Name.Trim();

            var svgImage = writer.Write(value);

            Barcodes.Add(new BarcodeRow
            {
                Name = loc.Name,
                Description = loc.Description,
                Value = value,
                SvgContent = svgImage.Content
            });
        }
    }
}
