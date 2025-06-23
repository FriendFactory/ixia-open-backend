using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Common.Infrastructure.Database;
using Frever.Client.Core.Features.Social.Profiles;
using Frever.Client.Core.Features.Social.PublicProfiles;
using Frever.ClientService.Contract.Social;
using Frever.Shared.MainDb.Entities;

namespace Frever.Client.Core.Features.Social.DataAccess;

public interface IMainDbRepository
{
    Task<NestedTransaction> BeginTransaction();

    Task<ProfileInternal[]> GetStartFollowRecommendations(
        Group currentGroup,
        GeoCluster geoCluster,
        HashSet<long> existing,
        int numOfProfiles
    );

    Task<ProfileInternal[]> GetTopProfiles(
        long currentGroupId,
        string nickname,
        int skip,
        int count,
        bool excludeMinors
    );

    Task<ProfileInternal[]> GetFollowersProfilesAsync(
        long currentGroupId,
        long groupId,
        string nickname,
        int skip,
        int take
    );

    Task<ProfileInternal[]> GetFollowedProfilesAsync(
        long currentGroupId,
        long groupId,
        string nickname,
        int skip,
        int take
    );

    Task<ProfileInternal[]> GetFriends(
        long currentGroupId,
        long groupId,
        string nickname,
        bool canStartChatOnly,
        int skip,
        int take
    );

    Task<ProfileInternal[]> GetBlockedUsersProfiles(long userMainGroupId);

    Task<ProfileInternal> GetProfile(long currentGroupId, long groupId);

    Task<PublicProfile> GetPublicProfile(string nickname);

    Task<ProfileKpi> GetProfileVideoKpi(long groupId, long currentGroupId);

    IQueryable<Group> GetGroup(long id);

    Task<long> GetGroupIdByEmail(string email);

    IQueryable<User> GetUserById(long userId);

    IQueryable<UserRole> GetUserRoles(long groupId);

    IQueryable<GroupBioLink> GetGroupBioLinks(long groupId);

    Task<GroupInfo[]> FindProfilesByPhone(string[] phones, long mainGroupId, long[] blockedGroups);

    IQueryable<Country> GetCountries();

    Task<bool> InitialAccountBalanceAdded(long groupId);

    Task UpdateGroup(Group group);

    Task UnBlockUser(long blockedGroupId, long blockedByGroupId);

    Task BlockUser(long blockedGroupId, long blockedByGroupId);

    Task UpdateGroupBioLinks(long groupId, Dictionary<string, string> bioLinks);

    Task<bool> FollowAsync(long userMainGroupId, long groupId);

    Task<bool> UnFollowAsync(long userMainGroupId, long groupId);

    Task<int> GetMutualFriendCount(long currentGroupId, long groupId);
}

public class GroupInfo
{
    public long GroupId { get; set; }

    public string GroupNickName { get; set; }

    public string PhoneNumber { get; set; }

    public bool IsFollowing { get; set; }

    public DateTime RegistrationDate { get; set; }
}