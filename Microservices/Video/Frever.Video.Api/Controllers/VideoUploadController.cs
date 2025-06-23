using System;
using System.Threading.Tasks;
using Common.Infrastructure.Middleware;
using Common.Models;
using FluentValidation;
using Frever.Client.Core.Features.AI.UserGeneratedContent.Content;
using Frever.Video.Core.Features.Uploading;
using Frever.Video.Core.Features.Uploading.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Frever.Video.Api.Controllers;

/// <summary>
///     Supports uploading and getting videos for level.
///     Uploading flow:
///     - POST /video/{levelId} - initializes new upload. Method checks if user is allowed to upload a video,
///     creates new upload and returns an URL to upload video to.
///     - (Externally) User uploads video file to given URL
///     - PUT /video/{levelId} -  commits upload. Checks if uploaded file matches passed size,
///     and inits video processing.
///     This flow solves following tasks:
///     - Decreases number of traffic goes through video server
///     - Prevents from uploading incomplete video (if user halts application in the middle of uploading)
/// </summary>
[ApiController]
[Authorize]
[Route("video")]
public class VideoUploadController(IVideoUploadService videoUploadService, ILogger<VideoUploadController> logger) : ControllerBase
{
    private readonly ILogger<VideoUploadController> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    private readonly IVideoUploadService _videoUploadService =
        videoUploadService ?? throw new ArgumentNullException(nameof(videoUploadService));

    [HttpPost]
    [Route("upload")]
    public async Task<ActionResult> InitUploadVideoForLevel()
    {
        try
        {
            var response = await _videoUploadService.CreateVideoUpload();

            return Ok(response);
        }
        catch (ValidationException ex)
        {
            return BadRequest(ex.Errors);
        }
    }

    [HttpPut]
    [Route("upload-non-level/{uploadId}")]
    public async Task<ActionResult> CompleteNonLevelVideoUploading(
        [FromRoute] string uploadId,
        [FromBody] CompleteNonLevelVideoUploadingRequest completeNonLevelUploadingRequest
    )
    {
        _logger.LogTrace("Complete non-level video uploading for {UploadId}", uploadId);
        try
        {
            var video = await _videoUploadService.CompleteNonLevelVideoUploading(uploadId, completeNonLevelUploadingRequest);

            _logger.LogInformation("Non level video uploading completed successfully: video id={VideoId}", video.Id);

            if (video == null || video.Id == 0)
                return BadRequest("Video conversion error");

            return Ok(video.Id);
        }
        catch (ValidationException ex)
        {
            _logger.LogWarning(ex, "Video uploading validation errors: {Error}", JsonConvert.SerializeObject(ex.Message));

            return BadRequest(ex.Errors);
        }
    }

    [HttpPut]
    [Route("publish-ai")]
    public async Task<ActionResult> PublishAiContent([FromBody] CompleteNonLevelVideoUploadingRequest request)
    {
        try
        {
            var video = await _videoUploadService.PublishAiContent(request);

            if (video == null || video.Id == 0)
                return BadRequest(
                    new ErrorDetailsViewModel
                    {
                        ErrorCode = "VIDEO_CONVERSION_INIT_ERROR", StatusCode = 400, Message = "Error starting video conversion",
                    }
                );

            return Ok(video.Id);
        }
        catch (AiContentModerationException ex)
        {
            return BadRequest(
                new
                {
                    ex.Message,
                    ErrorCode = ErrorCodes.ModerationError,
                    ex.Errors,
                    StatusCode = 400
                }
            );
        }
    }
}