using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;

namespace Frever.ClientService.Api;

public class Program
{
    public static void Main(string[] args)
    {
        CreateWebHostBuilder(args).Build().Run();
    }

    public static IWebHostBuilder CreateWebHostBuilder(string[] args)
    {
        return WebHost.CreateDefaultBuilder(args)
                      .UseStartup<Startup>()
                      .ConfigureLogging(
                           builder =>
                           {
                               builder.ClearProviders();
                               builder.AddSimpleConsole(
                                   options =>
                                   {
                                       options.IncludeScopes = true;
                                       options.UseUtcTimestamp = true;
                                       options.SingleLine = true;
                                       options.TimestampFormat = "yyyy-MM-ddTHH:mm:ss.fff ";
                                       options.ColorBehavior = LoggerColorBehavior.Disabled;
                                   }
                               );
                           }
                       );
    }
}