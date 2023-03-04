namespace GoodsTracker.DataCollector.DB.Entities;

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

using GoodsTracker.DataCollector.DB.Entities.Enumerators;

public class Vendor
{
    [Key]
    public int Id { get; set; }
    public string? Name1 { get; set; }
    public string? Name2 { get; set; }
    public string? Name3 { get; set; }
    public VendorType Type { get; set; }
    public string? Website { get; set; }

    [MaxLength(3)]
    public string? Land { get; set; }

    [Column(TypeName = "text")]
    public Uri? LogoImageLink { get; set; }
}