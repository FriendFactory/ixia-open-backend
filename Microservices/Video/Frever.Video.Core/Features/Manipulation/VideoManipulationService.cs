using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using AuthServer.Permissions.Services;
using AuthServerShared;
using Common.Infrastructure;
using Common.Infrastructure.Caching;
using Common.Infrastructure.Caching.CacheKeys;
using Common.Infrastructure.Messaging;
using Frever.Client.Core.Features.AI.UserGeneratedContent.Content;
using Frever.Client.Shared.ActivityRecording;
using Frever.Shared.MainDb.Entities;
using Frever.Video.Contract;
using Frever.Video.Core.Features.AssetUrlGeneration;
using Frever.Video.Core.Features.Caching;
using Frever.Video.Core.Features.Manipulation.DataAccess;
using Frever.Video.Core.Features.Shared;
using Frever.Videos.Shared.CachedVideoKpis;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NotificationService;
using NotificationService.Client.Messages;
using VideoInfo = Frever.Video.Contract.VideoInfo;

#pragma warning disable CS8618, CS8602

namespace Frever.Video.Core.Features.Manipulation;

internal sealed class VideoManipulationService(
    IVideoManipulationRepository repo,
    IOneVideoAccessor oneVideoAccessor,
    ILogger<VideoManipulationService> log,
    INotificationAddingService notificationService,
    IVideoCachingService videoCachingService,
    IUserPermissionService userPermissionService,
    IUserActivityRecordingService userActivityRecordingService,
    ICache cache,
    UserInfo currentUser,
    IVideoKpiCachingService kpiCachingService,
    ISnsMessagingService snsMessagingService,
    ITaggingGroupProvider taggingGroup,
    IAiGeneratedContentService aiGeneratedContentService
) : IVideoManipulationService
{
    private const int MaxPinnedVideos = 3;

    public async Task<VideoInfo> LikeVideo(long videoId)
    {
        await userPermissionService.EnsureCurrentUserActive();

        var video = await GetVideoById(videoId);
        if (video == null)
            throw new AppErrorWithStatusCodeException("Video is not found or not available", HttpStatusCode.NotFound);

        log.LogTrace("User {CurrentUserId} likes video {VideoId}", currentUser.UserMainGroupId, videoId);

        var likeAdded = await repo.LikeVideo(videoId, currentUser.UserId);
        if (!likeAdded)
            return await GetVideoById(videoId);

        await kpiCachingService.AddVideoLike(videoId);

        await userActivityRecordingService.OnVideoLike(videoId, video.GroupId);

        if (video.GroupId != currentUser)
            await notificationService.NotifyNewLikeOnVideo(
                new NotifyNewLikeOnVideoMessage {VideoId = video.Id, CurrentGroupId = currentUser}
            );

        return await GetVideoById(videoId);
    }

    public async Task<VideoInfo> UnlikeVideo(long videoId)
    {
        await userPermissionService.EnsureCurrentUserActive();

        var video = await GetVideoById(videoId);
        if (video == null)
            throw new AppErrorWithStatusCodeException("Video is not found or not available", HttpStatusCode.NotFound);

        log.LogTrace("User {CurrentUserId} DOES NOT likes video {VideoId}", currentUser.UserMainGroupId, videoId);

        var likeRemoved = await repo.UnlikeVideo(videoId, currentUser.UserId);
        if (!likeRemoved)
            return await GetVideoById(videoId);

        await kpiCachingService.RemoveVideoLike(videoId);

        await snsMessagingService.PublishSnsMessageForVideoUnliked(videoId, currentUser, DateTime.UtcNow);

        return await GetVideoById(videoId);
    }

    public async Task<VideoInfo> UpdateVideoAccess(long videoId, UpdateVideoAccessRequest model)
    {
        await userPermissionService.EnsureCurrentUserActive();

        var video = await GetVideoById(videoId);
        if (video == null || video.GroupId != currentUser)
            throw new AppErrorWithStatusCodeException("Video is not found or not available", HttpStatusCode.NotFound);

        var taggedIds = await GetTaggedGroupIds(model);

        if (!await repo.UpdateVideoAccess(videoId, model.Access, taggedIds))
        {
            await videoCachingService.DeleteVideoDetailsCache(videoId);
            return video;
        }

        if (video.RemixedFromVideoId.HasValue)
            await UpdateRemixCount(videoId, model.Access, video.Access);

        await videoCachingService.DeleteVideoDetailsCache(videoId);

        if (model.Access == VideoAccess.Public)
            await notificationService.NotifyNewVideo(new NotifyNewVideoMessage {VideoId = videoId, CurrentGroupId = currentUser});

        return video;
    }

    public async Task<long[]> GetTaggedFriends(long videoId)
    {
        await userPermissionService.EnsureCurrentUserActive();

        var video = await GetVideoById(videoId);
        if (video == null)
            throw new AppErrorWithStatusCodeException("Video is not found or not available", HttpStatusCode.NotFound);

        return await repo.GetTaggedFriends(videoId);
    }

    public async Task<VideoInfo> SetPinned(long videoId, bool isPinned)
    {
        await userPermissionService.EnsureCurrentUserActive();

        var pinnedVideos = await repo.GetPinnedVideo(currentUser).OrderBy(v => v.PinOrder).ToArrayAsync();

        var video = await repo.GetVideoById(videoId).Where(v => v.GroupId == currentUser).FirstOrDefaultAsync();
        if (video == null)
            throw AppErrorWithStatusCodeException.NotFound("Video is not found", "NotFound");

        if (isPinned)
        {
            if (pinnedVideos.Any(v => v.Id == videoId))
                return await oneVideoAccessor.GetVideo(FetchVideoInfoFrom.WriteDb, currentUser, videoId);

            if (pinnedVideos.Length >= MaxPinnedVideos)
                throw AppErrorWithStatusCodeException.BadRequest("Unable to pin more video", "MaxVideosPinned");

            if (video.Access != VideoAccess.Public || video.IsDeleted)
                throw AppErrorWithStatusCodeException.BadRequest("Unable to pin non-public video", "PinPrivateVideo");

            var all = pinnedVideos.Append(video).ToArray();
            for (var i = 0; i < all.Length; i++)
                all[i].PinOrder = i + 1;

            await repo.SaveChanges();

            foreach (var item in all)
                await cache.DeleteKey(VideoCacheKeys.VideoInfoKey(item.Id));
        }
        else
        {
            if (pinnedVideos.All(v => v.Id != videoId))
                throw AppErrorWithStatusCodeException.BadRequest("Video were not pinned", "NotPinned");

            video.PinOrder = null;
            await repo.SaveChanges();

            await cache.DeleteKey(VideoCacheKeys.VideoInfoKey(video.Id));
        }

        return await oneVideoAccessor.GetVideo(FetchVideoInfoFrom.WriteDb, currentUser, videoId);
    }

    public async Task<VideoInfo> UpdateVideo(long videoId, VideoPatchRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        await userPermissionService.EnsureCurrentUserActive();

        var originalVideo = await repo.GetVideoById(videoId).Where(v => v.GroupId == currentUser).FirstOrDefaultAsync();

        if (originalVideo == null)
            throw AppErrorWithStatusCodeException.NotAuthorized("Cannot edit other person's video", "NotAuthorized");

        if (request.IsLinksChanged && await userPermissionService.IsCurrentUserStarCreator())
        {
            var normalizedLinks = CloudFrontVideoAssetUrlGenerator.NormalizeLinks(request.Links);
            await UpdateVideoLinks(videoId, normalizedLinks);
        }

        if (request.AllowComment != null || request.AllowRemix != null)
            await repo.UpdateVideoParams(videoId, request.AllowRemix, request.AllowComment);

        await videoCachingService.DeleteVideoDetailsCache(videoId);

        return await GetVideoById(videoId);
    }

    public async Task DeleteVideo(long videoId)
    {
        await userPermissionService.EnsureCurrentUserActive();

        var video = await GetVideoById(videoId);
        if (video is null || video.GroupId != currentUser)
            throw new AppErrorWithStatusCodeException("Video is not found or not available", HttpStatusCode.NotFound);

        if (video.IsDeleted)
            return;

        await repo.MarkOwnVideoAsDeleted(videoId);

        if (video.RemixedFromVideoId.HasValue && video.Access == VideoAccess.Public)
            await kpiCachingService.UpdateVideoKpi(video.RemixedFromVideoId.Value, e => e.Remixes, -1);

        if (video.AiContentId != null)
            await aiGeneratedContentService.Delete(video.AiContentId.Value);

        await videoCachingService.DeleteVideoDetailsCache(videoId);
    }

    private async Task UpdateVideoLinks(long videoId, Dictionary<string, string> links)
    {
        links = CloudFrontVideoAssetUrlGenerator.NormalizeLinks(links);
        await repo.SetVideoLinks(videoId, links);
    }

    private async Task<long[]> GetTaggedGroupIds(UpdateVideoAccessRequest model)
    {
        var taggedIds = Array.Empty<long>();

        if ((model.TaggedFriendIds?.Length ?? 0) > 0 && model.Access == VideoAccess.ForTaggedGroups)
            taggedIds = await taggingGroup.GetGroupsCanBeTagged(currentUser, model.TaggedFriendIds ?? []);

        return taggedIds;
    }

    private async Task UpdateRemixCount(long videoId, VideoAccess requestedAccess, VideoAccess currentAccess)
    {
        if (currentAccess == VideoAccess.Public && requestedAccess != VideoAccess.Public)
            await kpiCachingService.UpdateVideoKpi(videoId, e => e.Remixes, -1);
        else if (currentAccess != VideoAccess.Public && requestedAccess == VideoAccess.Public)
            await kpiCachingService.UpdateVideoKpi(videoId, e => e.Remixes, 1);
    }

    private async Task<VideoInfo> GetVideoById(long videoId)
    {
        return await oneVideoAccessor.GetVideo(FetchVideoInfoFrom.WriteDb, currentUser, videoId);
    }
}