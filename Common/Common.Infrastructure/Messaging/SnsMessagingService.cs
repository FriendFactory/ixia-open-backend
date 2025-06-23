using System;
using System.Threading.Tasks;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Common.Infrastructure.Messaging;

public class SnsMessagingService : ISnsMessagingService
{
    private readonly string _groupDeletedTopicArn;
    private readonly string _videoUnlikedTopicArn;
    private readonly string _groupUnfollowedTopicArn;
    private readonly string _groupFollowedTopicArn;

    private readonly ILogger<SnsMessagingService> _log;
    private readonly IAmazonSimpleNotificationService _sns;

    public SnsMessagingService(
        IAmazonSimpleNotificationService sns,
        ILogger<SnsMessagingService> log,
        IOptions<SnsMessagingSettings> options
    )
    {
        _sns = sns ?? throw new ArgumentNullException(nameof(sns));
        _log = log ?? throw new ArgumentNullException(nameof(log));

        var snsMessagingSettings = options?.Value;
        _groupDeletedTopicArn = snsMessagingSettings != null ? snsMessagingSettings.GroupDeletedTopicArn : "";
        _videoUnlikedTopicArn = snsMessagingSettings != null ? snsMessagingSettings.VideoUnlikedTopicArn : "";
        _groupUnfollowedTopicArn = snsMessagingSettings != null ? snsMessagingSettings.GroupUnfollowedTopicArn : "";
        _groupFollowedTopicArn = snsMessagingSettings != null ? snsMessagingSettings.GroupFollowedTopicArn : "";
    }

    public async Task PublishSnsMessageForGroupDeleted(long groupId)
    {
        var payload = new GroupDeletedMessage {GroupId = groupId}.ToJson();
        var snsMessage = new SnsMessage {Subject = "GroupDeletedMessage", Payload = payload, MessageId = Guid.NewGuid().ToString()};
        _log.LogInformation(
            "publishing message for GroupDeleted, Subject={}, MessageId={}, GroupId={} to sns",
            snsMessage.Subject,
            snsMessage.MessageId,
            groupId
        );
        if (!string.IsNullOrEmpty(_groupDeletedTopicArn) && !_groupDeletedTopicArn.Contains("not-used"))
            await _sns.PublishAsync(new PublishRequest {TopicArn = _groupDeletedTopicArn, Message = snsMessage.ToJson()});
    }

    public async Task PublishSnsMessageForVideoUnliked(long videoId, long groupId, DateTime time)
    {
        var payload = new VideoUnlikedMessage {VideoId = videoId, GroupId = groupId, Time = time}.ToJson();
        var snsMessage = new SnsMessage {Subject = "VideoUnlikedMessage", Payload = payload, MessageId = Guid.NewGuid().ToString()};
        _log.LogInformation(
            "publishing message for VideoUnliked, Subject={}, MessageId={}, VideoId={}, GroupId={} to sns",
            snsMessage.Subject,
            snsMessage.MessageId,
            videoId,
            groupId
        );
        if (!string.IsNullOrEmpty(_videoUnlikedTopicArn) && !_videoUnlikedTopicArn.Contains("not-used"))
            await _sns.PublishAsync(new PublishRequest {TopicArn = _videoUnlikedTopicArn, Message = snsMessage.ToJson()});
    }

    public async Task PublishSnsMessageForGroupUnfollowed(long followingId, long followerId, bool isMutual, DateTime time)
    {
        var payload = new GroupUnfollowedMessage
                      {
                          FollowingId = followingId,
                          FollowerId = followerId,
                          IsMutual = isMutual,
                          Time = time,
                          UnfollowedTime = DateTime.Now
                      }.ToJson();
        var snsMessage = new SnsMessage {Subject = "GroupUnfollowedMessage", Payload = payload, MessageId = Guid.NewGuid().ToString()};
        _log.LogInformation(
            "publishing message for GroupUnfollowed, Subject={}, MessageId={}, FollowingId={}, FollowerId={} to sns",
            snsMessage.Subject,
            snsMessage.MessageId,
            followingId,
            followerId
        );
        if (!string.IsNullOrEmpty(_groupUnfollowedTopicArn) && !_groupUnfollowedTopicArn.Contains("not-used"))
            await _sns.PublishAsync(new PublishRequest {TopicArn = _groupUnfollowedTopicArn, Message = snsMessage.ToJson()});
    }

    public async Task PublishSnsMessageForGroupFollowed(long followingId, long followerId, bool isMutual, DateTime time)
    {
        var payload = new GroupFollowedMessage
                      {
                          FollowingId = followingId,
                          FollowerId = followerId,
                          IsMutual = isMutual,
                          Time = time
                      };
        var snsMessage = new SnsMessage
                         {
                             Subject = "GroupFollowedMessage", Payload = payload.ToJson(), MessageId = Guid.NewGuid().ToString()
                         };
        _log.LogInformation(
            "publishing message for GroupFollowed, Subject={}, MessageId={}, FollowingId={}, FollowerId={} to sns",
            snsMessage.Subject,
            snsMessage.MessageId,
            followingId,
            followerId
        );
        if (!string.IsNullOrEmpty(_groupFollowedTopicArn) && !_groupFollowedTopicArn.Contains("not-used"))
            await _sns.PublishAsync(new PublishRequest {TopicArn = _groupFollowedTopicArn, Message = snsMessage.ToJson()});
    }
}