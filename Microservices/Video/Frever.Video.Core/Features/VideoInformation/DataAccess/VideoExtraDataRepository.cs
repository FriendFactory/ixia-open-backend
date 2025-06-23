using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Frever.Shared.MainDb;
using Microsoft.EntityFrameworkCore;

namespace Frever.Video.Core.Features.VideoInformation.DataAccess;

public interface IVideoExtraDataRepository
{
    Task<long[]> GetLikedVideoIds(long userId, long[] videoIds);

    Task<Dictionary<long, int>> GetFollowGroupIds(long userId, IEnumerable<long> groupIds, DateTime date);
}

public class PersistentVideoExtraDataRepository(IWriteDb db) : IVideoExtraDataRepository
{
    public Task<long[]> GetLikedVideoIds(long userId, long[] videoIds)
    {
        return db.Like.Where(l => l.UserId == userId).Where(l => videoIds.Contains(l.VideoId)).Select(l => l.VideoId).ToArrayAsync();
    }

    public Task<Dictionary<long, int>> GetFollowGroupIds(long userId, IEnumerable<long> groupIds, DateTime date)
    {
        var result = db.Like.Where(e => e.UserId == userId && e.Time >= date)
                       .Join(db.Video, l => l.VideoId, v => v.Id, (l, v) => v.GroupId)
                       .Where(e => groupIds.Contains(e))
                       .GroupBy(e => e)
                       .Select(g => new {g.Key, LikesCount = g.Count()})
                       .ToDictionaryAsync(e => e.Key, e => e.LikesCount);

        return result;
    }
}