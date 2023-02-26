namespace GoodsTracker.DataCollector.Common.Configuration;

public sealed class TrackerConfig
{
    public string TrackerName { get; init; } = String.Empty;
    public IEnumerable<ScraperConfig> ScrapersConfigurations { get; init; } = new List<ScraperConfig>();
}