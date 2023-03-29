using GoodsTracker.DataCollector.Common.Mappers.Abstractions;
using GoodsTracker.DataCollector.Models;
using GoodsTracker.DataCollector.Models.Constants;

namespace GoodsTracker.DataCollector.Common.Mappers;

public class BasicMapper : IItemMapper
{
    public ItemModel MapItemFields(Dictionary<ItemFields, string> fields)
    {
        Func<string, string?> noAffect = static _ => _;
        return new ItemModel
        {
            Name1 = TryGetValueOrDefault(fields, ItemFields.Name1, AdjustNameIfRequired),
            Name2 = TryGetValueOrDefault(fields, ItemFields.Name2, noAffect),
            Name3 = TryGetValueOrDefault(fields, ItemFields.Name3, noAffect),
            Price = TryGetValueOrDefault(fields, ItemFields.Price, ParseDecimalOrDefault),
            Discount = TryGetValueOrDefault(fields, ItemFields.Discount, ParseDecimalOrDefault),
            Country = TryGetValueOrDefault(fields, ItemFields.Country, noAffect),
            Producer = TryGetValueOrDefault(fields, ItemFields.Producer, noAffect),
            VendorCode = TryGetValueOrDefault(fields, ItemFields.VendorCode, ParseLongOrDefault),
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
            ) ?? new List<string>(),
            Link = TryGetValueOrDefault(fields, ItemFields.ImageLink, noAffect),
            Adult = TryGetValueOrDefault(fields, ItemFields.Adult, ParseBooleanOrDefault),
            Guid = TryGetValueOrDefault(fields, ItemFields.Guid, ParseGuidOrDefault),
        };
    }

    protected static long? ParseLongOrDefault(string numberValue)
    {
        if (Int64.TryParse(numberValue, out long result))
        {
            return result;
        }
        return null;
    }

    protected static float? ParseFloatOrDefault(string numberValue)
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

    protected static decimal? ParseDecimalOrDefault(string numberValue)
    {
        if (
            decimal.TryParse(
                numberValue,
                System.Globalization.NumberStyles.Float,
                System.Globalization.NumberFormatInfo.InvariantInfo,
                out decimal result
            )
        )
        {
            return result;
        }
        return null;
    }

    protected static bool? ParseBooleanOrDefault(string boolValue)
    {
        if (bool.TryParse(boolValue, out bool result))
        {
            return result;
        }
        return null;
    }

    protected static List<string> ParseCategoriesOrEmpty(string categoriesValue)
    {
        ArgumentNullException.ThrowIfNull(categoriesValue);

        return categoriesValue.Split(IItemMapper.CategoriesSeparator).ToList();
    }

    protected static Guid? ParseGuidOrDefault(string guidValue)
    {
        if (Guid.TryParse(guidValue, out Guid result))
        {
            return result;
        }
        return null;
    }

    protected static string AdjustPriceIfRequired(string rawPrice)
    {
        ArgumentNullException.ThrowIfNull(rawPrice);

        return rawPrice.Replace(',', '.');
    }

    protected static string AdjustNameIfRequired(string itemName)
    {
        ArgumentNullException.ThrowIfNull(itemName);

        return itemName.Replace("'", " ");
    }

    // TODO: move to extension method
    protected static TValue? TryGetValueOrDefault<TValue>(
        Dictionary<ItemFields, string> dict,
        ItemFields field,
        Func<string, TValue?> affect
    )
    {
        ArgumentNullException.ThrowIfNull(affect);
        ArgumentNullException.ThrowIfNull(dict);

        return dict.ContainsKey(field) ? affect(dict[field]) : default(TValue);
    }

    public ItemModel MapItemFields(Dictionary<ItemFields, object> fields)
    {
        throw new NotImplementedException();
    }
}
