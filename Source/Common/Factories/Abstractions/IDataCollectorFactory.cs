using GoodsTracker.DataCollector.Common.Adapters.Abstractions;
using GoodsTracker.DataCollector.Common.Configuration;
using GoodsTracker.DataCollector.Common.Mappers.Abstractions;
using GoodsTracker.DataCollector.Common.Parsers.Abstractions;
using GoodsTracker.DataCollector.Common.Requesters.Abstractions;
using GoodsTracker.DataCollector.Common.Scrapers.Abstractions;
using GoodsTracker.DataCollector.Common.Trackers.Abstractions;
using Microsoft.Extensions.Logging;

namespace GoodsTracker.DataCollector.Common.Factories.Abstractions;
public interface IDataCollectorFactory : IDisposable
{
    IItemMapper CreateMapper(string mapperName);
    IItemParser CreateParser(string parserName, ILoggerFactory loggerFactory);
    IScraper CreateScraper(
        ScraperConfig config,
        ILoggerFactory loggerFactory,
        IItemParser? parser = null,
        IItemMapper? mapper = null,
        IRequester? requester = null
    );
    IItemTracker CreateTracker(
        TrackerConfig config,
        ILoggerFactory loggerFactory);
    IRequester CreateRequester(string requesterName, HttpClient? client = null);
    IDataAdapter CreateDataAdapter(AdapterConfig config, ILoggerFactory loggerFactory);
}