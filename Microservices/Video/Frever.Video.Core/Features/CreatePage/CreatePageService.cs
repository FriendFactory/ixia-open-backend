using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AuthServer.Permissions.Services;
using AuthServerShared;
using Common.Infrastructure;
using Common.Models;
using Frever.Cache;
using Frever.Client.Shared.Files;
using Frever.Client.Shared.Social.Services;
using Frever.ClientService.Contract.Ai;
using Frever.ClientService.Contract.Sounds;
using Frever.Shared.MainDb.Entities;
using Frever.Video.Contract;
using Frever.Video.Core.Features.Hashtags;
using Frever.Videos.Shared.MusicGeoFiltering;
using Frever.Videos.Shared.MusicGeoFiltering.AbstractApi;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace Frever.Video.Core.Features.CreatePage;

public interface ICreatePageService
{
    Task<CreatePageContentResponse> GetCreatePageContent(string testGroup);
    Task<HashtagInfo[]> GetRowHashtags(long id, string target, int takeNext);
    Task<ExternalSongDto[]> GetRowSongs(long id, string target, int takeNext);
    Task<VideoInfo[]> GetRowVideos(long id, string target, int takeNext);
    Task<AiGeneratedContentShortInfo[]> GetRowImages(long id, string target, int takeNext);
}

public sealed partial class CreatePageService(
    UserInfo currentUser,
    IUserPermissionService userPermissionService,
    ISocialSharedService socialSharedService,
    IVideoLoader videoLoader,
    IHashtagService hashtagService,
    ICreatePageRepository repo,
    ICurrentLocationProvider locationProvider,
    IMusicGeoFilter musicGeoFilter,
    CountryCodeLookup countryCodeLookup,
    IFileStorageService fileStorage,
    IBlobCache<CreatePageContent> cache
) : ICreatePageService
{
    private const int MaxLoadedPages = 30;

    public async Task<CreatePageContentResponse> GetCreatePageContent(string testGroup)
    {
        await userPermissionService.EnsureCurrentUserActive();

        var content = new CreatePageContentResponse();

        var rows = await GetCachedRows();
        if (rows == null)
            return content;

        var filtered = rows.Where(e => testGroup == null || (e.TestGroup?.Equals(testGroup, StringComparison.OrdinalIgnoreCase) ?? true))
                           .Select(e => new CreatePageRowShortResponse {Id = e.Id, Title = e.Title, ContentType = e.Type});

        var isoCode = (await locationProvider.Get()).CountryIso3Code;
        if (!await countryCodeLookup.IsMusicEnabled(isoCode))
            filtered = filtered.Where(e => e.ContentType != CreatePageContentType.Song);

        content.Rows.AddRange(filtered);

        return content;
    }

    public async Task<HashtagInfo[]> GetRowHashtags(long id, string target, int takeNext)
    {
        await userPermissionService.EnsureCurrentUserActive();

        var row = await GetCachedRowById(id);
        if (row == null)
            return [];

        if (row.Type != CreatePageContentType.Hashtag)
            throw AppErrorWithStatusCodeException.BadRequest("Row has a different content type", "WrongContentType");

        var result = await GetPaginated(
                         row.ContentIds,
                         target,
                         takeNext,
                         e => e.Id,
                         hashtagService.GetHashtagByIds
                     );

        return result;
    }

    public async Task<ExternalSongDto[]> GetRowSongs(long id, string target, int takeNext)
    {
        await userPermissionService.EnsureCurrentUserActive();

        var row = await GetCachedRowById(id);
        if (row == null)
            return [];

        if (row.Type != CreatePageContentType.Song)
            throw AppErrorWithStatusCodeException.BadRequest("Row has a different content type", "WrongContentType");

        var isoCode = (await locationProvider.Get()).CountryIso3Code;

        var result = await GetPaginated(
                         row.ContentIds,
                         target,
                         takeNext,
                         e => e.Id,
                         e => GetExternalSongByIds(e, isoCode)
                     );

        return result;
    }

    public async Task<VideoInfo[]> GetRowVideos(long id, string target, int takeNext)
    {
        await userPermissionService.EnsureCurrentUserActive();

        var row = await GetCachedRowById(id);
        if (row == null)
            return [];

        if (row.Type != CreatePageContentType.Video)
            throw AppErrorWithStatusCodeException.BadRequest("Row has a different content type", "WrongContentType");

        var isoCode = (await locationProvider.Get()).CountryIso3Code;
        var blocked = await socialSharedService.GetBlocked(currentUser);

        var result = await GetPaginated(
                         row.ContentIds,
                         target,
                         takeNext,
                         e => e.Id,
                         e => GetVideoByIds(e, isoCode, blocked)
                     );

        return result;
    }

    public async Task<AiGeneratedContentShortInfo[]> GetRowImages(long id, string target, int takeNext)
    {
        await userPermissionService.EnsureCurrentUserActive();

        var row = await GetCachedRowById(id);
        if (row == null)
            return [];

        if (row.Type != CreatePageContentType.Image)
            throw AppErrorWithStatusCodeException.BadRequest("Row has a different content type", "WrongContentType");

        var result = await GetPaginated(
                         row.ContentIds,
                         target,
                         takeNext,
                         e => e.Id,
                         GetGeneratedContent
                     );

        return result;
    }

    private async Task<VideoInfo[]> GetVideoByIds(long[] videoIds, string isoCode, long[] blockedGroupIds)
    {
        if (videoIds is null || videoIds.Length == 0)
            return [];

        var dbVideos = await repo.GetAvailableVideos(videoIds, blockedGroupIds)
                                 .Select(e => new {e.Id, e.SongInfo})
                                 .ToArrayAsync();
        if (dbVideos.Length == 0)
            return [];

        var filtered = await musicGeoFilter.FilterOutUnavailableSongs(
                           isoCode,
                           dbVideos,
                           e => e.SongInfo?.Where(s => s.IsExternal).Select(s => s.Id).ToArray() ?? [],
                           e => e.SongInfo?.Where(s => !s.IsExternal).Select(s => s.Id).ToArray() ?? []
                       );
        if (filtered.Length == 0)
            return [];

        var videos = filtered.Select(e => new VideoWithSong {Id = e.Id, Key = e.Id, SongInfo = JsonConvert.SerializeObject(e.SongInfo)});

        return await videoLoader.LoadVideoPage(FetchVideoInfoFrom.WriteDb, videos);
    }

    private async Task<ExternalSongDto[]> GetExternalSongByIds(long[] songIds, string isoCode)
    {
        if (songIds is null || songIds.Length == 0)
            return [];

        var filteredIds = await musicGeoFilter.FindUnavailableExternalSongs(isoCode, songIds.ToHashSet());

        var songs = await repo.GetAvailableExternalSongs(songIds.Except(filteredIds));
        if (songs.Length == 0)
            return [];

        return songs.Select(
                         e => new ExternalSongDto
                              {
                                  Id = e.Id,
                                  IsAvailable = true,
                                  UsageCount = e.UsageCount,
                                  Key = e.Id.ToString()
                              }
                     )
                    .ToArray();
    }

    private async Task<AiGeneratedContentShortInfo[]> GetGeneratedContent(long[] contentIds)
    {
        var content = await repo.GetAvailableGeneratedContent(contentIds);
        if (content.Length == 0)
            return [];

        foreach (var item in content)
        {
            await fileStorage.InitUrls<Group>([item.Group]);
            await fileStorage.InitUrls<AiGeneratedImage>([item.Image]);
        }

        return content.Select(Map).ToArray();

        AiGeneratedContentShortInfo Map(GeneratedContent d)
        {
            return new AiGeneratedContentShortInfo
                   {
                       Id = d.Content.Id,
                       Group = new GroupInfo {Id = d.Group.Id, Files = d.Group.Files, NickName = d.Group.NickName},
                       RemixedFromAiGeneratedContentId = d.Content.RemixedFromAiGeneratedContentId,
                       Type = d.Image == null ? AiGeneratedContentType.Video : AiGeneratedContentType.Image,
                       CreatedAt = d.Content.CreatedAt,
                       Files = d.Image?.Files
                   };
        }
    }

    private static async Task<T[]> GetPaginated<T>(
        long[] ids,
        string target,
        int takeNext,
        Func<T, long> getId,
        Func<long[], Task<T[]>> getData
    )
    {
        List<T> result = [];

        var index = string.IsNullOrWhiteSpace(target) ? 0 : Array.IndexOf(ids, long.Parse(target));

        for (var i = 0; i < MaxLoadedPages; i++)
        {
            var selectedIds = ids.Skip(index).Take(takeNext).ToArray();
            if (selectedIds.Length == 0)
                break;

            var data = await getData(selectedIds);
            if (data is {Length: > 0})
            {
                result.AddRange(data);
                if (result.Count >= takeNext)
                    break;
            }

            index += takeNext;
            if (index >= ids.Length)
                break;
        }

        return ids.Join(result, i => i, getId, (_, e) => e).Take(takeNext).ToArray();
    }
}