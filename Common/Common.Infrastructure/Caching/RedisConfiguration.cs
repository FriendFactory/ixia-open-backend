using System;
using Common.Infrastructure.Caching.Advanced.SortedList;
using Common.Infrastructure.Caching.Advanced.SortedSet;
using Common.Infrastructure.Caching.CacheKeys;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace Common.Infrastructure.Caching;

public static class RedisConfiguration
{
    public static RedisSettings BindRedisSettings(this IConfiguration configuration)
    {
        var redisSettings = new RedisSettings();
        configuration.Bind("Redis", redisSettings);
        redisSettings.Validate();

        return redisSettings;
    }

    public static void AddRedis(this IServiceCollection services, RedisSettings settings, string environmentVersion)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(settings);

        settings.Validate();

        services.AddSingleton(settings);

        services.AddSingleton<IConnectionMultiplexer>(
            provider =>
            {
                var s = provider.GetRequiredService<RedisSettings>();

                return ConnectionMultiplexer.Connect(s.ConnectionString);
            }
        );

        services.AddSingleton<ICache, RedisCache>();
        services.AddSingleton<ISortedSetCache, RedisSortedSetCache>();
        services.AddSingleton<ISortedListCache, SortedListCache>();

        CacheKeyVersion.SetKeyVersion(environmentVersion);
    }
}