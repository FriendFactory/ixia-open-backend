using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AssetStoragePathProviding;
using AuthServerShared;
using Common.Infrastructure.Utils;
using Frever.Client.Shared.Social.Services;
using Frever.Shared.MainDb.Entities;
using Frever.Video.Contract;
using Frever.Video.Core.Features.AssetUrlGeneration;
using Frever.Video.Core.Features.VideoInformation.DataAccess;
using Frever.Videos.Shared.CachedVideoKpis;

namespace Frever.Video.Core.Features.VideoInfoExtraData;

public interface IVideoExtraDataProvider
{
    Task SetExtraVideoInfo(params VideoInfo[] videos);
}

public class VideoExtraDataProvider(
    IFollowRelationService followRelationService,
    UserInfo currentUser,
    IVideoKpiCachingService kpiCachingService,
    ISocialSharedService socialSharedService,
    IVideoExtraDataRepository repo,
    IVideoAssetUrlGenerator urlGenerator,
    VideoNamingHelper videoNamingHelper
) : IVideoExtraDataProvider
{
    private const int FollowLikesCount = 3;

    public async Task SetExtraVideoInfo(params VideoInfo[] videos)
    {
        var videoIds = videos.Select(v => v.Id).ToArray();
        var groupIds = videos.SelectMany(v => new[] {v.GroupId, v.OriginalCreator?.Id})
                             .Where(v => v.HasValue)
                             .Select(v => v.Value)
                             .Distinct()
                             .ToArray();

        var followRelations = await followRelationService.GetFollowRelations(currentUser, new HashSet<long>(groupIds));

        var ownerIds = videos.Select(e => e.GroupId).Where(e => !followRelations.ContainsKey(e) || !followRelations[e].IsFollowed);
        var date = DateTime.UtcNow.AddDays(-7).StartOfDay();
        var followGroupIds = await repo.GetFollowGroupIds(currentUser.UserId, ownerIds, date);

        var kpis = await kpiCachingService.GetVideosKpis(videoIds);
        var groupInfo = await socialSharedService.GetGroupShortInfo(groupIds);
        var likedVideos = (await repo.GetLikedVideoIds(currentUser.UserId, videoIds)).ToHashSet();

        var tagged = videos.SelectMany(e => e.TaggedGroups)
                           .Concat(videos.SelectMany(e => e.Mentions))
                           .Select(e => e.GroupId)
                           .Distinct()
                           .ToArray();
        var blockedGroups = await socialSharedService.GetBlocked(currentUser, tagged);

        foreach (var video in videos)
        {
            video.Kpi = kpis.GetValueOrDefault(video.Id, new VideoKpi {VideoId = video.Id});
            video.LikedByCurrentUser = likedVideos.Contains(video.Id);

            video.Owner = groupInfo.GetValueOrDefault(video.GroupId);
            video.OriginalCreator = groupInfo.GetValueOrDefault(video.OriginalCreator?.Id ?? 0);

            if (followRelations.TryGetValue(video.GroupId, out var fr))
            {
                video.IsFollowed = fr.IsFollowed;
                video.IsFollower = fr.IsFollower;
                video.IsFriend = fr.IsFriend;
            }

            video.IsFollowRecommended = followGroupIds.GetValueOrDefault(video.GroupId) >= FollowLikesCount;

            await UpdateVideoInfoUrls(video);
            RemoveVideoInfoBlockedUsers(blockedGroups, video);
        }
    }

    private async Task UpdateVideoInfoUrls(VideoInfo video)
    {
        video.ThumbnailUrl = urlGenerator.GetThumbnailUrl(video);
        video.RedirectUrl = videoNamingHelper.GetVideoUrl(video);

        var cookies = await urlGenerator.CreateSignedCookie(video);
        video.SignedCookies = new Dictionary<string, string>
                              {
                                  {cookies.Policy.Key, cookies.Policy.Value},
                                  {cookies.Signature.Key, cookies.Signature.Value},
                                  {cookies.KeyPairId.Key, cookies.KeyPairId.Value}
                              };
    }

    private static void RemoveVideoInfoBlockedUsers(long[] blockedGroups, VideoInfo video)
    {
        if (video.Mentions != null)
        {
            var blockedMentions = video.Mentions.Where(g => blockedGroups.Contains(g.GroupId)).Distinct().ToHashSet();

            if (!string.IsNullOrWhiteSpace(video.Description))
                foreach (var mention in blockedMentions)
                    video.Description = video.Description.Replace($"@{mention.GroupId}", "");

            video.Mentions = video.Mentions.Except(blockedMentions).ToList();
        }

        video.TaggedGroups = video.TaggedGroups.Where(g => !blockedGroups.Contains(g.GroupId)).ToArray();
    }
}