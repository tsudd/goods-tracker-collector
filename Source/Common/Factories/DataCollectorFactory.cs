using GoodsTracker.DataCollector.Common.Adapters;
using GoodsTracker.DataCollector.Common.Adapters.Abstractions;
using GoodsTracker.DataCollector.Common.Configuration;
using GoodsTracker.DataCollector.Common.Factories.Abstractions;
using GoodsTracker.DataCollector.Common.Mappers.Abstractions;
using GoodsTracker.DataCollector.Common.Parsers;
using GoodsTracker.DataCollector.Common.Parsers.Abstractions;
using GoodsTracker.DataCollector.Common.Requesters.Abstractions;
using GoodsTracker.DataCollector.Common.Scrapers;
using GoodsTracker.DataCollector.Common.Scrapers.Abstractions;
using GoodsTracker.DataCollector.Common.Trackers;
using GoodsTracker.DataCollector.Common.Trackers.Abstractions;
using GoodsTracker.DataCollector.DB.Context;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;

namespace GoodsTracker.DataCollector.Common.Factories;

public sealed class DataCollectorFactory : IDataCollectorFactory
{
    private static IWebDriver? driverInstanse;
    private readonly ILoggerFactory loggerFactory;
    private readonly IDbContextFactory<CollectorContext> contextFactory;

    public DataCollectorFactory(ILoggerFactory loggerFactory, IDbContextFactory<CollectorContext> contextFactory)
    {
        this.loggerFactory = loggerFactory;
        this.contextFactory = contextFactory;
    }

    public IDataAdapter CreateDataAdapter(IOptions<AdapterConfig> options)
    {
        var providedConfig = options.Value;

        return providedConfig.AdapterName switch
        {
            nameof(PostgresAdapter) => new PostgresAdapter(
                providedConfig, this.loggerFactory.CreateLogger<PostgresAdapter>(),
                this.contextFactory.CreateDbContext()),
            nameof(CsvAdapter) => new CsvAdapter(providedConfig, this.loggerFactory.CreateLogger<CsvAdapter>()),
            var _ => throw new ArgumentException(
                $"couldn't create {providedConfig.AdapterName}: no such data adapter in the app"),
        };
    }

    public IItemParser CreateParser(string parserName)
    {
        return parserName switch
        {
            nameof(YaNeighborsParser) => new YaNeighborsParser(this.loggerFactory.CreateLogger<YaNeighborsParser>()),
            nameof(EvrooptParser) => new EvrooptParser(),
            var _ => throw new ArgumentException($"couldn't create {parserName}: no such parser in the app."),
        };
    }

    public IScraper CreateScraper(
        ScraperConfig config, IItemParser? parser = null, IItemMapper? mapper = null, IRequester? requester = null)
    {
        return config.Name switch
        {
            nameof(YaNeighborsScraper) => new YaNeighborsScraper(
                config, this.loggerFactory.CreateLogger<YaNeighborsScraper>(),
                parser ?? this.CreateParser(config.ParserName), GetWebDriverInstance(), mapper),
            nameof(EvrooptScraper) => new EvrooptScraper(
                config, this.loggerFactory.CreateLogger<EvrooptScraper>(),
                parser ?? this.CreateParser(config.ParserName), GetWebDriverInstance(), mapper),
            var _ => throw new ArgumentException($"couldn't create {config.Name}: no such scraper in the app"),
        };
    }

    public IItemTracker CreateTracker(IOptions<TrackerConfig> options)
    {
        var config = options.Value;

        return config.TrackerName switch
        {
            nameof(BasicTracker) => new BasicTracker(options, this.loggerFactory.CreateLogger<BasicTracker>(), this),
            var _ => throw new ArgumentException($"couldn't create {config.TrackerName}: no such tracker in the app"),
        };
    }

    private static IWebDriver GetWebDriverInstance()
    {
        if (driverInstanse is null)
        {
            driverInstanse = new ChromeDriver();
        }

        return driverInstanse;
    }

    public void Dispose()
    {
        driverInstanse?.Dispose();
    }
}
