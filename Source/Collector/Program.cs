using GoodsTracker.DataCollector.Common.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Configuration;
using GoodsTracker.DataCollector.Common.Trackers.Abstractions;
using GoodsTracker.DataCollector.Common.Factories.Abstractions;
using GoodsTracker.DataCollector.Common.Factories;

var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");

var config = new ConfigurationBuilder()
    .AddJsonFile($"appsettings.{environment}.json")
    .AddEnvironmentVariables()
    .Build();

using var loggerFactory = LoggerFactory.Create(builder =>
{
    builder
        .AddConsole()
        .AddConfiguration(config.GetSection("Logging"));
});
var log = loggerFactory.CreateLogger<Program>();
LoggerMessage.Define(
    LogLevel.Information, 0,
    $"Configuration was loaded. Tracker is starting now in {environment} mode.")(
        log, null);

//------------------handling tracker configuration
// TODO: get tracker configuraiton deeply 
var trackerConfig = new TrackerConfig()
{
    TrackerName = config.GetSection("TrackerConfig").GetValue<string>("TrackerName"),
    ScrapersConfigurations = config.GetSection("TrackerConfig:ScrapersConfigurations").Get<List<ScraperConfig>>()
};
var shopIDs = config.GetSection("ShopIDs").Get<IEnumerable<string>>();
var adapterConfig = new AdapterConfig
{
    AdapterName = config.GetSection("AdapterConfig:AdapterName").Get<string>(),
    Arguments = Environment.GetEnvironmentVariable("HANA_ConnectionString")
        ?? throw new ApplicationException("couldn't get connection string from env"),
    LocalPath = config.GetSection("AdapterConfig:LocalPath").Get<string>()
};
var alternativeAdapterConfig = config.GetSection("AlternativeAdapterConfig").Get<AdapterConfig>();
if (shopIDs is null)
{
    LoggerMessage.Define(
        LogLevel.Error, 0,
        "Couldn't define config for the tracker: wrong format of the configuration file")(
            log, null);
    return;
}
if (alternativeAdapterConfig is null)
{
    LoggerMessage.Define(
        LogLevel.Warning, 0,
        "Couldn't get config for alternative data adapter. Data might be lost by proceeding without it.")(
            log, null);
}
IDataCollectorFactory? collectorFactory;
try
{
    // TODO: replace explicit call with reflection statement
    collectorFactory = DataCollectorFactory.GetInstance();
}
catch (ArgumentException ex)
{
    LoggerMessage.Define(
        LogLevel.Error, 0,
        $"Invalid configuration for factories: {ex.Message}")(
            log, ex);
    return;
}
LoggerMessage.Define(
    LogLevel.Information, 0,
    $"Tracker to be launched: '{trackerConfig.TrackerName}'. Number of configs for scrapers: '{trackerConfig.ScrapersConfigurations.Count()}'")(
        log, null);

//------------------initialization of the tracker with provided config
LoggerMessage.Define(
    LogLevel.Information, 0,
    "Tracker instance creation...")(
        log, null);
IItemTracker? tracker;
try
{
    tracker = collectorFactory.CreateTracker(trackerConfig, loggerFactory);
}
catch (ArgumentException ex)
{
    LoggerMessage.Define(
        LogLevel.Error, 0,
        $"Error while tracker initialization: {ex.Message}")(
            log, ex);
    return;
}
catch (Exception ex)
{
    LoggerMessage.Define(
        LogLevel.Error, 0,
        $"Unspecified error occurred while tracker creation: {ex.Message}")(
            log, ex);
    return;
}

//------------------fetching data with configured tracker
LoggerMessage.Define(
    LogLevel.Information, 0,
    "Starting scraping items.")(
        log, null);
await tracker.FetchItemsAsync().ConfigureAwait(false);

//------------------record fetch data

LoggerMessage.Define(
    LogLevel.Information, 0,
    "Sending fetched data to the adapter")(
        log, null);

try
{
    var adapter = collectorFactory.CreateDataAdapter(adapterConfig, loggerFactory);
    adapter.SaveItems(tracker, shopIDs);
}
catch (ApplicationException ex)
{
    if (alternativeAdapterConfig is not null)
    {
        LoggerMessage.Define(
            LogLevel.Warning, 0,
            $"Error occured during saving of items into the DB: {ex.Message}. Saving items using alternative data adapter for future restore")(
                log, ex);
        var alternativeAdapter = collectorFactory.CreateDataAdapter(alternativeAdapterConfig, loggerFactory);
        alternativeAdapter.SaveItems(tracker, shopIDs);
    }
    else
    {
        LoggerMessage.Define(
            LogLevel.Error, 0,
            $"Error occured during data save: {ex.Message}")(
                log, ex);
    }

}

//------------------clearing & disposing
LoggerMessage.Define(
            LogLevel.Information, 0,
            "Clearing fetched data...")(
                log, null);

tracker.ClearData();
collectorFactory.Dispose();

LoggerMessage.Define(
            LogLevel.Information, 0,
            "Tracker has ended its work.")(
                log, null);
