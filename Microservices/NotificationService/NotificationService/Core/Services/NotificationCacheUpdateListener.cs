using System;
using System.Threading.Tasks;
using Common.Infrastructure.Caching;
using Common.Infrastructure.Caching.CacheKeys;
using Frever.Cache.PubSub;
using Frever.Protobuf;
using Microsoft.Extensions.Logging;
using NotificationService.Client;
using NotificationService.Client.Messages;
using StackExchange.Redis;

namespace NotificationService.Core;

// ReSharper disable once ClassNeverInstantiated.Global
public class NotificationCacheUpdateListener(ILogger<NotificationCacheUpdateListener> log, INotificationAddingService service, ICache cache)
    : IPubSubSubscriber
{
    private const int MessageVersion = 1;

    public string SubscriptionKey => NotificationSubscriptionKeys.SubscriptionKey;

    public async Task OnMessage(RedisValue messageBytes)
    {
        var versionMessage = ProtobufConvert.DeserializeObject<MessageWithVersion>(messageBytes);
        if (versionMessage.Version > MessageVersion)
        {
            log.LogInformation(
                "Message version {ReceivedVersion} is newer than than listener version version {CurrentVersion}, skipping",
                versionMessage.Version,
                MessageVersion
            );
            return;
        }

        var key = NotificationCacheKeys.NotificationPerInstanceKey(versionMessage.NotificationId.ToString());
        var anyDeleted = await cache.DeleteKey(key);
        if (!anyDeleted)
        {
            log.LogInformation("Key {Key} was not added to cache, skipping", key);
            return;
        }

        log.LogInformation("Message v{Version} received: ev={Event}", versionMessage.Version, versionMessage.Event);

        await Notify(messageBytes, versionMessage.Event);
    }

    private Task Notify(RedisValue m, NotificationEvent e)
    {
        return e switch
               {
                   NotificationEvent.NewFollower       => service.NotifyNewFollower(ToMessage<NotifyNewFollowerMessage>(m)),
                   NotificationEvent.NewLikeOnVideo    => service.NotifyNewLikeOnVideo(ToMessage<NotifyNewLikeOnVideoMessage>(m)),
                   NotificationEvent.NewVideo          => service.NotifyNewVideo(ToMessage<NotifyNewVideoMessage>(m)),
                   NotificationEvent.VideoDeleted      => service.NotifyVideoDeleted(ToMessage<NotifyVideoDeletedMessage>(m)),
                   NotificationEvent.NewCommentOnVideo => service.NotifyNewCommentOnVideo(ToMessage<NotifyNewCommentOnVideoMessage>(m)),
                   NotificationEvent.NewMentionInCommentOnVideo => service.NotifyNewMentionsInCommentOnVideo(
                       ToMessage<NotifyNewMentionInCommentOnVideoMessage>(m)
                   ),
                   NotificationEvent.AiContentGenerated => service.NotifyAiContentGenerated(ToMessage<NotifyAiContentGeneratedMessage>(m)),
                   _ => throw new ArgumentOutOfRangeException(nameof(NotificationEvent), "Unknown notification event")
               };
    }

    private static T ToMessage<T>(byte[] message)
    {
        return ProtobufConvert.DeserializeObject<T>(message);
    }
}