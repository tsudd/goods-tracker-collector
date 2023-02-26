namespace GoodsTracker.DataCollector.Models;
public class Item
{
    public const string DEFAULT_ITEM_NAME = "UNNAMED ITEM";
    public string? Name1 { get; set; } = DEFAULT_ITEM_NAME;
    public string? Name2 { get; set; }
    public string? Name3 { get; set; }
    public string? Price { get; set; }
    public string? Discount { get; set; }
    public string? Country { get; set; }
    public string? Producer { get; set; }
    public int? VendorCode { get; set; }
    public float? Wieght { get; set; }
    public string? WieghtUnit { get; set; }
    public string? Compound { get; set; }
    public float? Protein { get; set; }
    public float? Fat { get; set; }
    public float? Carbo { get; set; }
    public float? Portion { get; set; }
    public List<string>? Categories { get; set; }
    public string? Link { get; set; }
    public string CategoriesEnum => Categories != null ? string.Join('|', Categories) : "";

    public override bool Equals(object? obj)
    {
        if (obj == null)
            return false;
        if (object.ReferenceEquals(this, obj))
            return true;
        var item = obj as Item;
        return this.Name1 == item?.Name1
            && this.Name2 == item?.Name2
            && this.Name3 == item?.Name3
            && this.Price == item?.Price
            && this.Discount == item?.Discount
            && this.Link == item?.Link;
    }

    public override int GetHashCode()
    {
        return $"{this.Name1}{this.Price}{this.Discount}{this.Link}{this.Name2}".GetHashCode();
    }
}
