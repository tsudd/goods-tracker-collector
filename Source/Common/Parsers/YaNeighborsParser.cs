using System.Text.RegularExpressions;

using GoodsTracker.DataCollector.Common.Mappers.Abstractions;
using GoodsTracker.DataCollector.Common.Parsers.Abstractions;
using GoodsTracker.DataCollector.Models.Constants;

using System.Text.Json;

using GoodsTracker.DataCollector.Common.Parsers.Extensions;

using FluentResults;

namespace GoodsTracker.DataCollector.Common.Parsers;

internal sealed class YaNeighborsParser : ItemParser
{
    private static readonly Regex itemTitleRegex = new(@"^(.*)(\s(\d+\.?\d*\s?)(\w*)?)$", RegexOptions.Compiled);
    private static readonly Regex itemWeightRegex = new(@"^(\d*)\s?(\w+)$", RegexOptions.Compiled);
    private const int maxLengthOfSecondTitle = 70;

    public override Result<Dictionary<ItemFields, string>> ParseItem(string rawItem)
    {
        Result<JsonDocument> getDocumentResult = ParseJsonDocument(rawItem);

        if (getDocumentResult.IsFailed)
        {
            return getDocumentResult.ToResult();
        }

        using JsonDocument? productDoc = getDocumentResult.Value;
        JsonElement root = productDoc.RootElement;
        var fields = new Dictionary<ItemFields, string>();
        JsonElement itemNode = root.GetProperty("menu_item");

        // TODO: create general method for all try get prop values
        if (itemNode.TryGetPropertyValue("name", out string rawTitle))
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

                if (itemNode.TryGetPropertyValue("weight", out string fullWeight))
                {
                    var fullWeightMatch = itemWeightRegex.Match(fullWeight);

                    if (fullWeightMatch.Length == fullWeight.Length)
                    {
                        fields.AddItemWeight(
                            fullWeightMatch.Groups[1]
                                           .Value);

                        fields.AddItemWeightUnit(
                            fullWeightMatch.Groups[2]
                                           .Value);
                    }
                }
            }
        }
        else
        {
            return Result.Fail("couldn't parse item base info: 'name'");
        }

        if (itemNode.TryGetPropertyValue("decimalPrice", out string price))
        {
            fields.AddItemPrice(price);
        }
        else
        {
            return Result.Fail("couldn't parse item base info: 'price'");
        }

        if (itemNode.TryGetPropertyValue("decimalPromoPrice", out string cutPrice))
        {
            fields.AddItemCutPrice(cutPrice);
        }

        if (itemNode.TryGetPropertyValue("adult", static o => o.GetBoolean(), out bool isAdult))
        {
            fields.AddItemAdult(isAdult.ToString());
        }

        if (itemNode.TryGetPropertyValue("id", static o => o.GetInt64(), out long vendorCode))
        {
            // TODO: try dictionary with objects, instead of string
            fields.AddItemVendorCode(vendorCode.ToString());
        }

        if (itemNode.TryGetPropertyValue("public_id", out string productId))
        {
            fields.AddItemGuid(productId);
        }

        if (itemNode.TryGetPropertyValue("description", out string name2) && name2.Length <= maxLengthOfSecondTitle)
        {
            fields.AddItemName2(name2);
        }

        if (itemNode.TryGetProperty("picture", out JsonElement pictureElement))
        {
            if (pictureElement.TryGetPropertyValue("url", out string pictureLink))
            {
                fields.AddItemImageLink(pictureLink.Replace("{w}x{h}", "500x500"));
            }
        }

        if (root.TryGetProperty("detailed_data", out JsonElement detailsElement))
        {
            TryReadItemDetails(detailsElement, ref fields);
        }

        if (root.TryGetProperty("categories", out JsonElement categoriesElement))
        {
            TryReadItemCategories(categoriesElement, ref fields);
        }

        return Result.Ok(fields);
    }

    private static void TryReadItemDetails(JsonElement detailsElement, ref Dictionary<ItemFields, string> fieldsDict)
    {
        foreach (JsonElement detail in detailsElement.EnumerateArray())
        {
            if (detail.TryGetProperty("type", out JsonElement type))
            {
                if (type.GetString() != "description")
                {
                    continue;
                }
            }

            if (!detail.TryGetProperty("payload", out JsonElement payload))
            {
                continue;
            }

            if (!payload.TryGetProperty("descriptions", out JsonElement descriptions))
            {
                continue;
            }

            foreach (JsonElement description in descriptions.EnumerateArray())
            {
                if (!description.TryGetProperty("title", out JsonElement descriptionElement))
                {
                    continue;
                }

                if (descriptionElement.GetString() == "Manufacturer")
                {
                    if (description.TryGetPropertyValue("text", out string value))
                    {
                        fieldsDict.AddItemProducer(value);
                    }
                }
                else if (descriptionElement.GetString() == "Country")
                {
                    if (description.TryGetPropertyValue("text", out string value))
                    {
                        fieldsDict.AddItemCountry(value);
                    }
                }
                else if (descriptionElement.GetString() == "Description" ||
                         descriptionElement.GetString() == "Описание")
                {
                    if (description.TryGetPropertyValue("text", out string value))
                    {
                        fieldsDict.AddItemCompound(value);
                    }
                }
            }
        }
    }

    private static void TryReadItemCategories(
        JsonElement categoriesElement, ref Dictionary<ItemFields, string> fieldsDict)
    {
        var categories = new List<string>();

        foreach (JsonElement category in categoriesElement.EnumerateArray())
        {
            if (category.TryGetPropertyValue("name", out string categoryName))
            {
                categories.Add(categoryName);
            }
        }

        fieldsDict.AddItemCategories(string.Join(IItemMapper.CategoriesSeparator, categories.Distinct()));
    }
}
