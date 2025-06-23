using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Common.Infrastructure;
using Common.Infrastructure.Sounds;
using Common.Infrastructure.Utils;
using FluentValidation;
using Frever.Client.Core.Features.Sounds.FavoriteSounds;
using Frever.Client.Core.Features.Sounds.Song;
using Frever.Client.Core.Features.Sounds.UserSounds;
using Frever.Client.Core.Features.Sounds.UserSounds.Trending;
using Frever.Client.Core.Utils.HttpResults;
using Frever.ClientService.Contract.Sounds;
using Frever.Shared.MainDb.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SongInfo = Frever.ClientService.Contract.Sounds.SongInfo;

namespace Frever.ClientService.Api.Features.Assets;

[Authorize]
[Route("api/assets")]
public class AssetController(
    IUserSoundAssetService userSoundAssetService,
    ISongAssetService songAssetService,
    ITrendingUserSoundService trendingUserSoundService,
    IFavoriteSoundService favoriteSoundService
) : ControllerBase
{
    [HttpPost]
    [Route("song")]
    [ProducesResponseType(typeof(SongInfo[]), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSongListAsync([FromBody] SongFilterModel model)
    {
        var result = await songAssetService.GetSongListAsync(model);

        return Ok(result);
    }

    [HttpGet]
    [Route("song/{id}")]
    [ProducesResponseType(typeof(SongInfo), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSongById([FromRoute] long id)
    {
        var result = await songAssetService.GetSongById(id);
        if (result is null)
            return NotFound();

        return Ok(result);
    }

    [HttpGet]
    [Route("ExternalSong/{id}")]
    [ProducesResponseType(typeof(ExternalSongDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetExternalSongById([FromRoute] long id)
    {
        var result = await songAssetService.GetExternalSongById(id);
        if (result is null)
            return NotFound();

        return Ok(result);
    }

    [HttpGet]
    [Route("promoted-song")]
    [ProducesResponseType(typeof(PromotedSongDto[]), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPromotedSongs([FromQuery] int take = 30, [FromQuery] int skip = 0)
    {
        var result = await songAssetService.GetPromotedSongs(skip, take);

        return Ok(result);
    }

    [HttpPost]
    [Route("UserSound/my")]
    [ProducesResponseType(typeof(UserSoundFullInfo[]), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUserSoundListAsync([FromBody] UserSoundFilterModel model)
    {
        var result = await userSoundAssetService.GetUserSoundListAsync(model);

        return Ok(result);
    }

    [HttpGet]
    [Route("UserSound/{id}")]
    [ProducesResponseType(typeof(UserSoundFullInfo), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetUserSoundById([FromRoute] long id)
    {
        var result = await userSoundAssetService.GetUserSoundById(id);
        if (result is null)
            return NotFound();

        return Ok(result);
    }

    [HttpGet]
    [Route("UserSound/trending")]
    [ProducesResponseType(typeof(UserSoundFullInfo[]), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTrendingUserSounds([FromQuery] string filter, [FromQuery] int skip, [FromQuery] int take)
    {
        var result = await trendingUserSoundService.GetTrendingUserSound(filter, skip, take);

        return Ok(result);
    }

    [HttpPost]
    [Route("UserSound")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(UserSoundFullInfo))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ValidationErrorResult))]
    public async Task<IActionResult> SaveUserSound([FromBody] UserSoundCreateModel input)
    {
        try
        {
            var savedUserSound = await userSoundAssetService.SaveUserSound(input);

            return Ok(savedUserSound);
        }
        catch (ValidationException ex)
        {
            return BadRequest(new ValidationErrorResult(ex));
        }
    }

    [HttpPatch]
    [Route("UserSound/{userSoundId}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(UserSoundFullInfo))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ValidationErrorResult))]
    public async Task<IActionResult> PatchUserSound([FromRoute] long userSoundId, [FromBody] Dictionary<string, object> request)
    {
        if (request.TryGetValue(nameof(UserSound.Name).ToCamelCase(), out var nameOb))
        {
            var name = nameOb.ToString();
            var updated = await userSoundAssetService.RenameUserSound(userSoundId, name);
            return Ok(updated);
        }

        throw AppErrorWithStatusCodeException.BadRequest("Nothing to patch", "MissingFields");
    }

    [HttpPost]
    [Route("sounds")]
    [ProducesResponseType(typeof(SoundsDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSounds([FromBody] SoundsRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var model = new SongFilterModel {Ids = request.SongIds, Take = request.SongIds?.Length ?? 0};

        var result = new SoundsDto
                     {
                         Songs = await songAssetService.GetSongListAsync(model),
                         UserSounds = await userSoundAssetService.GetUserSoundByIds(request.UserSoundIds),
                         ExternalSongs = await songAssetService.GetAvailableExternalSongs(request.ExternalSongIds)
                     };

        return Ok(result);
    }

    [HttpGet]
    [Route("favorite-sound")]
    [ProducesResponseType(typeof(FavoriteSoundDto[]), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyFavoriteSounds(
        [FromQuery] bool? commercialOnly,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 10
    )
    {
        var result = await favoriteSoundService.GetMyFavoriteSounds(commercialOnly, skip, take);

        return Ok(result);
    }

    [HttpPost]
    [Route("favorite-sound/{id}/{type}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(FavoriteSoundDto))]
    public async Task<IActionResult> AddFavoriteSound([FromRoute] long id, [FromRoute] FavoriteSoundType type)
    {
        var result = await favoriteSoundService.AddFavoriteSound(id, type);
        if (result == null)
            return NotFound();

        return Ok(result);
    }

    [HttpDelete]
    [Route("favorite-sound/{id}/{type}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> RemoveFavoriteSound([FromRoute] long id, [FromRoute] FavoriteSoundType type)
    {
        await favoriteSoundService.RemoveFavoriteSound(id, type);

        return NoContent();
    }
}

public class SoundsRequest
{
    public long[] SongIds { get; set; }
    public long[] UserSoundIds { get; set; }
    public long[] ExternalSongIds { get; set; }
}