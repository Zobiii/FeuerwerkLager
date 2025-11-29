namespace FeuerwerkLager.Models;

using System.ComponentModel.DataAnnotations;

public class StockEntry
{
    public int Id { get; set; }

    public int ArticleId { get; set; }
    public Article Article { get; set; } = null!;

    public int? LocationId { get; set; }           // null = frei
    public Location? Location { get; set; }

    [Range(0, int.MaxValue)]
    public int FullUnits { get; set; }             // Volle Gebinde/Schachteln/Einheiten

    [Range(0, int.MaxValue)]
    public int LoosePieces { get; set; }           // Lose Einzelteile aus angebrochenen Gebinden

    /// <summary>
    /// Gesamt-Stückzahl: volle Einheiten + lose Stücke.
    /// Bei nicht-multipart Artikeln entspricht das der Stückzahl; lose Teile sollten dort 0 sein.
    /// </summary>
    public int TotalPieces => StockMath.TotalPieces(this);
}
