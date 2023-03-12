using System.Text.Json;
using GoodsTracker.DataCollector.Common.Configuration;
using GoodsTracker.DataCollector.Common.Scrapers.Abstractions;
using GoodsTracker.DataCollector.Common.Trackers.Abstractions;
using HtmlAgilityPack;
using Microsoft.Extensions.Logging;
using GoodsTracker.DataCollector.Models;
using GoodsTracker.DataCollector.Common.Factories.Abstractions;

namespace GoodsTracker.DataCollector.Common.Trackers;

public class BasicTracker : IItemTracker
{
    public List<IScraper> Scrapers { get; private set; }
    public Dictionary<string, List<ItemModel>> _shopItems;
    private ILogger<BasicTracker> _logger;
    private TrackerConfig _config;

    public BasicTracker(
        TrackerConfig config,
        ILoggerFactory loggerFactory,
        IDataCollectorFactory factory
    )
    {
        _config = config;
        _logger = loggerFactory.CreateLogger<BasicTracker>();
        _shopItems = new Dictionary<string, List<ItemModel>>();
        Scrapers = new List<IScraper>();
        LoggerMessage.Define(
                LogLevel.Information, 0,
                "Creating scrapers from provided configs...")(
                    this._logger, null);
        foreach (var conf in _config.ScrapersConfigurations)
        {
            Scrapers.Add(
                factory.CreateScraper(
                    conf,
                    loggerFactory,
                    factory.CreateParser(conf.ParserName, loggerFactory)
                )
            );
            _shopItems.Add(conf.ShopID, new List<ItemModel>());
            LoggerMessage.Define(
                LogLevel.Information, 0,
                $"'{conf.Name}' was created")(
                    this._logger, null);
        }
        LoggerMessage.Define(
                LogLevel.Information, 0,
                "Tracker was created.")(
                    this._logger, null);
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
        foreach (var scraper in Scrapers)
        {
            var conf = scraper.GetConfig();

            LoggerMessage.Define(
                LogLevel.Information, 0,
                $"Scraping from '{conf.ShopName}'...")(
                    this._logger, null);

            try
            {
                _shopItems[conf.ShopID].AddRange(await scraper.GetItemsAsync());

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

    public IEnumerable<ItemModel>? GetShopItems(string shopId)
    {
        try
        {
            return _shopItems[shopId];
        }
        catch (Exception ex)
        {
            LoggerMessage.Define(
                    LogLevel.Warning, 0,
                    $"'{_config.TrackerName}' couldn't return shop items: {ex.Message}")(
                        this._logger, ex);
            return null;
        }
    }
}
