using HtmlAgilityPack;
using GoodsTracker.DataCollector.Models.Constants;

namespace GoodsTracker.DataCollector.Common.Parsers.Abstractions;

public interface IItemParser
{
    List<Dictionary<string, string>> ParseItems(string rawItems);
    Dictionary<string, string> ParseItem(string rawItem);
    Dictionary<ItemFields, string> ParseItem(HtmlDocument itemPage);
}