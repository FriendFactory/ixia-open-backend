using System;
using System.Runtime.CompilerServices;
using Common.Models.Files;

[assembly: InternalsVisibleTo("AssetStorage.PathProviding.Tests")]

namespace AssetStoragePathProviding;

public interface IFileBucketPathService
{
    string GetAssetMainFolder(Type assetType, long id);

    string GetFilePathOnBucket(
        Type assetType,
        long id,
        Platform? platform,
        FileType fileType,
        Resolution? resolution = null,
        FileExtension? extension = null
    );

    string GetPathToTempUploadFile(string uploadId);

    string GetPathToVersionedStorageFile(string key, string version, FileExtension extension);

    string GetVersionedFilePathOnBucket(
        Type assetType,
        long id,
        string version,
        Platform? platform,
        FileType fileType,
        Resolution? resolution = null,
        FileExtension? extension = null
    );

    string GetVersionIndependentAssetPathPrefix(
        Type assetType,
        long id,
        Platform? platform,
        FileType fileType,
        Resolution? resolution = null
    );
}