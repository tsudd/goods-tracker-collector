namespace GoodsTracker.DataCollector.DB.Context;

using GoodsTracker.DataCollector.DB.Entities;

using Microsoft.EntityFrameworkCore;

public class CollectorContext : DbContext
{
    public CollectorContext(DbContextOptions options)
        : base(options)
    {
    }

    public DbSet<Category> Categories { get; set; } = null!;
    public DbSet<Vendor> Vendors { get; set; } = null!;
    public DbSet<Stream> Streams { get; set; } = null!;
    public DbSet<Item> Items { get; set; } = null!;
    public DbSet<ItemRecord> ItemRecords { get; set; } = null!;
    public DbSet<FavoriteItem> FavoriteItems { get; set; } = null!;
    public DbSet<Producer> Producers { get; set; } = null!;
    public DbSet<ItemError> ItemErrors { get; set; } = null!;

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        _ = optionsBuilder.UseSnakeCaseNamingConvention();
        _ = optionsBuilder.UseNpgsql("Server=127.0.0.1;Port=5432;Database=trackerDB;UID=sa;PWD=sa");
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasPostgresExtension("btree_gin");
        modelBuilder.HasPostgresExtension("pg_trgm");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CollectorContext).Assembly);
    }
}
