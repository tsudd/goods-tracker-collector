namespace Microsoft.Extensions.DependencyInjection;

using GoodsTracker.DataCollector.Collector.Options;
using GoodsTracker.DataCollector.Collector.Services;
using GoodsTracker.DataCollector.Common.Adapters.Abstractions;
using GoodsTracker.DataCollector.Common.Configuration;
using GoodsTracker.DataCollector.Common.Factories;
using GoodsTracker.DataCollector.Common.Factories.Abstractions;
using GoodsTracker.DataCollector.Common.Trackers.Abstractions;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

public static class DataCollectorServices
{
    public static void AddCollectorServices(this IServiceCollection services)
    {
        services.AddSingleton<IDataCollectorFactory, DataCollectorFactory>();

        services.AddSingleton<IItemTracker>((serviceProvider) =>
        {
            var factory = serviceProvider.GetRequiredService<IDataCollectorFactory>();

            return factory.CreateTracker(serviceProvider.GetRequiredService<IOptions<TrackerConfig>>());
        });

        services.AddSingleton<IDataAdapter>((serviceProvider) =>
        {
            var factory = serviceProvider.GetRequiredService<IDataCollectorFactory>();

            return factory.CreateDataAdapter(
                serviceProvider.GetRequiredService<IOptions<AdapterConfig>>());
        });

        services.AddHostedService<DataCollectorService>();
    }

    public static void ApplyCollectorConfigurations(this IServiceCollection services, HostBuilderContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var configuration = context.Configuration;
        services.Configure<TrackerConfig>(configuration.GetSection(nameof(TrackerConfig)));
        services.Configure<DataCollectorOptions>(configuration.GetSection(nameof(DataCollectorOptions)));
        services.Configure<AdapterConfig>(configuration.GetSection(nameof(AdapterConfig)));
    }
}
