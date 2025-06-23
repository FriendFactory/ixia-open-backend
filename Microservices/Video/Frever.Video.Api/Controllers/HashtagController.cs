using System;
using System.Threading.Tasks;
using Common.Infrastructure;
using Frever.Video.Contract;
using Frever.Video.Core.Features.Hashtags;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Frever.Video.Api.Controllers;

[ApiController]
[Authorize]
[Route("hashtag")]
public class HashtagController(IHashtagService hashtagService) : ControllerBase
{
    private readonly IHashtagService _hashtagService = hashtagService ?? throw new ArgumentNullException(nameof(hashtagService));

    /// <summary>
    ///     Gets hashtags
    /// </summary>
    /// <param name="requestOptions"></param>
    /// <returns>
    ///     Returns paginated hashtags filtered by name and ordered by ViewsCount descending.
    ///     Page size (Top) is set to 10 by default.
    /// </returns>
    [HttpGet]
    [Route("all")]
    [ProducesResponseType(typeof(HashtagInfo[]), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetAllAsync([FromQuery] HashtagRequest requestOptions)
    {
        try
        {
            var hashtags = await _hashtagService.GetHashtagListAsync(requestOptions);

            return Ok(hashtags);
        }
        catch (AppErrorWithStatusCodeException ex)
        {
            return StatusCode((int) ex.StatusCode, ex.Message);
        }
    }
}