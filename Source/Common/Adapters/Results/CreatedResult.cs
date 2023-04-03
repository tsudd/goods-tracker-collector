using FluentResults;

namespace GoodsTracker.DataCollector.Common.Adapters.Results;

internal sealed class CreatedResult : ISuccess
{
    public string Message => "Entity has been created successfully.";

    public Dictionary<string, object> Metadata => new Dictionary<string, object>();
}
