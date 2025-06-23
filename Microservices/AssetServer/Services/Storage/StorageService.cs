using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Amazon.S3;
using Amazon.S3.Model;
using ByteSizeLib;

namespace AssetServer.Services.Storage;

internal sealed class StorageService(IAmazonS3 client, string bucketName) : IStorageService
{
    public async Task<S3FileInfo> GetFileInfo(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(key));

        try
        {
            var objects = await client.ListObjectsV2Async(new ListObjectsV2Request {BucketName = bucketName, Prefix = key});


            if (objects.KeyCount == 0)
                return S3FileInfo.NotFound;

            var fileKey = objects.S3Objects[0];

            var tags = await client.GetObjectTaggingAsync(new GetObjectTaggingRequest {Key = fileKey.Key, BucketName = bucketName});

            return new S3FileInfo(fileKey.Key, tags.Tagging.ToDictionary(t => t.Key, t => t.Value, StringComparer.OrdinalIgnoreCase));
        }
        catch (AmazonS3Exception ex)
        {
            if (ex.ErrorCode == "NoSuchKey")
                return S3FileInfo.NotFound;

            throw;
        }
    }

    public async Task<string> CopyFileAsync(string fromPath, string toPath, Dictionary<string, string> tags = null)
    {
        var copyObjectReq = new CopyObjectRequest
                            {
                                SourceBucket = bucketName,
                                DestinationBucket = bucketName,
                                DestinationKey = toPath,
                                SourceKey = fromPath,
                                TagSet = Map(tags)
                            };
        var resp = await client.CopyObjectAsync(copyObjectReq);

        return resp.VersionId;
    }

    public async Task<long> GetFileSizeKb(string path)
    {
        var getObjectMetadataRequest = new GetObjectMetadataRequest {BucketName = bucketName, Key = path};
        var meta = await client.GetObjectMetadataAsync(getObjectMetadataRequest);
        var filesSizeBytes = meta.Headers.ContentLength;
        var sizeData = ByteSize.FromBytes(filesSizeBytes);

        return (long) sizeData.KiloBytes;
    }

    public async Task DeleteDirectoryWithAllFiles(string directory)
    {
        var allFiles = await client.ListObjectsV2Async(new ListObjectsV2Request {Prefix = directory, BucketName = bucketName});

        if (allFiles.S3Objects.Count == 0)
            return;

        var resp = await client.DeleteObjectsAsync(
                       new DeleteObjectsRequest
                       {
                           Objects = allFiles.S3Objects.Select(f => new KeyVersion {Key = f.Key}).ToList(), BucketName = bucketName
                       }
                   );

        if (resp.DeleteErrors != null && resp.DeleteErrors.Count != 0)
            throw new InvalidOperationException(resp.DeleteErrors.Select(x => x.Message).Aggregate((x, y) => x + ". " + y));
    }

    public string GetTempUrlForUploadingFile(string filePathOnBucket)
    {
        var preSignedUrlReq = new GetPreSignedUrlRequest
                              {
                                  BucketName = bucketName,
                                  Key = filePathOnBucket,
                                  Verb = HttpVerb.PUT,
                                  Expires = DateTime.Now.AddMinutes(15)
                              };

        return client.GetPreSignedURL(preSignedUrlReq);
    }

    public string GetTempUrlForReadingFile(string filePathOnBucket)
    {
        var preSignedUrlReq = new GetPreSignedUrlRequest
                              {
                                  BucketName = bucketName,
                                  Key = filePathOnBucket,
                                  Verb = HttpVerb.GET,
                                  Expires = DateTime.Now.AddMinutes(15)
                              };

        return client.GetPreSignedURL(preSignedUrlReq);
    }

    public string GetTempUrlForCheckingFileExistence(string filePathOnBucket)
    {
        if (string.IsNullOrWhiteSpace(filePathOnBucket))
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(filePathOnBucket));

        var preSignedUrlReq = new GetPreSignedUrlRequest
                              {
                                  BucketName = bucketName,
                                  Key = filePathOnBucket,
                                  Verb = HttpVerb.HEAD,
                                  Expires = DateTime.Now.AddMinutes(15)
                              };

        return client.GetPreSignedURL(preSignedUrlReq);
    }


    public async Task<byte[]> GetFileAsync(string filePath)
    {
        var req = new GetObjectRequest {BucketName = bucketName, Key = filePath};

        using var resp = await client.GetObjectAsync(req);
        await using var stream = resp.ResponseStream;
        var bytes = await ReadFully(stream);

        return bytes;
    }

    private List<Tag> Map(Dictionary<string, string> tags)
    {
        var mappedTags = new List<Tag>();
        if (tags != null)
            mappedTags.AddRange(tags.Select(tag => new Tag {Key = tag.Key, Value = tag.Value}));

        return mappedTags;
    }

    private async Task<byte[]> ReadFully(Stream input)
    {
        await using var ms = new MemoryStream();
        await input.CopyToAsync(ms);

        return ms.ToArray();
    }
}