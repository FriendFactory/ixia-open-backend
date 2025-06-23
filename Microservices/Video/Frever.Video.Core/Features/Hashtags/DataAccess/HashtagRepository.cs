using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Frever.Shared.MainDb;
using Frever.Shared.MainDb.Entities;
using Microsoft.EntityFrameworkCore;

namespace Frever.Video.Core.Features.Hashtags.DataAccess;

public interface IHashtagRepository
{
    IQueryable<Hashtag> GetAllAsNoTracking();

    Task<Hashtag[]> GetHashtagByIds(IEnumerable<long> hashtagIds);

    Task<long[]> GetTrendingHashtagIds(int take);

    Task<List<VideoWithSong>> GetVideoByHashtagId(
        GeoCluster geoCluster,
        long currentGroupId,
        long hashtagId,
        long target,
        int takeNext
    );
}

internal sealed class PersistentHashtagRepository(IReadDb db) : IHashtagRepository
{
    public IQueryable<Hashtag> GetAllAsNoTracking()
    {
        return db.Hashtag.AsNoTracking();
    }

    public Task<Hashtag[]> GetHashtagByIds(IEnumerable<long> hashtagIds)
    {
        return db.Hashtag.Where(e => !e.IsDeleted && hashtagIds.Contains(e.Id)).ToArrayAsync();
    }

    public Task<long[]> GetTrendingHashtagIds(int take)
    {
        var date = DateTime.UtcNow.AddDays(-7);

        var sql = $"""
                   with U as (
                       select vh."HashtagId", count(vh."VideoId") as UsageCount
                       from "VideoAndHashtag" vh
                       inner join "Video" v on vh."VideoId" = v."Id"
                       where v."CreatedTime" >= '{date:yyyy-MM-dd HH:mm:ss}'
                       group by vh."HashtagId"
                   )
                   select h."Id"
                   from "Hashtag" h
                   left join U on h."Id" = U."HashtagId"
                   order by U.UsageCount desc nulls last, h."Name"
                   limit {take}
                   """;

        return db.SqlQueryRaw<long>(sql).ToArrayAsync();
    }

    public Task<List<VideoWithSong>> GetVideoByHashtagId(
        GeoCluster geoCluster,
        long currentGroupId,
        long hashtagId,
        long target,
        int takeNext
    )
    {
        return db.GetVideoByHashtagIdQuery(geoCluster, currentGroupId, hashtagId, target).Take(takeNext).ToListAsync();
    }
}