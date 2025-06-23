using System;
using System.Net;
using System.Threading.Tasks;
using FluentValidation;
using Frever.Video.Core.Features.MediaConversion.Mp3Extraction;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

#pragma warning disable CS8618

namespace Frever.Video.Api.Controllers;

[ApiController]
[Authorize]
[Route("transcoding")]
public class TranscodingController(IVideoToMp3TranscodingService transcodingService) : ControllerBase
{
    private readonly IVideoToMp3TranscodingService _transcodingService =
        transcodingService ?? throw new ArgumentNullException(nameof(transcodingService));

    [HttpPost]
    [Route("upload")]
    public async Task<ActionResult> InitTranscoding()
    {
        try
        {
            var response = await _transcodingService.InitTranscoding();

            return Ok(response);
        }
        catch (ValidationException ex)
        {
            return BadRequest(ex.Errors);
        }
    }

    [HttpPut]
    [Route("transcode")]
    [ProducesResponseType((int) HttpStatusCode.OK, Type = typeof(TranscodeResult))]
    [ProducesResponseType((int) HttpStatusCode.BadRequest, Type = typeof(TranscodeResult))]
    public async Task<ActionResult> Transcode([FromBody] TranscodingRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.TranscodingId))
            return BadRequest("TranscodeId is empty");

        var result = await _transcodingService.Transcode(request.TranscodingId, TimeSpan.FromSeconds(request.DurationSec));
        if (result.Ok)
            return Ok(result);

        return BadRequest(result);
    }
}

public class TranscodingRequest
{
    public string TranscodingId { get; set; }

    public int DurationSec { get; set; }
}