namespace GoodsTracker.DataCollector.Common.Configuration;

// TODO: provide configuration with list of categories to be exclude from scraping
public sealed class ScraperConfig
{
    public string Name { get; init; } = string.Empty;
    public string ParserName { get; init; } = string.Empty;
    public string ShopName { get; init; } = string.Empty;
    public int ShopId { get; init; }
    public Dictionary<string, string> Headers { get; init; } = new();
    public string ShopUrl { get; init; } = string.Empty;
    public Uri ShopUri => new(this.ShopUrl);
    public int BrowserWidth { get; init; } = 1024;
    public int BrowserHeight { get; init; } = 1024;
    public string ShopStartRecource { get; init; } = string.Empty;
    public string? ShopApi { get; init; }
    public Dictionary<string, string> HTMLSections { get; init; } = new();
    public Dictionary<string, string> Metadata { get; init; } = new();
    public string ItemMapper { get; init; } = string.Empty;
}
