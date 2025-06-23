using System;
using System.Linq;
using System.Threading.Tasks;
using AuthServerShared;
using Common.Infrastructure.Utils;
using Common.Models;
using Frever.Shared.MainDb.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Frever.Client.Shared.ActivityRecording;

public class UserActivityRecordingService : IUserActivityRecordingService
{
    private readonly UserInfo _currentUser;
    private readonly ILogger _log;
    private readonly IGroupActivityRepository _repo;

    public UserActivityRecordingService(ILoggerFactory loggerFactory, UserInfo currentUser, IGroupActivityRepository repo)
    {
        ArgumentNullException.ThrowIfNull(loggerFactory);

        _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
        _repo = repo ?? throw new ArgumentNullException(nameof(repo));
        _log = loggerFactory.CreateLogger("Frever.UserActivityRecordingService");
    }

    public async Task OnLogin()
    {
        _log.LogInformation("Recording login for group={GroupId}", _currentUser.UserMainGroupId);

        var activity = new UserActivity
                       {
                           ActionType = UserActionType.Login, GroupId = _currentUser.UserMainGroupId, OccurredAt = DateTime.UtcNow
                       };

        await RecordActivity(activity);
    }

    public async Task OnPublishedVideoShare(long videoId)
    {
        using var scope = _log.BeginScope("OnPublishedVideoShare({Id}) ", videoId);

        var groupId = await _repo.GetVideoById(videoId).Select(v => v.GroupId).FirstOrDefaultAsync();
        if (groupId == 0)
        {
            _log.LogInformation("Group for video {Id} is not found", videoId);
            return;
        }

        if (groupId != _currentUser)
        {
            _log.LogInformation("Video {Id} is not owned by current user", videoId);
            return;
        }

        var today = DateTime.UtcNow.StartOfDay();

        var shared = await _repo.GetLifetimeGroupActivity(_currentUser)
                                .Where(a => a.ActionType == UserActionType.PublishedVideoShare && a.OccurredAt >= today)
                                .AnyAsync(a => a.RefVideoId == videoId);
        if (shared)
        {
            _log.LogInformation(
                "Recording video {VideoId} share for group {GroupId} already logged",
                videoId,
                _currentUser.UserMainGroupId
            );
            return;
        }

        var todayShares = await _repo.GetLifetimeGroupActivity(_currentUser)
                                     .CountAsync(a => a.ActionType == UserActionType.PublishedVideoShare && a.OccurredAt >= today);
        if (todayShares >= Constants.RewardedShareCount)
        {
            _log.LogWarning("Maximum video sharing limit has been reached");
            return;
        }

        var activity = new UserActivity
                       {
                           ActionType = UserActionType.PublishedVideoShare,
                           GroupId = _currentUser,
                           OccurredAt = DateTime.UtcNow,
                           RefVideoId = videoId
                       };
        await RecordActivity(activity);

        _log.LogInformation(
            "Activity recorded for group={GroupId} type={ActionType}, id={Id}",
            activity.GroupId,
            activity.ActionType,
            activity.Id
        );
    }

    public async Task OnVideoLike(long videoId, long groupId)
    {
        using var scope = _log.BeginScope("OnVideoLike((videoId={VideoId}, groupId={GroupId}) ", videoId, groupId);

        _log.LogInformation("Recording video like given {VideoId}, group {GroupId}", videoId, _currentUser.UserMainGroupId);

        if (groupId == _currentUser)
        {
            _log.LogInformation("Don't record like given to yourself");
            return;
        }

        await AddLikeOrRatingActivity(
            videoId,
            1,
            groupId,
            _currentUser,
            UserActionType.LikeReceived
        );
    }

    public async Task OnVideoRating(long videoId, long groupId, long? currentGroupId, int rating)
    {
        using var scope = _log.BeginScope("OnVideoRating(videoId={VideoId}, rating={Rating}) ", videoId, rating);

        _log.LogInformation("Recording video rating given {VideoId}, group {GroupId}", videoId, currentGroupId);

        await AddLikeOrRatingActivity(
            videoId,
            rating,
            groupId,
            currentGroupId,
            UserActionType.RatingReceived
        );
    }

    private async Task AddLikeOrRatingActivity(
        long videoId,
        int value,
        long groupId,
        long? currentGroupId,
        UserActionType actionType
    )
    {
        if (await _repo.GetLifetimeGroupActivity(groupId)
                       .AnyAsync(a => a.ActionType == actionType && a.RefVideoId == videoId && a.RefActorGroupId == currentGroupId))
        {
            _log.LogInformation(
                "{ActionType} for video {VideoId} for group {GroupId} by group {UserMainGroupId} already logged",
                actionType,
                videoId,
                groupId,
                currentGroupId
            );
            return;
        }

        await RecordActivity(
            new UserActivity
            {
                GroupId = groupId,
                ActionType = actionType,
                OccurredAt = DateTime.UtcNow,
                RefVideoId = videoId,
                RefActorGroupId = currentGroupId,
                Value = value
            }
        );
    }

    private async Task RecordActivity(UserActivity activity)
    {
        ArgumentNullException.ThrowIfNull(activity);

        await _repo.RecordActivity(activity);

        _log.LogInformation(
            "Activity recorded for group={GroupId} type={ActionType}, xp={Xp} [id={ActivityId}]",
            activity.GroupId,
            activity.ActionType,
            activity.Xp,
            activity.Id
        );
    }
}