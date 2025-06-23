using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using Common.Infrastructure.Utils;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace Frever.Cache.Strategies;

[AttributeUsage(AttributeTargets.Property)]
public class RedisHashIgnoreAttribute : Attribute;

public class RedisHash<TData>
    where TData : class, new()
{
    // ReSharper disable once StaticMemberInGenericType
    private static readonly Dictionary<string, PropertyInfo> Properties;

    private static readonly PropertyInfo DictionaryIndexerProperty =
        typeof(Dictionary<string, object>).GetProperties().First(p => p.GetIndexParameters().Length > 0);

    static RedisHash()
    {
        var props = typeof(TData).GetProperties(BindingFlags.FlattenHierarchy | BindingFlags.Public | BindingFlags.Instance)
                                 .Where(p => p.CanRead && p.CanWrite)
                                 .Where(p => !p.IsDefined(typeof(RedisHashIgnoreAttribute)));
        Properties = new Dictionary<string, PropertyInfo>();

        foreach (var p in props)
            Properties[p.Name] = p;

        if (Properties.Count == 0)
            throw new InvalidOperationException(
                $"Can't cache {typeof(TData).Name} in HashCache since it has no public read/write properties"
            );
    }

    public class Cache(IConnectionMultiplexer redis, ILoggerFactory loggerFactory) : IHashCache<TData>
    {
        private readonly IConnectionMultiplexer _redis = redis ?? throw new ArgumentNullException(nameof(redis));
        private readonly ILogger _redisLogger = loggerFactory.CreateLogger("Frever.Redis");

        public async Task<TData> GetOrCache(string key, Func<Task<TData>> get, Predicate<TData> shouldRefresh, TimeSpan expiration)
        {
            if (get == null)
                throw new ArgumentNullException(nameof(get));
            if (shouldRefresh == null)
                throw new ArgumentNullException(nameof(shouldRefresh));
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(key));

            var db = _redis.GetDatabase();

            var values = db.HashGetAll(key);

            async Task<TData> ReadFromSourceAndUpdateCache()
            {
                var obj = await get();
                if (obj == null)
                    return null;

                var entries = ToHashValues(obj);
                db.HashSet(key, entries);
                db.KeyExpire(key, expiration.Spread());

                return obj;
            }

            if (values.Length == 0)
            {
                var obj = await ReadFromSourceAndUpdateCache();
                return obj;
            }

            var result = FromHashValues(values);
            if (shouldRefresh(result))
            {
                var obj = await ReadFromSourceAndUpdateCache();
                return obj;
            }

            return result;
        }

        public async Task<TResult> GetOrCache<TResult>(
            string key,
            Expression<Func<TData, TResult>> selector,
            Func<Task<TData>> get,
            Predicate<TResult> shouldRefresh,
            TimeSpan expiration
        )
        {
            ArgumentNullException.ThrowIfNull(get);
            ArgumentNullException.ThrowIfNull(shouldRefresh);

            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(key));

            var db = _redis.GetDatabase();

            TResult GetFromCache()
            {
                if (selector is LambdaExpression lambda)
                {
                    if (lambda.Body is UnaryExpression || lambda.Body is MemberExpression)
                    {
                        var member = selector.ToMemberName();

                        var hashValue = db.HashGet(key, member);
                        return (TResult) FromRedisValue(typeof(TResult), hashValue);
                    }

                    if (lambda.Body is NewExpression ctor)
                    {
                        var props = ctor.Arguments.OfType<MemberExpression>()
                                        .Select(x => x.Member)
                                        .OfType<PropertyInfo>()
                                        .ToDictionary(x => x.Name);

                        var ignoringProps = props.Values.Where(p => p.IsDefined(typeof(RedisHashIgnoreAttribute))).ToArray();
                        if (ignoringProps.Any())
                            throw new ArgumentException(
                                $"These properties are marked as ignored and can't be used: {string.Join(", ", ignoringProps.Select(p => p.Name))}",
                                nameof(selector)
                            );

                        var propNames = props.Keys.Select(k => (RedisValue) k).ToArray();

                        var redisValues = db.HashGet(key, propNames);

                        var values = new Dictionary<string, object>();
                        for (var i = 0; i < redisValues.Length; i++)
                        {
                            var prop = props[propNames[i].ToString()];
                            values[prop.Name] = redisValues[i].HasValue   ? FromRedisValue(prop.PropertyType, redisValues[i]) :
                                                prop.PropertyType.IsClass ? null : Activator.CreateInstance(prop.PropertyType);
                        }

                        var param = Expression.Parameter(values.GetType(), "x");

                        var args = ctor.Arguments.OfType<MemberExpression>()
                                       .Select(
                                            m => Expression.Convert(
                                                Expression.MakeIndex(
                                                    param,
                                                    DictionaryIndexerProperty,
                                                    new[] {Expression.Constant(m.Member.Name)}
                                                ),
                                                props[m.Member.Name].PropertyType
                                            )
                                        )
                                       .Cast<Expression>()
                                       .ToArray();

                        var factory = Expression.Lambda<Func<Dictionary<string, object>, TResult>>(
                            Expression.New(ctor.Constructor, args),
                            param
                        );

                        var res = factory.Compile()(values);
                        return res;
                    }
                }

                throw new NotSupportedException("Only member access or new {} expression could be used");
            }

            async Task<bool> RefreshCache()
            {
                var obj = await get();
                if (obj == null)
                    return true;

                var entries = ToHashValues(obj);
                db.HashSet(key, entries);
                db.KeyExpire(key, expiration.Spread());

                return false;
            }

            if (!db.KeyExists(key))
            {
                var isEmptyGet = await RefreshCache();
                if (isEmptyGet)
                    return default;
            }

            var result = GetFromCache();

            if (shouldRefresh(result))
            {
                _redisLogger.LogInformation("RedisHash.GetOrCache: deleting cache key {Key} because shouldRefresh returned true", key);
                db.KeyDelete(key);
                var isEmptyGet = await RefreshCache();
                if (isEmptyGet)
                    return default;

                return GetFromCache();
            }

            return result;
        }

        public async Task<List<TData>> GetByKeys(string[] keys)
        {
            // TODO: RedisBatch: Update this method to use batch
            ArgumentNullException.ThrowIfNull(keys);

            var db = _redis.GetDatabase();

            var batch = db.CreateBatch();
            var hashTasks = keys.Select(k => batch.HashGetAllAsync(k));
            batch.Execute();
            var values = await Task.WhenAll(hashTasks);

            var res = new List<TData>();
            for (var i = 0; i < keys.Length; i++)
            {
                var entries = values[i];
                if (entries != null && entries.Any())
                {
                    var obj = FromHashValues(entries);
                    res.Add(obj);
                }
            }

            return res;
        }

        public async Task PutToCache(string key, Func<Task<TData>> get, TimeSpan expiration)
        {
            ArgumentNullException.ThrowIfNull(get);

            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(key));

            var db = _redis.GetDatabase();
            if (await db.KeyExistsAsync(key))
                return;

            var obj = await get();
            if (obj == null)
                return;

            var entries = ToHashValues(obj);
            db.HashSet(key, entries);
            db.KeyExpire(key, expiration.Spread());
        }

        public async Task<Dictionary<TId, TData>> GetOrCacheMany<TId>(
            TId[] ids,
            Func<TId, string> getKey,
            Func<TId[], Task<Dictionary<TId, TData>>> getMissingData,
            TimeSpan expiration
        )
        {
            ArgumentNullException.ThrowIfNull(ids);
            ArgumentNullException.ThrowIfNull(getKey);
            ArgumentNullException.ThrowIfNull(getMissingData);

            var db = _redis.GetDatabase();

            var allKeys = ids.Select(getKey).ToArray();

            var getAllBatch = db.CreateBatch();
            var hashTasks = allKeys.Select(k => getAllBatch.HashGetAllAsync(k)).ToArray();
            getAllBatch.Execute();
            var existingValues = await Task.WhenAll(hashTasks);

            var missingIds = ids.Where((_, index) => (existingValues[index]?.Length ?? 0) == 0).ToArray();

            var missingData = await getMissingData(missingIds);

            var putMissingBatch = db.CreateBatch();
            var putTasks = missingIds.Where(id => missingData.ContainsKey(id))
                                     .Select(id => new {Id = id, Key = getKey(id), Data = missingData[id]})
                                     .Select(
                                          async a =>
                                          {
                                              putMissingBatch.HashSetAsync(a.Key, ToHashValues(a.Data));
                                              putMissingBatch.KeyExpireAsync(a.Key, expiration.Spread());
                                          }
                                      )
                                     .ToArray();
            putMissingBatch.Execute();
            await Task.WhenAll(putTasks);

            var result = new Dictionary<TId, TData>();

            for (var i = 0; i < ids.Length; i++)
            {
                var existing = existingValues[i];
                if (existing != null && existing.Length > 0)
                {
                    var obj = FromHashValues(existing);
                    result[ids[i]] = obj;
                }
                else
                {
                    if (missingData.TryGetValue(ids[i], out var missing))
                        result[ids[i]] = missing;
                }
            }

            return result;
        }

        public Task DeleteFromCache(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(key));

            var db = _redis.GetDatabase();
            _redisLogger.LogInformation("RedisHash.DeleteFromCache {Key}", key);
            db.KeyDelete(key);
            return Task.CompletedTask;
        }

        public Task Increment(string key, Expression<Func<TData, int>> prop, int by)
        {
            if (prop == null)
                throw new ArgumentNullException(nameof(prop));
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(key));

            var propName = prop.ToMemberName();

            var db = _redis.GetDatabase();
            if (db.KeyExists(key))
                db.HashIncrement(key, propName, by);

            return Task.CompletedTask;
        }

        public Task Increment(string key, Expression<Func<TData, long>> prop, int by)
        {
            ArgumentNullException.ThrowIfNull(prop);

            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(key));

            var propName = prop.ToMemberName();

            var db = _redis.GetDatabase();
            if (db.KeyExists(key))
                db.HashIncrement(key, propName, by);

            return Task.CompletedTask;
        }

        public Task SetPropertyValue<TProp>(string key, Expression<Func<TData, TProp>> prop, TProp value)
        {
            ArgumentNullException.ThrowIfNull(prop);

            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(key));

            var db = _redis.GetDatabase();
            if (db.KeyExists(key))
            {
                var propName = prop.ToMemberName();

                if (!Properties.TryGetValue(propName, out var propertyInfo))
                    throw new InvalidOperationException($"Invalid member: {propName} is not a valid public read/write property");

                if (EqualityComparer<TProp>.Default.Equals(value, default))
                    db.HashDelete(key, propName);
                else
                    db.HashSet(key, propName, ToRedisValue(propertyInfo.PropertyType, value));
            }

            return Task.CompletedTask;
        }

        private TData FromHashValues(HashEntry[] entries)
        {
            ArgumentNullException.ThrowIfNull(entries);

            var result = new TData();

            foreach (var e in entries)
                if (Properties.TryGetValue(e.Name, out var prop))
                {
                    var value = FromRedisValue(prop.PropertyType, e.Value);
                    prop.SetValue(result, value);
                }

            return result;
        }

        private HashEntry[] ToHashValues(TData obj)
        {
            ArgumentNullException.ThrowIfNull(obj);

            var result = new List<HashEntry>();

            foreach (var prop in Properties.Values)
            {
                var value = prop.GetValue(obj);
                if (value != null)
                    result.Add(new HashEntry(prop.Name, ToRedisValue(prop.PropertyType, value)));
            }

            return result.ToArray();
        }

        private static RedisValue ToRedisValue(Type type, object value)
        {
            if (type == typeof(int))
                return (int) value;
            if (type == typeof(int?))
                return (int?) value;
            if (type == typeof(long))
                return (long) value;
            if (type == typeof(long?))
                return (long?) value;
            if (type == typeof(string))
                return (string) value;

            return value.ToRedisValue();
        }

        private static object FromRedisValue(Type type, RedisValue value)
        {
            if (type == typeof(int))
                return (int) value;
            if (type == typeof(int?))
                return (int?) value;
            if (type == typeof(long))
                return (long) value;
            if (type == typeof(long?))
                return (long?) value;
            if (type == typeof(string))
                return (string) value;

            return value.FromValue(type);
        }
    }
}