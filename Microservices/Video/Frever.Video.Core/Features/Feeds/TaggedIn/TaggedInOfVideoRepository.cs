using System.Collections.Generic;
using System.Threading.Tasks;
using Frever.Shared.MainDb;
using Frever.Shared.MainDb.Entities;

namespace Frever.Video.Core.Features.Feeds.TaggedIn;

public interface ITaggedInVideoRepository
{
    Task<List<VideoWithSong>> GetVideoUserTaggedIn(
        long groupId,
        long currentGroupId,
        long target,
        int takeNext,
        int takePrevious
    );
}

public class PersistentTaggedInVideoRepository(IReadDb db) : ITaggedInVideoRepository
{
    public Task<List<VideoWithSong>> GetVideoUserTaggedIn(
        long groupId,
        long currentGroupId,
        long target,
        int takeNext,
        int takePrevious
    )
    {
        return db.GetTaggedGroupVideoQuery(
            groupId,
            currentGroupId,
            target,
            takeNext,
            takePrevious
        );
    }
}