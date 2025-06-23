using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Frever.ClientService.Contract.Social;
using Frever.Shared.MainDb;
using Frever.Shared.MainDb.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Frever.Client.Shared.Social.DataAccess;

internal sealed class MainRepository(IWriteDb writeDb, ILogger<MainRepository> log) : IMainRepository
{
    public IQueryable<BlockedUser> GetBlockedUsers()
    {
        return writeDb.BlockedUser;
    }

    public IQueryable<long> GetFriends(long groupId)
    {
        return writeDb.Follower.Where(x => x.FollowerId == groupId && x.IsMutual)
                      .Where(e => !e.Following.IsBlocked && e.Following.DeletedAt == null)
                      .Select(x => x.FollowingId);
    }

    public IQueryable<long> GetFollowings(long groupId)
    {
        return writeDb.Follower.Where(e => e.FollowerId == groupId)
                      .Where(e => !e.Following.IsBlocked && e.Following.DeletedAt == null)
                      .Select(e => e.FollowingId);
    }

    public IQueryable<long> GetFollowers(long groupId)
    {
        return writeDb.Follower.Where(e => e.FollowingId == groupId)
                      .Where(e => !e.FollowerNavigation.IsBlocked && e.FollowerNavigation.DeletedAt == null)
                      .Select(e => e.FollowerId);
    }

    public Task<bool> IsFollowed(long followerId, long followingId)
    {
        return writeDb.Follower.AnyAsync(e => e.FollowerId == followerId && e.FollowingId == followingId);
    }

    public Task<bool> IsFriend(long followerId, long followingId)
    {
        return writeDb.Follower.AnyAsync(e => e.FollowerId == followerId && e.FollowingId == followingId && e.IsMutual);
    }

    public Task<FollowerStats> GetFollowerStatsForApproximation(long groupId)
    {
        return writeDb.FollowerStats.Where(f => f.GroupId == groupId).AsNoTracking().FirstOrDefaultAsync();
    }

    public async Task<(int, int, int)> GetFollowerStatsDeltaSinceAggregation(long groupId)
    {
        var sql = $"""
                   with check_time as (
                       select last_execution_time as time from stats.timer_execution where timer_name = 'follower-stats-aggregation'
                   )
                   select 'follower' as type, count(1) as count from "Follower" where "FollowingId" = {groupId} and "Time" >= (select time from check_time)
                   union all
                   select 'following', count(1) as count from "Follower" where "FollowerId" = {groupId} and "Time" >= (select time from check_time)
                   union all
                   select 'friend', count(1) as count from "Follower" where "FollowerId" = {groupId} and "IsMutual" and "Time" >= (select time from check_time);
                   """;
        var typeCountMap = await writeDb.SqlQueryRaw<FollowTypeAndCount>(sql).AsNoTracking().ToDictionaryAsync(x => x.Type, x => x.Count);
        var followingCountDelta = typeCountMap.GetValueOrDefault("following", 0);
        var followerCountDelta = typeCountMap.GetValueOrDefault("follower", 0);
        var friendCountDelta = typeCountMap.GetValueOrDefault("friend", 0);
        return (friendCountDelta, followingCountDelta, followerCountDelta);
    }

    public Task<GroupShortInfo[]> GetGroupWithMainCharacters(params long[] groups)
    {
        if (groups == null || groups.Length == 0)
            return Task.FromResult(Array.Empty<GroupShortInfo>());

        if (groups.Length >= 1000)
            log.LogWarning("Passed in {Count} groups to GetGroupWithMainCharacters", groups.Length);

        return writeDb.User.Where(x => groups.Contains(x.MainGroupId) && x.MainGroup.DeletedAt == null)
                      .Select(x => new GroupShortInfo {Id = x.MainGroupId, Nickname = x.MainGroup.NickName, Files = x.MainGroup.Files})
                      .AsNoTracking()
                      .ToArrayAsync();
    }
}