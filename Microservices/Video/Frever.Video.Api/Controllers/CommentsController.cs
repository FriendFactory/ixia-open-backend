using System;
using System.Threading.Tasks;
using Frever.Video.Core.Features.Comments;
using Frever.Video.Core.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Frever.Video.Api.Controllers;

[ApiController]
[Authorize]
public class CommentsController(ICommentReadingService commentReadingService, ICommentModificationService commentModificationService)
    : ControllerBase
{
    private readonly ICommentModificationService _commentModificationService =
        commentModificationService ?? throw new ArgumentNullException(nameof(commentModificationService));

    private readonly ICommentReadingService _commentReadingService =
        commentReadingService ?? throw new ArgumentNullException(nameof(commentReadingService));

    [HttpGet]
    [Route("video/{videoId}/comment/by-id/{commentId}", Order = 1)]
    public async Task<ActionResult> GetCommentById([FromRoute] long videoId, [FromRoute] long commentId)
    {
        var comment = await _commentReadingService.GetCommentById(videoId, commentId);

        if (comment == null)
            return NotFound();
        return Ok(comment);
    }

    [HttpGet]
    [Route("video/{videoId}/comment/root", Order = 0)]
    public async Task<ActionResult> GetRootComments([FromRoute] long videoId, [FromQuery] int takeNewer = 0, [FromQuery] int takeOlder = 20)
    {
        var key = Request.Query["key"].ToString();

        var comments = await _commentReadingService.GetRootComments(videoId, key, takeOlder, takeNewer);

        if (comments == null)
            return NotFound("Video is not found or not available");

        return Ok(comments);
    }

    [HttpGet]
    [Route("video/{videoId}/comment/thread/{rootCommentKey}")]
    public async Task<ActionResult> GetThreadComments(
        [FromRoute] long videoId,
        [FromRoute] string rootCommentKey,
        [FromQuery] int takeNewer = 0,
        [FromQuery] int takeOlder = 20
    )
    {
        if (string.IsNullOrWhiteSpace(rootCommentKey))
            return BadRequest("Root comment key is required");

        var key = Request.Query["key"].ToString();

        var comments = await _commentReadingService.GetThreadComments(
                           videoId,
                           rootCommentKey,
                           key,
                           takeOlder,
                           takeNewer
                       );

        if (comments == null)
            return NotFound("Video is not found or not available");

        return Ok(comments);
    }

    [HttpGet]
    [Route("video/{videoId}/who-commented")]
    public async Task<IActionResult> GetWhoCommented([FromRoute] long videoId)
    {
        var commenterGroupIds = await _commentReadingService.GetWhoCommented(videoId);
        if (commenterGroupIds == null)
            return NotFound();

        return Ok(await commenterGroupIds.ToArrayAsyncSafe());
    }

    [HttpPost]
    [Route("video/{videoId}/comment")]
    public async Task<ActionResult> AddComment([FromRoute] long videoId, [FromBody] AddCommentRequest request)
    {
        var newComment = await _commentModificationService.AddComment(videoId, request);

        return Ok(newComment);
    }

    [HttpPut]
    [Route("video/{videoId}/comment/{commentId}/like")]
    public async Task<IActionResult> LikeComment([FromRoute] long videoId, [FromRoute] long commentId)
    {
        var comment = await _commentModificationService.LikeComment(videoId, commentId);
        return Ok(comment);
    }

    [HttpDelete]
    [Route("video/{videoId}/comment/{commentId}/like")]
    public async Task<IActionResult> UnlikeComment([FromRoute] long videoId, [FromRoute] long commentId)
    {
        var comment = await _commentModificationService.UnlikeComment(videoId, commentId);
        return Ok(comment);
    }

    [HttpGet]
    [Route("video/{videoId}/comment/pinned")]
    public async Task<IActionResult> PinnedComments([FromRoute] long videoId)
    {
        var pinnedComments = await _commentReadingService.GetPinnedComments(videoId);

        return Ok(pinnedComments);
    }

    [HttpPut]
    [Route("video/{videoId}/comment/{commentId}/pin")]
    public async Task<IActionResult> PinComment([FromRoute] long videoId, [FromRoute] long commentId)
    {
        var comment = await _commentModificationService.PinComment(videoId, commentId);
        if (comment == null)
            return NotFound();

        return Ok(comment);
    }

    [HttpDelete]
    [Route("video/{videoId}/comment/{commentId}/pin")]
    public async Task<IActionResult> UnPinComment([FromRoute] long videoId, [FromRoute] long commentId)
    {
        var comment = await _commentModificationService.UnPinComment(videoId, commentId);
        if (comment == null)
            return NotFound();

        return Ok(comment);
    }
}