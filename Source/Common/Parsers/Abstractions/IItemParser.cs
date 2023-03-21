using GoodsTracker.DataCollector.Models.Constants;

namespace GoodsTracker.DataCollector.Common.Parsers.Abstractions;

public interface IItemParser
{
    Dictionary<ItemFields, string> ParseItem(string rawItem);
}
