using FeuerwerkLager.Models;
using Xunit;

namespace FeuerwerkLager.Tests;

public class StockMathTests
{
    [Fact]
    public void TotalPieces_Multipart_AddsFullUnitsAndLoose()
    {
        var article = new Article
        {
            IsMultiPart = true,
            PiecesPerUnit = 10
        };
        var entry = new StockEntry
        {
            Article = article,
            FullUnits = 2,
            LoosePieces = 3
        };

        Assert.Equal(23, StockMath.TotalPieces(entry));
        Assert.Equal(23, entry.TotalPieces);
    }

    [Fact]
    public void TotalNem_Multipart_UsesNemPerPiece()
    {
        var article = new Article
        {
            IsMultiPart = true,
            PiecesPerUnit = 10,
            NEM = 50 // pro Einheit
        };
        var entry = new StockEntry
        {
            Article = article,
            FullUnits = 2,
            LoosePieces = 3
        };

        // NEM pro Stück = 5, Stücke = 23 -> 115
        Assert.Equal(115, StockMath.TotalNem(entry));
    }

    [Fact]
    public void TotalNem_NonMultipart_TreatsNemPerUnit()
    {
        var article = new Article
        {
            IsMultiPart = false,
            NEM = 2
        };
        var entry = new StockEntry
        {
            Article = article,
            FullUnits = 3,
            LoosePieces = 1
        };

        Assert.Equal(8, StockMath.TotalNem(entry));
    }

    [Fact]
    public void TotalNem_NoNem_ReturnsNull()
    {
        var article = new Article
        {
            IsMultiPart = true,
            PiecesPerUnit = 5,
            NEM = null
        };
        var entry = new StockEntry
        {
            Article = article,
            FullUnits = 1,
            LoosePieces = 2
        };

        Assert.Null(StockMath.TotalNem(entry));
    }
}
