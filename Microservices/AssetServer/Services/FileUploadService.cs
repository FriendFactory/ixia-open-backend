using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AssetServer.Models;
using AssetServer.Services.Storage;
using AssetStoragePathProviding;
using AuthServer.Permissions.Services;
using Common.Models.Files;

namespace AssetServer.Services;

internal sealed class FileUploadService(
    IStorageService storageService,
    IFileBucketPathService fileBucketPathService,
    IUserPermissionService userPermissionService
) : FileDeployingServiceBase, IFileUploadService
{
    private readonly IUserPermissionService _userPermissionService = userPermissionService ?? throw new ArgumentNullException(nameof(userPermissionService));


    public InitConversionModel GetTemporaryConversionUrl(string fileExtension)
    {
        var conversionInfo = FileConversionHelper.GetConversionInfoFromExtension(fileExtension);
        if (conversionInfo.MediaType == MediaType.Unsupported)
            throw new ArgumentException("File format conversion is not supported");

        if (!conversionInfo.NeedsConversion)
        {
            var uploadFile = $"{Guid.NewGuid().ToString()}{conversionInfo.OriginalFileExtension}";
            var uploadFilePath = fileBucketPathService.GetPathToTempUploadFile(uploadFile);

            return new InitConversionModel
                   {
                       UploadUrl = storageService.GetTempUrlForUploadingFile(uploadFilePath),
                       UploadId = uploadFile,
                       ConvertedFileUrl = storageService.GetTempUrlForReadingFile(uploadFilePath),
                       CheckFileConvertedUrl = storageService.GetTempUrlForCheckingFileExistence(uploadFilePath),
                       OriginalFileExtension = conversionInfo.OriginalFileExtension,
                       TargetFileExtension = conversionInfo.TargetFileExtension
                   };
        }

        var uploadFileName = $"{Guid.NewGuid().ToString()}{conversionInfo.OriginalFileExtension}{FileConversionHelper.ConversionSuffix}";

        var bucketLocalPath = fileBucketPathService.GetPathToTempUploadFile(uploadFileName);
        var targetFileName = FileConversionHelper.GetTargetFilePath(uploadFileName);
        var targetFilePath = fileBucketPathService.GetPathToTempUploadFile(targetFileName);

        return new InitConversionModel
               {
                   UploadUrl = storageService.GetTempUrlForUploadingFile(bucketLocalPath),
                   UploadId = targetFileName,
                   ConvertedFileUrl = storageService.GetTempUrlForReadingFile(targetFilePath),
                   CheckFileConvertedUrl = storageService.GetTempUrlForCheckingFileExistence(targetFilePath),
                   OriginalFileExtension = conversionInfo.OriginalFileExtension,
                   TargetFileExtension = conversionInfo.TargetFileExtension
               };
    }

    public async Task<SavePreloadedFileResp> SavePreloadedFile(
        Type assetType,
        long id,
        FileType fileType,
        Platform? platform,
        Resolution? resolution,
        string uploadId,
        Dictionary<string, string> tags = null
    )
    {
        await _userPermissionService.EnsureCurrentUserActive();

        var preloadedLocalPath = fileBucketPathService.GetPathToTempUploadFile(uploadId);
        var destLocalPath = fileBucketPathService.GetFilePathOnBucket(
            assetType,
            id,
            platform,
            fileType,
            resolution
        );

        var destVersion = await storageService.CopyFileAsync(preloadedLocalPath, destLocalPath, tags);

        long? sizeKb = null;
        if (NeedSendFileSizeInResponse(assetType, fileType))
            sizeKb = await storageService.GetFileSizeKb(destLocalPath);

        // Sergii: Don't delete preloads manually, it would broke sending same upload for multiple assets
        // Auto deletion should be setup on bucket

        // await _storageService.DeleteFileAsync(preloadedLocalPath);

        return new SavePreloadedFileResp {Version = destVersion, SizeKb = sizeKb};
    }

    public InitUploadModel GetTemporaryUploadingUrl()
    {
        var tempUniqueId = Guid.NewGuid().ToString();
        var bucketLocalPath = fileBucketPathService.GetPathToTempUploadFile(tempUniqueId);

        return new InitUploadModel {UploadUrl = storageService.GetTempUrlForUploadingFile(bucketLocalPath), UploadId = tempUniqueId};
    }
}