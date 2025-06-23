using System;
using System.Threading.Tasks;
using Frever.Cache.PubSub;
using Frever.Client.Shared.CommercialMusic;
using Microsoft.Extensions.Logging;

namespace Frever.Client.Core.Features.CommercialMusic.LicenseChecking;

public interface IContentDeletionClient
{
    Task DeleteExternalSongById(long songId);
}

public class RpcContentDeletionClient : IContentDeletionClient
{
    private readonly ILogger _log;
    private readonly IPubSubPublisher _pubSubPublisher;

    public RpcContentDeletionClient(ILoggerFactory loggerFactory, IPubSubPublisher pubSubPublisher)
    {
        ArgumentNullException.ThrowIfNull(loggerFactory);

        _pubSubPublisher = pubSubPublisher ?? throw new ArgumentNullException(nameof(pubSubPublisher));
        _log = loggerFactory.CreateLogger("Frever.ContentDeletionClient");
    }

    public async Task DeleteExternalSongById(long songId)
    {
        using var scope = _log.BeginScope("DeleteSong(songId={sid}): ", songId);

        try
        {
            var message = new DeleteSongMessage {SongId = songId};
            await _pubSubPublisher.Publish(CommercialMusicCacheKeys.Channel, message);
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Error deleting song with related content");
            throw;
        }
    }
}