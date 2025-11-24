using FeuerwerkLager.Models;
using Microsoft.EntityFrameworkCore;

namespace FeuerwerkLager.Data;

public class FireworksContext : DbContext
{
    public FireworksContext(DbContextOptions<FireworksContext> options) : base(options)
    {
    }

    public DbSet<Article> Articles => Set<Article>();
    public DbSet<Location> Locations => Set<Location>();
    public DbSet<StockEntry> StockEntries => Set<StockEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Quantity >= 0
        modelBuilder.Entity<StockEntry>()
            .Property(s => s.Quantity)
            .HasDefaultValue(0);

        modelBuilder.Entity<StockEntry>()
            .ToTable("StockEntries", builder =>
            {
                builder.HasCheckConstraint("CK_StockEntries_Quantity_NonNegative", "[Quantity] >= 0");
            });

        // Einzigartigkeit: Pro (Article, Location) nur eine Zeile
        modelBuilder.Entity<StockEntry>()
            .HasIndex(s => new { s.ArticleId, s.LocationId })
            .IsUnique();
    }
}
