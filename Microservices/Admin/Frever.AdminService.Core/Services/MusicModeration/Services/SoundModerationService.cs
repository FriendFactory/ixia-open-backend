using System.Linq;
using System.Threading.Tasks;
using AuthServer.Permissions.Services;
using AuthServerShared;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Common.Infrastructure;
using Common.Models.Database.Interfaces;
using FluentValidation;
using Frever.AdminService.Core.Services.MusicModeration.Contracts;
using Frever.AdminService.Core.Utils;
using Frever.Cache.Resetting;
using Frever.Client.Shared.Files;
using Frever.Shared.MainDb;
using Frever.Shared.MainDb.Entities;
using Microsoft.AspNet.OData.Query;
using Microsoft.EntityFrameworkCore;

namespace Frever.AdminService.Core.Services.MusicModeration.Services;

public interface ISoundModerationService
{
    Task<ResultWithCount<SongDto>> GetSongs(ODataQueryOptions<SongDto> options);
    Task<ResultWithCount<UserSoundDto>> GetUserSounds(ODataQueryOptions<UserSoundDto> options);
    Task<ResultWithCount<PromotedSongDto>> GetPromotedSongs(ODataQueryOptions<PromotedSongDto> options);
    Task SaveSong(SongDto song);
    Task SavePromotedSong(PromotedSongDto model);
    Task DeletePromotedSong(long id);
}

public class SoundModerationService(
    UserInfo currentUser,
    IWriteDb db,
    IMapper mapper,
    ICacheReset cacheReset,
    IFileStorageService fileStorage,
    IUserPermissionService permissionService,
    IValidator<SongDto> songValidator,
    IValidator<PromotedSongDto> promotedSongValidator
) : ISoundModerationService
{
    public async Task<ResultWithCount<SongDto>> GetSongs(ODataQueryOptions<SongDto> options)
    {
        await permissionService.EnsureHasAssetReadAccess();

        var result = await db.Song.AsNoTracking().ProjectTo<SongDto>(mapper.ConfigurationProvider).ExecuteODataRequestWithCount(options);

        await fileStorage.InitUrls<PromotedSong>(result.Data);

        return result;
    }

    public async Task<ResultWithCount<UserSoundDto>> GetUserSounds(ODataQueryOptions<UserSoundDto> options)
    {
        await permissionService.EnsureHasAssetReadAccess();

        var result = await db.UserSound.AsNoTracking()
                             .ProjectTo<UserSoundDto>(mapper.ConfigurationProvider)
                             .ExecuteODataRequestWithCount(options);

        await fileStorage.InitUrls<UserSound>(result.Data);

        return result;
    }

    public async Task<ResultWithCount<PromotedSongDto>> GetPromotedSongs(ODataQueryOptions<PromotedSongDto> options)
    {
        await permissionService.EnsureHasCategoryReadAccess();

        var result = await db.PromotedSong.AsNoTracking()
                             .ProjectTo<PromotedSongDto>(mapper.ConfigurationProvider)
                             .ExecuteODataRequestWithCount(options);

        await fileStorage.InitUrls<PromotedSong>(result.Data);

        return result;
    }

    public async Task SaveSong(SongDto model)
    {
        await permissionService.EnsureHasAssetFullAccess();

        await songValidator.ValidateAndThrowAsync(model);

        var song = model.Id == 0 ? await CreateEntity<Song>() : await db.Song.FirstOrDefaultAsync(e => e.Id == model.Id);
        if (song is null)
            throw AppErrorWithStatusCodeException.NotFound("Song not found", "ERROR_SONG_NOT_FOUND");

        var usageCount = song.UsageCount;
        var uploaderUserId = song.Id == 0 ? currentUser.UserMainGroupId : song.UploaderUserId;
        mapper.Map(model, song);

        song.UsageCount = usageCount;
        song.UploaderUserId = uploaderUserId;
        song.UpdatedByUserId = currentUser.UserMainGroupId;
        await db.SaveChangesAsync();

        var uploader = fileStorage.CreateFileUploader();
        await uploader.UploadFiles<Song>(song);
        await db.SaveChangesAsync();

        await uploader.WaitForCompletion();
        await cacheReset.ResetOnDependencyChange(typeof(Song), null);
    }

    public async Task SavePromotedSong(PromotedSongDto model)
    {
        await permissionService.EnsureHasCategoryFullAccess();

        await promotedSongValidator.ValidateAndThrowAsync(model);
        if (model.AvailableForCountries is {Length: > 0})
            await ValidateMarketingCountries(model);

        var song = model.Id == 0 ? await CreateEntity<PromotedSong>() : await db.PromotedSong.FirstOrDefaultAsync(e => e.Id == model.Id);
        if (song == null)
            throw AppErrorWithStatusCodeException.NotFound("PromotedSong not found", "ERROR_PROMOTED_SONG_NOT_FOUND");

        mapper.Map(model, song);
        await db.SaveChangesAsync();

        var uploader = fileStorage.CreateFileUploader();
        await uploader.UploadFiles<PromotedSong>(song);
        await db.SaveChangesAsync();

        await uploader.WaitForCompletion();
        await cacheReset.ResetOnDependencyChange(typeof(PromotedSong), null);
    }

    public async Task DeletePromotedSong(long id)
    {
        await permissionService.EnsureHasCategoryFullAccess();

        var song = await db.PromotedSong.FindAsync(id);
        if (song == null)
            return;

        db.PromotedSong.Remove(song);
        await db.SaveChangesAsync();

        await cacheReset.ResetOnDependencyChange(typeof(PromotedSong), null);
    }

    private async Task ValidateMarketingCountries(PromotedSongDto model)
    {
        if (model.ExternalSongId.HasValue)
        {
            var externalSong = await db.ExternalSongs.FindAsync(model.ExternalSongId);
            if (externalSong is null)
                throw AppErrorWithStatusCodeException.BadRequest("ExternalSong is not found or not accessible", "ExternalSongNotFound");

            var excluded = externalSong.ExcludedCountries.Where(e => model.AvailableForCountries.Contains(e)).ToArray();
            if (excluded.Length > 0)
                throw AppErrorWithStatusCodeException.BadRequest(
                    $"ExternalSong is not available for countries {string.Join(',', excluded)}",
                    "UnavailableCountry"
                );
        }

        if (model.SongId.HasValue)
        {
            var song = await db.Song.FindAsync(model.SongId);
            if (song is null)
                throw AppErrorWithStatusCodeException.BadRequest("Song is not found or not accessible", "ExternalSongNotFound");

            if (song.AvailableForCountries == null || song.AvailableForCountries.Length == 0)
                return;

            var notAvailable = model.AvailableForCountries.Where(e => !song.AvailableForCountries.Contains(e)).ToArray();
            if (notAvailable.Length > 0)
                throw AppErrorWithStatusCodeException.BadRequest(
                    $"Song is not available for countries {string.Join(',', notAvailable)}",
                    "UnavailableCountry"
                );
        }
    }

    private async Task<T> CreateEntity<T>()
        where T : class, IEntity, new()
    {
        var entity = new T();
        await db.Set<T>().AddAsync(entity);
        return entity;
    }
}