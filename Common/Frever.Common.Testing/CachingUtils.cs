using Common.Infrastructure.Caching;
using Microsoft.Extensions.DependencyInjection;

namespace Frever.Common.Testing;

public static class CachingUtils
{
    public static async Task ResetRedisCache(this IServiceProvider provider)
    {
        ArgumentNullException.ThrowIfNull(provider);

        var cache = provider.GetRequiredService<ICache>();
        await cache.Server().FlushAllDatabasesAsync();
    }
}