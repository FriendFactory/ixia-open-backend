using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Common.Infrastructure.Videos;
using Frever.Shared.MainDb;
using Frever.Shared.MainDb.Entities;
using Microsoft.EntityFrameworkCore;

namespace Frever.Video.Core.Features.CreatePage;

public interface ICreatePageRepository
{
    Task<ContentRow[]> GetContentRows();

    Task<long[]> GetTrendingVideoRemixIds(int take);

    IQueryable<Frever.Shared.MainDb.Entities.Video> GetAvailableVideos(IEnumerable<long> videoIds, IEnumerable<long> blockedGroupIds);

    Task<long[]> GetTrendingExternalSongIds(int take);

    Task<ExternalSong[]> GetAvailableExternalSongs(IEnumerable<long> songIds);

    Task<GeneratedContent[]> GetAvailableGeneratedContent(IEnumerable<long> contentIds);
}

public sealed class CreatePageRepository(IWriteDb writeDb, IReadDb readDb) : ICreatePageRepository
{
    public Task<ContentRow[]> GetContentRows()
    {
        return writeDb.ContentRow.Where(e => e.IsEnabled).OrderBy(e => e.SortOrder).AsNoTracking().ToArrayAsync();
    }

    public Task<long[]> GetTrendingVideoRemixIds(int take)
    {
        return readDb.Video.Where(v => v.Access == VideoAccess.Public && v.PublishTypeId != KnownVideoTypes.VideoMessageId)
                     .Where(v => !v.IsDeleted && v.LevelId != null)
                     .Where(v => v.Group.DeletedAt == null && !v.Group.IsBlocked)
                     .Where(v => !readDb.VideoReport.Any(r => r.VideoId == v.Id && r.HideVideo))
                     .OrderByDescending(v => v.VideoKpi.Remixes)
                     .Take(take)
                     .Select(e => e.Id)
                     .ToArrayAsync();
    }

    public IQueryable<Frever.Shared.MainDb.Entities.Video> GetAvailableVideos(IEnumerable<long> videoIds, IEnumerable<long> blockedGroupIds)
    {
        return readDb.Video.Where(v => videoIds.Contains(v.Id) && !blockedGroupIds.Contains(v.GroupId))
                     .Where(v => !v.IsDeleted && v.Access == VideoAccess.Public && v.PublishTypeId != KnownVideoTypes.VideoMessageId)
                     .Where(v => v.Group.DeletedAt == null && !v.Group.IsBlocked)
                     .Where(v => !readDb.VideoReport.Any(r => r.VideoId == v.Id && r.HideVideo));
    }

    public async Task<long[]> GetTrendingExternalSongIds(int take)
    {
        var date = DateTime.UtcNow.AddDays(-7);

        var sql = $"""
                   select mc."ExternalTrackId"
                   from "MusicController" as mc
                         inner join "Event" as e on mc."EventId" = e."Id"
                         inner join "ExternalSong" es on es."ExternalTrackId" = mc."ExternalTrackId"
                   where e."CreatedTime" >= '{date:yyyy-MM-dd HH:mm:ss}'
                   and mc."ExternalTrackId" is not null
                   and not es."IsManuallyDeleted"
                   and not es."IsDeleted"
                   and es."NotClearedSince" is null
                   group by mc."ExternalTrackId"
                   order by count(*)::int desc
                   limit {take}
                   """;

        var result = await readDb.SqlQueryRaw<long>(sql).ToArrayAsync();
        if (result.Length >= take * 0.8)
            return result;

        var missing = await readDb.ExternalSongs.Where(e => !result.Contains(e.Id))
                                  .Where(e => !e.IsManuallyDeleted && !e.IsDeleted && e.NotClearedSince == null)
                                  .OrderByDescending(e => e.UsageCount)
                                  .Select(e => e.Id)
                                  .Take(take - result.Length)
                                  .ToArrayAsync();

        return result.Concat(missing).ToArray();
    }

    public Task<ExternalSong[]> GetAvailableExternalSongs(IEnumerable<long> songIds)
    {
        return readDb.ExternalSongs.Where(e => !e.IsManuallyDeleted && !e.IsDeleted && e.NotClearedSince == null)
                     .Where(e => songIds.Contains(e.Id))
                     .ToArrayAsync();
    }

    public Task<GeneratedContent[]> GetAvailableGeneratedContent(IEnumerable<long> contentIds)
    {
        return writeDb.AiGeneratedContent.Where(c => c.DeletedAt == null && contentIds.Contains(c.Id))
                      .Where(c => c.Type == AiGeneratedContent.KnownTypeImage && writeDb.Video.Any(v => v.AiContentId == c.Id))
                      .Join(writeDb.Group, e => e.GroupId, i => i.Id, (e, i) => new {Content = e, Group = i})
                      .Join(
                           writeDb.AiGeneratedImage,
                           e => e.Content.AiGeneratedImageId,
                           i => i.Id,
                           (e, i) => new GeneratedContent {Content = e.Content, Image = i, Group = e.Group}
                       )
                      .AsNoTracking()
                      .ToArrayAsync();
    }
}

public class GeneratedContent
{
    public AiGeneratedContent Content { get; set; }
    public AiGeneratedImage Image { get; set; }
    public Group Group { get; set; }
}