namespace GoodsTracker.DataCollector.Models;

public class ItemModel
{
    public string? Name1 { get; init; }
    public string? Name2 { get; init; }
    public string? Name3 { get; init; }
    public decimal? Price { get; init; }
    public decimal? Discount { get; init; }
    public string? Country { get; init; }
    public string? Producer { get; init; }
    public long? VendorCode { get; init; }
    public float? Weight { get; init; }
    public string? WeightUnit { get; init; }
    public string? Compound { get; init; }
    public float? Protein { get; init; }
    public float? Fat { get; init; }
    public float? Carbo { get; init; }
    public float? Portion { get; init; }
    public Guid? Guid { get; init; }
    public bool? Adult { get; init; }
    public List<string> Categories { get; init; } = new List<string>();
    public string? Link { get; init; }
    public string CategoriesEnum => Categories != null ? string.Join('|', Categories) : "";
}
