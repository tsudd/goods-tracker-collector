namespace GoodsTracker.DataCollector.Common.Parsers.Extensions;

using GoodsTracker.DataCollector.Models.Constants;

public static class ItemFieldsDictionaryExtensions
{
    public static void AddItemName1(this Dictionary<ItemFields, string> dict, string value)
    {
        dict.Add(ItemFields.Name1, value);
    }

    public static void AddItemName2(this Dictionary<ItemFields, string> dict, string value)
    {
        dict.Add(ItemFields.Name2, value);
    }

    public static void AddItemName3(this Dictionary<ItemFields, string> dict, string value)
    {
        dict.Add(ItemFields.Name3, value);
    }

    public static void AddItemWeight(this Dictionary<ItemFields, string> dict, string value)
    {
        dict.Add(ItemFields.Weight, value);
    }

    public static void AddItemWeightUnit(this Dictionary<ItemFields, string> dict, string value)
    {
        dict.Add(ItemFields.WeightUnit, value);
    }

    public static void AddItemPrice(this Dictionary<ItemFields, string> dict, string value)
    {
        dict.Add(ItemFields.Price, value);
    }

    public static void AddItemCutPrice(this Dictionary<ItemFields, string> dict, string value)
    {
        dict.Add(ItemFields.Discount, value);
    }

    public static void AddItemAdult(this Dictionary<ItemFields, string> dict, string value)
    {
        dict.Add(ItemFields.Adult, value);
    }

    public static void AddItemVendorCode(this Dictionary<ItemFields, string> dict, string value)
    {
        dict.Add(ItemFields.VendorCode, value);
    }

    public static void AddItemGuid(this Dictionary<ItemFields, string> dict, string value)
    {
        dict.Add(ItemFields.Guid, value);
    }

    public static void AddItemImageLink(this Dictionary<ItemFields, string> dict, string value)
    {
        dict.Add(ItemFields.ImageLink, value);
    }

    public static void AddItemProducer(this Dictionary<ItemFields, string> dict, string value)
    {
        dict.Add(ItemFields.Producer, value);
    }

    public static void AddItemCountry(this Dictionary<ItemFields, string> dict, string value)
    {
        dict.Add(ItemFields.Country, value);
    }

    public static void AddItemCompound(this Dictionary<ItemFields, string> dict, string value)
    {
        dict.Add(ItemFields.Compound, value);
    }

    public static void AddItemCategories(this Dictionary<ItemFields, string> dict, string value)
    {
        dict.Add(ItemFields.Categories, value);
    }
}
