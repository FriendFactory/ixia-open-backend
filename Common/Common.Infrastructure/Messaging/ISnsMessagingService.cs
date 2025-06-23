using System;
using System.Threading.Tasks;

namespace Common.Infrastructure.Messaging;

public interface ISnsMessagingService
{
    public Task PublishSnsMessageForGroupDeleted(long groupId);
    public Task PublishSnsMessageForVideoUnliked(long videoId, long groupId, DateTime time);
    public Task PublishSnsMessageForGroupUnfollowed(long followingId, long followerId, bool isMutual, DateTime time);
    public Task PublishSnsMessageForGroupFollowed(long followingId, long followerId, bool isMutual, DateTime time);
}