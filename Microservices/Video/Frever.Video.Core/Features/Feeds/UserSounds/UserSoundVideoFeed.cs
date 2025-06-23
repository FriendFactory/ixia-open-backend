using System.Threading.Tasks;
using AuthServer.Permissions.Services;
using AuthServerShared;
using Common.Infrastructure.Sounds;
using Frever.Video.Contract;
using Frever.Videos.Shared.MusicGeoFiltering;
using Microsoft.Extensions.Logging;

namespace Frever.Video.Core.Features.Feeds.UserSounds;

public interface IUserSoundVideoFeed
{
    Task<VideoInfo[]> GetSoundVideoFeed(long soundId, FavoriteSoundType type, string targetVideo, int takeNext);
}

public class UserSoundVideoFeed(
    IUserPermissionService userPermissionService,
    UserInfo currentUser,
    ILogger<UserSoundVideoFeed> log,
    ICurrentLocationProvider location,
    IMusicGeoFilter geoFilter,
    IVideoLoader videoLoader,
    IUserSoundFeedRepository repo
) : IUserSoundVideoFeed
{
    public async Task<VideoInfo[]> GetSoundVideoFeed(long soundId, FavoriteSoundType type, string targetVideo, int takeNext)
    {
        await userPermissionService.EnsureCurrentUserActive();

        log.LogInformation(
            "GetForSoundVideosFeed: soundId={SoundId}, type={Type}, target={Target}, next={TakeNext}",
            soundId,
            type,
            targetVideo,
            takeNext
        );

        var loc = await location.Get();

        if (type == FavoriteSoundType.ExternalSong && await geoFilter.AreAnySongUnavailable(loc.CountryIso3Code, [soundId], null))
            return [];

        if (type == FavoriteSoundType.Song && await geoFilter.AreAnySongUnavailable(loc.CountryIso3Code, null, [soundId]))
            return [];

        return await videoLoader.LoadVideoPage(
                   FetchVideoInfoFrom.ReadDb,
                   (target, next, _) => repo.GetSoundVideo(
                       currentUser,
                       soundId,
                       type,
                       target,
                       next
                   ),
                   Sorting.Asc,
                   targetVideo,
                   takeNext
               );
    }
}