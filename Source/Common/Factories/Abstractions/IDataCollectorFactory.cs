using GoodsTracker.DataCollector.Common.Adapters.Abstractions;
using GoodsTracker.DataCollector.Common.Configuration;
using GoodsTracker.DataCollector.Common.Mappers.Abstractions;
using GoodsTracker.DataCollector.Common.Parsers.Abstractions;
using GoodsTracker.DataCollector.Common.Requesters.Abstractions;
using GoodsTracker.DataCollector.Common.Scrapers.Abstractions;
using GoodsTracker.DataCollector.Common.Trackers.Abstractions;

using Microsoft.Extensions.Options;

namespace GoodsTracker.DataCollector.Common.Factories.Abstractions;
public interface IDataCollectorFactory : IDisposable
{
    IItemParser CreateParser(string parserName);
    IScraper CreateScraper(
        ScraperConfig config,
        IItemParser? parser = null,
        IItemMapper? mapper = null,
        IRequester? requester = null
    );
    IItemTracker CreateTracker(
        IOptions<TrackerConfig> options);
    IDataAdapter CreateDataAdapter(IOptions<AdapterConfig> config);
}
