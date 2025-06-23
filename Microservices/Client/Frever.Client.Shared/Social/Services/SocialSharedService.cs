using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Common.Infrastructure.EnvironmentInfo;
using Frever.Client.Shared.Files;
using Frever.Client.Shared.Social.DataAccess;
using Frever.ClientService.Contract.Social;
using Frever.Shared.MainDb.Entities;
using Microsoft.EntityFrameworkCore;

namespace Frever.Client.Shared.Social.Services;

internal sealed class SocialSharedService(IMainRepository mainRepository, IFileStorageService fileStorage, EnvironmentInfo environmentInfo)
    : ISocialSharedService
{
    private static readonly List<long> ApproximateCountForGroupIdsForProd = [742];

    private static readonly Dictionary<string, List<long>> UseApproximateCountForGroupIds =
        new() {{EnvironmentInfo.KnownEnvironmentTypes.Production, ApproximateCountForGroupIdsForProd}};

    private readonly IMainRepository _mainRepository = mainRepository ?? throw new ArgumentNullException(nameof(mainRepository));
    private readonly EnvironmentInfo _environmentInfo = environmentInfo ?? throw new ArgumentNullException(nameof(environmentInfo));

    public async Task<long[]> GetBlocked(long currentGroupId, long[] groupIds = null)
    {
        var query = _mainRepository.GetBlockedUsers().Where(u => u.BlockedUserId == currentGroupId || u.BlockedByUserId == currentGroupId);

        if (groupIds is {Length: > 0})
            query = query.Where(e => groupIds.Contains(e.BlockedByUserId) || groupIds.Contains(e.BlockedUserId));

        var blockedUsers = await query.ToArrayAsync();

        var blockedGroupsIds = blockedUsers.Select(u => u.BlockedUserId)
                                           .Concat(blockedUsers.Select(u => u.BlockedByUserId))
                                           .Where(g => g != currentGroupId)
                                           .Distinct()
                                           .ToArray();
        return blockedGroupsIds;
    }

    public async Task<FollowStatCount> GetFollowStatCount(long groupId, long currentGroupId)
    {
        int friendsCount, followingsCount, followersCount;
        if (CanGetCountApproximately(groupId, currentGroupId))
        {
            (friendsCount, followingsCount, followersCount) = await GetCountApproximately(groupId);
            if (friendsCount == -1 && followingsCount == -1 && followersCount == -1)
            {
                (friendsCount, followingsCount, followersCount) = await GetCountAccurately(groupId);
            }
            else
            {
                var delta = await _mainRepository.GetFollowerStatsDeltaSinceAggregation(groupId);
                friendsCount += delta.Item1;
                followingsCount += delta.Item2;
                followersCount += delta.Item3;
            }
        }
        else
        {
            (friendsCount, followingsCount, followersCount) = await GetCountAccurately(groupId);
        }

        return new FollowStatCount {FriendsCount = friendsCount, FollowingsCount = followingsCount, FollowersCount = followersCount};
    }

    private bool CanGetCountApproximately(long groupId, long currentGroupId)
    {
        if (!UseApproximateCountForGroupIds.TryGetValue(_environmentInfo.Type, out var groupIds))
            return groupId != currentGroupId;

        if (groupIds.Contains(groupId))
            return true;

        return groupId != currentGroupId;
    }

    private async Task<(int, int, int)> GetCountApproximately(long groupId)
    {
        var followerStats = await _mainRepository.GetFollowerStatsForApproximation(groupId);

        return followerStats != null
                   ? (followerStats.FriendsCount, followerStats.FollowingCount, followerStats.FollowersCount)
                   : (-1, -1, -1);
    }

    private async Task<(int, int, int)> GetCountAccurately(long groupId)
    {
        var friendsCount = await _mainRepository.GetFriends(groupId).CountAsync();
        var followingsCount = await _mainRepository.GetFollowings(groupId).CountAsync();
        var followersCount = await _mainRepository.GetFollowers(groupId).CountAsync();

        return (friendsCount, followingsCount, followersCount);
    }

    public Task<bool> IsFollowed(long followerId, long followingId)
    {
        return _mainRepository.IsFollowed(followerId, followingId);
    }

    public Task<bool> IsFriend(long followerId, long followingId)
    {
        return _mainRepository.IsFriend(followerId, followingId);
    }

    public Task<bool> IsBlocked(long groupId, long currentGroupId)
    {
        if (groupId == currentGroupId)
            return Task.FromResult(false);

        return _mainRepository.GetBlockedUsers()
                              .AnyAsync(
                                   u => (u.BlockedUserId == groupId && u.BlockedByUserId == currentGroupId) ||
                                        (u.BlockedUserId == currentGroupId && u.BlockedByUserId == groupId)
                               );
    }

    public async Task<Dictionary<long, GroupShortInfo>> GetGroupShortInfo(params long[] groupIds)
    {
        var groupWithMainCharacters = await _mainRepository.GetGroupWithMainCharacters(groupIds);
        if (groupWithMainCharacters.Length == 0)
            return [];

        await fileStorage.InitUrls<Group>(groupWithMainCharacters);

        var result = new Dictionary<long, GroupShortInfo>();

        foreach (var group in groupWithMainCharacters)
            result[group.Id] = group;

        return result;
    }
}

public class GroupFileConfig : DefaultFileMetadataConfiguration<Group>
{
    public GroupFileConfig()
    {
        AddThumbnail(128, "jpeg");
        AddFile("cover", "jpeg", false);
    }
}