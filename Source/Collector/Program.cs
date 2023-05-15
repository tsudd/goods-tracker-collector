using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;

using IHost host = Host.CreateDefaultBuilder(args)
    .UseConsoleLifetime()
    .ConfigureAppConfiguration((hostingContext, configuration) =>
    {
        configuration.Sources.Clear();

        IHostEnvironment env = hostingContext.HostingEnvironment;

        configuration
            .AddJsonFile($"appsettings.{env.EnvironmentName}.json", true, true)
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
    })
    .ConfigureServices((context, services) =>
    {
        services.ApplyCollectorConfigurations(context);
        services.AddCollectorServices();
    })
    .Build();

await host.RunAsync().ConfigureAwait(false);
