using System;
using System.Linq;
using AuthServerShared;
using Common.Models;
using Common.Models.Database.Interfaces;
using Frever.Shared.MainDb.Entities;

namespace Frever.Client.Core.Utils;

public static class QueryableUtils
{
    public static IQueryable<TAsset> ReadyOnly<TAsset>(this IQueryable<TAsset> source)
        where TAsset : class, IStageable
    {
        return source.Where(a => a.ReadinessId == Readiness.KnownReadinessReady);
    }

    public static IQueryable<TAsset> AccessibleForUser<TAsset>(this IQueryable<TAsset> source, UserInfo user)
        where TAsset : class, IGroupAccessible
    {
        ArgumentNullException.ThrowIfNull(user);

        return source.Where(a => a.GroupId == user.UserMainGroupId || a.GroupId == Constants.PublicAccessGroupId);
    }

    public static IQueryable<TAsset> AccessibleForEveryone<TAsset>(this IQueryable<TAsset> source)
        where TAsset : class, IGroupAccessible
    {
        return source.Where(a => a.GroupId == 1);
    }
}