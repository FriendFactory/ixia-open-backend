using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Common.Infrastructure.Sounds;
using Frever.Shared.MainDb;
using Frever.Shared.MainDb.Entities;
using Microsoft.EntityFrameworkCore;

namespace Frever.Video.Core.Features.Feeds.UserSounds;

public interface IUserSoundFeedRepository
{
    Task<List<VideoWithSong>> GetSoundVideo(
        long currentGroupId,
        long soundId,
        FavoriteSoundType type,
        long target,
        int takeNext
    );
}

public class PersistentUserSoundFeedRepository(IReadDb db) : IUserSoundFeedRepository
{
    public Task<List<VideoWithSong>> GetSoundVideo(
        long currentGroupId,
        long soundId,
        FavoriteSoundType type,
        long target,
        int takeNext
    )
    {
        // var soundType = type switch
        //                 {
        //                     FavoriteSoundType.Song => nameof(MusicController.SongId),
        //                     FavoriteSoundType.ExternalSong => nameof(MusicController.ExternalTrackId),
        //                     FavoriteSoundType.UserSound => nameof(MusicController.UserSoundId),
        //                     _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        //                 };
        //
        // return db.GetSoundVideoQuery(currentGroupId, soundId, soundType, target).Take(takeNext).ToListAsync();

        List<VideoWithSong> result = [];
        return Task.FromResult(result);
    }
}