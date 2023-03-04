namespace GoodsTracker.DataCollector.DB.Entities;

using System.ComponentModel.DataAnnotations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

[PrimaryKey("ItemId", "StreamId")]
public class ItemRecord
{
    public int ItemId { get; set; }
    public int StreamId { get; set; }

    [Required]
    public decimal Price { get; set; }
    public decimal CutPrice { get; set; }
    public bool OnDiscount { get; set; } = false;

    // FK
    [Required]
    public Item Item { get; set; } = null!;

    [Required]
    public Stream Stream { get; set; } = null!;

    public sealed class EntityConfiguration : IEntityTypeConfiguration<ItemRecord>
    {
        public void Configure(EntityTypeBuilder<ItemRecord> builder)
        {
            builder.Property(p => p.Price).HasPrecision(9, 2);
            builder.Property(p => p.CutPrice).HasPrecision(9, 2);
        }
    }
}