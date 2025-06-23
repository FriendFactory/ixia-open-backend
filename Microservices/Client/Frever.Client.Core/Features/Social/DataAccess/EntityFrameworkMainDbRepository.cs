using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Common.Infrastructure.Database;
using Common.Infrastructure.Messaging;
using Frever.Client.Core.Features.Social.Profiles;
using Frever.Client.Core.Features.Social.PublicProfiles;
using Frever.ClientService.Contract.Social;
using Frever.Shared.MainDb;
using Frever.Shared.MainDb.Entities;
using Microsoft.EntityFrameworkCore;
using static System.String;

namespace Frever.Client.Core.Features.Social.DataAccess;

internal sealed class EntityFrameworkMainDbRepository(IReadDb readDb, IWriteDb writeDb, ISnsMessagingService snsMessagingService)
    : IMainDbRepository
{
    private readonly ISnsMessagingService _snsMessagingService =
        snsMessagingService ?? throw new ArgumentNullException(nameof(snsMessagingService));

    public async Task<ProfileInternal[]> GetStartFollowRecommendations(
        Group currentGroup,
        GeoCluster geoCluster,
        HashSet<long> existing,
        int numOfProfiles
    )
    {
        var groupIds = await readDb.GetGroupWithAgeInfo(currentGroup.Id, geoCluster.Id)
                                   .Where(e => e.GroupId != currentGroup.Id)
                                   .Where(a => !existing.Contains(currentGroup.Id))
                                   .OrderBy(a => a.AgeDiff)
                                   .Take(numOfProfiles)
                                   .Select(a => a.GroupId)
                                   .ToArrayAsync();

        return groupIds.Length == 0 ? [] : await GetProfileByGroupIds(currentGroup.Id, groupIds);
    }

    public async Task<ProfileInternal[]> GetTopProfiles(
        long currentGroupId,
        string nickname,
        int skip,
        int count,
        bool excludeMinors
    )
    {
        var sql = $"""
                   with partial as (
                            select g."Id" as id, g."ToplistPosition" as top_list, g."TotalVideos" as top_videos
                            from "Group" as g
                            where g."Id" <> {currentGroupId} and g."IsBlocked" = false and g."DeletedAt" is null and g."IsTemporary" = false
                            {(!IsNullOrWhiteSpace(nickname) ? """and lower(g."NickName") like {0}""" : "")}
                            {(excludeMinors ? """and not g."IsMinor" """ : Empty)}
                            order by g."ToplistPosition", g."TotalVideos" desc
                            limit {count + skip + Math.Min(count * 10, 500)}
                       )
                       select p.id
                       from partial as p
                           inner join "User" as u1 ON p.id = u1."MainGroupId"
                           and not exists (select 1 from "BlockedUser" as b where b."BlockedByUserId" = {currentGroupId} and b."BlockedUserId" = p.id)
                           and not exists (select 1 from "BlockedUser" as b0 where b0."BlockedUserId" = {currentGroupId} and b0."BlockedByUserId" = p.id)
                           order by p.top_list, p.top_videos desc
                           limit {count} offset {skip}
                   """;
        var groupIds = await readDb.SqlQueryRaw<long>(sql, !IsNullOrWhiteSpace(nickname) ? nickname.ToLower() + "%" : "").ToArrayAsync();

        return groupIds.Length == 0 ? [] : await GetProfileByGroupIds(currentGroupId, groupIds);
    }

    public async Task<ProfileInternal[]> GetFollowersProfilesAsync(
        long currentGroupId,
        long groupId,
        string nickname,
        int skip,
        int take
    )
    {
        var query = writeDb.Follower.Where(f => f.FollowingId == groupId).Select(e => e.FollowerNavigation);

        var groupIds = await GetGroupIdsByLevel(
                           query,
                           currentGroupId,
                           nickname,
                           skip,
                           take
                       );

        return groupIds.Length == 0 ? [] : await GetProfileByGroupIds(currentGroupId, groupIds);
    }

    public async Task<ProfileInternal[]> GetFollowedProfilesAsync(
        long currentGroupId,
        long groupId,
        string nickname,
        int skip,
        int take
    )
    {
        var query = writeDb.Follower.Where(f => f.FollowerId == groupId).Select(e => e.Following);

        var groupIds = await GetGroupIdsByLevel(
                           query,
                           currentGroupId,
                           nickname,
                           skip,
                           take
                       );

        return groupIds.Length == 0 ? [] : await GetProfileByGroupIds(currentGroupId, groupIds);
    }

    public Task<int> GetMutualFriendCount(long currentGroupId, long groupId)
    {
        return writeDb.Follower.Where(f1 => f1.FollowerId == currentGroupId && f1.IsMutual)
                      .Join(
                           writeDb.Follower.Where(f2 => f2.FollowerId == groupId && f2.IsMutual),
                           f1 => f1.FollowingId,
                           f2 => f2.FollowingId,
                           (f1, f2) => f1
                       )
                      .Where(f => !f.Following.IsBlocked && f.Following.DeletedAt == null)
                      .CountAsync();
    }

    public async Task<ProfileInternal[]> GetFriends(
        long currentGroupId,
        long groupId,
        string nickname,
        bool canStartChatOnly,
        int skip,
        int take
    )
    {
        var query = readDb.GetRankedFriendList(groupId).Where(g => !canStartChatOnly || !g.IsMinor);

        if (!IsNullOrWhiteSpace(nickname))
            query = query.Where(e => e.NickName.StartsWith(nickname.ToLower())).Select(e => e);

        var groupIds = await query.Skip(skip).Take(take).Select(g => g.GroupId).ToArrayAsync();

        return groupIds.Length == 0 ? [] : await GetProfileByGroupIds(currentGroupId, groupIds);
    }

    public async Task<ProfileInternal[]> GetBlockedUsersProfiles(long currentGroupId)
    {
        var groupIds = await writeDb.BlockedUser.Where(u => u.BlockedByUserId == currentGroupId)
                                    .Select(u => u.BlockedUserId)
                                    .ToArrayAsync();

        return groupIds.Length == 0 ? [] : await GetProfileByGroupIds(currentGroupId, groupIds);
    }

    public Task<ProfileInternal> GetProfile(long currentGroupId, long groupId)
    {
        var ownProfile = currentGroupId == groupId;

        return writeDb.User.Where(e => e.MainGroupId == groupId)
                      .Select(e => e.MainGroup)
                      .GroupJoin(writeDb.FollowerStats, g => g.Id, k => k.GroupId, (g, k) => new {Group = g, Kpi = k})
                      .SelectMany(k => k.Kpi.DefaultIfEmpty(), (g, k) => new {g.Group, Kpi = k})
                      .Select(
                           e => new ProfileInternal
                                {
                                    Id = e.Group.Id,
                                    MainGroupId = e.Group.Id,
                                    NickName = e.Group.NickName,
                                    Bio = e.Group.Bio,
                                    YouFollowUser =
                                        !ownProfile &&
                                        e.Group.FollowerFollowing.Any(f => f.FollowerId == currentGroupId),
                                    UserFollowsYou =
                                        !ownProfile &&
                                        e.Group.FollowerFollowerNavigation.Any(f => f.FollowingId == currentGroupId),
                                    CreatedTime = e.Group.CreatedTime,
                                    Files = e.Group.Files,
                                    KPI = new ProfileKpi
                                          {
                                              FollowersCount = e.Kpi == null ? 0 : e.Kpi.FollowersCount,
                                              FollowingCount = e.Kpi == null ? 0 : e.Kpi.FollowingCount,
                                              FriendsCount = e.Kpi == null ? 0 : e.Kpi.FriendsCount,
                                              VideoLikesCount = e.Group.TotalLikes
                                          }
                                }
                       )
                      .FirstOrDefaultAsync();
    }

    public Task<PublicProfile> GetPublicProfile(string nickname)
    {
        return writeDb.User.Where(e => e.MainGroup.NickName == nickname)
                      .Where(e => !e.MainGroup.IsBlocked && e.MainGroup.DeletedAt == null)
                      .Select(
                           e => new PublicProfile
                                {
                                    MainGroupId = e.MainGroupId,
                                    NickName = e.MainGroup.NickName,
                                    Bio = e.MainGroup.Bio,
                                    FollowersCount =
                                        writeDb.FollowerStats.Where(f => f.GroupId == e.MainGroupId)
                                               .Select(f => f.FollowersCount)
                                               .FirstOrDefault()
                                }
                       )
                      .FirstOrDefaultAsync();
    }

    public IQueryable<Group> GetGroup(long id)
    {
        return writeDb.Group.Where(g => g.Id == id);
    }

    public Task<long> GetGroupIdByEmail(string email)
    {
        ArgumentNullException.ThrowIfNull(email);

        return writeDb.User.Where(e => e.Email.ToLower() == email.ToLower()).Select(e => e.MainGroupId).FirstOrDefaultAsync();
    }

    public IQueryable<User> GetUserById(long userId)
    {
        return writeDb.User.Where(u => u.Id == userId);
    }

    public IQueryable<UserRole> GetUserRoles(long groupId)
    {
        return writeDb.UserRole.Where(u => u.GroupId == groupId);
    }

    public IQueryable<GroupBioLink> GetGroupBioLinks(long groupId)
    {
        return writeDb.GroupBioLink.Where(l => l.GroupId == groupId);
    }

    public IQueryable<Country> GetCountries()
    {
        return readDb.Country;
    }

    public Task<bool> InitialAccountBalanceAdded(long groupId)
    {
        return writeDb.AssetStoreTransactions.AnyAsync(
            t => t.GroupId == groupId && t.TransactionType == AssetStoreTransactionType.InitialAccountBalance
        );
    }

    public async Task UpdateGroup(Group group)
    {
        ArgumentNullException.ThrowIfNull(group);

        await writeDb.SaveChangesAsync();
    }

    public async Task UpdateGroupBioLinks(long groupId, Dictionary<string, string> bioLinks)
    {
        ArgumentNullException.ThrowIfNull(bioLinks);

        var existing = await writeDb.GroupBioLink.Where(l => l.GroupId == groupId).ToArrayAsync();

        foreach (var (type, url) in bioLinks)
        {
            var link = existing.FirstOrDefault(l => StringComparer.OrdinalIgnoreCase.Equals(l.LinkType, type.ToLower()));

            if (IsNullOrWhiteSpace(url))
            {
                if (link != null)
                    writeDb.GroupBioLink.Remove(link);
            }
            else
            {
                if (link == null)
                {
                    link = new GroupBioLink {LinkType = type, GroupId = groupId};
                    writeDb.GroupBioLink.Add(link);
                }

                link.Link = url;
            }
        }

        foreach (var link in existing)
            if (!bioLinks.ContainsKey(link.LinkType.ToLower()))
                writeDb.GroupBioLink.Remove(link);

        await writeDb.SaveChangesAsync();
    }

    public async Task<GroupInfo[]> FindProfilesByPhone(string[] phones, long mainGroupId, long[] blockedGroups)
    {
        if (phones.Length == 0)
            return [];

        var userParam = Expression.Parameter(typeof(User));

        Expression body = null;
        foreach (var phone in phones)
        {
            var comparison = Expression.Call(
                Expression.Property(userParam, nameof(User.PhoneNumber)),
                typeof(string).GetMethod(nameof(string.EndsWith), [typeof(string)]),
                Expression.Constant(phone)
            );
            if (body == null)
                body = comparison;
            else
                body = Expression.Or(body, comparison);
        }

        var phoneFilter = Expression.Lambda<Func<User, bool>>(body, true, userParam);

        return await writeDb.User.Where(u => !blockedGroups.Contains(u.MainGroupId))
                            .Where(phoneFilter)
                            .Join(writeDb.Group, u => u.MainGroupId, g => g.Id, (u, g) => new {User = u, Group = g})
                            .Select(
                                 a => new GroupInfo
                                      {
                                          GroupId = a.Group.Id,
                                          PhoneNumber = a.User.PhoneNumber,
                                          GroupNickName = a.Group.NickName,
                                          IsFollowing = writeDb.Follower.Any(
                                              f => f.FollowerId == mainGroupId && f.FollowingId == a.Group.Id
                                          ),
                                          RegistrationDate = a.Group.CreatedTime
                                      }
                             )
                            .ToArrayAsync();
    }

    public Task<NestedTransaction> BeginTransaction()
    {
        return writeDb.BeginTransactionSafe();
    }

    public async Task<bool> FollowAsync(long userMainGroupId, long groupId)
    {
        await using var transaction = await writeDb.BeginTransactionSafe();
        var isUserFollowsTheGroup = await IsUserFollowToAnother(userMainGroupId, groupId).AnyAsync();

        if (isUserFollowsTheGroup)
            return false;

        var groupFollowsYou = await IsUserFollowToAnother(groupId, userMainGroupId).FirstOrDefaultAsync();

        var following = new Follower
                        {
                            FollowingId = groupId,
                            FollowerId = userMainGroupId,
                            IsMutual = groupFollowsYou != null,
                            State = FollowerState.Following,
                            Time = DateTime.UtcNow
                        };

        writeDb.Follower.Add(following);

        if (groupFollowsYou != null)
            groupFollowsYou.IsMutual = true;

        await writeDb.SaveChangesAsync();

        await transaction.Commit();
        await _snsMessagingService.PublishSnsMessageForGroupFollowed(groupId, userMainGroupId, following.IsMutual, following.Time);
        return true;
    }

    public async Task<bool> UnFollowAsync(long userMainGroupId, long groupId)
    {
        await using var transaction = await writeDb.BeginTransactionSafe();

        var following = await IsUserFollowToAnother(userMainGroupId, groupId).SingleOrDefaultAsync();

        if (following == null)
            return false;

        writeDb.Follower.Remove(following);
        var originIsMutual = following.IsMutual;

        var groupFollowsYou = await IsUserFollowToAnother(groupId, userMainGroupId).FirstOrDefaultAsync();
        if (groupFollowsYou != null)
            groupFollowsYou.IsMutual = false;

        await writeDb.SaveChangesAsync();
        await transaction.Commit();
        await _snsMessagingService.PublishSnsMessageForGroupUnfollowed(groupId, userMainGroupId, originIsMutual, following.Time);
        return true;
    }

    public async Task BlockUser(long blockedGroupId, long blockedByGroupId)
    {
        var blockedUser =
            await writeDb.BlockedUser.FirstOrDefaultAsync(u => u.BlockedUserId == blockedGroupId && u.BlockedByUserId == blockedByGroupId);
        if (blockedUser == null)
        {
            writeDb.BlockedUser.Add(new BlockedUser {BlockedUserId = blockedGroupId, BlockedByUserId = blockedByGroupId});

            await writeDb.SaveChangesAsync();
        }
    }

    public async Task UnBlockUser(long blockedGroupId, long blockedByGroupId)
    {
        var blockedUser =
            await writeDb.BlockedUser.FirstOrDefaultAsync(u => u.BlockedUserId == blockedGroupId && u.BlockedByUserId == blockedByGroupId);
        if (blockedUser != null)
        {
            writeDb.BlockedUser.Remove(blockedUser);

            await writeDb.SaveChangesAsync();
        }
    }

    public async Task<ProfileKpi> GetProfileVideoKpi(long groupId, long currentGroupId)
    {
        var videoQuery = await writeDb.GetGroupAvailableVideoQuery(groupId, currentGroupId);

        var result = await videoQuery.GroupBy(e => true)
                                     .Select(
                                          e => new ProfileKpi
                                               {
                                                   TotalVideoCount = e.Count(),
                                                   PublishedVideoCount = e.Count(v => v.Access != VideoAccess.Private)
                                               }
                                      )
                                     .FirstOrDefaultAsync();

        if (result is null)
            return new ProfileKpi();

        result.TaggedInVideoCount = await writeDb.GetTaggedGroupVideoCount(groupId, currentGroupId);

        return result;
    }

    private async Task<long[]> GetGroupIdsByLevel(
        IQueryable<Group> query,
        long currentGroupId,
        string nickname,
        int skip,
        int take,
        bool ignoreBlocked = true
    )
    {
        if (!IsNullOrWhiteSpace(nickname))
            query = query.Where(e => e.NickName.StartsWith(nickname.ToLower())).Select(e => e);

        query = query.Where(ignoreBlocked ? e => !e.IsBlocked && e.DeletedAt == null : NonBlockedGroupFilter(currentGroupId));

        if (await TopListPositionInitialized())
            query = query.OrderBy(a => a.ToplistPosition).ThenByDescending(i => i.TotalVideos);
        else
            query = query.OrderByDescending(i => i.TotalVideos);

        return await query.Select(a => a.Id).Skip(skip).Take(take).ToArrayAsync();
    }

    private async Task<ProfileInternal[]> GetProfileByGroupIds(long currentGroupId, long[] groupIds)
    {
        var followStats = await writeDb.FollowerStats.Where(f => groupIds.Contains(f.GroupId)).AsNoTracking().ToArrayAsync();
        var followStatsByGroupId = followStats.ToDictionary(f => f.GroupId);

        var currentFollower = await writeDb.Follower.Where(
                                                e => (e.FollowerId == currentGroupId && groupIds.Contains(e.FollowingId)) ||
                                                     (e.FollowingId == currentGroupId && groupIds.Contains(e.FollowerId))
                                            )
                                           .ToArrayAsync();

        var followers = currentFollower.Where(e => e.FollowingId == currentGroupId).Select(e => e.FollowerId).Distinct().ToHashSet();
        var followings = currentFollower.Where(e => e.FollowerId == currentGroupId).Select(e => e.FollowingId).Distinct().ToHashSet();

        var result = await writeDb.User.Where(e => groupIds.Contains(e.MainGroupId))
                                  .Select(
                                       e => new ProfileInternal
                                            {
                                                MainGroupId = e.MainGroupId,
                                                NickName = e.MainGroup.NickName,
                                                CreatedTime = e.MainGroup.CreatedTime,
                                                Files = e.MainGroup.Files
                                            }
                                   )
                                  .AsNoTracking()
                                  .ToArrayAsync();

        foreach (var group in result)
        {
            group.YouFollowUser = followings.Contains(group.MainGroupId);
            group.UserFollowsYou = followers.Contains(group.MainGroupId);

            var friendInfo = currentFollower.FirstOrDefault(
                f => f.FollowerId == currentGroupId && f.FollowingId == group.MainGroupId && f.IsMutual
            );

            if (friendInfo != null)
                group.IsNewFriend = (DateTime.UtcNow - friendInfo.Time).TotalDays < 9;

            if (!followStatsByGroupId.TryGetValue(group.MainGroupId, out var kpi))
                continue;

            group.KPI.FollowersCount = kpi.FollowersCount;
            group.KPI.FollowingCount = kpi.FollowingCount;
            group.KPI.FriendsCount = kpi.FriendsCount;
        }

        var sorted = result.Select(p => new {Profile = p, SortOrder = Array.FindIndex(groupIds, i => i == p.MainGroupId)})
                           .OrderBy(a => a.SortOrder)
                           .Select(a => a.Profile)
                           .ToArray();

        return sorted;
    }

    /// <summary>
    ///     Check whether user with userMainGroupId follows to a groupId
    /// </summary>
    /// <param name="userMainGroupId">Who follow</param>
    /// <param name="groupId">To whom following </param>
    /// <returns></returns>
    private IQueryable<Follower> IsUserFollowToAnother(long userMainGroupId, long groupId)
    {
        return writeDb.Follower.Where(f => f.FollowingId == groupId && f.FollowerId == userMainGroupId);
    }

    private Expression<Func<Group, bool>> NonBlockedGroupFilter(long groupId)
    {
        return g => !g.IsBlocked && g.DeletedAt == null && !writeDb.BlockedUser.Any(
                        bu => (bu.BlockedByUserId == groupId && bu.BlockedUserId == g.Id) ||
                              (bu.BlockedUserId == groupId && bu.BlockedByUserId == g.Id)
                    );
    }

    private Task<bool> TopListPositionInitialized()
    {
        return writeDb.Group.AnyAsync(g => g.ToplistPosition != null);
    }
}