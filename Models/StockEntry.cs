namespace FeuerwerkLager.Models;

public class StockEntry
{
    public int Id { get; set; }

    public int ArticleId { get; set; }
    public Article Article { get; set; } = null!;

    public int? LocationId { get; set; }           // null = frei
    public Location? Location { get; set; }

    public int Quantity { get; set; }              // z.B. 12 Stück
}
