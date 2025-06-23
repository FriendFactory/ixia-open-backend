using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Amazon.S3;
using Amazon.S3.Model;
using AssetServer.Shared.AssetCopying;
using AuthServer.Permissions.Services;
using Common.Infrastructure.Aws.Crypto;
using Common.Infrastructure.CloudFront;
using Common.Infrastructure.Utils;
using Common.Models;
using Frever.Cache;
using Frever.ClientService.Contract.Common;
using Frever.ClientService.Contract.StorageFiles;
using Frever.Shared.MainDb.Entities;
using Microsoft.EntityFrameworkCore;

namespace Frever.Client.Core.Features.StorageFiles;

public interface IStorageFileService
{
    Task<InitUploadModel> GetTemporaryUploadingUrl(FileExtension? extension);

    Task<StorageFileDto> GetStorageFileByKey(StorageFileShortDto model);

    Task<string> GetCdnStorageFileUrl(string version, string key, FileExtension extension);
}

public class StorageFileService(
    IAmazonS3 s3,
    AssetCopyingOptions options,
    CloudFrontConfiguration cdnOptions,
    IStorageFileRepository repo,
    IBlobCache<StorageFileDto[]> cache,
    IUserPermissionService userPermissionService
) : IStorageFileService
{
    private static readonly Regex KeyRegex = new("^[a-zA-Z0-9_/]+$");

    public async Task<InitUploadModel> GetTemporaryUploadingUrl(FileExtension? extension)
    {
        var extensionString = extension.HasValue ? $".{extension.Value.ToString().ToLowerInvariant()}" : string.Empty;
        var s3Path = $"{Constants.TemporaryFolder}/{Guid.NewGuid().ToString()}{extensionString}";

        return new InitUploadModel {UploadUrl = await GetPreSignedUrl(s3Path)};
    }

    public async Task<StorageFileDto> GetStorageFileByKey(StorageFileShortDto model)
    {
        if (model?.Key == null)
            throw new ArgumentNullException(nameof(model));

        await userPermissionService.EnsureCurrentUserActive();

        var all = await cache.GetOrCache(nameof(StorageFile).FreverCacheKey(), GetDbStorageFiles, TimeSpan.FromDays(3));

        return all.FirstOrDefault(e => e.Key == model.Key);

        Task<StorageFileDto[]> GetDbStorageFiles()
        {
            return repo.GetStorageFiles()
                       .Select(
                            e => new StorageFileDto {Key = e.Key, Version = e.Version, Extension = Enum.Parse<FileExtension>(e.Extension)}
                        )
                       .ToArrayAsync();
        }
    }

    public Task<string> GetCdnStorageFileUrl(string version, string key, FileExtension extension)
    {
        if (string.IsNullOrWhiteSpace(version))
            throw new ArgumentNullException(nameof(version));
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentNullException(nameof(key));

        var path = GetPathToVersionedStorageFile(key, version, extension);

        var url = UriUtils.CombineUri(cdnOptions.CloudFrontHost, path);

        var result = FreverAmazonCloudFrontSigner.SignUrlCanned(
            url,
            cdnOptions.CloudFrontCertKeyPairId,
            DateTime.Now.AddMinutes(cdnOptions.CloudFrontSignedCookieLifetimeMinutes)
        );

        return Task.FromResult(result);
    }

    private static string GetPathToVersionedStorageFile(string key, string version, FileExtension extension)
    {
        if (!KeyRegex.IsMatch(key))
            throw new InvalidOperationException($"Path {key} contains invalid characters");

        return $"{Constants.FilesFolder}/{key}/{version}/content.{extension.ToString().ToLower()}";
    }

    private Task<string> GetPreSignedUrl(string filePathOnBucket)
    {
        var preSignedUrlReq = new GetPreSignedUrlRequest
                              {
                                  BucketName = options.BucketName,
                                  Key = filePathOnBucket,
                                  Verb = HttpVerb.PUT,
                                  Expires = DateTime.Now.AddMinutes(15)
                              };

        return s3.GetPreSignedURLAsync(preSignedUrlReq);
    }
}

public class InitUploadModel
{
    public string UploadUrl { get; set; }
}