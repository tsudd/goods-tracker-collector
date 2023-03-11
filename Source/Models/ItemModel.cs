namespace GoodsTracker.DataCollector.Models;

public class ItemModel
{
    public const string DEFAULT_ITEM_NAME = "UNNAMED ITEM";
    public string? Name1 { get; init; } = DEFAULT_ITEM_NAME;
    public string? Name2 { get; init; }
    public string? Name3 { get; init; }
    public string? Price { get; init; }
    public string? Discount { get; init; }
    public string? Country { get; init; }
    public string? Producer { get; init; }
    public int? VendorCode { get; init; }
    public float? Weight { get; init; }
    public string? WeightUnit { get; init; }
    public string? Compound { get; init; }
    public float? Protein { get; init; }
    public float? Fat { get; init; }
    public float? Carbo { get; init; }
    public float? Portion { get; init; }
    public Guid? Guid { get; init; }
    public bool? Adult { get; init; }
    public List<string>? Categories { get; init; }
    public string? Link { get; init; }
    public string CategoriesEnum => Categories != null ? string.Join('|', Categories) : "";

    public override bool Equals(object? obj)
    {
        if (obj == null)
            return false;
        if (object.ReferenceEquals(this, obj))
            return true;
        var item = obj as ItemModel;
        return this.Name1 == item?.Name1
            && this.Name2 == item?.Name2
            && this.Name3 == item?.Name3
            && this.Price == item?.Price
            && this.Discount == item?.Discount
            && this.Link == item?.Link;
    }

    public override int GetHashCode()
    {
        return string.GetHashCode(
            $"{this.Name1}{this.Price}{this.Discount}{this.Link}{this.Name2}"
        );
    }
}
