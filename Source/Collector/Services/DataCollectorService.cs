namespace GoodsTracker.DataCollector.Collector.Services;

using System.Threading;

using GoodsTracker.DataCollector.Collector.Options;
using GoodsTracker.DataCollector.Common.Adapters.Abstractions;
using GoodsTracker.DataCollector.Common.Trackers.Abstractions;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

public class DataCollectorService : BackgroundService
{
    private readonly ILogger _logger;
    private readonly IItemTracker _tracker;
    private readonly DataCollectorOptions _options;
    private readonly IDataAdapter _dataAdapter;
    private readonly IHostApplicationLifetime _appLifeTime;
    public DataCollectorService(
        ILogger<DataCollectorService> logger,
        IItemTracker tracker,
        IOptions<DataCollectorOptions> options,
        IDataAdapter providedDataAdapter,
        IHostApplicationLifetime appLifeTime)
    {
        ArgumentNullException.ThrowIfNull(options);

        _logger = logger;
        _tracker = tracker;
        _options = options.Value;
        _dataAdapter = providedDataAdapter;
        _appLifeTime = appLifeTime;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        LoggerMessage.Define(
            LogLevel.Information, 0,
            "Starting scraping items.")(
                _logger, null);

        _appLifeTime.StopApplication();

        await _tracker.FetchItemsAsync().ConfigureAwait(false);

        LoggerMessage.Define(
            LogLevel.Information, 0,
            "Sending fetched data to the adapter")(
                _logger, null);

        try
        {
            _dataAdapter.SaveItems(_tracker, _options.ShopIds);
        }
        catch (ApplicationException ex)
        {
            LoggerMessage.Define(
                LogLevel.Error, 0,
                $"Error occured during data save: {ex.Message}")(
                    _logger, ex);
        }

        LoggerMessage.Define(
            LogLevel.Information, 0,
            "Clearing fetched data")(
                _logger, null);
    }

    public override void Dispose()
    {
        _tracker.ClearData();
        base.Dispose();
    }
}
