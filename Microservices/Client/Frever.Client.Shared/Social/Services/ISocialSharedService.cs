using System.Collections.Generic;
using System.Threading.Tasks;
using Frever.ClientService.Contract.Social;

namespace Frever.Client.Shared.Social.Services;

public interface ISocialSharedService
{
    Task<long[]> GetBlocked(long currentGroupId, long[] groupIds = null);

    Task<FollowStatCount> GetFollowStatCount(long groupId, long currentGroupId);

    Task<bool> IsBlocked(long groupId, long currentGroupId);

    Task<bool> IsFollowed(long followerId, long followingId);

    Task<bool> IsFriend(long followerId, long followingId);

    Task<Dictionary<long, GroupShortInfo>> GetGroupShortInfo(params long[] groupIds);
}