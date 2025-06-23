using System;
using System.Collections.Generic;
using System.Linq;
using AssetServer.Models.AssetFileSettingsDTO;
using AssetStoragePathProviding;
using AssetStoragePathProviding.Settings;

namespace AssetServer.Services;

internal sealed class ConfigsService(IAssetFilesConfigs configs) : IConfigsService
{
    public List<AssetFilesSettings> GetConfigsInfo()
    {
        return configs.GetConfigs().Select(x => Map(x.Key, x.Value.FilesSettings.ToArray())).ToList();
    }

    private AssetFilesSettings Map(Type assetType, FileSettings[] filesSettings)
    {
        return new AssetFilesSettings
               {
                   AssetTypeName = assetType.Name,
                   Settings = filesSettings.Select(
                                                x => new FileSettingsDTO
                                                     {
                                                         File = x.FileType,
                                                         Extensions = x.Extensions,
                                                         Resolution = x.Resolution,
                                                         IsPlatformDependent = x.IsPlatformDependent
                                                     }
                                            )
                                           .ToList()
               };
    }
}