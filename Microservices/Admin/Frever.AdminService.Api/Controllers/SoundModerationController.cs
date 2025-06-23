using System;
using System.Linq;
using System.Threading.Tasks;
using Common.Infrastructure;
using Frever.AdminService.Core.Services.MusicModeration.Contracts;
using Frever.AdminService.Core.Services.MusicModeration.Services;
using Microsoft.AspNet.OData.Query;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

#pragma warning disable CA2007

namespace Frever.AdminService.Api.Controllers;

[ApiController]
[Route("api/sound/moderation")]
public class SoundModerationController(
    ISoundModerationService soundModerationService,
    IMusicDeleteService musicDeleteService,
    IMusicIsrcService musicIsrcService
) : ControllerBase
{
    [HttpPost]
    [Route("/api/music/moderation/fill-up-missing-isrc")]
    public async Task<IActionResult> FillUpMissingIsrc()
    {
        await musicIsrcService.FillUpMissingIsrcOnExternalSongs();
        return NoContent();
    }

    [HttpGet]
    [Route("song")]
    public async Task<IActionResult> GetSong(ODataQueryOptions<SongDto> options)
    {
        var result = await soundModerationService.GetSongs(options);
        return Ok(result);
    }

    [HttpGet]
    [Route("user-sound")]
    public async Task<IActionResult> GetUserSounds(ODataQueryOptions<UserSoundDto> options)
    {
        var result = await soundModerationService.GetUserSounds(options);
        return Ok(result);
    }

    [HttpGet]
    [Route("promoted-song")]
    public async Task<IActionResult> GetPromotedSongs(ODataQueryOptions<PromotedSongDto> options)
    {
        var result = await soundModerationService.GetPromotedSongs(options);
        return Ok(result);
    }

    [HttpPost]
    [Route("song")]
    public async Task<IActionResult> SaveSong([FromBody] SongDto model)
    {
        await soundModerationService.SaveSong(model);
        return NoContent();
    }

    [HttpPost]
    [Route("promoted-song")]
    public async Task<IActionResult> SavePromotedSong([FromBody] PromotedSongDto model)
    {
        await soundModerationService.SavePromotedSong(model);
        return NoContent();
    }

    [HttpDelete]
    [Route("promoted-song/{id}")]
    public async Task<IActionResult> DeletePromotedSong(long id)
    {
        await soundModerationService.DeletePromotedSong(id);
        return NoContent();
    }

    [HttpDelete]
    [HttpPost]
    [Route("audio")]
    public async Task<IActionResult> SetAudioDeleteStatus(
        [FromQuery] long? songId,
        [FromQuery] long? userSoundId,
        [FromQuery] long? externalSongId
    )
    {
        var allParams = new[] {songId, userSoundId, externalSongId};
        if (allParams.All(p => p == null))
            throw AppErrorWithStatusCodeException.BadRequest(
                $"{nameof(songId)}, {nameof(userSoundId)} or {nameof(externalSongId)} parameter required",
                "ParameterRequired"
            );

        if (allParams.Count(p => p != null) > 1)
            throw AppErrorWithStatusCodeException.BadRequest(
                $"Only one of {nameof(songId)}, {nameof(userSoundId)} or {nameof(externalSongId)} parameter should be provided",
                "TooManyParameters"
            );

        var isDeleted = StringComparer.OrdinalIgnoreCase.Equals(Request.Method, HttpMethods.Delete);

        if (songId != null)
            await musicDeleteService.SetDeleteContentBySongId(songId.Value, isDeleted);
        else if (userSoundId != null)
            await musicDeleteService.SetDeletedContentByUserSoundId(userSoundId.Value, isDeleted);
        else if (externalSongId != null)
            await musicDeleteService.SetDeletedContentByExternalSongId(externalSongId.Value, isDeleted);

        return NoContent();
    }
}