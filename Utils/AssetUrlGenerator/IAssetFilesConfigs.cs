using System;
using System.Collections.Immutable;
using AssetStoragePathProviding.Settings;
using Common.Models.Files;

namespace AssetStoragePathProviding;

public interface IAssetFilesConfigs
{
    bool IsSupported(Type targetType, FileType fileType, Resolution? resolution);

    FileExtension[] GetExtensions(Type targetType, FileType fileInfoFile);

    FileSettings[] GetSettings(Type targetType);

    ImmutableDictionary<Type, AssetFilesSetting> GetConfigs();

    Type ResolveAssetType(string assetType);
}