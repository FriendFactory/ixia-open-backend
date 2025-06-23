using System;
using System.IO;
using System.Threading.Tasks;
using ACRCloudSdkCore;
using ACRCloudSdkCore.Exceptions;
using Amazon.S3;
using Amazon.S3.Model;
using AssetServer.Shared.AssetCopying;
using AuthServerShared;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Frever.Client.Core.Features.MediaFingerprinting;

public class AcrCloudMediaFingerprintingService : IMediaFingerprintingService
{
    private const string ExportPath = "media-fingerprinting/";

    private static readonly JsonSerializerSettings JsonSerializerSettings =
        new() {ContractResolver = new CamelCasePropertyNamesContractResolver(), Formatting = Formatting.Indented};

    private readonly UserInfo _currentUser;
    private readonly ILogger _log;
    private readonly MediaFingerprintingOptions _options;
    private readonly IAmazonS3 _s3;
    private readonly AssetCopyingOptions _assetCopyingOptions;

    public AcrCloudMediaFingerprintingService(
        MediaFingerprintingOptions options,
        ILoggerFactory loggerFactory,
        UserInfo currentUser,
        IAmazonS3 s3,
        AssetCopyingOptions assetCopyingOptions
    )
    {
        if (assetCopyingOptions == null)
            throw new ArgumentNullException(nameof(assetCopyingOptions));
        ArgumentNullException.ThrowIfNull(loggerFactory);

        _options = options ?? throw new ArgumentNullException(nameof(options));
        _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
        _s3 = s3 ?? throw new ArgumentNullException(nameof(s3));
        _assetCopyingOptions = assetCopyingOptions;

        _options.Validate();
        _log = loggerFactory.CreateLogger("Frever.MediaFingerprinting");
    }

    public async Task<MediaFingerprintingResult> CheckS3File(string key, TimeSpan duration)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(key));

        var buffer = await _s3.GetObjectAsync(_assetCopyingOptions.BucketName, key);
        using var ms = new MemoryStream();
        await buffer.ResponseStream.CopyToAsync(ms);

        return await CheckBuffer(ms.ToArray(), duration, $"s3://{_assetCopyingOptions.BucketName}/{key}");
    }

    private async Task<MediaFingerprintingResult> CheckBuffer(byte[] data, TimeSpan duration, string inputFileHint)
    {
        _log.LogInformation("Checking media by group ID={GroupId}, duration {Duration}", _currentUser.UserMainGroupId, duration);

        if (duration.TotalSeconds < ACRCloudExtractTools.DefaultDurationSeconds)
        {
            duration = TimeSpan.FromSeconds(ACRCloudExtractTools.DefaultDurationSeconds);
            _log.LogInformation("Zero or negative duration, reset to default duration {Duration}", duration);
        }

        var options = new ACRCloudOptions(_options.Host, _options.AccessKey, _options.AccessSecret);
        var recognizer = new ACRCloudRecognizer(options);

        try
        {
            var result = await recognizer.RecognizeByFileAsync(data, TimeSpan.Zero, duration);

            if (result != null)
                _log.LogInformation(result.ResponseRoot.ToString());

            if (result == null)
                return new MediaFingerprintingResult {Ok = true, ContainsCopyrightedContent = false};

            await RecordS3Log(true, result.ResponseRoot.ToString(), inputFileHint);
            return new MediaFingerprintingResult
                   {
                       Ok = false,
                       ContainsCopyrightedContent = true,
                       ErrorCode = "CopyrightedContent",
                       ErrorMessage = $"Contains {string.Join("|", result.Artists)} - {result.Title}",
                       Response = result.ResponseRoot.ToString()
                   };
        }
        catch (LimitExceededException ex)
        {
            _log.LogError(ex, "ACR Cloud Limit Exceeded");
            return new MediaFingerprintingResult
                   {
                       ContainsCopyrightedContent = false,
                       Ok = false,
                       ErrorCode = "LimitExceeded",
                       ErrorMessage = "Limit exceeded, please try again later"
                   };
        }
        catch (InvalidDataException ex)
        {
            _log.LogError(ex, "Error getting audio from media");

            await RecordS3Log(false, ex.ToString(), inputFileHint);

            return new MediaFingerprintingResult
                   {
                       ContainsCopyrightedContent = false,
                       Ok = false,
                       ErrorCode = "InvalidMediaFile",
                       ErrorMessage = "Error checking file. Please try another one"
                   };
        }
    }

    private async Task RecordS3Log(bool wereRecognized, string response, string inputFile)
    {
        var json = JsonConvert.SerializeObject(new {wereRecognized, content = response, file = inputFile}, JsonSerializerSettings);
        var key =
            $"{ExportPath}{_currentUser.UserMainGroupId}_{(wereRecognized ? "ok" : "error")}_{DateTime.Now:yyyy-MM-ddTHH-mm-ss}_{Guid.NewGuid()}.json";


        await _s3.PutObjectAsync(new PutObjectRequest {BucketName = _options.LogBucket, ContentBody = json, Key = key});
    }
}