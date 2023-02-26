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

var loggerFactory = LoggerFactory.Create(builder =>
{
    builder
        .SetMinimumLevel(LogLevel.Information)
        .AddConsole()
        .AddConfiguration(config.GetSection("Logging"));
});
var log = loggerFactory.CreateLogger<Program>();
log.LogInformation($"Configuration was loaded. Tracker is starting now in {environment} mode.");

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
if (trackerConfig is null || shopIDs is null || adapterConfig is null)
{
    log.LogError("Couldn't define config for the tracker: wrong format of the configuration file");
    return;
}
if (alternativeAdapterConfig is null)
{
    log.LogWarning("Couldn't get config for alternative data adapter. Data might be lost by proceeding without it.");
}
IDataCollectorFactory? collectorFactory;
try
{
    // TODO: replace explicit call with reflection statement
    collectorFactory = DataCollectorFactory.GetInstance();
}
catch (ArgumentException ex)
{
    log.LogError($"Invalid configuration for factories: {ex.Message}");
    return;
}
log.LogInformation("Tracker to be launched: '{0}'. Number of configs for scrapers: '{1}'",
    trackerConfig.TrackerName,
    trackerConfig.ScrapersConfigurations.Count());

//------------------initialization of the tracker with provided config
log.LogInformation("Tracker instance creation...");
IItemTracker? tracker;
try
{
    tracker = collectorFactory.CreateTracker(trackerConfig, loggerFactory);
}
catch (ArgumentException ex)
{
    log.LogError("Error while tracker initialization: {0}", ex.Message);
    return;
}
catch (Exception ex)
{
    log.LogError("Unspecified error occurred while tracker creation: {0}", ex.Message);
    return;
}

//------------------fetching data with configured tracker
log.LogInformation("Starting scraping items.");
await tracker.FetchItemsAsync();

//------------------record fetch data

log.LogInformation("Sending fetched data to the DB adapter");

try
{
    var adapter = collectorFactory.CreateDataAdapter(adapterConfig, loggerFactory);
    adapter.SaveItems(tracker, shopIDs);
}
catch (ApplicationException ex)
{
    if (alternativeAdapterConfig is not null)
    {
        log.LogWarning(
        $"Error occured during saving of items into the DB: {ex.Message}. Saving items using alternative data adapter for future restore");
        var alternativeAdapter = collectorFactory.CreateDataAdapter(alternativeAdapterConfig, loggerFactory);
        alternativeAdapter.SaveItems(tracker, shopIDs);
    }
    else
    {
        log.LogError($"Error occured during data save: {ex.Message}");
    }

}

//------------------clearing & disposing
log.LogInformation("Clearing fetched data...");
tracker.ClearData();
collectorFactory.Dispose();

log.LogInformation("Tracker has ended its work.");