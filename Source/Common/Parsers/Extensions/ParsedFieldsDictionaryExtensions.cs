namespace GoodsTracker.DataCollector.Common.Parsers.Extensions;

using GoodsTracker.DataCollector.Models.Constants;

internal static class ItemFieldsDictionaryExtensions
{
    internal static void AddItemName1(this Dictionary<ItemFields, string> dict, string value)
    {
        dict.Add(ItemFields.Name1, value);
    }

    internal static void AddItemName2(this Dictionary<ItemFields, string> dict, string value)
    {
        dict.Add(ItemFields.Name2, value);
    }

    internal static void AddItemName3(this Dictionary<ItemFields, string> dict, string value)
    {
        dict.Add(ItemFields.Name3, value);
    }

    internal static void AddItemWeight(this Dictionary<ItemFields, string> dict, string value)
    {
        dict.Add(ItemFields.Weight, value);
    }

    internal static void AddItemWeightUnit(this Dictionary<ItemFields, string> dict, string value)
    {
        dict.Add(ItemFields.WeightUnit, value);
    }

    internal static void AddItemPrice(this Dictionary<ItemFields, string> dict, string value)
    {
        dict.Add(ItemFields.Price, value);
    }

    internal static void AddItemCutPrice(this Dictionary<ItemFields, string> dict, string value)
    {
        dict.Add(ItemFields.Discount, value);
    }

    internal static void AddItemAdult(this Dictionary<ItemFields, string> dict, string value)
    {
        dict.Add(ItemFields.Adult, value);
    }

    internal static void AddItemVendorCode(this Dictionary<ItemFields, string> dict, string value)
    {
        dict.Add(ItemFields.VendorCode, value);
    }

    internal static void AddItemGuid(this Dictionary<ItemFields, string> dict, string value)
    {
        dict.Add(ItemFields.Guid, value);
    }

    internal static void AddItemImageLink(this Dictionary<ItemFields, string> dict, string value)
    {
        dict.Add(ItemFields.ImageLink, value);
    }

    internal static void AddItemProducer(this Dictionary<ItemFields, string> dict, string value)
    {
        dict.Add(ItemFields.Producer, value);
    }

    internal static void AddItemCountry(this Dictionary<ItemFields, string> dict, string value)
    {
        dict.Add(ItemFields.Country, value);
    }

    internal static void AddItemCompound(this Dictionary<ItemFields, string> dict, string value)
    {
        dict.Add(ItemFields.Compound, value);
    }

    internal static void AddItemCategories(this Dictionary<ItemFields, string> dict, string value)
    {
        dict.Add(ItemFields.Categories, value);
    }

    internal static void AddItemProtein(this Dictionary<ItemFields, string> dict, string protein)
    {
        dict.Add(ItemFields.Protein, protein);
    }

    internal static void AddItemFat(this Dictionary<ItemFields, string> dict, string fat)
    {
        dict.Add(ItemFields.Fat, fat);
    }

    internal static void AddItemCarbo(this Dictionary<ItemFields, string> dict, string carbo)
    {
        dict.Add(ItemFields.Carbo, carbo);
    }
}
