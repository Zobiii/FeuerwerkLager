namespace FeuerwerkLager.Models;

public class Location
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;      // z.B. "Karton A1"
    public string? Description { get; set; }

    public ICollection<StockEntry> StockEntries { get; set; } = new List<StockEntry>();
}
