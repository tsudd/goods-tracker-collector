using FluentResults;

namespace GoodsTracker.DataCollector.Common.Adapters.Results;

internal sealed class FailedResult : IError
{
    public FailedResult(string message)
    {
        this.Message = message;
    }

    public string Message { get; }
    public Dictionary<string, object> Metadata => new();
    public List<IError> Reasons => new();
}
