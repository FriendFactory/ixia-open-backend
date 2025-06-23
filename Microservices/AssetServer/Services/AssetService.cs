using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using AssetServer.Models;
using AssetServer.Services.Storage;
using AssetStoragePathProviding;
using Common.Models.Files;
using Microsoft.Extensions.Logging;

namespace AssetServer.Services;

internal sealed class AssetService : IAssetService
{
    private readonly IAssetAccessService _assetAccessService;
    private readonly IAssetFilesConfigs _assetFilesConfigs;
    private readonly ICloudFrontService _cloudFrontService;
    private readonly IFileBucketPathService _fileBucketPathService;

    private readonly ILogger _log;
    private readonly IPermissionService _permissionService;
    private readonly IStorageService _storageService;

    public AssetService(
        ILogger<AssetService> logger,
        IAssetFilesConfigs assetFilesConfigs,
        IFileBucketPathService fileBucketPathService,
        IAssetAccessService assetAccessService,
        IStorageService storageService,
        IPermissionService permissionService,
        ICloudFrontService cloudFrontService
    )
    {
        _log = logger ?? throw new ArgumentNullException(nameof(logger));
        _assetFilesConfigs = assetFilesConfigs ?? throw new ArgumentNullException(nameof(assetFilesConfigs));
        _fileBucketPathService = fileBucketPathService ?? throw new ArgumentNullException(nameof(fileBucketPathService));
        _assetAccessService = assetAccessService ?? throw new ArgumentNullException(nameof(assetAccessService));
        _storageService = storageService ?? throw new ArgumentNullException(nameof(storageService));
        _permissionService = permissionService ?? throw new ArgumentNullException(nameof(permissionService));
        _cloudFrontService = cloudFrontService ?? throw new ArgumentNullException(nameof(cloudFrontService));
    }

    public async Task<ServiceResult<string>> GetCdnFileUrl(
        string assetTypeName,
        long id,
        string version,
        Platform platform,
        FileType fileType,
        Resolution? resolution = null
    )
    {
        var assetType = _assetFilesConfigs.ResolveAssetType(assetTypeName);
        if (assetType == null)
            throw new ArgumentException("Unsupported asset type");

        var result = await GetS3FilePath(
                         assetType,
                         id,
                         version,
                         platform,
                         fileType,
                         resolution
                     );

        if (result.IsError)
            return new ServiceResult<string>(result.ErrorMessage, result.StatusCode);

        var cloudFrontUrl = _cloudFrontService.CreateCdnUrl(result.Data.Path);

        if (IsFileProtected(assetType, fileType) || result.Data.NeedSignUrl)
            cloudFrontUrl = await _cloudFrontService.SignUrl(cloudFrontUrl);

        return new ServiceResult<string>(cloudFrontUrl);
    }

    public async Task<ServiceResult<string>> GetCdnStorageFileUrl(string version, string key, FileExtension? extension)
    {
        if (string.IsNullOrWhiteSpace(version))
            throw new ArgumentNullException(nameof(version));
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentNullException(nameof(key));

        var path = _fileBucketPathService.GetPathToVersionedStorageFile(key, version, extension ?? FileExtension.Png);

        var fileInfo = await _storageService.GetFileInfo(path);
        if (!fileInfo.IsFileExists)
            return new ServiceResult<string>($"StorageFile {key}/{version} not found", HttpStatusCode.NotFound);

        var cloudFrontUrl = _cloudFrontService.CreateCdnUrl(fileInfo.Key);

        var result = await _cloudFrontService.SignUrl(cloudFrontUrl);

        return new ServiceResult<string>(result);
    }

    private bool IsFileProtected(Type assetType, FileType fileType)
    {
        return _permissionService.IsFileProtected(assetType, fileType);
    }

    /// <summary>
    ///     For <paramref name="assetType" /> with given <paramref name="id" /> returns a path to file stored on S3.
    ///     If user has no access to file method returns error result.
    /// </summary>
    private async Task<ServiceResult<S3Path>> GetS3FilePath(
        Type assetType,
        long id,
        string version,
        Platform platform,
        FileType fileType,
        Resolution? resolution = null
    )
    {
        _log.LogDebug(
            "Getting S3 file path for {AssetTypeName} ID={Id} ({Platform} {FileType} {Resolution})",
            assetType.Name,
            id,
            platform,
            fileType,
            resolution
        );

        var isFileSupported = _assetFilesConfigs.IsSupported(assetType, fileType, resolution);

        if (!isFileSupported)
        {
            _log.LogWarning("File type {FileType} {Resolution} is not supported for {AssetTypeName}", fileType, resolution, assetType.Name);

            return new ServiceResult<S3Path>(
                $"Model:{assetType.Name} does not support: {fileType} {resolution}.",
                HttpStatusCode.BadRequest
            );
        }

        foreach (var src in EnumeratePossibleFileSources(
                     assetType,
                     id,
                     version,
                     platform,
                     fileType,
                     resolution
                 ))
        {
            var fileInfo = await _storageService.GetFileInfo(src.Path);

            if (fileInfo.IsFileExists)
            {
                _log.LogDebug("Versioned file {SrcPath} exists", src.Path);
                if (fileType == FileType.Thumbnail || await _permissionService.HasPermissions(assetType, id, fileInfo.TagInfo))
                    return new ServiceResult<S3Path>(new S3Path {Path = fileInfo.Key, NeedSignUrl = src.NeedSignUrl});

                _log.LogWarning($"Access to versioned {fileInfo} forbidden");

                return new ServiceResult<S3Path>("Not accessible", HttpStatusCode.Forbidden);
            }
        }

        return await FallbackToFileSource(
                   assetType,
                   id,
                   platform,
                   fileType,
                   resolution
               );
    }

    private IEnumerable<S3Path> EnumeratePossibleFileSources(
        Type assetType,
        long id,
        string version,
        Platform? platform,
        FileType fileType,
        Resolution? resolution = null
    )
    {
        // Versioned path with platform
        yield return new S3Path
                     {
                         Path = _fileBucketPathService.GetVersionedFilePathOnBucket(
                             assetType,
                             id,
                             version,
                             platform,
                             fileType,
                             resolution
                         )
                     };

        // Versioned path without platform
        yield return new S3Path
                     {
                         Path = _fileBucketPathService.GetVersionedFilePathOnBucket(
                             assetType,
                             id,
                             version,
                             null,
                             fileType,
                             resolution
                         )
                     };

        // Old style path (without version)
        yield return new S3Path
                     {
                         Path = _fileBucketPathService.GetFilePathOnBucket(
                             assetType,
                             id,
                             platform,
                             fileType,
                             resolution
                         )
                     };
    }


    /// <summary>
    ///     Get file content using FileInfo.Source of corresponding file.
    ///     Note that source file is assumed to be always accessible to user.
    /// </summary>
    private async Task<ServiceResult<S3Path>> FallbackToFileSource(
        Type assetType,
        long id,
        Platform platform,
        FileType fileType,
        Resolution? resolution = null
    )
    {
        var fileInfo = await _assetAccessService.GetAssetFileFromDb(
                           assetType,
                           id,
                           platform,
                           fileType,
                           resolution
                       );

        if (fileInfo?.Source == null)
        {
            _log.LogWarning(
                "File {FileType} {Resolution} for {AssetTypeName} {Id} has no fallback source",
                fileType,
                resolution,
                assetType.Name,
                id
            );

            return new ServiceResult<S3Path>("File is not found", HttpStatusCode.NotFound);
        }

        if (!string.IsNullOrWhiteSpace(fileInfo.Source.UploadId))
        {
            _log.LogDebug("Temporary upload found: {SourceUploadId}", fileInfo.Source.UploadId);

            var tempUploadFile = _fileBucketPathService.GetPathToTempUploadFile(fileInfo.Source.UploadId);

            return new ServiceResult<S3Path>(new S3Path {Path = tempUploadFile, NeedSignUrl = true});
        }

        if (fileInfo.Source.CopyFrom != null)
        {
            var copyFromAssetType = assetType;

            var sourceAssetPath = string.IsNullOrWhiteSpace(fileInfo.Source.CopyFrom.Version)
                                      ? _fileBucketPathService.GetFilePathOnBucket(
                                          copyFromAssetType,
                                          fileInfo.Source.CopyFrom.Id,
                                          platform,
                                          fileType,
                                          resolution,
                                          fileInfo.Extension
                                      )
                                      : _fileBucketPathService.GetVersionedFilePathOnBucket(
                                          copyFromAssetType,
                                          fileInfo.Source.CopyFrom.Id,
                                          fileInfo.Source.CopyFrom.Version,
                                          platform,
                                          fileType,
                                          resolution,
                                          fileInfo.Extension
                                      );

            return new ServiceResult<S3Path>(new S3Path {Path = sourceAssetPath});
        }

        return new ServiceResult<S3Path>("Not found", HttpStatusCode.NotFound);
    }
}

public class S3Path
{
    public string Path { get; set; }

    public bool NeedSignUrl { get; set; }
}