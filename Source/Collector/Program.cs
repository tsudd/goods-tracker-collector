using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;

using IHost host = Host.CreateDefaultBuilder(args)
                       .UseConsoleLifetime()
                       .ConfigureAppConfiguration(
                           static (hostingContext, configuration) =>
                           {
                               configuration.Sources.Clear();
                               IHostEnvironment env = hostingContext.HostingEnvironment;

                               configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                                            .AddJsonFile($"appsettings.{env.EnvironmentName}.json", true, true)
                                            .AddEnvironmentVariables();
                           })
                       .ConfigureServices(
                           static (context, services) =>
                           {
                               services.ApplyCollectorConfigurations(context);
                               services.AddCollectorServices();
                           })
                       .Build();

await host.RunAsync()
          .ConfigureAwait(false);
