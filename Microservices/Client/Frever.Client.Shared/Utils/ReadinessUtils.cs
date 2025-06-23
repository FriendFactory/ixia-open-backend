using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using AuthServer.Permissions.Services;
using AuthServerShared;
using Frever.Shared.MainDb.Entities;

namespace Frever.Client.Shared.Utils;

public static class ReadinessUtils
{
    public static IEnumerable<TAsset> ReadyForUserRole<TAsset>(this IEnumerable<TAsset> source, UserInfo user)
        where TAsset : IStageable
    {
        return source.AsQueryable().ReadyForUserRole(user);
    }

    public static IQueryable<TAsset> ReadyForUserRole<TAsset>(this IQueryable<TAsset> source, UserInfo user)
        where TAsset : IStageable
    {
        if (user.AccessScopes.Contains(KnownAccessScopes.ReadinessFull))
            return source;

        var readinessLambda = BuildReadinessLambda<TAsset>(user);

        return source.Where(readinessLambda);
    }

    public static bool ReadyForUserRole(this IStageable asset, UserInfo user)
    {
        if (user.AccessScopes.Contains(KnownAccessScopes.ReadinessFull))
            return true;

        var readinessLambda = BuildReadinessLambda<IStageable>(user).Compile();

        return readinessLambda(asset);
    }


    public static Expression<Func<TAsset, bool>> BuildReadinessLambda<TAsset>(UserInfo user)
        where TAsset : IStageable
    {
        var parameter = Expression.Parameter(typeof(TAsset), "asset");
        var readiness = Expression.Property(parameter, nameof(IStageable.ReadinessId));
        var readinessLambda = Expression.Lambda<Func<TAsset, bool>>(Expression.Equal(readiness, Expression.Constant(2L)), parameter);

        if (user.AccessScopes.Contains(KnownAccessScopes.ReadinessArtists))
        {
            var artistRoleReadiness = Expression.Lambda<Func<TAsset, bool>>(
                Expression.LessThan(readiness, Expression.Constant(10L)),
                parameter
            );

            readinessLambda = Expression.Lambda<Func<TAsset, bool>>(
                Expression.OrElse(readinessLambda.Body, artistRoleReadiness.Body),
                readinessLambda.Parameters[0]
            );
        }

        if (user.CreatorAccessLevels.Length > 0)
        {
            var contains = typeof(Enumerable).GetMethods(BindingFlags.Public | BindingFlags.Static)
                                             .Single(m => m.Name == nameof(Enumerable.Contains) && m.GetParameters().Length == 2)
                                             .MakeGenericMethod(readiness.Type);
            var creatorAccessLevelsRoleReadiness = Expression.Lambda<Func<TAsset, bool>>(
                Expression.Call(
                    contains,
                    Expression.Constant(user.CreatorAccessLevels),
                    Expression.Property(parameter, nameof(IStageable.ReadinessId))
                ),
                readinessLambda.Parameters[0]
            );

            readinessLambda = Expression.Lambda<Func<TAsset, bool>>(
                Expression.OrElse(readinessLambda.Body, creatorAccessLevelsRoleReadiness.Body),
                readinessLambda.Parameters[0]
            );
        }

        return readinessLambda;
    }
}