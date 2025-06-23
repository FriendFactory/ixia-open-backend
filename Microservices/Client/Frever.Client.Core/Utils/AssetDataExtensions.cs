using System;
using System.Collections.Generic;
using System.Linq;
using AuthServer.Permissions.Services;
using AuthServerShared;
using Frever.Client.Core.Utils.Models;
using Frever.Client.Shared.Utils;
using Frever.Shared.MainDb.Entities;

namespace Frever.Client.Core.Utils;

internal static class AssetDataExtensions
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

        var readinessLambda = ReadinessUtils.BuildReadinessLambda<TAsset>(user);

        return source.Where(readinessLambda);
    }

    public static bool ReadyForUserRole(this IStageable asset, UserInfo user)
    {
        if (user.AccessScopes.Contains(KnownAccessScopes.ReadinessFull))
            return true;

        var readinessLambda = ReadinessUtils.BuildReadinessLambda<IStageable>(user).Compile();

        return readinessLambda(asset);
    }

    public static IEnumerable<TAsset> AvailableForUserRole<TAsset>(this IEnumerable<TAsset> source, UserInfo user)
        where TAsset : IStageable, IPublication
    {
        return user.AccessScopes.Contains(KnownAccessScopes.ReadinessFull) ? source : source.Where(CheckAssetAvailability);
    }

    private static bool CheckAssetAvailability<TAsset>(TAsset asset)
        where TAsset : IStageable, IPublication
    {
        var currentDate = DateTime.UtcNow;

        return asset.ReadinessId != 2 || (asset.PublicationDate == null && asset.DepublicationDate == null) ||
               (!asset.DepublicationDate.HasValue && asset.PublicationDate.HasValue && asset.PublicationDate <= currentDate) ||
               (!asset.PublicationDate.HasValue && asset.DepublicationDate.HasValue && asset.DepublicationDate >= currentDate) ||
               (asset.PublicationDate.GetValueOrDefault() <= currentDate && asset.DepublicationDate.GetValueOrDefault() >= currentDate);
    }
}