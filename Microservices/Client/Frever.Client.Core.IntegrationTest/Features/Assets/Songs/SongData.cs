using Frever.Common.IntegrationTesting;
using Frever.Shared.MainDb.Entities;
using Microsoft.EntityFrameworkCore;

namespace Frever.Client.Core.IntegrationTest.Features.Assets.Songs;

public static class SongData
{
    public static async Task WithRandomPromotedSong(this DataEnvironment dataEnv, int count)
    {
        ArgumentNullException.ThrowIfNull(dataEnv);

        var songs = await dataEnv.Db.Song.Where(s => s.ReadinessId == Readiness.KnownReadinessReady)
                                 .Where(s => s.PublicationDate == null && s.DepublicationDate == null)
                                 .ToArrayAsync();

        dataEnv.Db.PromotedSong.AddRange(songs.Select(s => new PromotedSong {IsEnabled = true, SongId = s.Id, Files = []}));
        await dataEnv.Db.SaveChangesAsync();
    }
}