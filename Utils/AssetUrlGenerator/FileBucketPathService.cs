using System;
using System.Collections.Immutable;
using System.Linq;
using System.Text.RegularExpressions;
using AssetStoragePathProviding.Settings;
using Common.Models;
using Common.Models.Files;
using static System.String;

namespace AssetStoragePathProviding;

//Examples :
//{AssetMainFolder}
//Asset/assetType/id
//{AssetMainFolder}/{id}/{platformPrefix}{fileName}{fileExtension}
//{AssetMainFolder}/{id}/Thumbnail.png
//{AssetMainFolder}/{id}/iOS/{fileName}.{fileExtension}
internal class FileBucketPathService(IAssetFilesConfigs configuration) : IFileBucketPathService
{
    private readonly ImmutableDictionary<Type, AssetFilesSetting> _assetsSettings = configuration.GetConfigs();

    public string GetAssetMainFolder(Type assetType, long id)
    {
        if (!_assetsSettings.TryGetValue(assetType, out _))
            throw new InvalidOperationException($"Type {assetType.Name} is not supported. Check {nameof(FileBucketPathService)} config.");

        var result = GetAssetMainFolderPath(assetType, id.ToString());

        return result;
    }

    public string GetPathToTempUploadFile(string uploadId)
    {
        return $"{Constants.TemporaryFolder}/{uploadId}";
    }

    public string GetPathToVersionedStorageFile(string key, string version, FileExtension extension)
    {
        var regex = new Regex(@"^[a-zA-Z0-9_/]+$");
        if (!regex.IsMatch(key))
            throw new InvalidOperationException($"Path {key} contains invalid characters");

        return $"{Constants.FilesFolder}/{key}/{version}/content.{extension.ToString().ToLower()}";
    }

    public string GetFilePathOnBucket(
        Type assetType,
        long id,
        Platform? platform,
        FileType fileType,
        Resolution? resolution = null,
        FileExtension? extension = null
    )
    {
        var assetSettings = GetAssetSettings(assetType);

        var fileSettings = GetFileSettings(assetSettings, fileType, resolution, assetType.Name);

        var fileName = fileSettings.Name;

        var platformPrefix = GetPlatformPrefix(assetSettings, platform, fileType);

        var fileExtension = GetExtension(fileSettings, extension);

        var mainFolder = GetAssetMainFolderPath(assetType, id.ToString());

        var path = $"{mainFolder}/{platformPrefix}{fileName}{fileExtension}";

        return path;
    }

    public string GetVersionedFilePathOnBucket(
        Type assetType,
        long id,
        string version,
        Platform? platform,
        FileType fileType,
        Resolution? resolution = null,
        FileExtension? extension = null
    )
    {
        var assetSettings = GetAssetSettings(assetType);

        var fileSettings = GetFileSettings(assetSettings, fileType, resolution, assetType.Name);

        var path = GetVersionIndependentAssetPathPrefix(
            assetType,
            id,
            platform,
            fileType,
            resolution
        );

        var parts = new[] {path, version, "content" + GetExtension(fileSettings, extension)};

        return Join("/", parts);
    }

    public string GetVersionIndependentAssetPathPrefix(
        Type assetType,
        long id,
        Platform? platform,
        FileType fileType,
        Resolution? resolution = null
    )
    {
        var parts = fileType == FileType.MainFile
                        ? [GetAssetMainFolderPath(assetType, id.ToString()).TrimEnd('/'), "Main", platform?.ToString() ?? Empty]
                        : new[]
                          {
                              GetAssetMainFolderPath(assetType, id.ToString()).TrimEnd('/'),
                              "Thumbnail",
                              resolution?.ToString().Trim('_') ?? Empty
                          };

        var path = Join("/", parts.Where(s => !IsNullOrWhiteSpace(s)));

        return path;
    }

    private static string GetPlatformPrefix(AssetFilesSetting filesSettings, Platform? platform, FileType fileType)
    {
        if (fileType == FileType.MainFile && filesSettings.IsPlatformDependent)
        {
            if (platform == null || platform == Platform.Undefined)
                platform = Platform.iOS;

            return $"{platform}/";
        }

        return Empty;
    }

    private string GetExtension(FileSettings setting, FileExtension? extension)
    {
        if (setting.Extensions.Length == 1 && setting.Extensions[0] != FileExtension.Empty)
            return "." + setting.Extensions[0].ToString().ToLower();

        if (extension == null)
            return Empty;

        if (!(bool) setting.Extensions?.Contains(extension.Value))
            throw new InvalidOperationException($"Extension {extension} is not supported for {setting.FileType}");

        if (extension.Value == FileExtension.Empty)
            return Empty;

        return "." + extension.Value.ToString().ToLower();
    }

    private static string GetAssetMainFolderPath(Type assetType, string id)
    {
        return $"Assets/{assetType.Name}/{id}";
    }

    private AssetFilesSetting GetAssetSettings(Type assetType)
    {
        ArgumentNullException.ThrowIfNull(assetType);

        if (!_assetsSettings.TryGetValue(assetType, out var assetSettings))
            throw new InvalidOperationException($"Type {assetType.Name} is not supported. Check {nameof(FileBucketPathService)} config.");

        return assetSettings;
    }

    private FileSettings GetFileSettings(AssetFilesSetting assetFilesSettings, FileType fileType, Resolution? resolution, string typeName)
    {
        var fileSettings = assetFilesSettings.GetSettings(fileType, resolution);

        if (fileSettings == null)
            throw new InvalidOperationException($"Type {typeName} does not support {fileType} {resolution}");

        return fileSettings;
    }
}