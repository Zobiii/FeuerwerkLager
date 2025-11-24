namespace FeuerwerkLager.Models;

public class Article
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;          // Pflicht
    public string? ProductNumber { get; set; }                // optional
    public double? NEM { get; set; }                          // optional (z.B. Gramm)
    public string Company { get; set; } = string.Empty;       // Pflicht

    // Neue Felder
    public string Category { get; set; } = string.Empty;      // z.B. Raketen, Batterie, ...
    public bool IsMultiPart { get; set; }                     // Hat Einzelteile?
    public int? PiecesPerUnit { get; set; }                   // Anzahl Einzelteile pro Artikel (z.B. 100)

    public string? Notes { get; set; }                        // optional

    public ICollection<StockEntry> StockEntries { get; set; } = new List<StockEntry>();
}
