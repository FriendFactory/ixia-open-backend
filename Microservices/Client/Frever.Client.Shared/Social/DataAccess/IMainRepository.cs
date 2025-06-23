using System.Linq;
using System.Threading.Tasks;
using Frever.ClientService.Contract.Social;
using Frever.Shared.MainDb.Entities;

namespace Frever.Client.Shared.Social.DataAccess;

public interface IMainRepository
{
    IQueryable<BlockedUser> GetBlockedUsers();

    IQueryable<long> GetFriends(long groupId);

    IQueryable<long> GetFollowings(long groupId);

    IQueryable<long> GetFollowers(long groupId);

    Task<bool> IsFollowed(long followerId, long followingId);

    Task<bool> IsFriend(long followerId, long followingId);

    Task<FollowerStats> GetFollowerStatsForApproximation(long groupId);

    Task<(int, int, int)> GetFollowerStatsDeltaSinceAggregation(long groupId);

    Task<GroupShortInfo[]> GetGroupWithMainCharacters(params long[] groups);
}