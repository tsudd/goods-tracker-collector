using GoodsTracker.DataCollector.Common.Mappers.Abstractions;
using GoodsTracker.DataCollector.Models;
using GoodsTracker.DataCollector.Models.Constants;

namespace GoodsTracker.DataCollector.Common.Mappers;

public class BasicMapper : IItemMapper
{
    public ItemModel MapItemFields(Dictionary<ItemFields, string> fields)
    {
        Func<string, string?> noAffect = static _ => _;
        return new ItemModel()
        {
            Name1 = TryGetValueOrDefault(fields, ItemFields.Name1, AdjustNameIfRequired),
            Name2 = TryGetValueOrDefault(fields, ItemFields.Name2, noAffect),
            Name3 = TryGetValueOrDefault(fields, ItemFields.Name3, noAffect),
            Price = TryGetValueOrDefault(fields, ItemFields.Price, AdjustPriceIfRequired),
            Discount = TryGetValueOrDefault(fields, ItemFields.Discount, AdjustPriceIfRequired),
            Country = TryGetValueOrDefault(fields, ItemFields.Country, noAffect),
            Producer = TryGetValueOrDefault(fields, ItemFields.Producer, noAffect),
            VendorCode = TryGetValueOrDefault(fields, ItemFields.VendorCode, ParseIntOrDefault),
            Weight = TryGetValueOrDefault(fields, ItemFields.Weight, ParseFloatOrDefault),
            WeightUnit = TryGetValueOrDefault(fields, ItemFields.WeightUnit, noAffect),
            Compound = TryGetValueOrDefault(fields, ItemFields.Compound, noAffect),
            Carbo = TryGetValueOrDefault(fields, ItemFields.Carbo, ParseFloatOrDefault),
            Fat = TryGetValueOrDefault(fields, ItemFields.Fat, ParseFloatOrDefault),
            Protein = TryGetValueOrDefault(fields, ItemFields.Protein, ParseFloatOrDefault),
            Portion = TryGetValueOrDefault(fields, ItemFields.Portion, ParseFloatOrDefault),
            Categories = TryGetValueOrDefault(
                fields,
                ItemFields.Categories,
                ParseCategoriesOrEmpty
            ),
            Link = TryGetValueOrDefault(fields, ItemFields.ImageLink, noAffect),
            Adult = TryGetValueOrDefault(fields, ItemFields.Adult, ParseBooleanOrDefault),
            Guid = TryGetValueOrDefault(fields, ItemFields.Guid, ParseGuidOrDefault),
        };
    }

    protected int? ParseIntOrDefault(string numberValue)
    {
        if (Int32.TryParse(numberValue, out int result))
        {
            return result;
        }
        return null;
    }

    protected float? ParseFloatOrDefault(string numberValue)
    {
        if (
            float.TryParse(
                numberValue,
                System.Globalization.NumberStyles.Float,
                System.Globalization.NumberFormatInfo.InvariantInfo,
                out float result
            )
        )
        {
            return result;
        }
        return null;
    }

    protected bool? ParseBooleanOrDefault(string boolValue)
    {
        if (bool.TryParse(boolValue, out bool result))
        {
            return result;
        }
        return null;
    }

    protected List<string> ParseCategoriesOrEmpty(string categoriesValue)
    {
        return categoriesValue.Split(IItemMapper.CategoriesSeparator).ToList();
    }

    protected Guid? ParseGuidOrDefault(string guidValue)
    {
        if (Guid.TryParse(guidValue, out Guid result))
        {
            return result;
        }
        return null;
    }

    protected string AdjustPriceIfRequired(string rawPrice)
    {
        return rawPrice.Replace(',', '.');
    }

    protected string AdjustNameIfRequired(string itemName)
    {
        return itemName.Replace("'", " ");
    }

    protected TValue? TryGetValueOrDefault<TValue>(
        Dictionary<ItemFields, string> dict,
        ItemFields field,
        Func<string, TValue?> affect
    ) => dict.ContainsKey(field) ? affect(dict[field]) : default(TValue);
}
