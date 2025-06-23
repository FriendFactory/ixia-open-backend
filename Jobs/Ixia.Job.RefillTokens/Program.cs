using System;
using Common.Infrastructure.Aws;
using Common.Infrastructure.Caching;
using Common.Infrastructure.Database;
using Common.Infrastructure.EnvironmentInfo;
using Frever.Client.Core.Features.InAppPurchases;
using Frever.Shared.AssetStore.DailyTokenRefill;
using Frever.Shared.MainDb;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Ixia.Job.RefillTokens;

public static class Program
{
    public static void Main(string[] args)
    {
        var configuration = new ConfigurationBuilder().AddJsonFile("appsettings.json", true)
                                                      .AddJsonFile(
                                                           $"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")}.json",
                                                           true
                                                       )
                                                      .AddEnvironmentVariables()
                                                      .AddBeanstalkConfig()
                                                      .Build();

        var serviceCollection = new ServiceCollection();

        serviceCollection.AddLogging(l => l.AddConsole());

        // Business services
        var dbConnectionConfig = configuration.GetDbConnectionConfiguration();

        serviceCollection.AddFreverDataWritingAccess(dbConnectionConfig);
        var envInfo = configuration.BindEnvironmentInfo();
        serviceCollection.AddSingleton(envInfo);
        var redisSettings = configuration.BindRedisSettings();
        serviceCollection.AddRedis(redisSettings, envInfo.Version);


        var inAppPurchaseOptions = new InAppPurchaseOptions();
        configuration.Bind("InAppPurchases", inAppPurchaseOptions);
        serviceCollection.AddInAppPurchasesCore(inAppPurchaseOptions);

        using var provider = serviceCollection.BuildServiceProvider();

        var service = provider.GetRequiredService<IDailyTokenRefillService>();
        service.BatchRefillDailyTokens(false).Wait();
    }
}