using System;
using System.Threading.Tasks;
using Common.Infrastructure;
using Frever.AdminService.Core.Services.HashtagModeration;
using Frever.AdminService.Core.Services.HashtagModeration.Contracts;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;

#pragma warning disable CA2007, CA1031

namespace Frever.AdminService.Api.Controllers;

[ApiController]
[Route("api/hashtag/moderation")]
public class HashtagModerationController(IHashtagModerationService hashtagService) : ControllerBase
{
    private readonly IHashtagModerationService _hashtagService = hashtagService ?? throw new ArgumentNullException(nameof(hashtagService));

    [HttpGet]
    public async Task<ActionResult> GetAll([FromQuery] GetHashtagsRequest hashtagsRequest)
    {
        var result = await _hashtagService.GetAll(hashtagsRequest);

        return Ok(result);
    }

    /// <summary>
    ///     Marks hashtag as deleted
    /// </summary>
    /// <param name="hashtagId"></param>
    /// <returns>
    /// </returns>
    [HttpDelete]
    [Route("{hashtagId}")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesErrorResponseType(typeof(AppErrorWithStatusCodeException))]
    public async Task<IActionResult> SoftDeleteAsync([FromRoute] long hashtagId)
    {
        try
        {
            var result = await _hashtagService.SoftDeleteAsync(hashtagId);

            if (result)
                return NoContent();

            return NotFound();
        }
        catch (AppErrorWithStatusCodeException ex)
        {
            return StatusCode((int) ex.StatusCode, ex.Message);
        }
    }

    /// <summary>
    ///     Updates hashtag by id.
    /// </summary>
    /// <param name="hashtagId"></param>
    /// <param name="hashtag">
    ///     Examples:
    ///     {
    ///     "name": "hashtagName"
    ///     }
    ///     {
    ///     "challengeSortOrder": 1
    ///     }
    ///     {
    ///     "name": "hashtagName",
    ///     "challengeSortOrder": 1
    ///     }
    /// </param>
    /// <returns></returns>
    [HttpPatch]
    [Route("{hashtagId}")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesErrorResponseType(typeof(AppErrorWithStatusCodeException))]
    public async Task<IActionResult> UpdateAsync([FromRoute] long hashtagId, [FromBody] JObject hashtag)
    {
        try
        {
            var updated = await _hashtagService.UpdateByIdAsync(hashtagId, hashtag);

            if (updated is null)
                return NotFound();

            return Ok(updated);
        }
        catch (AppErrorWithStatusCodeException ex)
        {
            return StatusCode((int) ex.StatusCode, ex.Message);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }
}