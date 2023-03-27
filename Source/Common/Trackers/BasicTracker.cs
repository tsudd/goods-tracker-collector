using GoodsTracker.DataCollector.Common.Configuration;
using GoodsTracker.DataCollector.Common.Scrapers.Abstractions;
using GoodsTracker.DataCollector.Common.Trackers.Abstractions;
using Microsoft.Extensions.Logging;
using GoodsTracker.DataCollector.Models;
using GoodsTracker.DataCollector.Common.Factories.Abstractions;
using Microsoft.Extensions.Options;

namespace GoodsTracker.DataCollector.Common.Trackers;

public class BasicTracker : IItemTracker
{
    private List<IScraper> _scrapers = new List<IScraper>();
    private Dictionary<int, IList<ItemModel>> _shopItems;
    private readonly ILogger<BasicTracker> _logger;
    private readonly TrackerConfig _config;

    public BasicTracker(
        IOptions<TrackerConfig> config,
        ILogger<BasicTracker> logger,
        IDataCollectorFactory factory
    )
    {
        ArgumentNullException.ThrowIfNull(config);
        ArgumentNullException.ThrowIfNull(factory);

        _config = config.Value;
        _logger = logger;
        _shopItems = new Dictionary<int, IList<ItemModel>>();
        foreach (var conf in _config.ScrapersConfigurations)
        {
            _scrapers.Add(
                factory.CreateScraper(
                    conf,
                    factory.CreateParser(conf.ParserName)
                )
            );
        }
    }

    public void ClearData()
    {
        foreach (var shopItems in _shopItems)
        {
            shopItems.Value.Clear();
        }
    }

    public async Task FetchItemsAsync()
    {
        foreach (var scraper in _scrapers)
        {
            var conf = scraper.GetConfig();

            LoggerMessage.Define(
                LogLevel.Information, 0,
                $"Scraping from '{conf.ShopName}'...")(
                    this._logger, null);

            try
            {
                _shopItems.Add(conf.ShopID, (await scraper.GetItemsAsync().ConfigureAwait(false)));

                LoggerMessage.Define(
                    LogLevel.Information, 0,
                    $"{_shopItems[conf.ShopID].Count} items were scraped from {conf.ShopName}")(
                        this._logger, null);
            }
            catch (Exception ex)
            {
                NotifyScraperError(conf, ex);
            }
        }
    }

    private void NotifyScraperError(ScraperConfig conf, Exception ex)
    {
        LoggerMessage.Define(
                    LogLevel.Critical, 0,
                    $"'{conf.Name}' has ended its work: {ex.Message}")(
                        this._logger, ex);
    }

    public IEnumerable<ItemModel> GetShopItems(int shopId)
    {
        if (_shopItems.ContainsKey(shopId))
        {
            return _shopItems[shopId];
        }
        LoggerMessage.Define(
                LogLevel.Warning, 0,
                $"'{_config.TrackerName}' couldn't return shop items: nothing has been tracked")(
                    this._logger, null);
        return Array.Empty<ItemModel>();
    }
}
