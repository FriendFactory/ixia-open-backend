using Common.Infrastructure;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;

namespace AssetServer;

public class Program
{
    public static void Main(string[] args)
    {
        AWSEnvironmentSetup.Setup();
        CreateWebHostBuilder(args).Build().Run();
    }

    public static IWebHostBuilder CreateWebHostBuilder(string[] args)
        => WebHost.CreateDefaultBuilder(args)
                  .ConfigureLogging(
                       (hostingContext, logging) =>
                       {
                           logging.AddConfiguration(hostingContext.Configuration.GetSection("Logging"));
                           logging.AddSimpleConsole(
                               options =>
                               {
                                   options.IncludeScopes = true;
                                   options.UseUtcTimestamp = true;
                                   options.SingleLine = true;
                                   options.TimestampFormat = "yyyy-MM-ddTHH:mm:ss.fff ";
                                   options.ColorBehavior = LoggerColorBehavior.Disabled;
                               }
                           );
#if DEBUG
                           logging.AddDebug();
#endif
                       }
                   )
                  .UseStartup<Startup>()
                  .UseKestrel(options => { options.Limits.MaxRequestBodySize = long.MaxValue; })
                  .UseIIS();
}