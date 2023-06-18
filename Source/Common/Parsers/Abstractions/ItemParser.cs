namespace GoodsTracker.DataCollector.Common.Parsers.Abstractions;

using System.Text.Json;

using FluentResults;

using GoodsTracker.DataCollector.Models.Constants;

internal abstract class ItemParser : IItemParser
{
    protected static Result<JsonDocument> ParseJsonDocument(string rawJson)
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

    public abstract Result<Dictionary<ItemFields, string>> ParseItem(string rawItem);
}
