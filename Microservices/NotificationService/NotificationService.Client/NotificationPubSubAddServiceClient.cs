using System;
using System.Threading.Tasks;
using Common.Infrastructure.Caching;
using Common.Infrastructure.Caching.CacheKeys;
using Frever.Cache.PubSub;
using NotificationService.Client.Messages;

namespace NotificationService.Client;

public class NotificationPubSubAddServiceClient(IPubSubPublisher publisher, ICache cache) : INotificationAddingService
{
    private const int Version = 1;

    private readonly ICache _cache = cache ?? throw new ArgumentNullException(nameof(cache));
    private readonly IPubSubPublisher _publisher = publisher ?? throw new ArgumentNullException(nameof(publisher));

    public Task NotifyNewFollower(NotifyNewFollowerMessage message)
    {
        return PublishNotificationMessage(message);
    }

    public Task NotifyNewLikeOnVideo(NotifyNewLikeOnVideoMessage message)
    {
        return PublishNotificationMessage(message);
    }

    public Task NotifyNewVideo(NotifyNewVideoMessage message)
    {
        return PublishNotificationMessage(message);
    }

    public Task NotifyNewCommentOnVideo(NotifyNewCommentOnVideoMessage message)
    {
        return PublishNotificationMessage(message);
    }

    public Task NotifyNewMentionsInCommentOnVideo(NotifyNewMentionInCommentOnVideoMessage message)
    {
        return PublishNotificationMessage(message);
    }

    public Task NotifyVideoDeleted(NotifyVideoDeletedMessage message)
    {
        return PublishNotificationMessage(message);
    }

    public Task NotifyAiContentGenerated(NotifyAiContentGeneratedMessage message)
    {
        return PublishNotificationMessage(message);
    }

    private async Task PublishNotificationMessage(IMessageBase message)
    {
        ArgumentNullException.ThrowIfNull(message);

        message.Version = Version;
        message.NotificationId = Guid.NewGuid();

        var key = NotificationCacheKeys.NotificationPerInstanceKey(message.NotificationId.ToString());
        await _cache.Put(key, true, TimeSpan.FromMinutes(15));

        await _publisher.Publish(NotificationSubscriptionKeys.SubscriptionKey, message);
    }
}