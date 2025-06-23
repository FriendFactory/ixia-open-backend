using System;
using System.IO;
using System.Threading.Tasks;
using Common.Infrastructure.EmailSending;
using Common.Infrastructure.RateLimit;
using Frever.Client.Core.Features.CommercialMusic;
using Frever.ClientService.Api;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Ixia.Job.LoadExternalSongsFromBlokur;

internal class Program
{
    private static void Main(string[] args)
    {
        Console.WriteLine("Start processing blokur CSV");
        IEmailSendingService emailSendingService = null;
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
            AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
            using var serviceProvider = serviceCollection.BuildServiceProvider();
            using var scope = serviceProvider.CreateScope();

            log = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger(typeof(Program).Namespace);
            using var _ = log.BeginScope("[{wid}]", Guid.NewGuid().ToString("N"));

            emailSendingService = scope.ServiceProvider.GetRequiredService<IEmailSendingService>();

            log.LogInformation("Start downloading and applying Blokur CSV");

            Run(scope.ServiceProvider, log).Wait();
            log.LogInformation("Blokur CSV processed successfully");
        }
        catch (Exception e)
        {
            log?.LogError(e, "Error processing Blokur CSV");
            Console.WriteLine(e.ToString());
            emailSendingService?.SendEmail(
                                     new SendEmailParams
                                     {
                                         To = AlertingClientRateLimitMiddleware.EmailAddr,
                                         Subject = "Error processing Blokur CSV",
                                         Body = @$"Error processing Blokur CSV.
                                                        
                                            {e}"
                                     }
                                 )
                                .Wait();
            Environment.Exit(-2);
        }
    }

    private static async Task Run(IServiceProvider services, ILogger log)
    {
        var musicService = services.GetRequiredService<IRefreshLocalMusicService>();

        log.LogInformation("Start downloading all tracks CSV");

        var filePath = await musicService.DownloadTracksCsv();

        log.LogInformation("CSV downloaded in {p}, size={s}MB", filePath, new FileInfo(filePath).Length / 1024 / 1024);

        await musicService.RefreshTrackInfoFromCsv(filePath);
    }
}