namespace GoodsTracker.DataCollector.Common.Parsers;

using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;

using FluentResults;

using GoodsTracker.DataCollector.Common.Mappers.Abstractions;
using GoodsTracker.DataCollector.Common.Parsers.Abstractions;
using GoodsTracker.DataCollector.Common.Parsers.Extensions;
using GoodsTracker.DataCollector.Models.Constants;

internal sealed class EvrooptParser : IItemParser
{
    private static readonly Regex itemTitleRegex = new(
        @"^(.*?)(?:,\s)?\s?((\d+\.?\d*\s?)(\w*)?)\.?\s*$", RegexOptions.Compiled);

    private static readonly IFormatProvider formatProvider = CultureInfo.InvariantCulture;

    [SuppressMessage("ReSharper", "StringLiteralTypo")]
    public Result<Dictionary<ItemFields, string>> ParseItem(string rawItem)
    {
        Result<JsonDocument> getDocumentResult = TryParseJsonDocument(rawItem);

        if (getDocumentResult.IsFailed)
        {
            return getDocumentResult.ToResult();
        }

        using JsonDocument productDoc = getDocumentResult.Value;
        JsonElement root = productDoc.RootElement;
        var fields = new Dictionary<ItemFields, string>();

        JsonElement itemNode = root.GetProperty("pageProps")
                                   .GetProperty("product");

        Result getNameAndResult = AddItemNameAndWeightInfo(itemNode, fields);

        if (getNameAndResult.IsFailed)
        {
            return getNameAndResult.ToResult<Dictionary<ItemFields, string>>();
        }

        Result getPriceInfo = AddPriceInfo(itemNode, fields);

        if (getPriceInfo.IsFailed)
        {
            return getPriceInfo.ToResult<Dictionary<ItemFields, string>>();
        }

        if (TryGetItemVendorCode(itemNode, out string vendorCode))
        {
            fields.AddItemVendorCode(vendorCode);
        }

        fields.AddItemAdult("false");

        if (TryGetItemCompound(itemNode, out string descriptionElement))
        {
            fields.AddItemCompound(descriptionElement);
        }

        GetCharacteristicsIfAvailable(itemNode, fields);
        GetProducerInfoIfAvailable(itemNode, fields);

        if (TryGetCategories(itemNode, out string categories))
        {
            fields.AddItemCategories(categories);
        }

        JsonElement.ArrayEnumerator imageNodes = root.GetProperty("pageProps")
                                                     .GetProperty("images")
                                                     .EnumerateArray();

        if (imageNodes.Any() &&
            imageNodes.First()
                      .TryGetPropertyValue("url", out string imageUrl))
        {
            fields.AddItemImageLink(imageUrl);
        }

        return Result.Ok(fields);
    }

    // TODO: move to abstract class
    private static Result<JsonDocument> TryParseJsonDocument(string rawJson)
    {
        try
        {
            return Result.Ok(JsonDocument.Parse(rawJson));
        }
        catch (JsonException ex)
        {
            return Result.Fail(new Error("couldn't parse item's JSON").CausedBy(ex));
        }
    }

    private static Result AddItemNameAndWeightInfo(JsonElement itemNode, Dictionary<ItemFields, string> fields)
    {
        if (itemNode.TryGetPropertyValue("ProductName", out string rawTitle))
        {
            Match itemTitleMatch = itemTitleRegex.Match(rawTitle);

            if (itemTitleMatch.Success)
            {
                fields.AddItemName1(
                    itemTitleMatch.Groups[1]
                                  .Value);

                fields.AddItemWeight(
                    itemTitleMatch.Groups[3]
                                  .Value);

                fields.AddItemWeightUnit(
                    itemTitleMatch.Groups[4]
                                  .Value);
            }
            else
            {
                fields.AddItemName1(rawTitle);

                if (itemNode.TryGetPropertyValue("MeasureValue", static o => o.GetDouble(), out double weight))
                {
                    fields.AddItemWeight(weight.ToString(formatProvider));
                }

                if (itemNode.TryGetPropertyValue("NetMeasure", out string weightUnit))
                {
                    fields.AddItemWeightUnit(weightUnit);
                }
            }
        }
        else
        {
            return Result.Fail("couldn't parse item base info: 'name'");
        }

        return Result.Ok();
    }

    private static Result AddPriceInfo(JsonElement itemNode, Dictionary<ItemFields, string> fields)
    {
        if (itemNode.TryGetProperty("Price", out JsonElement priceList))
        {
            JsonElement priceElement = priceList.EnumerateArray()
                                                .FirstOrDefault();

            if (priceElement.TryGetProperty("Discount", out JsonElement discount) &&
                discount.ValueKind != JsonValueKind.Null)
            {
                if (priceElement.TryGetPropertyValue("PriceOld", static o => o.GetDouble(), out double priceOld))
                {
                    fields.AddItemPrice(priceOld.ToString(formatProvider));
                }
                else
                {
                    return Result.Fail("couldn't parse item's price with the discount");
                }

                if (priceElement.TryGetPropertyValue("PriceRed", static o => o.GetDouble(), out double redPrice))
                {
                    fields.AddItemCutPrice(redPrice.ToString(formatProvider));
                }
            }
            else if (priceElement.TryGetPropertyValue("Price", static o => o.GetDouble(), out double price))
            {
                fields.AddItemPrice(price.ToString(formatProvider));
            }
            else
            {
                return Result.Fail("couldn't parse item's price");
            }
        }
        else
        {
            Result.Fail("couldn't parse item's price");
        }

        return Result.Ok();
    }

    private static bool TryGetItemVendorCode(JsonElement itemNode, out string vendorCode)
    {
        if (itemNode.TryGetPropertyValue("ProductId", static o => o.GetInt32(), out int productId))
        {
            vendorCode = productId.ToString(formatProvider);

            return true;
        }

        vendorCode = string.Empty;

        return false;
    }

    private static bool TryGetItemCompound(JsonElement itemNode, out string compound)
    {
        if (itemNode.TryGetProperty("Description", out JsonElement descriptionElement))
        {
            var descriptions = new List<string>();

            foreach (JsonElement description in descriptionElement.EnumerateArray())
            {
                descriptions.AddRange(
                    description.EnumerateObject()
                               .Select(static property => property.Value.GetString()!));
            }

            compound = string.Join(" ", descriptions);

            return true;
        }

        compound = string.Empty;

        return false;
    }

    private static void GetCharacteristicsIfAvailable(JsonElement itemNode, Dictionary<ItemFields, string> fields)
    {
        const string proteinName = "Белки";
        const string fatName = "Жиры";
        const string carboName = "Углеводы";

        if (!itemNode.TryGetProperty("CustomPropertyGroup", out JsonElement customProperties))
        {
            return;
        }

        foreach (JsonElement property in customProperties.EnumerateArray())
        {
            if (property.TryGetPropertyValue("PropertyValue", out string propertyValue))
            {
                string? propertyName = property.GetProperty("PropertyName")
                                               .GetString();

                switch (propertyName)
                {
                    case proteinName:
                        fields.AddItemProtein(propertyValue);

                        break;

                    case fatName:
                        fields.AddItemFat(propertyValue);

                        break;

                    case carboName:
                        fields.AddItemCarbo(propertyValue);

                        break;
                }
            }
        }
    }

    private static void GetProducerInfoIfAvailable(JsonElement itemNode, Dictionary<ItemFields, string> fields)
    {
        if (!itemNode.TryGetProperty("LegalInformation", out JsonElement legalInfo))
        {
            return;
        }

        JsonElement producerInfo = legalInfo.EnumerateArray()
                                            .FirstOrDefault();

        if (producerInfo.TryGetPropertyValue("ManufacturerName", out string producerName))
        {
            fields.AddItemProducer(producerName);
        }

        if (producerInfo.TryGetPropertyValue("CountryOfManufacture", out string countryName))
        {
            fields.AddItemCountry(countryName);
        }
    }

    private static bool TryGetCategories(JsonElement itemNode, out string categories)
    {
        if (itemNode.TryGetProperty("BreadCrumbs", out JsonElement categoriesRoot))
        {
            categories = GetCategories(
                categoriesRoot.EnumerateArray()
                              .FirstOrDefault());

            return true;
        }

        categories = string.Empty;

        return false;
    }

    private static string GetCategories(JsonElement categoriesRoot)
    {
        var category = "";

        if (categoriesRoot.TryGetPropertyValue("CategoryListName", out string categoryName))
        {
            category = categoryName;
        }

        if (!categoriesRoot.TryGetProperty("Child", out JsonElement parentCategory))
        {
            return category;
        }

        JsonElement childCategory = parentCategory.EnumerateArray()
                                                  .FirstOrDefault();

        return string.Join(IItemMapper.CategoriesSeparator, category, GetCategories(childCategory));
    }
}
