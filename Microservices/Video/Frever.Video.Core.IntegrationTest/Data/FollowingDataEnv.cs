using Frever.Common.IntegrationTesting;
using Frever.Shared.MainDb.Entities;
using Microsoft.EntityFrameworkCore;

namespace Frever.Video.Core.IntegrationTest.Data;

public static class FollowingDataEnv
{
    public static async Task WithFollowing(this DataEnvironment dataEnv, params FollowingDataEnvParam[] followParams)
    {
        ArgumentNullException.ThrowIfNull(dataEnv);
        ArgumentNullException.ThrowIfNull(followParams);

        var allFollow = await dataEnv.Db.Follower.ToArrayAsync();
        foreach (var item in followParams)
        {
            var existing = allFollow.FirstOrDefault(f => f.FollowerId == item.GroupId && f.FollowingId == item.FollowsGroupId);
            if (existing == null)
                dataEnv.Db.Set<Follower>()
                       .Add(new Follower {FollowerId = item.GroupId, FollowingId = item.FollowsGroupId, IsMutual = item.IsMutual});
            else
                existing.IsMutual = item.IsMutual;
        }

        await dataEnv.Db.SaveChangesAsync();
    }

    public class FollowingDataEnvParam
    {
        public long GroupId { get; set; }

        public long FollowsGroupId { get; set; }

        public bool IsMutual { get; set; }
    }
}