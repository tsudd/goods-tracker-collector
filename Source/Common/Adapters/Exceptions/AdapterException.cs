namespace GoodsTracker.DataCollector.Common.Adapters.Exceptions;

[Serializable]
public class AdapterException : Exception
{
    public string ShopId { get; private set; } = string.Empty;

    public AdapterException()
    {
    }

    public AdapterException(string message)
        : base(message)
    {
    }

    public AdapterException(string message, string shopId)
        : base(message)
    {
        this.ShopId = shopId;
    }

    public AdapterException(string message, Exception inner)
        : base(message, inner)
    {
    }

    protected AdapterException(
        System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context)
        : base(info, context)
    {
    }
}
