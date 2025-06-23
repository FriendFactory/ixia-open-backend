using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Frever.Shared.MainDb;
using Microsoft.EntityFrameworkCore;

namespace Frever.Video.Core.Features.VideoInfoExtraData;

public interface IFollowRelationService
{
    Task<Dictionary<long, FollowRelationInfo>> GetFollowRelations(long currentUserGroupId, ISet<long> groupIds);
}

public class PersistentFollowRelationService(IWriteDb db) : IFollowRelationService
{
    public async Task<Dictionary<long, FollowRelationInfo>> GetFollowRelations(long currentUserGroupId, ISet<long> groupIds)
    {
        var relations = await db.Follower.Where(f => f.FollowingId == currentUserGroupId && groupIds.Contains(f.FollowerId))
                                .Concat(db.Follower.Where(f => f.FollowerId == currentUserGroupId && groupIds.Contains(f.FollowingId)))
                                .Select(f => new {f.FollowerId, f.FollowingId, f.IsMutual})
                                .ToArrayAsync();

        return groupIds
              .Select(
                   id => new FollowRelationInfo
                         {
                             GroupId = id,
                             IsFollowed = relations.Any(r => r.FollowingId == currentUserGroupId && r.FollowerId == id),
                             IsFollower = relations.Any(r => r.FollowingId == id && r.FollowerId == currentUserGroupId)
                         }
               )
              .ToDictionary(a => a.GroupId);
    }
}

public class FollowRelationInfo
{
    public long GroupId { get; set; }

    public bool IsFollowed { get; set; }

    public bool IsFollower { get; set; }

    public bool IsFriend => IsFollowed && IsFollower;
}