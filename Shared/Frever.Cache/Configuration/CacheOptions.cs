using System;
using Frever.Cache.Strategies;
using Microsoft.Extensions.DependencyInjection;

namespace Frever.Cache.Configuration;

public class CacheOptions(IServiceCollection services) : CacheOptions.IInMemoryConfigurator,
                                                         CacheOptions.IRedisConfigurator,
                                                         CacheOptions.IDoubleCachingInMemoryConfigurator
{
    private readonly IServiceCollection _services = services ?? throw new ArgumentNullException(nameof(services));

    public IInMemoryConfigurator InMemory => this;

    public IRedisConfigurator Redis => this;

    public IDoubleCachingInMemoryConfigurator InMemoryDoubleCache => this;

    void IDoubleCachingInMemoryConfigurator.Blob<TData>(
        SerializeAs? serializer = null,
        bool cloneNonCachedValue = false,
        params Type[] entityDependencies
    )
    {
        _services.AddSingleton(_ => new DoubleCacheInMemoryBlob<TData>(entityDependencies, serializer, cloneNonCachedValue));
        _services.AddScoped<IBlobCache<TData>, DoubleCacheInMemoryBlob<TData>.Cache>();
    }

    void IInMemoryConfigurator.Blob<TData>(SerializeAs? serializer, bool cloneNonCachedValue, params Type[] entityDependencies)
    {
        _services.AddSingleton(_ => new InMemoryBlob<TData>(entityDependencies, serializer, cloneNonCachedValue));
        _services.AddScoped<IBlobCache<TData>, InMemoryBlob<TData>.Cache>();
    }

    void IInMemoryConfigurator.Dictionary<TId, TData>(
        string baseKey,
        SerializeAs? serializer,
        bool cloneNonCachedValue,
        bool removeExpiredValues,
        params Type[] entityDependencies
    )
    {
        _services.AddSingleton(
            _ => new InMemoryDictionary<TId, TData>(baseKey, entityDependencies, cloneNonCachedValue, removeExpiredValues)
        );
        _services.AddScoped<IDictionaryCache<TId, TData>, InMemoryDictionary<TId, TData>.Cache>();
    }


    void IRedisConfigurator.PagedList<TData>(
        SerializeAs? serializer,
        int pageSize,
        int initialPageSize,
        bool reloadInitialData,
        Type[] globalDependencies,
        Type[] userDependencies
    )
    {
        _services.AddSingleton(
            _ => new RedisPagedList<TData>(
                serializer,
                pageSize,
                initialPageSize,
                reloadInitialData,
                globalDependencies,
                userDependencies
            )
        );

        _services.AddScoped<IListCache<TData>, RedisPagedList<TData>.Cache>();
    }

    void IRedisConfigurator.Dictionary<TId, TData>(string baseKey, SerializeAs? serializer, params Type[] entityDependencies)
    {
        _services.AddSingleton(_ => new RedisDictionary<TId, TData>(baseKey, entityDependencies, serializer));
        _services.AddScoped<IDictionaryCache<TId, TData>, RedisDictionary<TId, TData>.Cache>();
    }

    void IRedisConfigurator.Hash<TData>()
    {
        _services.AddSingleton(_ => new RedisHash<TData>());
        _services.AddScoped<IHashCache<TData>, RedisHash<TData>.Cache>();
    }

    void IRedisConfigurator.Blob<TData>(SerializeAs? serializer = null, bool cloneNonCachedValue = false, params Type[] entityDependencies)
    {
        _services.AddSingleton(_ => new RedisBlob<TData>(entityDependencies, serializer, cloneNonCachedValue));
        _services.AddScoped<IBlobCache<TData>, RedisBlob<TData>.Cache>();
    }

    public interface IInMemoryConfigurator
    {
        /// <summary>
        ///     Configures cache for storing data in memory.
        ///     Cache stores full data in memory and only flag for resetting in redis.
        ///     <param name="serializer">
        ///         If not null than object is cloned (by serializing and deserializing) before storing in
        ///         cache
        ///     </param>
        ///     <param name="cloneNonCachedValue">
        ///         If true the value retrieved from data would be cloned via serialization and
        ///         immediate deserialization.
        ///     </param>
        ///     <param name="entityDependencies">An array of entity types which change should reset cache for the data.</param>
        /// </summary>
        void Blob<TData>(SerializeAs? serializer = null, bool cloneNonCachedValue = false, params Type[] entityDependencies);

        /// <summary>
        ///     Configures cache to store data in memory indexed by id.
        ///     Cache stored full data in memory and only flag for resetting in redis.
        ///     To consume the configured cache inject <see cref="IDictionaryCache{TId,TData}" /> instance.
        /// </summary>
        void Dictionary<TId, TData>(
            string baseKey,
            SerializeAs? serializer,
            bool cloneNonCachedValue,
            bool removeExpiredValues,
            params Type[] entityDependencies
        );
    }

    public interface IDoubleCachingInMemoryConfigurator
    {
        /// <summary>
        ///     Configures cache for storing data in both memory and redis.
        ///     Cache stores full data in memory but also stores a copy of data in redis to ensure all replicas of
        ///     the service will have the same data in-memory.
        ///     <param name="serializer">
        ///         If not null than object is cloned (by serializing and deserializing) before storing in
        ///         cache
        ///     </param>
        ///     <param name="cloneNonCachedValue">
        ///         If true the value retrieved from data would be cloned via serialization and
        ///         immediate deserialization.
        ///     </param>
        ///     <param name="entityDependencies">An array of entity types which change should reset cache for the data.</param>
        /// </summary>
        void Blob<TData>(SerializeAs? serializer = null, bool cloneNonCachedValue = false, params Type[] entityDependencies);
    }

    public interface IRedisConfigurator
    {
        void PagedList<TData>(
            SerializeAs? serializer,
            int pageSize,
            int initialPageSize,
            bool reloadInitialData,
            Type[] globalDependencies = null,
            Type[] userDependencies = null
        );

        /// <summary>
        ///     Configures cache to store data in redis indexed by id.
        ///     To consume the configured cache inject <see cref="IDictionaryCache{TId,TData}" /> instance.
        /// </summary>
        void Dictionary<TId, TData>(string baseKey, SerializeAs? serializer, params Type[] entityDependencies);

        /// <summary>
        ///     Configure cache to store instance of class as Redis hash (value of each property would be stored separately).
        ///     That allow operating with individual properties (including atomic operations).
        /// </summary>
        void Hash<TData>()
            where TData : class, new();

        /// <summary>
        ///     Configures cache for storing data in Redis as serialized set of bytes.
        /// </summary>
        void Blob<TData>(SerializeAs? serializer = null, bool cloneNonCachedValue = false, params Type[] entityDependencies);
    }
}