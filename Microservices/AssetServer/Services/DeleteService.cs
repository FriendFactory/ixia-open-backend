using System;
using System.Threading.Tasks;
using AssetServer.Services.Storage;
using AssetStoragePathProviding;
using AuthServer.Permissions.Services;
using Common.Infrastructure;

namespace AssetServer.Services;

internal class DeleteService(
    IStorageService storageService,
    IFileBucketPathService pathService,
    IUserPermissionService userPermissionService
) : IDeleteService
{
    public async Task DeleteAsset(Type assetType, long id)
    {
        var isArtist = await userPermissionService.IsCurrentUserEmployee();
        if(!isArtist)
            throw AppErrorWithStatusCodeException.NotArtist();

        var assetFilesDirectory = pathService.GetAssetMainFolder(assetType, id);

        await storageService.DeleteDirectoryWithAllFiles(assetFilesDirectory);
    }
}