using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;
using AssetServer.Shared.AssetCopying;
using Common.Infrastructure.Aws.Crypto;
using Common.Infrastructure.CloudFront;
using Common.Infrastructure.Utils;

namespace Frever.Client.Shared.Files;

public class AwsS3FileStorageBackend(
    CloudFrontConfiguration cdnOptions,
    IAmazonS3 s3,
    TransferUtility transferUtility,
    AssetCopyingOptions options
) : IFileStorageBackend
{
    public string Bucket => options.BucketName;

    public string MakeCdnUrl(string filePath, bool signed = false)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        var cdnUrl = UriUtils.CombineUri(cdnOptions.CloudFrontHost, filePath);
        if (!signed)
            return cdnUrl;

        var result = FreverAmazonCloudFrontSigner.SignUrlCanned(
            cdnUrl,
            cdnOptions.CloudFrontCertKeyPairId,
            DateTime.Now.AddMinutes(cdnOptions.CloudFrontSignedCookieLifetimeMinutes)
        );
        return result;
    }

    public async Task UploadToBucket(string targetKey, byte[] content)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(targetKey);
        ArgumentNullException.ThrowIfNull(content);
        if (content.Length == 0)
            throw new ArgumentException("Empty file content");

        using var stream = new MemoryStream(content);
        var uploadRequest = new TransferUtilityUploadRequest
                            {
                                BucketName = options.BucketName,
                                Key = targetKey,
                                InputStream = stream,
                                StorageClass = S3StorageClass.Standard,
                                CannedACL = S3CannedACL.Private
                            };

        await transferUtility.UploadAsync(uploadRequest, CancellationToken.None);
    }

    public async Task CopyFrom(string sourceBucket, string sourceKey, string targetKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceBucket);
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(targetKey);

        var request = new CopyObjectRequest
                      {
                          SourceBucket = sourceBucket,
                          SourceKey = sourceKey,
                          DestinationBucket = options.BucketName,
                          DestinationKey = targetKey
                      };

        await s3.CopyObjectAsync(request, CancellationToken.None);
    }
}