using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Amazon.S3;
using AssetStoragePathProviding;
using Common.Infrastructure.Aws;
using Common.Infrastructure.ModerationProvider;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Frever.ClientService.Api.Features.Moderation;

[Authorize]
[Route("api/moderate")]
public class ModerationController : ControllerBase
{
    private readonly List<string> _allowedVisualFormat =
    [
        "gif",
        "jpg",
        "jpeg",
        "heic",
        "png",
        "webp",
        "mp4",
        "MP4",
        "webm",
        "avi",
        "mkv",
        "wmv",
        "mov"
    ];

    private readonly IConfiguration _configuration;
    private readonly IFileBucketPathService _fileBucketPathService;
    private readonly ILogger<ModerationController> _log;
    private readonly IModerationProviderApi _moderationProviderApi;
    private readonly IAmazonS3 _s3;

    public ModerationController(
        IModerationProviderApi moderationProviderApi,
        ILogger<ModerationController> log,
        IFileBucketPathService fileBucketPathService,
        IAmazonS3 s3,
        IConfiguration configuration
    )
    {
        _moderationProviderApi = moderationProviderApi ?? throw new ArgumentNullException(nameof(moderationProviderApi));
        _log = log ?? throw new ArgumentNullException(nameof(log));
        _fileBucketPathService = fileBucketPathService ?? throw new ArgumentNullException(nameof(fileBucketPathService));
        _s3 = s3 ?? throw new ArgumentNullException(nameof(s3));
        _configuration = configuration;
    }

    [HttpPost]
    [Route("text")]
    [Produces("application/json")]
    public async Task<IActionResult> ModerateText([FromForm(Name = "text")] string text)
    {
        if (string.IsNullOrEmpty(text))
            return BadRequest("Text for moderation cannot be empty.");

        var body = $@"{{ ""text_data"": ""{text}"" }}";
        try
        {
            JToken.Parse(body);
        }
        catch (JsonReaderException e)
        {
            _log.LogInformation("Invalid JSON for text {}, with exception message {}. ", text, e.Message);
            return BadRequest();
        }

        var moderationResult = await _moderationProviderApi.CallModerationProviderApi(body);
        return new JsonResult(ModerationResponse.FromModerationResult(moderationResult)) {StatusCode = moderationResult.StatusCode};
    }

    [HttpPost]
    [Route("uploaded-visual/{uploadId}/{extension}")]
    public async Task<IActionResult> ModerateUploadedVisual([FromRoute] string uploadId, [FromRoute] string extension)
    {
        if (!_allowedVisualFormat.Contains(extension))
        {
            _log.LogInformation("Invalid format {Extension} for file", extension);
            return BadRequest();
        }

        var s3Path = _fileBucketPathService.GetPathToTempUploadFile(uploadId);
        var bucket = _configuration["AWS:bucket_name"];

        var content = await _s3.GetObjectAsync(bucket, s3Path, 10);
        using var ms = new MemoryStream();
        await content.ResponseStream.CopyToAsync(ms);

        if ("heic".Equals(extension) && uploadId.EndsWith(".jpg"))
        {
            _log.LogInformation("HEIC extension detected, but uploadId is {UploadId}, changing extension to jpg", uploadId);
            extension = "jpg";
        }

        var moderationResult = await _moderationProviderApi.CallModerationProviderApi(ms.ToArray(), extension, bucket + "/" + s3Path);
        return new JsonResult(ModerationResponse.FromModerationResult(moderationResult)) {StatusCode = moderationResult.StatusCode};
    }

    [HttpPost]
    [Route("visual")]
    [Produces("application/json")]
    public async Task<IActionResult> ModerateVisual([FromForm(Name = "visual")] IFormFile payload)
    {
        if (payload == null)
        {
            _log.LogInformation("No payload provided");
            return BadRequest();
        }

        var format = payload.FileName.Split('.')[^1];
        if (!_allowedVisualFormat.Contains(format))
        {
            _log.LogInformation("Invalid format {Format} for file {FileName}", format, payload.FileName);
            return BadRequest();
        }

        var moderationResult = await _moderationProviderApi.CallModerationProviderApi(payload);
        return new JsonResult(ModerationResponse.FromModerationResult(moderationResult)) {StatusCode = moderationResult.StatusCode};
    }
}