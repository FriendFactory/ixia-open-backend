using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using AssetStoragePathProviding.Settings;
using Common.Models.Attributes;
using Common.Models.Files;
using Frever.Shared.MainDb.Entities;

namespace AssetStoragePathProviding;

internal sealed class AssetFilesConfigs : IAssetFilesConfigs
{
    private static readonly ImmutableDictionary<Type, AssetFilesSetting> Assets = GetAssetConfigs();

    ImmutableDictionary<Type, AssetFilesSetting> IAssetFilesConfigs.GetConfigs()
    {
        return Assets;
    }

    public Type ResolveAssetType(string assetType)
    {
        if (string.IsNullOrWhiteSpace(assetType))
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(assetType));

        //TODO: drop later
        if (assetType.ToLower() == "makeup")
            assetType = nameof(AiMakeUp);

        return Assets.Keys.FirstOrDefault(
            t =>
            {
                if (StringComparer.OrdinalIgnoreCase.Equals(t.Name, assetType))
                    return true;

                if (t.GetCustomAttribute(typeof(EntityAssetAliasAttribute), true) is EntityAssetAliasAttribute attr)
                    return attr.Aliases.Any(a => StringComparer.OrdinalIgnoreCase.Equals(a, assetType));

                return false;
            }
        );
    }

    public bool IsSupported(Type targetType, FileType fileType, Resolution? resolution)
    {
        return Assets.TryGetValue(targetType, out var settings) &&
               settings.FilesSettings.Any(x => x.FileType == fileType && x.Resolution == resolution);
    }

    public FileExtension[] GetExtensions(Type targetType, FileType fileType)
    {
        if (Assets.TryGetValue(targetType, out var settings))
        {
            var targetSettings = settings.FilesSettings.FirstOrDefault(x => x.FileType == fileType);

            if (targetSettings == null)
                throw new Exception($"No extensions for {targetType.Name} {fileType}");

            return targetSettings.Extensions;
        }

        throw new Exception($"No extensions for {targetType.Name} {fileType}");
    }

    public FileSettings[] GetSettings(Type targetType)
    {
        if (Assets.TryGetValue(targetType, out var settings))
            return settings.FilesSettings.ToArray();

        throw new Exception($"No file settings for {targetType.Name}");
    }

    private static ImmutableDictionary<Type, AssetFilesSetting> GetAssetConfigs()
    {
        return new Dictionary<Type, AssetFilesSetting>
               {
                   {typeof(Song), new AssetFilesSetting(GetThreeIconSet(FileExtension.Png, new AudioFileSettings([FileExtension.Mp3])))},
                   {typeof(UserSound), new AssetFilesSetting([new AudioFileSettings(FileExtension.Mp3)])},
                   {
                       typeof(InAppProductDetails), new AssetFilesSetting(
                           [
                               new ThumbnailSettings(FileExtension.Png, Resolution._256x256),
                               new ThumbnailSettings(FileExtension.Png, Resolution._1024x1024)
                           ]
                       )
                   },
                   {typeof(PromotedSong), new AssetFilesSetting([new ThumbnailSettings(FileExtension.Png, Resolution._512x512)])},
                   {
                       typeof(AiMakeUp), new AssetFilesSetting(
                           [
                               new ImageSettings(FileType.MainFile, [FileExtension.Jpeg], null, "Image"),
                               new ThumbnailSettings(FileExtension.Jpeg, Resolution._128x128)
                           ]
                       )
                   }
               }.ToImmutableDictionary();
    }

    private static FileSettings[] GetThreeIconSet(FileExtension extension, FileSettings mainFileInfo = null)
    {
        var result = new List<FileSettings>(4)
                     {
                         new ThumbnailSettings(extension, Resolution._128x128),
                         new ThumbnailSettings(extension, Resolution._256x256),
                         new ThumbnailSettings(extension, Resolution._512x512)
                     };
        if (mainFileInfo != null)
            result.Add(mainFileInfo);

        return result.ToArray();
    }
}