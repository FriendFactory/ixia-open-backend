using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using Common.Infrastructure;
using Common.Models;
using Frever.Cache;
using Frever.Shared.MainDb;
using Frever.Shared.MainDb.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Frever.Videos.Shared.GeoClusters;

public interface IGeoClusterProvider
{
    Task<GeoCluster[]> All();

    Task<GeoCluster> One(long geoClusterId);

    bool ShouldIncludeEntity(GeoCluster geoCluster, string entityCountry, string entityLanguage);

    bool MatchUser(GeoCluster geoCluster, string[] userCountry, string[] userLanguage);

    Task<GeoCluster> DetectGeoClusterForGroup(long groupId);

    Task<GeoCluster[]> DetectGeoClustersForGroup(long groupId);

    Task<GeoCluster[]> DetectGeoClusters(string countryIso3, string languageIso3);

    Task<GeoCluster> UniverseGeoCluster();

    Expression<Func<TEntity, bool>> BuildGeoClusterVideoMatchPredicate<TEntity>(
        GeoCluster geoCluster,
        Expression<Func<TEntity, string>> countryExpr,
        Expression<Func<TEntity, string>> languageExpr
    )
        where TEntity : class;

    Expression<Func<TEntity, bool>> BuildGeoClusterGroupMatchPredicate<TEntity>(
        GeoCluster geoCluster,
        Expression<Func<TEntity, string>> countryExpr,
        Expression<Func<TEntity, string>> languageExpr
    )
        where TEntity : class;
}

public class GeoClusterProvider(IBlobCache<GeoCluster[]> cache, IReadDb mainDb) : IGeoClusterProvider
{
    public static readonly string[] IncludeAll = ["*"];

    private static readonly string[] Empty = [];
    private static readonly string CacheKey = "geo-cluster".FreverAssetCacheKey();

    private static readonly GeoCluster IncludeAllGeoCluster = new()
                                                              {
                                                                  Id = -1,
                                                                  Priority = -100000,
                                                                  Title = "Virtual geo-cluster including all videos",
                                                                  IsActive = true,
                                                                  ExcludeVideoFromCountry = Empty,
                                                                  ExcludeVideoWithLanguage = Empty,
                                                                  IncludeVideoFromCountry = IncludeAll,
                                                                  IncludeVideoWithLanguage = IncludeAll,
                                                                  HideForUserFromCountry = Empty,
                                                                  HideForUserWithLanguage = Empty,
                                                                  ShowForUserWithLanguage = IncludeAll,
                                                                  ShowToUserFromCountry = IncludeAll
                                                              };


    private readonly IBlobCache<GeoCluster[]> _cache = cache ?? throw new ArgumentNullException(nameof(cache));

    private readonly Dictionary<long, GeoCluster[]> _groupGeoClusterLocalCache = new();
    private readonly IReadDb _mainDb = mainDb ?? throw new ArgumentNullException(nameof(mainDb));

    public async Task<GeoCluster[]> All()
    {
        return await _cache.GetOrCache(CacheKey, ReadFromDb, TimeSpan.FromHours(6));
    }

    public async Task<GeoCluster> One(long geoClusterId)
    {
        var all = await All();
        var result = all.FirstOrDefault(e => e.Id == geoClusterId);
        if (result == null)
            throw AppErrorWithStatusCodeException.BadRequest("Invalid geo cluster ID", "InvalidGeoClusterId");

        return result;
    }

    public bool ShouldIncludeEntity(GeoCluster geoCluster, string entityCountry, string entityLanguage)
    {
        ArgumentNullException.ThrowIfNull(geoCluster);

        if (string.IsNullOrWhiteSpace(entityCountry))
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(entityCountry));
        if (string.IsNullOrWhiteSpace(entityLanguage))
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(entityLanguage));

        if (entityCountry.Length < 3)
            throw new ArgumentException("Please use Country ISO-3 code", nameof(entityCountry));

        if (entityLanguage.Length < 3)
            throw new ArgumentException("Please use Language ISO-3 code", nameof(entityLanguage));

        return geoCluster.IncludeVideoFromCountry.HasAtLeastOneMatch(entityCountry) &&
               geoCluster.IncludeVideoWithLanguage.HasAtLeastOneMatch(entityLanguage) &&
               !geoCluster.ExcludeVideoFromCountry.HasAtLeastOneMatch(entityCountry) &&
               !geoCluster.ExcludeVideoWithLanguage.HasAtLeastOneMatch(entityLanguage);
    }

    public bool MatchUser(GeoCluster geoCluster, string[] userCountry, string[] userLanguage)
    {
        ArgumentNullException.ThrowIfNull(geoCluster);

        return geoCluster.ShowToUserFromCountry.HasAtLeastOneMatch(userCountry) &&
               geoCluster.ShowForUserWithLanguage.HasAtLeastOneMatch(userLanguage) &&
               !geoCluster.HideForUserFromCountry.HasAtLeastOneMatch(userCountry) &&
               !geoCluster.HideForUserWithLanguage.HasAtLeastOneMatch(userLanguage);
    }

    public async Task<GeoCluster[]> DetectGeoClustersForGroup(long groupId)
    {
        if (_groupGeoClusterLocalCache.TryGetValue(groupId, out var cluster))
            return cluster;

        var group = await _mainDb.Group.Include(g => g.TaxationCountry)
                                 .Join(
                                      _mainDb.Language,
                                      g => g.DefaultLanguageId,
                                      l => l.Id,
                                      (g, l) => new {g.Id, CountryIso3 = g.TaxationCountry.ISOName, LanguageIso3 = l.IsoCode}
                                  )
                                 .FirstOrDefaultAsync(g => g.Id == groupId);

        if (group == null)
            throw AppErrorWithStatusCodeException.NotAuthorized("Not found", "GroupNotFound");

        var c = await DetectGeoClusters(group.CountryIso3, group.LanguageIso3);

        _groupGeoClusterLocalCache[groupId] = c;

        return c;
    }

    public async Task<GeoCluster[]> DetectGeoClusters(string countryIso3, string languageIso3)
    {
        var allGeoClusters = await All();
        if (allGeoClusters.Length == 0)
            throw new InvalidOperationException("At least one catchall geo-cluster must be defined");

        var countries = new[] {countryIso3 ?? Constants.FallbackCountryCode};
        var languages = new[] {languageIso3 ?? Constants.FallbackLanguageCode};

        var clusters = allGeoClusters.Where(c => MatchUser(c, countries, languages)).OrderByDescending(c => c.Priority).ToArray();
        if (!clusters.Any())
            clusters = allGeoClusters.OrderBy(g => g.Priority).Take(1).ToArray();

        return clusters;
    }

    public async Task<GeoCluster> UniverseGeoCluster()
    {
        var all = await All();
        return all.First(gc => gc.Id == IncludeAllGeoCluster.Id);
    }

    public Expression<Func<TEntity, bool>> BuildGeoClusterVideoMatchPredicate<TEntity>(
        GeoCluster geoCluster,
        Expression<Func<TEntity, string>> countryExpr,
        Expression<Func<TEntity, string>> languageExpr
    )
        where TEntity : class
    {
        ArgumentNullException.ThrowIfNull(geoCluster);
        ArgumentNullException.ThrowIfNull(languageExpr);

        var containsMethod = typeof(Enumerable).GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)
                                               .Where(m => m.Name == "Contains")
                                               .Where(m => m.ReturnType == typeof(bool))
                                               .First(m => m.GetParameters().Length == 2)
                                               .MakeGenericMethod(typeof(string));

        var parameter = Expression.Parameter(typeof(TEntity));
        Expression result = Expression.Constant(true);

        if (!geoCluster.IncludeVideoFromCountry.Any() || !geoCluster.ShowToUserFromCountry.Any() ||
            !geoCluster.IncludeVideoWithLanguage.Any() || !geoCluster.ShowForUserWithLanguage.Any())
            return Expression.Lambda<Func<TEntity, bool>>(Expression.Constant(false), true, parameter);

        var countryMember = countryExpr.GetPropertyAccess();
        var languageMember = languageExpr.GetPropertyAccess();

        MethodCallExpression BuildArrayContains(string[] array, PropertyInfo propertyInfo)
        {
            array ??= [];

            return Expression.Call(
                containsMethod,
                Expression.Constant(array, typeof(string[])),
                Expression.MakeMemberAccess(parameter, propertyInfo)
            );
        }

        if (!geoCluster.IncludeVideoFromCountry.IsNullOrEmpty() && !geoCluster.IncludeVideoFromCountry.ContainsWildcard())
            result = Expression.And(result, BuildArrayContains(geoCluster.IncludeVideoFromCountry, countryMember));

        if (!geoCluster.ExcludeVideoFromCountry.IsNullOrEmpty())
            result = Expression.And(result, Expression.Not(BuildArrayContains(geoCluster.ExcludeVideoFromCountry, countryMember)));

        if (!geoCluster.IncludeVideoWithLanguage.IsNullOrEmpty() && !geoCluster.IncludeVideoWithLanguage.ContainsWildcard())
            result = Expression.And(result, BuildArrayContains(geoCluster.IncludeVideoWithLanguage, languageMember));

        if (!geoCluster.ExcludeVideoWithLanguage.IsNullOrEmpty())
            result = Expression.And(result, Expression.Not(BuildArrayContains(geoCluster.ExcludeVideoWithLanguage, languageMember)));

        return Expression.Lambda<Func<TEntity, bool>>(result, true, parameter);
    }


    public Expression<Func<TEntity, bool>> BuildGeoClusterGroupMatchPredicate<TEntity>(
        GeoCluster geoCluster,
        Expression<Func<TEntity, string>> countryExpr,
        Expression<Func<TEntity, string>> languageExpr
    )
        where TEntity : class
    {
        ArgumentNullException.ThrowIfNull(geoCluster);
        ArgumentNullException.ThrowIfNull(languageExpr);

        var containsMethod = typeof(Enumerable).GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)
                                               .Where(m => m.Name == "Contains")
                                               .Where(m => m.ReturnType == typeof(bool))
                                               .First(m => m.GetParameters().Length == 2)
                                               .MakeGenericMethod(typeof(string));

        var parameter = Expression.Parameter(typeof(TEntity));
        Expression result = Expression.Constant(true);

        if (!geoCluster.ShowToUserFromCountry.Any() || !geoCluster.ShowForUserWithLanguage.Any())
            return Expression.Lambda<Func<TEntity, bool>>(Expression.Constant(false), true, parameter);

        var countryMember = countryExpr.GetPropertyAccess();
        var languageMember = languageExpr.GetPropertyAccess();

        MethodCallExpression BuildArrayContains(string[] array, PropertyInfo propertyInfo)
        {
            array ??= [];

            return Expression.Call(
                containsMethod,
                Expression.Constant(array, typeof(string[])),
                Expression.MakeMemberAccess(parameter, propertyInfo)
            );
        }

        if (!geoCluster.ShowToUserFromCountry.IsNullOrEmpty() && !geoCluster.ShowToUserFromCountry.ContainsWildcard())
            result = Expression.And(result, BuildArrayContains(geoCluster.ShowToUserFromCountry, countryMember));

        if (!geoCluster.ShowForUserWithLanguage.IsNullOrEmpty() && !geoCluster.ShowForUserWithLanguage.ContainsWildcard())
            result = Expression.And(result, BuildArrayContains(geoCluster.ShowForUserWithLanguage, languageMember));

        if (!geoCluster.HideForUserFromCountry.IsNullOrEmpty())
            result = Expression.And(result, Expression.Not(BuildArrayContains(geoCluster.HideForUserFromCountry, countryMember)));

        if (!geoCluster.HideForUserWithLanguage.IsNullOrEmpty())
            result = Expression.And(result, Expression.Not(BuildArrayContains(geoCluster.HideForUserWithLanguage, languageMember)));

        return Expression.Lambda<Func<TEntity, bool>>(result, true, parameter);
    }

    public async Task<GeoCluster> DetectGeoClusterForGroup(long groupId)
    {
        return (await DetectGeoClustersForGroup(groupId)).FirstOrDefault();
    }

    private async Task<GeoCluster[]> ReadFromDb()
    {
        var allStored = await _mainDb.GeoCluster.Where(c => c.IsActive).OrderByDescending(c => c.Priority).AsNoTracking().ToListAsync();

        allStored.Add(IncludeAllGeoCluster);

        return allStored.ToArray();
    }
}

internal static class StringSetExtensions
{
    public static bool HasAtLeastOneMatch(this string[] collection, string[] values)
    {
        if (collection == null || collection.Length == 0)
            return false;
        if (values == null || values.Length == 0)
            return false;

        if (collection.ContainsWildcard())
            return true;

        return collection.Any(e => values.Any(v => StringComparer.OrdinalIgnoreCase.Equals(e, v)));
    }

    public static bool HasAtLeastOneMatch(this string[] collection, string value)
    {
        if (collection == null || collection.Length == 0)
            return false;
        if (string.IsNullOrWhiteSpace(value))
            return false;

        if (collection.ContainsWildcard())
            return true;

        return collection.Any(e => StringComparer.OrdinalIgnoreCase.Equals(e, value));
    }

    public static bool ContainsWildcard(this string[] collection)
    {
        if (collection == null || collection.Length == 0)
            return false;

        return collection.Any(e => StringComparer.OrdinalIgnoreCase.Equals(e, Constants.Wildcard));
    }

    public static bool IsNullOrEmpty(this IEnumerable<string> source)
    {
        return source == null || !source.Any();
    }
}