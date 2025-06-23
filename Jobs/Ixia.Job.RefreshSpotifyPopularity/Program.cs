using System;
using System.Threading.Tasks;
using Frever.Client.Core.Features.CommercialMusic;
using Frever.ClientService.Api;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Ixia.Job.RefreshSpotifyPopularity;

internal class Program
{
    private static void Main(string[] args)
    {
        ILogger log = null;
        try
        {
            var serviceCollection = new ServiceCollection();
            var startup = new Startup();

            startup.ConfigureServices(serviceCollection);

            serviceCollection.AddLogging(
                builder =>
                {
                    builder.ClearProviders();
                    builder.AddConsole();
                    builder.AddDebug();
                }
            );

            using var serviceProvider = serviceCollection.BuildServiceProvider();
            using var scope = serviceProvider.CreateScope();

            log = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger(typeof(Program).Namespace);
            using var _ = log.BeginScope("[{wid}]", Guid.NewGuid().ToString("N"));

            log.LogInformation("Start refreshing Spotify popularity");

            Run(scope.ServiceProvider, log).Wait();
            log.LogInformation("Spotify popularity refreshed successfully");
        }
        catch (Exception e)
        {
            log?.LogError(e, "Error refreshing Spotify popularity");
        }
    }

    private static async Task Run(IServiceProvider services, ILogger log)
    {
        var spotifyService = services.GetRequiredService<ISpotifyPopularityService>();

        log.LogInformation("Start refreshing Spotify popularity score");
        await spotifyService.RefreshSpotifyPopularity();
    }
}