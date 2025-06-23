using System;
using System.Collections.Generic;
using Common.Models;
using Frever.Shared.MainDb.Entities;

namespace Frever.AdminService.Core.Services.ModelSettingsProviders;

public class AssetGroupProvider
{
    private readonly Dictionary<Type, bool> _assetsPublicGroupInfo = new() {{typeof(Song), true}, {typeof(UserSound), false}};

    public long GetAssetGroup(Type assetType, long userMainGroupId)
    {
        if (!_assetsPublicGroupInfo.TryGetValue(assetType, out var isPublicGroup))
            isPublicGroup = true;

        var result = userMainGroupId;

        if (isPublicGroup)
            result = Constants.PublicAccessGroupId;

        return result;
    }
}