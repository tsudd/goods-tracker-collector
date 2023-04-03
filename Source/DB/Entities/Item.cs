namespace GoodsTracker.DataCollector.DB.Entities;

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class Item
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }
    public Guid? PublicId { get; set; }

    [Required]
    public string Name1 { get; set; } = null!;
    public string? Name2 { get; set; }
    public string? Name3 { get; set; }

    [Column(TypeName = "text")]
    public Uri? ImageLink { get; set; }
    public float? Weight { get; set; }
    public string? WeightUnit { get; set; }
    public long? VendorCode { get; set; }
    public string? Compound { get; set; }
    public float? Protein { get; set; }
    public float? Fat { get; set; }
    public float? Carbo { get; set; }
    public float? Portion { get; set; }
    public uint? ProducerId { get; set; }
    public bool Adult { get; set; }
    public int VendorId { get; set; }

    [Column(TypeName = "text")]
    public string? Metadata { get; set; }

    // FK
    [Required]
    public Vendor Vendor { get; set; } = null!;

    public Producer? Producer { get; set; }

    public ICollection<Category> Categories { get; set; } = new List<Category>();

    public sealed class EntityConfiguration : IEntityTypeConfiguration<Item>
    {
        public void Configure(EntityTypeBuilder<Item> builder)
        {
            builder.HasIndex(static x => x.Name1)
                   .HasMethod("gin")
                   .HasOperators("gin_trgm_ops");

            builder.HasIndex(static x => x.VendorId);
        }
    }

}
