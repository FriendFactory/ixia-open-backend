using System;
using System.Threading.Tasks;
using Amazon.S3;
using Amazon.S3.Model;
using AuthServer.Permissions.Services;
using Common.Infrastructure.Utils;
using Frever.Video.Core.Features.MediaConversion.Client;

namespace Frever.Video.Core.Features.MediaConversion.Mp3Extraction;

public class MediaConvertVideoToMp3TranscodingService : IVideoToMp3TranscodingService
{
    private readonly IMediaConvertServiceClient _mediaConvertService;
    private readonly VideoServerOptions _options;
    private readonly IAmazonS3 _s3;
    private readonly IUserPermissionService _userPermissionService;

    public MediaConvertVideoToMp3TranscodingService(
        IUserPermissionService userPermissionService,
        VideoServerOptions options,
        IAmazonS3 s3,
        IMediaConvertServiceClient mediaConvertService
    )
    {
        _userPermissionService = userPermissionService ?? throw new ArgumentNullException(nameof(userPermissionService));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _s3 = s3 ?? throw new ArgumentNullException(nameof(s3));
        _mediaConvertService = mediaConvertService ?? throw new ArgumentNullException(nameof(mediaConvertService));
    }

    public async Task<TranscodingInfo> InitTranscoding()
    {
        await _userPermissionService.EnsureCurrentUserActive();

        var transcodingId = Guid.NewGuid().ToString();

        var inputFile = GetTranscodingInputFileName(transcodingId);

        var uploadUrl = _s3.GetPreSignedURL(
            new GetPreSignedUrlRequest
            {
                BucketName = _options.DestinationVideoS3BucketName,
                Key = inputFile,
                Verb = HttpVerb.PUT,
                Expires = DateTime.Now.AddMinutes(15)
            }
        );

        return new TranscodingInfo {TranscodingId = transcodingId, TranscodingFileUploadUrl = uploadUrl};
    }

    public async Task<TranscodeResult> Transcode(string transcodingId, TimeSpan duration)
    {
        if (string.IsNullOrWhiteSpace(transcodingId))
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(transcodingId));
        
        var inputFile = UriUtils.CombineUri($"s3://{_options.DestinationVideoS3BucketName}", GetTranscodingInputFileName(transcodingId));

        var outFileKey = $"{GetTranscodingInputFileName(transcodingId)}_out";
        var destinationFilePath = UriUtils.CombineUri($"s3://{_options.DestinationVideoS3BucketName}", outFileKey);

        await _mediaConvertService.CreateMp3ExtractionJob(inputFile, destinationFilePath);

        var s3Url = _s3.GetPreSignedURL(
            new GetPreSignedUrlRequest
            {
                BucketName = _options.DestinationVideoS3BucketName,
                Key = $"{outFileKey}.mp3",
                Verb = HttpVerb.GET,
                Expires = DateTime.Now.AddMinutes(30)
            }
        );

        return new TranscodeResult {Ok = true, ConvertedFileUrl = s3Url};
    }

    private static string GetTranscodingInputFileName(string transcodingId)
    {
        return $"Preloaded/{transcodingId}";
    }
}