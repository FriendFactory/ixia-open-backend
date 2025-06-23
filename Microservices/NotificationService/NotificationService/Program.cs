using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;

namespace NotificationService;

public class Program
{
    public static void Main(string[] args)
    {
        CreateHostBuilder(args).Build().Run();
    }

    public static IHostBuilder CreateHostBuilder(string[] args)
    {
        return Host.CreateDefaultBuilder(args)
                   .ConfigureWebHostDefaults(webBuilder => { webBuilder.UseStartup<Startup>(); })
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