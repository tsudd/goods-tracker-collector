using FluentResults;

namespace GoodsTracker.DataCollector.Common.Adapters.Results;

internal sealed class FailedResult : IError
{
    public FailedResult(string message)
    {
        Message = message;
    }

    public string Message { get; }

    public Dictionary<string, object> Metadata => new Dictionary<string, object>();

    public List<IError> Reasons => new List<IError>();
}
