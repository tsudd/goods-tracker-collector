using System.Text.RegularExpressions;
using GoodsTracker.DataCollector.Common.Mappers.Abstractions;
using GoodsTracker.DataCollector.Common.Parsers.Abstractions;
using Microsoft.Extensions.Logging;
using GoodsTracker.DataCollector.Models.Constants;
using System.Text.Json;
using GoodsTracker.DataCollector.Common.Parsers.Exceptions;
using GoodsTracker.DataCollector.Common.Parsers.Extensions;

namespace GoodsTracker.DataCollector.Common.Parsers;

public sealed class YaNeighborsParser : IItemParser
{
    private readonly static Regex itemTitleRegex = new Regex(
        @"^(.*)(\s(\d+\.?\d*\s?)(\w*)?)$",
        RegexOptions.Compiled
    );
    private readonly static Regex itemWeightRegex = new Regex(
        @"^(\d*)\s?(\w+)$",
        RegexOptions.Compiled
    );
    private const int maxLengthOfSecondTitle = 70;
    private ILogger<YaNeighborsParser> _logger;

    public YaNeighborsParser(ILogger<YaNeighborsParser> logger)
    {
        _logger = logger;
    }

    // TODO: get rid of cyclomatic complexity warning
    public Dictionary<ItemFields, string> ParseItem(string rawItem)
    {
        using var productDocument = TryParseJsonDocument(rawItem);
        var root = productDocument.RootElement;

        var fields = new Dictionary<ItemFields, string>();

        var itemNode = root.GetProperty("menu_item");

        // TODO: create general method for all try get prop values
        if (itemNode.TryGetPropertyValue("name", out string rawTitle))
        {
            var itemTitleMatch = itemTitleRegex.Match(rawTitle);
            if (itemTitleMatch.Success)
            {
                fields.AddItemName1(itemTitleMatch.Groups[1].Value);
                fields.AddItemWeight(itemTitleMatch.Groups[3].Value);
                fields.AddItemWeightUnit(itemTitleMatch.Groups[4].Value);
            }
            else
            {
                fields.AddItemName1(rawTitle);

                if (itemNode.TryGetPropertyValue("weight", out string fullWeight))
                {
                    var fullWeightMatch = itemWeightRegex.Match(fullWeight);
                    if (fullWeightMatch.Length == fullWeight.Length)
                    {
                        fields.AddItemWeight(fullWeightMatch.Groups[1].Value);
                        fields.AddItemWeightUnit(fullWeightMatch.Groups[2].Value);
                    }
                }
            }
        }
        else
        {
            throw new InvalidItemFormatException("couldn't parse item base info: 'name'");
        }

        if (itemNode.TryGetPropertyValue("decimalPrice", out string price))
        {
            fields.AddItemPrice(price);
        }
        else
        {
            throw new InvalidItemFormatException("couldn't parse item base info: 'price'");
        }

        if (itemNode.TryGetPropertyValue("decimalPromoPrice", out string cutPrice))
        {
            fields.AddItemCutPrice(cutPrice);
        }

        if (itemNode.TryGetPropertyValue("adult", static o => o.GetBoolean(), out bool isAdult))
        {
            fields.AddItemAdult(isAdult.ToString());
        }

        if (itemNode.TryGetPropertyValue("id", static o => o.GetInt32(), out int vendorCode))
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

        return fields;
    }

    private JsonDocument TryParseJsonDocument(string rawJson)
    {
        try
        {
            return JsonDocument.Parse(rawJson);
        }
        catch (JsonException)
        {
            throw new InvalidItemFormatException("couldn't parse item's JSON");
        }
    }

    private void TryReadItemDetails(JsonElement detailsElement, ref Dictionary<ItemFields, string> fieldsDict)
    {
        foreach (var detail in detailsElement.EnumerateArray())
        {
            if (detail.TryGetProperty("type", out JsonElement type))
            {
                if (type.GetString() != "description")
                {
                    continue;
                }
            }
            if (detail.TryGetProperty("payload", out JsonElement payload))
            {
                if (payload.TryGetProperty("descriptions", out JsonElement descriptions))
                {
                    foreach (var description in descriptions.EnumerateArray())
                    {
                        if (description.TryGetProperty("title", out JsonElement descriptionElement))
                        {
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
                            else if (descriptionElement.GetString() == "Description")
                            {
                                if (description.TryGetPropertyValue("text", out string value))
                                {
                                    fieldsDict.AddItemCompound(value);
                                }
                            }
                        }
                    }
                }
            }
        }
    }

    private void TryReadItemCategories(JsonElement categoriesElement, ref Dictionary<ItemFields, string> fieldsDict)
    {
        var categories = new List<string>();
        foreach (var category in categoriesElement.EnumerateArray())
        {
            if (category.TryGetPropertyValue("name", out string categoryName))
            {
                categories.Add(categoryName);
            }
        }
        fieldsDict.AddItemCategories(string.Join(IItemMapper.CategoriesSeparator, categories.Distinct()));
    }
}
