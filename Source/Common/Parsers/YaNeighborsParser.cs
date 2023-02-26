using System.Text;
using System.Text.RegularExpressions;
using GoodsTracker.DataCollector.Common.Mappers.Abstractions;
using GoodsTracker.DataCollector.Common.Parsers.Abstractions;
using HtmlAgilityPack;
using Microsoft.Extensions.Logging;
using GoodsTracker.DataCollector.Models.Constants;

namespace GoodsTracker.DataCollector.Common.Parsers;
public sealed class YaNeighborsParser : IItemParser
{
    // TODO: better names for regular expressions (check .NET guide)
    public readonly static Regex _itemTitleRegex = new Regex(
        @"^(.*)(\s(\d+\.?\d*\s?)(\w*)?)$", RegexOptions.Compiled
    );
    public readonly static Regex _itemWeightRegex = new Regex(
        @"^(\d*)\s?(\w+)$", RegexOptions.Compiled
    );
    public readonly static Regex _itemPriceRegex = new Regex(
        @"^([0-9]*[,0-9]*).*$"
    );
    private ILogger<YaNeighborsParser> _logger;

    public YaNeighborsParser(ILogger<YaNeighborsParser> logger)
    {
        _logger = logger;
    }

    public Dictionary<string, string> ParseItem(string rawItem)
    {
        throw new NotImplementedException();
    }

    // TODO: replace hard-coded html classes with some structure
    //TODO: divide into something
    public Dictionary<ItemFields, string> ParseItem(HtmlDocument itemPage)
    {
        var fields = new Dictionary<ItemFields, string>();

        // TODO: exception handling
        var rawTitle = SelectFieldWithNodePath(
                itemPage,
                "//h2[@class='UiKitText_root UiKitText_Title4Loose UiKitText_Bold UiKitText_Text']"
            )
            ??
            SelectFieldWithNodePath(
                itemPage,
                "//h2[@class='UiKitText_root UiKitText_Title2Loose UiKitText_Bold UiKitText_Text']"
            )
            ??
            throw new InvalidDataException("Parser couldn't recognize page structure (title)");
        var itemTitleMatch = _itemTitleRegex.Match(rawTitle);
        if (itemTitleMatch.Success)
        {
            fields.Add(
                ItemFields.Name1,
                itemTitleMatch.Groups[1].Value
            );
            fields.Add(
                ItemFields.Weight,
                itemTitleMatch.Groups[3].Value
            );
            fields.Add(
                ItemFields.WeightUnit,
                itemTitleMatch.Groups[4].Value
            );
        }
        else
        {
            fields.Add(
                ItemFields.Name1,
                rawTitle
            );

            var fullWeight =
                SelectFieldWithNodePath(
                    itemPage,
                    "//div[@class='UiKitProductFullCard_weight']"
                );
            if (fullWeight is not null)
            {
                var fullWeightMatch = _itemWeightRegex.Match(fullWeight);
                if (fullWeightMatch.Length == fullWeight.Length)
                {
                    fields.Add(
                    ItemFields.Weight,
                    fullWeightMatch.Groups[1].Value
                    );
                    fields.Add(
                        ItemFields.WeightUnit,
                        fullWeightMatch.Groups[2].Value
                    );
                }
            }
        }

        var itemNode =
            itemPage
                .DocumentNode
                .SelectSingleNode("//span[@class='UiKitCorePrice_price UiKitCorePrice_xl UiKitCorePrice_bold UiKitCorePrice_newPrice']");
        if (itemNode is null)
        {
            fields.Add(
                ItemFields.Price,
                SelectFieldWithPattern(
                    itemPage,
                    "//span[@class='UiKitCorePrice_price UiKitCorePrice_xl UiKitCorePrice_bold']",
                    _itemPriceRegex
                    )
                    ??
                    throw new InvalidDataException("Parser couldn't recognize page structure (price)"));
        }
        else
        {
            fields.Add(
                ItemFields.Discount,
                _itemPriceRegex.Match(itemNode.InnerText).Groups[1].Value
            );
            fields.Add(
                ItemFields.Price,
                SelectFieldWithPattern(
                    itemPage,
                    "//span[@class='UiKitCorePrice_price UiKitCorePrice_m UiKitCorePrice_medium UiKitCorePrice_oldPrice']",
                    _itemPriceRegex
                )
                ??
                throw new InvalidDataException("Parser couldn't recognize page structure")
            );
        }

        var descriptionNodes =
            itemPage
                .DocumentNode
                .SelectNodes("//h3[@class='UiKitProductCardDescriptions_descriptionTitle']");
        if (descriptionNodes is not null && descriptionNodes.Count > 1)
        {
            if (descriptionNodes.Count == 2)
            {
                fields.Add(ItemFields.Compound, descriptionNodes[0].NextSibling?.InnerText ?? "");
            }
            else if (descriptionNodes.Count == 3)
            {
                fields.Add(ItemFields.Producer, descriptionNodes[0].NextSibling?.InnerText ?? "");
                fields.Add(ItemFields.Country, descriptionNodes[1].NextSibling?.InnerText ?? "");
                fields.Add(ItemFields.Compound, descriptionNodes[2].NextSibling?.InnerText ?? "");
            }
        }

        var image =
            itemPage
                .DocumentNode
                .SelectSingleNode(
                    "//div[@class='SmartImage_root UiKitProductFullCard_picture SmartImage_hideCover']/img"
                );
        if (image != null)
        {
            fields.Add(ItemFields.ImageLink, image.GetAttributeValue("src", null));
        }

        var categorySequence =
            itemPage
                .DocumentNode
                .SelectNodes(
                    "//ul[@class='UiKitBreadcrumbs_root UiKitBreadcrumbs_l']/li/a/span"
                );
        if (categorySequence is not null)
        {
            var categories = new StringBuilder("");
            categories.Append(categorySequence[^1].InnerText + IItemMapper.CATEGORIES_SEPARATOR);
            categories.Append(categorySequence[^2].InnerText);
            fields.Add(
                ItemFields.Categories,
                categories.ToString()
            );
        }

        return fields;
    }

    private string? SelectFieldWithPattern(HtmlDocument page, string nodePath, Regex pattern)
    {
        var innerText = SelectFieldWithNodePath(page, nodePath);
        if (innerText is null || !pattern.IsMatch(innerText))
        {
            return null;
        }
        var textMatch = pattern.Match(innerText);
        return textMatch.Groups[1].Value;
    }

    private string? SelectFieldWithNodePath(HtmlDocument page, string nodePath)
    {
        return page
                .DocumentNode
                .SelectSingleNode(nodePath)
                ?.InnerText;
    }

    public List<Dictionary<string, string>> ParseItems(string rawItems)
    {
        throw new NotImplementedException();
    }
}