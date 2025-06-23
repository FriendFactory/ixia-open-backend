using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Common.Infrastructure.Videos;
using Common.Models;
using Frever.Shared.MainDb.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace Frever.Shared.MainDb;

public partial class MainDbContext
{
    public async Task<IQueryable<Video>> GetGroupAvailableVideoQuery(long groupId, long currentGroupId)
    {
        var sql = await GetGroupAvailableVideoSql(groupId, currentGroupId, null, "v.*");

        return Video.FromSqlRaw(sql);
    }

    public async Task<List<VideoWithSong>> GetGroupAvailableVideoQuery(
        long groupId,
        long currentGroupId,
        long target,
        int takeNext,
        int takePrevious,
        bool withTaskVideos,
        bool sortBySortOrder
    )
    {
        if (!sortBySortOrder)
        {
            var byIdSql = await GetGroupAvailableVideoSql(
                              groupId,
                              currentGroupId,
                              withTaskVideos,
                              """ v."Id", v."SongInfo", v."Id" as "Key" """
                          );
            return await GetPaginatedByPropDesc(
                       byIdSql,
                       "v.\"Id\"",
                       target,
                       takeNext,
                       takePrevious
                   );
        }

        var columns = $"""
                       v."Id", v."SongInfo",
                       case when v."PinOrder" is not null then cast({int.MaxValue - 1000} - coalesce(v."PinOrder", 0) as bigint) else v."Id" end as "Key"
                       """;

        var sql = $"""
                   select *
                   from (select v."Id", v."SongInfo", v."Key"
                         from (
                             {await GetGroupAvailableVideoSql(groupId, currentGroupId, withTaskVideos, columns)}
                         ) as v
                         order by v."Key" desc
                   ) as v where true
                   """;
        return await GetPaginatedByPropDesc(
                   sql,
                   "v.\"Key\"",
                   target,
                   takeNext,
                   takePrevious
               );
    }

    public IQueryable<VideoWithSong> GetSoundVideoQuery(long currentGroupId, long soundId, string soundType, long target)
    {
        var sql = $"""
                   select *
                   from (select v."Id", v."SongInfo", ROW_NUMBER() OVER (order by k.likes desc) as "Key"
                         from "Video" v
                                  inner join "Group" g on v."GroupId" = g."Id" and g."DeletedAt" is null and g."IsBlocked" = false
                                  inner join stats.video_kpi k on v."Id" = k.video_id
                                  left join "VideoReport" vr on v."Id" = vr."VideoId" and vr."HideVideo"
                         where v."Access" = '{VideoAccess.Public}'
                           and v."PublishTypeId" <> {KnownVideoTypes.VideoMessageId}
                           and v."IsDeleted" = false
                           and vr."VideoId" is null
                           and not exists(select 1
                                          from "BlockedUser" b
                                          where (b."BlockedByUserId" = g."Id" and b."BlockedUserId" = {currentGroupId})
                                             or (b."BlockedByUserId" = {currentGroupId} and b."BlockedUserId" = g."Id"))
                           and exists(select 1
                                      from "Event" e
                                               inner join "MusicController" mc on mc."EventId" = e."Id" and mc."{soundType}" = {soundId}
                                      where e."LevelId" = v."LevelId")
                         order by k.likes desc) as videos
                   where "Key" >= {target}
                   """;

        return Database.SqlQueryRaw<VideoWithSong>(sql);
    }

    public IQueryable<VideoWithSong> GetVideoByHashtagIdQuery(GeoCluster geoCluster, long currentGroupId, long hashtagId, long target)
    {
        var languageFilter = LanguageVideoFilter(geoCluster);
        var countryFilter = CountryVideoFilter(geoCluster);

        var sql = $"""
                   select *
                   from (select v."Id", v."SongInfo", v."Id" as "Key"
                         from "Video" v
                                  inner join "VideoAndHashtag" vh on v."Id" = vh."VideoId"
                                  inner join "Group" g on g."Id" = v."GroupId" and g."DeletedAt" is null and g."IsBlocked" = false
                                  left join "VideoReport" vr on v."Id" = vr."VideoId" and vr."HideVideo"
                         where vh."HashtagId" = {hashtagId}
                           and v."Access" = '{VideoAccess.Public}'
                           and v."PublishTypeId" <> {KnownVideoTypes.VideoMessageId}
                           and v."IsDeleted" = false
                           and vr."VideoId" is null
                           and not exists(select 1
                                          from "BlockedUser" b
                                          where (b."BlockedByUserId" = g."Id" and b."BlockedUserId" = {currentGroupId})
                                             or (b."BlockedByUserId" = {currentGroupId} and b."BlockedUserId" = g."Id"))
                           {languageFilter}
                           {countryFilter}
                         order by v."Id" desc) as videos
                   where "Key" <= {target}
                   """;

        return Database.SqlQueryRaw<VideoWithSong>(sql);
    }

    public IQueryable<VideoWithSong> GetTrendingVideoQuery(GeoCluster geoCluster, long currentGroupId, int videosCount, long target)
    {
        var languageFilter = LanguageVideoFilter(geoCluster);
        var countryFilter = CountryVideoFilter(geoCluster);

        var sql = $"""
                   select *
                   from (select v."Id", v."SongInfo", ROW_NUMBER() OVER (order by v.likes desc) as "Key"
                         from (select v."Id", v."SongInfo", k.likes
                               from "Video" as v
                                 inner join "Group" g on v."GroupId" = g."Id" and g."DeletedAt" is null and g."IsBlocked" = false
                                 inner join stats.video_kpi k on v."Id" = k.video_id
                                 left join "VideoReport" vr on v."Id" = vr."VideoId" and vr."HideVideo"
                               where v."Access" = '{VideoAccess.Public}'
                                 and v."PublishTypeId" <> {KnownVideoTypes.VideoMessageId}
                                 and v."IsDeleted" = false
                                 and vr."VideoId" is null
                                 and not exists(select 1
                                                from "BlockedUser" b
                                                where (b."BlockedByUserId" = g."Id" and b."BlockedUserId" = {currentGroupId})
                                                   or (b."BlockedByUserId" = {currentGroupId} and b."BlockedUserId" = g."Id"))
                                 {languageFilter}
                                 {countryFilter}
                               order by v."Id" desc
                               limit {videosCount}) as v
                         order by v.likes desc) as videos
                   where videos."Key" >= {target}
                   """;

        return Database.SqlQueryRaw<VideoWithSong>(sql);
    }

    public Task<int> GetTaggedGroupVideoCount(long groupId, long currentGroupId)
    {
        var taggedSql = GetTaggedGroupVideoIdQuery(groupId, currentGroupId);

        return Video.FromSqlRaw(taggedSql).CountAsync();
    }

    public Task<List<VideoWithSong>> GetTaggedGroupVideoQuery(
        long groupId,
        long currentGroupId,
        long target,
        int takeNext,
        int takePrevious
    )
    {
        var sql = $"""
                   select v."Id", v."SongInfo", v."Id" as "Key"
                   from "Video" v
                   join (
                       {GetTaggedGroupVideoIdQuery(groupId, currentGroupId)}
                   ) as tv on tv."Id" = v."Id"
                   """;

        return GetPaginatedByPropDesc(
            sql,
            "v.\"Id\"",
            target,
            takeNext,
            takePrevious
        );
    }

    public Task<List<VideoWithSong>> GetFriendVideoQuery(long currentGroupId, long target, int takeNext, int takePrevious)
    {
        var access = new[] {VideoAccess.Public, VideoAccess.ForFriends, VideoAccess.ForFollowers, VideoAccess.ForTaggedGroups};

        var sql = $"""
                   select v."Id", v."SongInfo", v."Id" as "Key"
                   from "Video" v
                        inner join "Group" g on v."GroupId" = g."Id" and g."DeletedAt" is null and g."IsBlocked" = false
                        left join "VideoReport" vr on v."Id" = vr."VideoId" and vr."HideVideo"
                        left join "Follower" f on v."GroupId" = f."FollowingId" and f."FollowerId" = {currentGroupId} and f."IsMutual"
                        left join "VideoGroupTag" vgt on v."Id" = vgt."VideoId" and vgt."GroupId" = {currentGroupId}
                   where v."Access" = any (array[{string.Join(',', access.Select(e => $"'{e}'"))}]::"VideoAccess"[])
                     and not (v."IsDeleted")
                     and v."PublishTypeId" <> {KnownVideoTypes.VideoMessageId}
                     and vr."VideoId" is null
                     and f."FollowingId" is not null
                     and (v."Access" <> '{VideoAccess.ForTaggedGroups}' or vgt."VideoId" is not null)
                   """;

        return GetPaginatedByPropDesc(
            sql,
            "v.\"Id\"",
            target,
            takeNext,
            takePrevious
        );
    }

    public Task<List<VideoWithSong>> GetFollowingVideoQuery(long currentGroupId, long target, int takeNext, int takePrevious)
    {
        var access = new[] {VideoAccess.Public, VideoAccess.ForFollowers, VideoAccess.ForTaggedGroups};

        var sql = $"""
                   select v."Id", v."SongInfo", v."Id" as "Key"
                   from "Video" v
                        inner join "Group" g on v."GroupId" = g."Id" and g."DeletedAt" is null and g."IsBlocked" = false
                        left join "VideoReport" vr on v."Id" = vr."VideoId" and vr."HideVideo"
                        left join "Follower" f on v."GroupId" = f."FollowingId" and f."FollowerId" = {currentGroupId}
                        left join "VideoGroupTag" vgt on v."Id" = vgt."VideoId" and vgt."GroupId" = {currentGroupId}
                   where v."Access" = any (array[{string.Join(',', access.Select(e => $"'{e}'"))}]::"VideoAccess"[])
                     and not (v."IsDeleted")
                     and v."PublishTypeId" <> {KnownVideoTypes.VideoMessageId}
                     and vr."VideoId" is null
                     and f."FollowingId" is not null
                     and (v."Access" <> '{VideoAccess.ForTaggedGroups}' or vgt."VideoId" is not null)
                   """;

        return GetPaginatedByPropDesc(
            sql,
            "v.\"Id\"",
            target,
            takeNext,
            takePrevious
        );
    }

    public Task<List<VideoWithSong>> GetFeaturedVideoIds(
        GeoCluster geoCluster,
        long currentGroupId,
        long target,
        int takeNext,
        int takePrevious
    )
    {
        var languageFilter = LanguageVideoFilter(geoCluster);
        var countryFilter = CountryVideoFilter(geoCluster);

        var sql = $"""
                   with featured_group_ids as (
                       select distinct u."MainGroupId" as groupId
                       from "User" u
                       where u."IsFeatured"
                   )
                   {PublicVideosSql(currentGroupId)}
                   and (v."ToplistPosition" is not null or exists (select 1 from featured_group_ids u where u.groupId = v."GroupId"))
                   {languageFilter}
                   {countryFilter}
                   """;

        return GetPaginatedByPropDesc(
            sql,
            "v.\"Id\"",
            target,
            takeNext,
            takePrevious
        );
    }

    public Task<List<VideoWithSong>> GetRemixesOfVideo(
        long videoId,
        long currentGroupId,
        long target,
        int takeNext,
        int takePrevious
    )
    {
        var sql = $"""
                   {PublicVideosSql(currentGroupId)}
                   and v."RemixedFromVideoId" = {videoId}
                   """;

        return GetPaginatedByPropDesc(
            sql,
            "v.\"Id\"",
            target,
            takeNext,
            takePrevious
        );
    }

    private static string PublicVideosSql(long currentGroupId)
    {
        return $"""
                select v."Id", v."SongInfo", v."Id" as "Key"
                from "Video" v
                inner join "Group" g on v."GroupId" = g."Id" and not g."IsBlocked" and g."DeletedAt" is null
                where v."Access" = 'Public' and not v."IsDeleted" and v."PublishTypeId" <> {KnownVideoTypes.VideoMessageId}
                and not exists (select 1 from "VideoReport" vr where vr."VideoId" = v."Id" and vr."HideVideo")
                and not exists(select 1
                               from "BlockedUser" b
                               where (b."BlockedByUserId" = g."Id" and b."BlockedUserId" = {currentGroupId})
                                  or (b."BlockedByUserId" = {currentGroupId} and b."BlockedUserId" = g."Id"))
                """;
    }

    private async Task<List<VideoWithSong>> GetPaginatedByPropDesc(
        string sql,
        string prop,
        long target,
        int takeNext,
        int takePrevious
    )
    {
        var list = new List<VideoWithSong>();

        // Video feed should be ordered by Id desc
        // So for takeNext we should take N videos with Ids less than (or equal) target
        if (takeNext > 0)
        {
            var takeNextSql = $"""
                               {sql}
                               and {prop} <= {target}
                               order by {prop} desc
                               limit {takeNext}
                               """;
            var page = await Database.SqlQueryRaw<VideoWithSong>(takeNextSql).ToArrayAsync();
            list.AddRange(page);
        }

        if (takePrevious > 0)
        {
            var takePreviousSql = $"""
                                   {sql}
                                   and {prop} > {target}
                                   order by {prop} asc
                                   limit {takePrevious}
                                   """;

            var page = await Database.SqlQueryRaw<VideoWithSong>(takePreviousSql).ToArrayAsync();
            list = page.Reverse().Concat(list).ToList();
        }

        return list;
    }

    private static string GetTaggedGroupVideoIdQuery(long groupId, long currentGroupId)
    {
        var taggedSql = $"""
                         with blocked_users as (
                             select distinct(blocked) from
                             (
                                 select "BlockedUserId" as blocked from "BlockedUser" where "BlockedByUserId" = {currentGroupId}
                                 union all
                                 select "BlockedByUserId" as blocked from "BlockedUser" where "BlockedUserId" = {currentGroupId}
                             ) as tmp where blocked != {currentGroupId}
                         ),
                         video_for_followers as (
                             select v0."Id" as videoId
                                 from "VideoGroupTag" as v
                                 inner join "Video" as v0
                                    on v."VideoId" = v0."Id" and v0."Access" = '{VideoAccess.ForFollowers}' and not v0."IsDeleted" and v0."PublishTypeId" <> {KnownVideoTypes.VideoMessageId}
                                 inner join "Group" g on v0."GroupId" = g."Id" and not g."IsBlocked" and g."DeletedAt" is null
                                 inner join "Follower" f on f."FollowingId" = g."Id"
                                 where f."FollowerId" = {currentGroupId} and not g."Id" in (select blocked from blocked_users) and v."GroupId" = {groupId}
                         ),
                         video_for_friends as (
                             select v0."Id" as videoId
                                 from "VideoGroupTag" AS v
                                 inner join "Video" AS v0
                                    on v."VideoId" = v0."Id" and v0."Access" = '{VideoAccess.ForFriends}' and not v0."IsDeleted" and v0."PublishTypeId" <> {KnownVideoTypes.VideoMessageId}
                                 inner join "Group" g on v0."GroupId" = g."Id" and not g."IsBlocked" and g."DeletedAt" is null
                                 inner join "Follower" f on f."FollowingId" = g."Id"
                                 where f."FollowerId" = {currentGroupId} and f."IsMutual" and not g."Id" in (select blocked from blocked_users) and v."GroupId" = {groupId}
                         ),
                         tmp as (
                             select v0."Id" as videoId
                                 from "VideoGroupTag" as v
                                 inner join "Video" as v0 on v."VideoId" = v0."Id"
                                 where ((v."GroupId" = {groupId}) and not (v0."IsDeleted") and v0."PublishTypeId" <> {KnownVideoTypes.VideoMessageId})
                                     and (
                                         (v0."GroupId" = {currentGroupId})
                                         or (
                                             not (v0."GroupId" in (select blocked from blocked_users))
                                             and (
                                                 v0."Access" = '{VideoAccess.Public}'
                                                 or ((v0."Access" = '{VideoAccess.ForTaggedGroups}')
                                                     and exists (
                                                             select 1
                                                             from "VideoGroupTag" AS v1
                                                             where (v0."Id" = v1."VideoId") and (v1."GroupId" = {currentGroupId})
                                                         )
                                                    )
                                                 )
                                        )
                                     )
                         ),
                         allVideos as (
                             select videoId from video_for_followers
                             union all
                             select videoId from video_for_friends
                             union all
                             select videoId from tmp
                         )
                         select distinct(videoId) as "Id" from allVideos
                         """;

        return taggedSql;
    }

    private async Task<string> GetGroupAvailableVideoSql(long groupId, long currentGroupId, bool? withTaskVideos, string columns)
    {
        var taskSql = withTaskVideos switch
                      {
                          null => string.Empty,
                          true => """ and v."SchoolTaskId" is not null """,
                          false => """ and v."SchoolTaskId" is null """
                      };

        if (groupId == currentGroupId)
        {
            var access = Enum.GetValues(typeof(VideoAccess)).Cast<VideoAccess>().Select(e => $"'{e}'");

            return $"""
                    select {columns}
                    from "Video" v
                    where v."GroupId" = {groupId}
                      and not v."IsDeleted"
                      and v."PublishTypeId" <> {KnownVideoTypes.VideoMessageId}
                      and v."Access" = any (array[{string.Join(',', access)}]::"VideoAccess"[])
                      {taskSql}
                    """;
        }

        var follower = await Follower.Where(e => e.FollowerId == currentGroupId && e.FollowingId == groupId)
                                     .Select(e => new {e.IsMutual})
                                     .FirstOrDefaultAsync();

        return $"""
                select {columns}
                from "Video" v
                    left join "VideoReport" vr on v."Id" = vr."VideoId" and vr."HideVideo"
                    left join "VideoGroupTag" vgt on v."Id" = vgt."VideoId" and vgt."GroupId" = {currentGroupId}
                where v."GroupId" = {groupId}
                  and not v."IsDeleted"
                  and v."PublishTypeId" <> {KnownVideoTypes.VideoMessageId}
                  and vr."VideoId" is null
                  {taskSql}
                  and (v."Access" = '{VideoAccess.Public}'
                  {(follower == null ? string.Empty : $"""or v."Access" = '{VideoAccess.ForFollowers}'""")}
                                  {(follower == null || !follower.IsMutual ? string.Empty : $"""or v."Access" = '{VideoAccess.ForFriends}'""")}
                                  or (v."Access" = '{VideoAccess.ForTaggedGroups}' and vgt."VideoId" is not null))
                """;
    }

    private static string CountryVideoFilter(GeoCluster geoCluster)
    {
        var countryFilter = string.Empty;

        var includeVideoFromCountry = geoCluster.IncludeVideoFromCountry ?? [];
        var excludeVideoFromCountry = geoCluster.ExcludeVideoFromCountry ?? [];

        if (includeVideoFromCountry.Length != 0 && !includeVideoFromCountry.Contains(Constants.Wildcard))
            countryFilter += $""" and v."Country" in ({string.Join(",", includeVideoFromCountry.Select(i => "'" + i + "'"))})""";

        if (includeVideoFromCountry.IsNullOrEmpty())
            countryFilter += " and false";

        if (excludeVideoFromCountry.Length != 0 && !excludeVideoFromCountry.Contains(Constants.Wildcard))
            countryFilter += $""" and v."Country" not in ({string.Join(",", excludeVideoFromCountry.Select(i => "'" + i + "'"))})""";

        if (excludeVideoFromCountry.Length != 0 && excludeVideoFromCountry.Contains("*"))
            countryFilter += " and false";

        return countryFilter;
    }

    private static string LanguageVideoFilter(GeoCluster geoCluster)
    {
        var languageFilter = string.Empty;

        var includeVideoWithLanguage = geoCluster.IncludeVideoWithLanguage ?? [];
        var excludeVideoWithLanguage = geoCluster.ExcludeVideoWithLanguage ?? [];

        if (includeVideoWithLanguage.Length != 0 && !includeVideoWithLanguage.Contains(Constants.Wildcard))
            languageFilter += $""" and v."Language" in ({string.Join(",", includeVideoWithLanguage.Select(i => "'" + i + "'"))})""";

        if (includeVideoWithLanguage.IsNullOrEmpty())
            languageFilter += " and false";

        if (excludeVideoWithLanguage.Length != 0 && !excludeVideoWithLanguage.Contains(Constants.Wildcard))
            languageFilter += $""" and v."Language" not in ({string.Join(",", excludeVideoWithLanguage.Select(i => "'" + i + "'"))})""";

        if (excludeVideoWithLanguage.Length != 0 && excludeVideoWithLanguage.Contains("*"))
            languageFilter += " and false";

        return languageFilter;
    }
}