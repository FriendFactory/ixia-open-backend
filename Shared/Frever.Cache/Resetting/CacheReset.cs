using System;
using System.Linq;
using System.Threading.Tasks;
using Common.Infrastructure.Caching;

namespace Frever.Cache.Resetting;

public class CacheReset(ICache cache, CacheDependencyTracker cacheDependencyTracker) : ICacheReset
{
    private readonly ICache _cache = cache ?? throw new ArgumentNullException(nameof(cache));

    private readonly CacheDependencyTracker _cacheDependencyTracker =
        cacheDependencyTracker ?? throw new ArgumentNullException(nameof(cacheDependencyTracker));

    public async Task ResetKeys(params string[] keys)
    {
        if (keys == null)
            return;

        foreach (var key in keys.Select(k => k.Trim()))
            await _cache.DeleteKeysWithPrefix(key);
    }

    public Task ResetOnDependencyChange(Type entity, long? currentGroupId)
    {
        ArgumentNullException.ThrowIfNull(entity);

        return _cacheDependencyTracker.Reset(entity, currentGroupId);
    }
}