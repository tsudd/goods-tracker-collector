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
    public Dictionary<string, List<Item>> _shopItems;
    private ILogger<BasicTracker> _logger;
    private TrackerConfig _config;
    public BasicTracker(
        TrackerConfig config,
        ILoggerFactory loggerFactory,
        IDataCollectorFactory factory)
    {
        _config = config;
        _logger = loggerFactory.CreateLogger<BasicTracker>();
        // _shopItems = new List<Tuple<string, List<Item>>>();
        _shopItems = new Dictionary<string, List<Item>>();
        Scrapers = new List<IScraper>();
        _logger.LogInformation("Scrapers creation from provided configs...");
        foreach (var conf in _config.ScrapersConfigurations)
        {
            Scrapers.Add(
                factory.CreateScraper(
                conf,
                loggerFactory,
                factory.CreateParser(conf.ParserName, loggerFactory)));
            _shopItems.Add(conf.ShopID, new List<Item>());
            _logger.LogInformation("'{0}' was created", conf.Name);
        }
        _logger.LogInformation("Tracker was created.");
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
            _logger.LogInformation("Scraping from '{0}'...", conf.ShopName);
            try
            {
                _shopItems[conf.ShopID].AddRange(await scraper.GetItems());
                _logger.LogInformation($"{_shopItems[conf.ShopID].Count} items were scraped from {conf.ShopName}");
            }
            catch (JsonException ex)
            {
                NotifyScraperError(conf, ex);
            }
            catch (HtmlWebException ex)
            {
                NotifyScraperError(conf, ex);
            }
            catch (Exception ex)
            {
                NotifyScraperError(conf, ex);
            }
        }
    }

    private void NotifyScraperError(ScraperConfig conf, Exception ex)
    {
        _logger.LogWarning($"'{conf.Name}' has ended its work: {ex.Message}");
    }

    public IEnumerable<Item>? GetShopItems(string shopId)
    {
        try
        {
            return _shopItems[shopId];
        }
        catch (Exception ex)
        {
            _logger.LogWarning("'{0}' couldn't return shop items: {1}", _config.TrackerName, ex.Message);
            return null;
        }
    }
}