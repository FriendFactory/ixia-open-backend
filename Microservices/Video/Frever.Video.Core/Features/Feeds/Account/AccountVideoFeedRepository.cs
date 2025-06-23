using System.Collections.Generic;
using System.Threading.Tasks;
using Frever.Shared.MainDb;
using Frever.Shared.MainDb.Entities;

namespace Frever.Video.Core.Features.Feeds.Account;

public interface IAccountVideoFeedRepository
{
    Task<List<VideoWithSong>> GetFriendVideo(long currentGroupId, long target, int takeNext, int takePrevious);

    Task<List<VideoWithSong>> GetFollowingVideo(long currentGroupId, long target, int takeNext, int takePrevious);

    Task<List<VideoWithSong>> GetGroupAvailableVideo(
        long groupId,
        long currentGroupId,
        long target,
        int takeNext,
        int takePrevious,
        bool withTaskVideos = false,
        bool sortBySortOrder = false
    );
}

public class PersistentAccountVideoFeedRepository(IReadDb db) : IAccountVideoFeedRepository
{
    public Task<List<VideoWithSong>> GetFriendVideo(long currentGroupId, long target, int takeNext, int takePrevious)
    {
        return db.GetFriendVideoQuery(currentGroupId, target, takeNext, takePrevious);
    }

    public Task<List<VideoWithSong>> GetFollowingVideo(long currentGroupId, long target, int takeNext, int takePrevious)
    {
        return db.GetFollowingVideoQuery(currentGroupId, target, takeNext, takePrevious);
    }

    public Task<List<VideoWithSong>> GetGroupAvailableVideo(
        long groupId,
        long currentGroupId,
        long target,
        int takeNext,
        int takePrevious,
        bool withTaskVideos = false,
        bool sortBySortOrder = false
    )
    {
        return db.GetGroupAvailableVideoQuery(
            groupId,
            currentGroupId,
            target,
            takeNext,
            takePrevious,
            withTaskVideos,
            sortBySortOrder
        );
    }
}