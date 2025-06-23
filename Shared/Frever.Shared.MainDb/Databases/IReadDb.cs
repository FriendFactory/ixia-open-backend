using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Frever.Shared.MainDb.Entities;
using Microsoft.EntityFrameworkCore.Query;

namespace Frever.Shared.MainDb;

public interface IReadDb
{
    IQueryable<Country> Country { get; }
    IQueryable<ExternalPlaylist> ExternalPlaylists { get; }
    IQueryable<ExternalSong> ExternalSongs { get; }
    IQueryable<Gender> Gender { get; }
    IQueryable<Genre> Genre { get; }
    IQueryable<GeoCluster> GeoCluster { get; }
    IQueryable<Group> Group { get; }
    IQueryable<Hashtag> Hashtag { get; }
    IQueryable<Language> Language { get; }
    IQueryable<Localization> Localization { get; }
    IQueryable<PromotedSong> PromotedSong { get; }
    IQueryable<Song> Song { get; }
    IQueryable<User> User { get; }
    IQueryable<Video> Video { get; }
    IQueryable<VideoKpi> VideoKpi { get; }
    IQueryable<VideoReport> VideoReport { get; }
    IQueryable<VideoView> VideoView { get; }
    IQueryable<VideoGroupTag> VideoGroupTag { get; }

    IQueryable<VideoWithSong> GetTrendingVideoQuery(GeoCluster geoCluster, long currentGroupId, int videosCount, long target);
    IQueryable<VideoWithSong> GetVideoByHashtagIdQuery(GeoCluster geoCluster, long currentGroupId, long hashtagId, long target);
    IQueryable<VideoWithSong> GetSoundVideoQuery(long currentGroupId, long soundId, string soundType, long target);

    Task<List<VideoWithSong>> GetFeaturedVideoIds(
        GeoCluster geoCluster,
        long currentGroupId,
        long target,
        int takeNext,
        int takePrevious
    );

    Task<List<VideoWithSong>> GetRemixesOfVideo(
        long videoId,
        long currentGroupId,
        long target,
        int takeNext,
        int takePrevious
    );

    Task<List<VideoWithSong>> GetTaggedGroupVideoQuery(
        long groupId,
        long currentGroupId,
        long target,
        int takeNext,
        int takePrevious
    );

    Task<List<VideoWithSong>> GetFriendVideoQuery(long currentGroupId, long target, int takeNext, int takePrevious);

    Task<List<VideoWithSong>> GetFollowingVideoQuery(long currentGroupId, long target, int takeNext, int takePrevious);

    Task<List<VideoWithSong>> GetGroupAvailableVideoQuery(
        long groupId,
        long currentGroupId,
        long target,
        int takeNext,
        int takePrevious,
        bool withTaskVideos,
        bool sortBySortOrder
    );

    IQueryable<GroupWithAge> GetGroupWithAgeInfo(long groupId, long geoClusterId);

    IQueryable<FriendInfo> GetRankedFriendList(long groupId);

    IQueryable<TResult> SqlQuery<TResult>([NotParameterized] FormattableString sql);

    IQueryable<TResult> SqlQueryRaw<TResult>([NotParameterized] string sql, params object[] parameters);
}