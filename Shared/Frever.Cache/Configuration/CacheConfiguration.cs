using System;
using Frever.Cache.Resetting;
using Frever.Cache.Supplement;
using Frever.Cache.Throttling;
using Microsoft.Extensions.DependencyInjection;

namespace Frever.Cache.Configuration;

public static class CacheConfiguration
{
    /// <summary>
    ///     Adds and allows to configure caching for entities.
    ///     Method could be called few times to configure different set of entities.
    /// </summary>
    public static void AddFreverCaching(this IServiceCollection services, Action<CacheOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        services.AddSingleton<ICacheReset, CacheReset>();
        services.AddScoped<CacheDependencyTracker>();

        services.AddScoped<RpsThrottler>();

        var options = new CacheOptions(services);

        configure(options);
    }

    public static void AddFreverCachingCurrentUserAccessor(
        this IServiceCollection services,
        Func<IServiceProvider, long?> getCurrentGroupId
    )
    {
        services.AddScoped<ICurrentGroupAccessor>(provider => new CustomCurrentGroupAccessor(getCurrentGroupId, provider));
    }
}