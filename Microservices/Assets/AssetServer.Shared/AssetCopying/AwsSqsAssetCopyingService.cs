using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Amazon.SQS;
using AssetServer.Shared.Messages;
using AssetStoragePathProviding;
using Common.Models.Files;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using static System.String;

namespace AssetServer.Shared.AssetCopying;

public sealed class AwsSqsAssetCopyingService : IAssetCopyingService
{
    private static readonly Regex NewVersionFormatRegex = new(@"\d{8}T\d{6}U[a-f0-9]{32}");

    private readonly IFileBucketPathService _fileBucketPathService;

    private readonly ILogger _logger;
    private readonly AssetCopyingOptions _options;
    private readonly IAmazonSQS _sqs;

    public AwsSqsAssetCopyingService(
        IFileBucketPathService fileBucketPathService,
        IAmazonSQS sqs,
        ILoggerFactory loggerFactory,
        AssetCopyingOptions options
    )
    {
        ArgumentNullException.ThrowIfNull(loggerFactory);

        if (fileBucketPathService != null)
            _fileBucketPathService = fileBucketPathService;

        _sqs = sqs ?? throw new ArgumentNullException(nameof(sqs));
        _options = options ?? throw new ArgumentNullException(nameof(options));

        _logger = loggerFactory.CreateLogger("Frever.AwsSqsAssetCopyingService");
    }

    public string GenerateNewVersion()
    {
        return $"{DateTime.Now:yyyyMMddTHHmmss}U{Guid.NewGuid():N}";
    }

    public async Task QueueAssetFileCopying(Type assetType, long id, FileInfo file, Dictionary<string, string> tags = null)
    {
        if (IsNullOrWhiteSpace(file.Version))
            throw new InvalidOperationException($"File for asset {assetType.Name} ID={id} has no version. Generate version first.");

        tags ??= new Dictionary<string, string>();

        _logger.LogDebug("Queue copying of {AssetType} ID={Id} {FileFile}", assetType, id, file.File);

        var targetFilePath = _fileBucketPathService.GetVersionedFilePathOnBucket(
            assetType,
            id,
            file.Version,
            file.Platform,
            file.File,
            file.Resolution,
            file.Extension
        );

        var sourceFilePath = file.Source switch
                             {
                                 var fileSource when !IsNullOrEmpty(fileSource.UploadId) => _fileBucketPathService.GetPathToTempUploadFile(
                                     file.Source.UploadId
                                 ),
                                 var fileSource when fileSource.CopyFrom != null => IsFileDeployedWithDeprecatedScheme(fileSource)
                                                                                        ? _fileBucketPathService.GetFilePathOnBucket(
                                                                                            assetType,
                                                                                            fileSource.CopyFrom.Id,
                                                                                            file.Platform,
                                                                                            file.File,
                                                                                            file.Resolution,
                                                                                            file.Extension
                                                                                        )
                                                                                        : _fileBucketPathService
                                                                                           .GetVersionedFilePathOnBucket(
                                                                                                assetType,
                                                                                                fileSource.CopyFrom.Id,
                                                                                                fileSource.CopyFrom.Version,
                                                                                                file.Platform,
                                                                                                file.File,
                                                                                                file.Resolution,
                                                                                                file.Extension
                                                                                            ),
                                 _ => throw new Exception("No file source")
                             };

        _logger.LogDebug("Copying from {SourceFilePath} to {TargetFilePath}", sourceFilePath, targetFilePath);

        var message = new CopyAssetMessage
                      {
                          Bucket = _options.BucketName,
                          Tags = tags,
                          FromKey = sourceFilePath,
                          ToKey = targetFilePath
                      };

        try
        {
            var messageBody = JsonConvert.SerializeObject(message);

            _logger.LogDebug("Send asset copy message: {MessageBody}", messageBody);
            await _sqs.SendMessageAsync(_options.AssetCopyingQueueUrl, JsonConvert.SerializeObject(message));
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error sending message to asset copying queue");

            throw;
        }
    }

    public async Task QueueFileCopyingBetweenTypes(
        Type fromAssetType,
        Type toAssetType,
        long id,
        FileInfo file,
        Dictionary<string, string> tags = null
    )
    {
        ArgumentNullException.ThrowIfNull(file);
        ArgumentException.ThrowIfNullOrWhiteSpace(file.Version);
        ArgumentNullException.ThrowIfNull(file.Source.CopyFrom);

        tags ??= new Dictionary<string, string>();

        _logger.LogDebug(
            "Queue copying of {FromAsseType} ID={FromAssetId} to {ToAssetType} ID={ToAssetId}",
            fromAssetType,
            file.Source.CopyFrom.Id,
            toAssetType,
            id
        );

        var sourceFilePath = _fileBucketPathService.GetVersionedFilePathOnBucket(
            fromAssetType,
            file.Source.CopyFrom.Id,
            file.Source.CopyFrom.Version,
            file.Platform,
            file.File,
            file.Resolution,
            file.Extension
        );

        var targetFilePath = _fileBucketPathService.GetVersionedFilePathOnBucket(
            toAssetType,
            id,
            file.Version,
            file.Platform,
            file.File,
            file.Resolution,
            file.Extension
        );

        _logger.LogDebug("Copying from {SourceFilePath} to {TargetFilePath}", sourceFilePath, targetFilePath);

        var message = new CopyAssetMessage
                      {
                          Bucket = _options.BucketName,
                          Tags = tags,
                          FromKey = sourceFilePath,
                          ToKey = targetFilePath
                      };
        try
        {
            var messageBody = JsonConvert.SerializeObject(message);

            _logger.LogDebug("Send asset copy message: {MessageBody}", messageBody);
            await _sqs.SendMessageAsync(_options.AssetCopyingQueueUrl, JsonConvert.SerializeObject(message));
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error sending message to asset copying queue");

            throw;
        }
    }

    private static bool IsFileDeployedWithDeprecatedScheme(FileSource fileSource)
    {
        return IsNullOrWhiteSpace(fileSource.CopyFrom.Version) || !NewVersionFormatRegex.IsMatch(fileSource.CopyFrom.Version);
    }
}