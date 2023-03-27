namespace GoodsTracker.DataCollector.Common.Configuration;

// TODO: provide configuration with list of categories to be exclude from scraping
public sealed class ScraperConfig
{
    public string Name { get; init; } = String.Empty;
    public string ParserName { get; init; } = String.Empty;
    public string ShopName { get; init; } = String.Empty;
    public int ShopID { get; init; }
    public Dictionary<string, string> Headers { get; init; } = new Dictionary<string, string>();
    public string ShopUrl { get; init; } = String.Empty;
    public string ShopStartRecource { get; init; } = String.Empty;
    public string? ShopApi { get; init; }
    public Dictionary<string, string> HTMLSections { get; init; } = new Dictionary<string, string>();
    public Dictionary<string, string> Metadata { get; init; } =
        new Dictionary<string, string>();
    public string ItemMapper { get; init; } = String.Empty;
}
