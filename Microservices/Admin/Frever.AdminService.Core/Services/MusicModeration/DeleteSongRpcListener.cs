using System;
using System.Threading.Tasks;
using Frever.AdminService.Core.Services.MusicModeration.Services;
using Frever.Cache.PubSub;
using Frever.Client.Shared.CommercialMusic;
using Frever.Protobuf;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace Frever.AdminService.Core.Services.MusicModeration;

public class DeleteSongRpcListener(IMusicDeleteService musicDeleteService, ILogger<DeleteSongRpcListener> log) : IPubSubSubscriber
{
    public string SubscriptionKey => CommercialMusicCacheKeys.Channel;

    public async Task OnMessage(RedisValue message)
    {
        var request = ProtobufConvert.DeserializeObject<DeleteSongMessage>(message);

        log.LogDebug("Delete song message received");
        if (request.Version != DeleteSongMessage.MessageVersion)
        {
            log.LogDebug("Incorrect version, skipping");
            return;
        }

        try
        {
            await musicDeleteService.SetDeletedContentByExternalSongId(request.SongId, true, false);
            log.LogInformation("Song {SongId} content deleted", request.SongId);
        }
        catch (Exception ex)
        {
            log.LogError(ex, "Error deleting song {SongId}", request.SongId);
        }
    }
}