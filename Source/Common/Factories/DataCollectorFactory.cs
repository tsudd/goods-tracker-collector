using GoodsTracker.DataCollector.Common.Adapters;
using GoodsTracker.DataCollector.Common.Adapters.Abstractions;
using GoodsTracker.DataCollector.Common.Configuration;
using GoodsTracker.DataCollector.Common.Factories.Abstractions;
using GoodsTracker.DataCollector.Common.Mappers;
using GoodsTracker.DataCollector.Common.Mappers.Abstractions;
using GoodsTracker.DataCollector.Common.Parsers;
using GoodsTracker.DataCollector.Common.Parsers.Abstractions;
using GoodsTracker.DataCollector.Common.Requesters;
using GoodsTracker.DataCollector.Common.Requesters.Abstractions;
using GoodsTracker.DataCollector.Common.Scrapers;
using GoodsTracker.DataCollector.Common.Scrapers.Abstractions;
using GoodsTracker.DataCollector.Common.Trackers;
using GoodsTracker.DataCollector.Common.Trackers.Abstractions;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;

namespace GoodsTracker.DataCollector.Common.Factories;
public class DataCollectorFactory : IDataCollectorFactory
{
    protected static IWebDriver? _driverInstanse;
    protected readonly ILoggerFactory _loggerFactory;
    public DataCollectorFactory(ILoggerFactory loggerFactory)
    {
        _loggerFactory = loggerFactory;
    }

    public IDataAdapter CreateDataAdapter(IOptions<AdapterConfig> options)
    {
        var providedConfig = options.Value;
        return providedConfig.AdapterName switch
        {
            nameof(CsvAdapter) => new CsvAdapter(
                    providedConfig,
                    _loggerFactory.CreateLogger<CsvAdapter>()),
            var _ =>
                throw new ArgumentException(
                    $"couldn't create {providedConfig.AdapterName}: no such data adapter in the app"
                )
        };
    }

    public IItemParser CreateParser(
        string parserName)
    {
        return parserName switch
        {
            nameof(YaNeighborsParser) => new YaNeighborsParser(_loggerFactory.CreateLogger<YaNeighborsParser>()),
            var _ => throw new ArgumentException($"couldn't create {parserName}: no such parser in the app."),
        };
    }

    public IScraper CreateScraper(
        ScraperConfig config,
        IItemParser? parser = null,
        IItemMapper? mapper = null,
        IRequester? requester = null)
    {
        return config.Name switch
        {
            nameof(YaNeighborsScraper) => new YaNeighborsScraper(
                    config,
                    _loggerFactory.CreateLogger<YaNeighborsScraper>(),
                    parser ?? CreateParser(config.ParserName),
                    GetWebDriverInstance(),
                    mapper),
            var _ =>
                    throw new ArgumentException(
                        $"couldn't create {config.Name}: no such scraper in the app"),
        };
    }

    public IItemTracker CreateTracker(
        IOptions<TrackerConfig> options)
    {
        var config = options.Value;
        return config.TrackerName switch
        {
            nameof(BasicTracker) =>
                new BasicTracker(
                    options,
                    _loggerFactory.CreateLogger<BasicTracker>(),
                    this
                ),
            var _ =>
                throw new ArgumentException(
                    $"couldn't create {config.TrackerName}: no such tracker in the app"),
        };
    }

    protected static IWebDriver GetWebDriverInstance()
    {
        if (_driverInstanse is null)
        {
            _driverInstanse = new ChromeDriver();
        }
        return _driverInstanse;
    }

    public void Dispose()
    {
        _driverInstanse?.Dispose();
    }
}
