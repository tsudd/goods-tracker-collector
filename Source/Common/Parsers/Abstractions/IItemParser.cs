using FluentResults;

using GoodsTracker.DataCollector.Models.Constants;

namespace GoodsTracker.DataCollector.Common.Parsers.Abstractions;

public interface IItemParser
{
    Result<Dictionary<ItemFields, string>> ParseItem(string rawItem);
}
