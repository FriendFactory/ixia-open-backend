using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Frever.Client.Shared.Social.Services;
using Frever.Shared.MainDb.Entities;
using Microsoft.Extensions.Logging;
using NotificationService.Client.Messages;
using NotificationService.DataAccess;
using StackExchange.Redis;

namespace NotificationService.Core;

public class NotificationAddingService : INotificationAddingService
{
    private static readonly Dictionary<NotificationType, TimeSpan> NotificationExpirationSettings =
        new() {{NotificationType.NewFollower, TimeSpan.FromDays(7)}};

    private readonly ILogger _log;
    private readonly IMainServerService _mainService;
    private readonly IPushNotificationSender _pushNotificationSender;
    private readonly INotificationRepository _repo;
    private readonly ISocialSharedService _socialSharedService;
    private readonly IVideoServerService _videoService;

    public NotificationAddingService(
        IMainServerService mainService,
        IPushNotificationSender pushNotificationSender,
        INotificationRepository repo,
        ISocialSharedService socialSharedService,
        IVideoServerService videoServerService,
        ILoggerFactory loggerFactory,
        IConnectionMultiplexer redisConnection
    )
    {
        _mainService = mainService ?? throw new ArgumentNullException(nameof(mainService));
        _pushNotificationSender = pushNotificationSender ?? throw new ArgumentNullException(nameof(pushNotificationSender));
        _repo = repo ?? throw new ArgumentNullException(nameof(repo));
        _socialSharedService = socialSharedService ?? throw new ArgumentNullException(nameof(socialSharedService));
        _videoService = videoServerService ?? throw new ArgumentNullException(nameof(videoServerService));

        ArgumentNullException.ThrowIfNull(loggerFactory);
        ArgumentNullException.ThrowIfNull(redisConnection);

        _log = loggerFactory.CreateLogger("Frever.NotificationAddingService");
    }

    public async Task NotifyNewFollower(NotifyNewFollowerMessage message)
    {
        ArgumentNullException.ThrowIfNull(message);

        _log.LogInformation(
            "NotifyNewFollower(followerGroupId: {FollowerGroupId}, currentGroupId: {CurrentGroupId})",
            message.FollowerGroupId,
            message.CurrentGroupId
        );

        var haveFollower = await _mainService.HaveFollower(message.FollowerGroupId, message.CurrentGroupId);
        if (!haveFollower)
        {
            _log.LogError(
                "User with group {CurrentGroupId} is not followed {FollowerGroupId}",
                message.CurrentGroupId,
                message.FollowerGroupId
            );
            return;
        }

        if (message.FollowerGroupId != message.CurrentGroupId)
            await WriteNotification(
                message.CurrentGroupId,
                false,
                NotificationType.NewFollower,
                [message.FollowerGroupId],
                message.CurrentGroupId
            );
    }

    public async Task NotifyNewLikeOnVideo(NotifyNewLikeOnVideoMessage message)
    {
        ArgumentNullException.ThrowIfNull(message);

        _log.LogInformation("NotifyNewLikeOnVideo(videoId: {VideoId}, groupId: {CurrentGroupId})", message.VideoId, message.CurrentGroupId);

        var videoInfo = await _videoService.GetVideoDetails(message.VideoId, message.CurrentGroupId, []);

        if (videoInfo == null)
        {
            _log.LogError("Video {VideoId} is not found or not accessible", message.VideoId);
            return;
        }

        if (videoInfo.GroupId != message.CurrentGroupId)
            await WriteNotification(
                message.CurrentGroupId,
                false,
                NotificationType.NewLikeOnVideo,
                [videoInfo.GroupId],
                message.CurrentGroupId,
                message.VideoId
            );
    }

    public async Task NotifyNewVideo(NotifyNewVideoMessage message)
    {
        ArgumentNullException.ThrowIfNull(message);

        using var scope = _log.BeginScope(
            $"NotifyNewVideo(videoId: {message.VideoId}, isConversionPerformed: {message.IsVideoConversionPerformed})"
        );

        var videoInfo = await _videoService.GetVideoDetails(message.VideoId, message.CurrentGroupId, []);

        if (videoInfo == null)
        {
            _log.LogError("Video {VideoId} is not found or not accessible", message.VideoId);
            return;
        }

        // If video conversion was enqueued and completed,
        // notify video owner that conversion is completed
        if (message.IsVideoConversionPerformed)
        {
            await WriteNotification(
                message.CurrentGroupId,
                true,
                NotificationType.YourVideoConverted,
                [videoInfo.GroupId],
                videoInfo.GroupId,
                videoInfo.Id
            );

            _log.LogInformation("Writing video conversion completed notification for groupId={GroupId}", videoInfo.GroupId);
        }

        if (videoInfo.GroupId != message.CurrentGroupId)
            await WriteNotification(
                message.CurrentGroupId,
                false,
                NotificationType.NewFriendVideo,
                await _mainService.GetFriendIds(message.CurrentGroupId),
                videoInfo.GroupId,
                videoInfo.Id
            );

        if ((VideoAccess) videoInfo.Access == VideoAccess.Private)
            return;

        if (videoInfo.CharacterTags.Length > 0)
            await WriteNotification(
                message.CurrentGroupId,
                false,
                NotificationType.YouTaggedOnVideo,
                videoInfo.CharacterTags,
                videoInfo.GroupId,
                videoInfo.Id
            );

        if (videoInfo.Access == (int) VideoAccess.ForTaggedGroups && videoInfo.NonCharacterTags.Length > 0)
            await WriteNotification(
                message.CurrentGroupId,
                false,
                NotificationType.NonCharacterTagOnVideo,
                videoInfo.NonCharacterTags,
                videoInfo.GroupId,
                videoInfo.Id
            );

        if (videoInfo.Access != (int) VideoAccess.ForTaggedGroups && videoInfo.Mentions.Length > 0)
            await WriteNotification(
                message.CurrentGroupId,
                false,
                NotificationType.NewMentionOnVideo,
                videoInfo.Mentions,
                videoInfo.GroupId,
                message.VideoId
            );

        if (videoInfo.RemixedFromVideoId != null)
        {
            var remixedVideoInfo = await _videoService.GetVideoDetails(videoInfo.RemixedFromVideoId.Value, 0, []);
            if (remixedVideoInfo != null && remixedVideoInfo.GroupId != message.CurrentGroupId)
                await WriteNotification(
                    message.CurrentGroupId,
                    false,
                    NotificationType.YourVideoRemixed,
                    [remixedVideoInfo.GroupId],
                    videoInfo.GroupId,
                    videoInfo.Id
                );
        }
    }

    public async Task NotifyNewCommentOnVideo(NotifyNewCommentOnVideoMessage message)
    {
        ArgumentNullException.ThrowIfNull(message);

        _log.LogInformation(
            "NotifyNewCommentOnVideo(videoId: {VideoId}, commentId: {CommentId}, commentedBy: {CommentedBy})",
            message.VideoId,
            message.CommentId,
            message.CommentedBy
        );

        var blocked = await _socialSharedService.GetBlocked(message.CommentedBy);

        var videoInfo = await _videoService.GetVideoDetails(message.VideoId, message.CommentedBy, blocked);

        if (videoInfo == null)
        {
            _log.LogError("Video {VideoId} is not found or not accessible", message.VideoId);
            return;
        }

        var comment = await _videoService.GetCommentInfo(message.VideoId, message.CommentId, blocked);
        if (comment == null)
        {
            _log.LogError("Comment {CommentId} is not found or not accessible", message.CommentId);
            return;
        }

        var isTopLevelComment = comment.ReplyToComment == null;

        if (isTopLevelComment && videoInfo.GroupId != message.CommentedBy)
        {
            _log.LogInformation("Send notification to THE VIDEO CREATOR for ALL TOP LEVEL comments");

            await WriteNotification(
                message.CommentedBy,
                true,
                NotificationType.NewCommentOnVideo,
                [videoInfo.GroupId],
                message.CommentedBy,
                videoInfo.Id,
                comment.Id
            );
        }

        if (!isTopLevelComment && comment.ReplyToComment.GroupId != message.CommentedBy)
        {
            _log.LogInformation("Send notification to the USER WHO GOT A REPLY");

            await WriteNotification(
                message.CommentedBy,
                true,
                NotificationType.NewCommentOnVideoYouHaveCommented,
                [comment.ReplyToComment.GroupId],
                message.CommentedBy,
                videoInfo.Id,
                comment.Id,
                dataAssetId: message.ReplyToCommentId
            );
        }
    }

    public async Task NotifyNewMentionsInCommentOnVideo(NotifyNewMentionInCommentOnVideoMessage message)
    {
        ArgumentNullException.ThrowIfNull(message);

        _log.LogInformation(
            "NotifyNewMentionsInCommentOnVideo(videoId: {VideoId}, commentId: {CommentId}, commentedBy: {CommentedBy}, mentionedGroupId: {MentionedGroupId})",
            message.VideoId,
            message.CommentId,
            message.CommentedBy,
            message.MentionedGroupId
        );

        await WriteNotification(
            message.CommentedBy,
            true,
            NotificationType.NewMentionInCommentOnVideo,
            [message.MentionedGroupId],
            message.CommentedBy,
            message.VideoId,
            message.CommentId,
            dataRefGroupId: message.MentionedGroupId
        );
    }

    public async Task NotifyVideoDeleted(NotifyVideoDeletedMessage message)
    {
        ArgumentNullException.ThrowIfNull(message);

        _log.LogInformation("NotifyVideoDeleted(videoId: {VideoId})", message.VideoId);

        var videoGroupId = await _videoService.GetVideoGroupId(message.VideoId);

        await WriteNotification(
            message.CurrentGroupId,
            true,
            NotificationType.VideoDeleted,
            [videoGroupId],
            message.CurrentGroupId,
            message.VideoId
        );
    }

    public async Task NotifyAiContentGenerated(NotifyAiContentGeneratedMessage message)
    {
        ArgumentNullException.ThrowIfNull(message);

        _log.LogInformation(
            "NotifyAiContentGenerated(contentId: {AiContentId}, groupId: {CurrentGroupId})",
            message.AiContentId,
            message.CurrentGroupId
        );

        await WriteNotification(
            message.CurrentGroupId,
            false,
            NotificationType.AiContentGenerated,
            [message.CurrentGroupId],
            message.CurrentGroupId,
            dataAiContentId: message.AiContentId
        );
    }

    private async Task WriteNotification(
        long currentGroupId,
        bool allowRepeatableNotifications,
        NotificationType notificationType,
        long[] targetGroupIds,
        long? dataGroupId,
        long? dataVideoId = null,
        long? dataRefId = null,
        long? dataRefGroupId = null,
        long? dataAssetId = null,
        long? dataAiContentId = null,
        TimeSpan? notFrequentlyThan = null
    )
    {
        if (!NotificationExpirationSettings.TryGetValue(notificationType, out var expiration))
            expiration = TimeSpan.FromDays(7);

        var notification = new Notification
                           {
                               TimeStamp = DateTime.UtcNow,
                               Expires = DateTime.UtcNow.Add(expiration),
                               Type = notificationType,
                               DataGroupId = dataGroupId,
                               DataVideoId = dataVideoId,
                               DataRefId = dataRefId,
                               DataRefGroupId = dataRefGroupId,
                               DataAiContentId = dataAiContentId,
                               DataAssetId = dataAssetId == null ? null : [dataAssetId.Value]
                           };

        var (sentNotification, sentToGroups) = await _repo.AddNotification(
                                                   notification,
                                                   targetGroupIds,
                                                   allowRepeatableNotifications,
                                                   notFrequentlyThan
                                               );

        if (sentNotification != null)
        {
            var blockedGroups = await _socialSharedService.GetBlocked(currentGroupId, sentToGroups);

            var targetGroups = sentToGroups.Where(g => !blockedGroups.Contains(g)).ToArray();

            if (notification.Type is NotificationType.YourVideoConverted or NotificationType.AiContentGenerated)
                return;

            var pushNotification = new PushNotification {Type = notification.Type, HasDataAssetId = notification.DataAssetId != null};

            await _pushNotificationSender.SendPush(targetGroups, pushNotification);
        }
    }
}