using System.Text;
using System.Text.RegularExpressions;
using GoodsTracker.DataCollector.Common.Mappers.Abstractions;
using GoodsTracker.DataCollector.Common.Parsers.Abstractions;
using HtmlAgilityPack;
using Microsoft.Extensions.Logging;
using GoodsTracker.DataCollector.Models.Constants;
using System.Text.Json;
using GoodsTracker.DataCollector.Common.Parsers.Exceptions;

namespace GoodsTracker.DataCollector.Common.Parsers;

public sealed class YaNeighborsParser : IItemParser
{
    // TODO: better names for regular expressions (check .NET guide)
    private readonly static Regex itemTitleRegex = new Regex(
        @"^(.*)(\s(\d+\.?\d*\s?)(\w*)?)$",
        RegexOptions.Compiled
    );
    private readonly static Regex itemWeightRegex = new Regex(
        @"^(\d*)\s?(\w+)$",
        RegexOptions.Compiled
    );
    private readonly static Regex itemPriceRegex = new Regex(@"^([0-9]*[,0-9]*).*$");
    private const int maxLengthOfSecondTitle = 70;

    private ILogger<YaNeighborsParser> _logger;

    public YaNeighborsParser(ILogger<YaNeighborsParser> logger)
    {
        _logger = logger;
    }

    // TODO: get rid of cyclomatic complexity warning
    public Dictionary<ItemFields, string> ParseItem(string rawItem)
    {
        using var productDocument = JsonDocument.Parse(rawItem);
        var root = productDocument.RootElement;

        var fields = new Dictionary<ItemFields, string>();

        var itemNode = root.GetProperty("menu_item");

        // TODO: create general method for all try get prop values
        if (itemNode.TryGetPropertyValue("name", out string rawTitle))
        {
            var itemTitleMatch = itemTitleRegex.Match(rawTitle);
            if (itemTitleMatch.Success)
            {
                fields.Add(ItemFields.Name1, itemTitleMatch.Groups[1].Value);
                fields.Add(ItemFields.Weight, itemTitleMatch.Groups[3].Value);
                fields.Add(ItemFields.WeightUnit, itemTitleMatch.Groups[4].Value);
            }
            else
            {
                fields.Add(ItemFields.Name1, rawTitle);

                if (itemNode.TryGetPropertyValue("weight", out string fullWeight))
                {
                    var fullWeightMatch = itemWeightRegex.Match(fullWeight);
                    if (fullWeightMatch.Length == fullWeight.Length)
                    {
                        fields.Add(ItemFields.Weight, fullWeightMatch.Groups[1].Value);
                        fields.Add(ItemFields.WeightUnit, fullWeightMatch.Groups[2].Value);
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
            fields.Add(ItemFields.Price, price);
        }
        else
        {
            throw new InvalidItemFormatException("couldn't parse item base info: 'price'");
        }

        if (itemNode.TryGetPropertyValue("decimalPromoPrice", out string cutPrice))
        {
            fields.Add(ItemFields.Discount, cutPrice);
        }

        if (itemNode.TryGetPropertyValue("adult", static o => o.GetBoolean(), out bool isAdult))
        {
            fields.Add(ItemFields.Adult, isAdult.ToString());
        }

        if (itemNode.TryGetPropertyValue("id", static o => o.GetInt32(), out int vendorCode))
        {
            // TODO: try dictionary with objects, instead of string
            fields.Add(ItemFields.VendorCode, vendorCode.ToString());
        }

        if (itemNode.TryGetPropertyValue("public_id", out string productId))
        {
            fields.Add(ItemFields.Guid, productId);
        }

        if (itemNode.TryGetPropertyValue("description", out string name2) && name2.Length <= maxLengthOfSecondTitle)
        {
            fields.Add(ItemFields.Name2, name2);
        }

        if (itemNode.TryGetProperty("picture", out JsonElement pictureElement))
        {
            if (pictureElement.TryGetPropertyValue("url", out string pictureLink))
            {
                fields.Add(ItemFields.ImageLink, pictureLink);
            }
        }

        if (root.TryGetProperty("detailed_data", out JsonElement detailsElement))
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
                                        fields.Add(ItemFields.Producer, value);
                                    }
                                }
                                else if (descriptionElement.GetString() == "Country")
                                {
                                    if (description.TryGetPropertyValue("text", out string value))
                                    {
                                        fields.Add(ItemFields.Country, value);
                                    }
                                }
                                else if (descriptionElement.GetString() == "Description")
                                {
                                    if (description.TryGetPropertyValue("text", out string value))
                                    {
                                        fields.Add(ItemFields.Compound, value);
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        if (itemNode.TryGetProperty("categories", out JsonElement categoriesElement))
        {
            var categories = new List<string>();
            foreach (var category in categoriesElement.EnumerateArray())
            {
                if (category.TryGetProperty("name", out JsonElement categoryName))
                {
                    categories.Add(categoryName.ToString());
                }
            }
            fields.Add(
                ItemFields.Categories,
                string.Join(IItemMapper.CategoriesSeparator, categories.Distinct())
            );
        }

        return fields;
    }

    public List<Dictionary<string, string>> ParseItems(string rawItems)
    {
        throw new NotImplementedException();
    }

    public Dictionary<ItemFields, string> ParseItem(HtmlDocument itemPage)
    {
        throw new NotImplementedException();
    }
}
