namespace GoodsTracker.DataCollector.Collector.Options;

public sealed class DataCollectorOptions
{
    public IEnumerable<int> ShopIds { get; set; } = Array.Empty<int>();
}
