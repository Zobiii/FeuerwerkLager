namespace FeuerwerkLager.Models;

/// <summary>
/// Zentrale Berechnungen für Bestandszeilen (Stückzahl/NEM).
/// </summary>
public static class StockMath
{
    public static int TotalPieces(StockEntry entry)
    {
        if (entry == null) return 0;
        var article = entry.Article;
        var perUnit = GetPiecesPerUnit(article);

        return (entry.FullUnits * perUnit) + entry.LoosePieces;
    }

    public static double? TotalNem(StockEntry entry)
    {
        if (entry?.Article == null)
            return null;

        var article = entry.Article;
        if (!article.NEM.HasValue)
            return null;

        var perUnit = GetPiecesPerUnit(article);
        var pieces = (entry.FullUnits * perUnit) + entry.LoosePieces;

        // Für nicht-multipart gilt NEM pro Einheit; sonst pro Stück.
        var nemPerPiece = (article.IsMultiPart && article.PiecesPerUnit.HasValue && article.PiecesPerUnit.Value > 0)
            ? article.NEM.Value / perUnit
            : article.NEM.Value;

        return nemPerPiece * pieces;
    }

    private static int GetPiecesPerUnit(Article? article)
    {
        if (article != null && article.IsMultiPart && article.PiecesPerUnit.HasValue && article.PiecesPerUnit.Value > 0)
        {
            return article.PiecesPerUnit.Value;
        }

        return 1;
    }
}
