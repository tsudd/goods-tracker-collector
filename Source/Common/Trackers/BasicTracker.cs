using GoodsTracker.DataCollector.Common.Configuration;
using GoodsTracker.DataCollector.Common.Scrapers.Abstractions;
using GoodsTracker.DataCollector.Common.Trackers.Abstractions;

using Microsoft.Extensions.Logging;

using GoodsTracker.DataCollector.Models;
using GoodsTracker.DataCollector.Common.Factories.Abstractions;

using Microsoft.Extensions.Options;

namespace GoodsTracker.DataCollector.Common.Trackers;

public sealed class BasicTracker : IItemTracker
{
    private readonly List<IScraper> scrapers = new();
    private readonly Dictionary<int, IList<ItemModel>> shopItems;
    private readonly ILogger<BasicTracker> logger;
    private readonly TrackerConfig config;

    public BasicTracker(IOptions<TrackerConfig> config, ILogger<BasicTracker> logger, IDataCollectorFactory factory)
    {
        ArgumentNullException.ThrowIfNull(config);
        ArgumentNullException.ThrowIfNull(factory);
        this.config = config.Value;
        this.logger = logger;
        this.shopItems = new Dictionary<int, IList<ItemModel>>();

        foreach (ScraperConfig conf in this.config.ScrapersConfigurations)
        {
            this.scrapers.Add(factory.CreateScraper(conf, factory.CreateParser(conf.ParserName)));
        }
    }

    public void ClearData()
    {
        foreach (KeyValuePair<int, IList<ItemModel>> fetchedInfo in this.shopItems)
        {
            fetchedInfo.Value.Clear();
        }
    }

    public bool IsThereAnythingToSave()
    {
        return this.shopItems.Any(static valuePair => valuePair.Value.Any());
    }

    public async Task FetchItemsAsync()
    {
        foreach (IScraper scraper in this.scrapers)
        {
            ScraperConfig conf = scraper.GetConfig();
            LoggerMessage.Define(LogLevel.Information, 0, $"Scraping from '{conf.ShopName}'...")(this.logger, null);

            try
            {
                this.shopItems.Add(
                    conf.ShopId, (await scraper.GetItemsAsync()
                                               .ConfigureAwait(false)));

                LoggerMessage.Define(
                    LogLevel.Information, 0,
                    $"{this.shopItems[conf.ShopId].Count} items were scraped from {conf.ShopName}")(this.logger, null);
            }
            catch (Exception ex)
            {
                this.NotifyScraperError(conf, ex);
            }
        }
    }

    private void NotifyScraperError(ScraperConfig conf, Exception ex)
    {
        LoggerMessage.Define(LogLevel.Critical, 0, $"'{conf.Name}' has ended its work: {ex.Message}")(this.logger, ex);
    }

    public IList<ItemModel> GetShopItems(int shopId)
    {
        if (this.shopItems.TryGetValue(shopId, out IList<ItemModel>? items))
        {
            return items;
        }

        LoggerMessage.Define(
            LogLevel.Warning, 0, $"'{this.config.TrackerName}' couldn't return shop items: nothing has been tracked")(
            this.logger, null);

        return Array.Empty<ItemModel>();
    }
}
