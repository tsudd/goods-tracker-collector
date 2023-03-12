namespace GoodsTracker.DataCollector.Collector.Options;

public sealed class DataCollectorOptions
{
    public IEnumerable<string> ShopIds { get; set; } = Array.Empty<string>();
}
