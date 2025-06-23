using System.Linq;
using System.Threading.Tasks;
using Frever.Shared.MainDb;
using Microsoft.EntityFrameworkCore;

namespace Frever.Video.Core.Features.Shared;

public interface ITaggingGroupProvider
{
    Task<long[]> GetGroupsCanBeTagged(long groupId, long[] taggedFriendIds);
}

public class PersistentTaggingGroupProvider(IWriteDb db) : ITaggingGroupProvider
{
    public Task<long[]> GetGroupsCanBeTagged(long groupId, long[] taggedFriendIds)
    {
        return db.Follower.Where(e => e.FollowingId == groupId && e.IsMutual)
                 .Where(e => taggedFriendIds.Contains(e.FollowerId))
                 .Select(e => e.FollowerNavigation)
                 .Select(e => e.Id)
                 .Distinct()
                 .ToArrayAsync();
    }
}