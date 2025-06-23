using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Frever.Shared.MainDb;
using Frever.Shared.MainDb.Entities;

namespace Frever.Video.Core.Features.Feeds.Remixes;

public interface IRemixesOfVideoRepository
{
    Task<List<VideoWithSong>> GetRemixesOfVideo(
        long videoId,
        long currentGroupId,
        long target,
        int takeNext,
        int takePrevious
    );

    IQueryable<Frever.Shared.MainDb.Entities.Video> GetVideo(long videoId);
}

public class PersistentRemixesOfVideoRepository(IReadDb db) : IRemixesOfVideoRepository
{
    public Task<List<VideoWithSong>> GetRemixesOfVideo(
        long videoId,
        long currentGroupId,
        long target,
        int takeNext,
        int takePrevious
    )
    {
        return db.GetRemixesOfVideo(
            videoId,
            currentGroupId,
            target,
            takeNext,
            takePrevious
        );
    }

    public IQueryable<Frever.Shared.MainDb.Entities.Video> GetVideo(long videoId)
    {
        return db.Video.Where(v => v.Id == videoId);
    }
}