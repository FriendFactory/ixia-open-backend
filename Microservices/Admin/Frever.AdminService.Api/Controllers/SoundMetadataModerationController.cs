using System.Threading.Tasks;
using Frever.AdminService.Core.Services.MusicModeration.Contracts;
using Frever.AdminService.Core.Services.MusicModeration.Services;
using Frever.Shared.MainDb.Entities;
using Microsoft.AspNet.OData.Query;
using Microsoft.AspNetCore.Mvc;

namespace Frever.AdminService.Api.Controllers;

[ApiController]
[Route("api/sound/metadata/moderation")]
public class SoundMetadataModerationController(ISoundMetadataService service) : ControllerBase
{
    [HttpGet]
    [Route("artist")]
    public async Task<IActionResult> GetArtist(ODataQueryOptions<ArtistDto> options)
    {
        var result = await service.GetMetadata<Artist, ArtistDto>(options);
        return Ok(result);
    }

    [HttpGet]
    [Route("album")]
    public async Task<IActionResult> GetAlbum(ODataQueryOptions<AlbumDto> options)
    {
        var result = await service.GetMetadata<Album, AlbumDto>(options);
        return Ok(result);
    }

    [HttpGet]
    [Route("brand")]
    public async Task<IActionResult> GetGenre(ODataQueryOptions<BrandDto> options)
    {
        var result = await service.GetMetadata<Brand, BrandDto>(options);
        return Ok(result);
    }

    [HttpGet]
    [Route("genre")]
    public async Task<IActionResult> GetGenre(ODataQueryOptions<GenreDto> options)
    {
        var result = await service.GetMetadata<Genre, GenreDto>(options);
        return Ok(result);
    }

    [HttpGet]
    [Route("label")]
    public async Task<IActionResult> GetLabel(ODataQueryOptions<LabelDto> options)
    {
        var result = await service.GetMetadata<Label, LabelDto>(options);
        return Ok(result);
    }

    [HttpGet]
    [Route("mood")]
    public async Task<IActionResult> GetMood(ODataQueryOptions<MoodDto> options)
    {
        var result = await service.GetMetadata<Mood, MoodDto>(options);
        return Ok(result);
    }

    [HttpPost]
    [Route("artist")]
    public async Task<IActionResult> SaveArtist([FromBody] ArtistDto model)
    {
        await service.SaveMetadata<Artist, ArtistDto>(model);
        return NoContent();
    }

    [HttpPost]
    [Route("album")]
    public async Task<IActionResult> SaveAlbum([FromBody] AlbumDto model)
    {
        await service.SaveMetadata<Album, AlbumDto>(model);
        return NoContent();
    }

    [HttpPost]
    [Route("brand")]
    public async Task<IActionResult> SaveGenre([FromBody] BrandDto model)
    {
        await service.SaveMetadata<Brand, BrandDto>(model);
        return NoContent();
    }

    [HttpPost]
    [Route("genre")]
    public async Task<IActionResult> SaveGenre([FromBody] GenreDto model)
    {
        await service.SaveMetadata<Genre, GenreDto>(model);
        return NoContent();
    }

    [HttpPost]
    [Route("label")]
    public async Task<IActionResult> SaveLabel([FromBody] LabelDto model)
    {
        await service.SaveMetadata<Label, LabelDto>(model);
        return NoContent();
    }

    [HttpPost]
    [Route("mood")]
    public async Task<IActionResult> SaveModd([FromBody] MoodDto model)
    {
        await service.SaveMetadata<Mood, MoodDto>(model);
        return NoContent();
    }
}