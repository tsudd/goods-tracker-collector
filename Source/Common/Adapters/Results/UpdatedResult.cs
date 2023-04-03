using FluentResults;

namespace GoodsTracker.DataCollector.Common.Adapters.Results;

internal sealed class UpdatedResult : ISuccess
{
    public string Message => "Entity has been updated successfully.";

    public Dictionary<string, object> Metadata => new Dictionary<string, object>();
}
