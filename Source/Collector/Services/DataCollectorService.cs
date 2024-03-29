namespace GoodsTracker.DataCollector.Collector.Services;

using System.Threading;

using GoodsTracker.DataCollector.Collector.Options;
using GoodsTracker.DataCollector.Common.Adapters.Abstractions;
using GoodsTracker.DataCollector.Common.Trackers.Abstractions;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

public sealed class DataCollectorService : BackgroundService
{
    private readonly ILogger<DataCollectorService> _logger;
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
            "Starting scraping of items.")(
                _logger, null);

        // TODO: better feedback about how did it go
        await _tracker.FetchItemsAsync().ConfigureAwait(false);

        LoggerMessage.Define(
            LogLevel.Information, 0,
            "Sending fetched data to the adapter")(
                _logger, null);

        try
        {
            await _dataAdapter.SaveItemsAsync(_tracker, _options.ShopIds).ConfigureAwait(false);
        }
        catch (ApplicationException ex)
        {
            LoggerMessage.Define(
                LogLevel.Error, 0,
                $"Error occured during data save: {ex.Message}")(
                    _logger, ex);
        }

        _appLifeTime.StopApplication();
    }

    public override void Dispose()
    {
        this.Dispose(true);
        GC.SuppressFinalize(this);
        base.Dispose();
    }

    public void Dispose(bool disposing)
    {
        if (disposing)
        {
            this._tracker.ClearData();
        }
    }
}
