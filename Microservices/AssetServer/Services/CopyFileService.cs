using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AssetServer.Services.Storage;
using AssetStoragePathProviding;
using AuthServer.Permissions.Services;
using Common.Models.Files;

namespace AssetServer.Services;

internal class CopyFileService(
    IStorageService storageService,
    IFileBucketPathService fileBucketPathService,
    IUserPermissionService userPermissionService
) : FileDeployingServiceBase, ICopyFileService
{
    private readonly IUserPermissionService _userPermissionService = userPermissionService ?? throw new ArgumentNullException(nameof(userPermissionService));

    public async Task<CopyFileResp> CopyFile(
        Type assetType,
        long sourceAssetId,
        FileType fileType,
        Platform? platform,
        Resolution? resolution,
        long destAssetId,
        Dictionary<string, string> tags = null
    )
    {
        await _userPermissionService.EnsureCurrentUserActive();

        var sourcePath = fileBucketPathService.GetFilePathOnBucket(
            assetType,
            sourceAssetId,
            platform,
            fileType,
            resolution
        );
        var destPath = fileBucketPathService.GetFilePathOnBucket(
            assetType,
            destAssetId,
            platform,
            fileType,
            resolution
        );
        var copiedFileVersion = await storageService.CopyFileAsync(sourcePath, destPath, tags);

        long? sizeKb = null;

        if (NeedSendFileSizeInResponse(assetType, fileType))
            sizeKb = await storageService.GetFileSizeKb(destPath);

        return new CopyFileResp {Version = copiedFileVersion, SizeKb = sizeKb};
    }
}