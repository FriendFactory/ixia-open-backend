using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Amazon.S3;
using Amazon.S3.Model;

namespace Common.Infrastructure.Aws;

public static class S3Operations
{
    public static async Task DeleteFolder(this IAmazonS3 s3, string bucket, string folder, Action<string> log)
    {
        ArgumentNullException.ThrowIfNull(s3);

        if (string.IsNullOrWhiteSpace(bucket))
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(bucket));
        if (string.IsNullOrWhiteSpace(folder))
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(folder));

        if (!folder.EndsWith("/"))
            folder += "/";

        var continuationToken = default(string);

        while (true)
        {
            log?.Invoke($"List of s3://{bucket}/{folder} . Continuation token is {continuationToken}");

            var request = new ListObjectsV2Request {Prefix = folder, BucketName = bucket};

            if (!string.IsNullOrWhiteSpace(continuationToken))
                request.ContinuationToken = continuationToken;

            var files = await s3.ListObjectsV2Async(request);

            if ((int) files.HttpStatusCode > 300)
            {
                log?.Invoke($"Error list files in {folder}: {files.HttpStatusCode}");

                break;
            }

            log?.Invoke($"{files.S3Objects.Count} files found");

            if (!files.S3Objects.Any())
                break;

            if (log != null)
                foreach (var file in files.S3Objects)
                    log($"Deleting s3://{file.BucketName}/{file.Key}");

            var response = await s3.DeleteObjectsAsync(
                               new DeleteObjectsRequest
                               {
                                   BucketName = bucket, Objects = files.S3Objects.Select(o => new KeyVersion {Key = o.Key}).ToList()
                               }
                           );

            log?.Invoke($"Delete objects response: {response.HttpStatusCode}, aws request id {response.ResponseMetadata?.RequestId}");

            if (!files.IsTruncated)
                break;
            continuationToken = files.NextContinuationToken;
        }
    }

    public static async Task<GetObjectResponse> GetObjectAsync(this IAmazonS3 s3, string bucketName, string key, int tryouts)
    {
        ArgumentNullException.ThrowIfNull(s3);

        if (string.IsNullOrWhiteSpace(bucketName))
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(bucketName));
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(key));

        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(tryouts);

        var delay = TimeSpan.FromMilliseconds(200);

        for (var i = 0; i < tryouts; i++)
            try
            {
                var response = await s3.GetObjectAsync(bucketName, key);
                if (response.HttpStatusCode == HttpStatusCode.NotFound)
                {
                    await Task.Delay(delay);
                    delay *= 2;
                    delay = delay > TimeSpan.FromSeconds(2) ? TimeSpan.FromSeconds(2) : delay;
                }
                else
                {
                    return response;
                }
            }
            catch (AmazonS3Exception)
            {
                await Task.Delay(delay);
                delay *= 2;
                delay = delay > TimeSpan.FromSeconds(2) ? TimeSpan.FromSeconds(2) : delay;
            }

        return await s3.GetObjectAsync(bucketName, key);
    }
}