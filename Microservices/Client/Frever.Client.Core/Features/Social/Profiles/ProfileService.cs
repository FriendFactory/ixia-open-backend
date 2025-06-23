using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AuthServer.Permissions.Services;
using AuthServerShared;
using Common.Infrastructure;
using Frever.Client.Core.Features.Social.DataAccess;
using Frever.Client.Shared.Files;
using Frever.Client.Shared.Social.Services;
using Frever.ClientService.Contract.Social;
using Frever.Shared.MainDb.Entities;
using Frever.Videos.Shared.GeoClusters;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Frever.Client.Core.Features.Social.Profiles;

internal sealed class ProfileService : IProfileService
{
    private readonly UserInfo _currentUser;
    private readonly IGeoClusterProvider _geoClusterProvider;
    private readonly ILogger _log;
    private readonly IMainDbRepository _mainDbRepo;
    private readonly ProfileServiceOptions _options;
    private readonly ISocialSharedService _socialSharedService;
    private readonly IUserPermissionService _userPermissionService;
    private readonly IFileStorageService _fileStorage;

    public ProfileService(
        UserInfo currentUser,
        IMainDbRepository mainDbRepo,
        IUserPermissionService userPermissionService,
        ISocialSharedService socialSharedService,
        ProfileServiceOptions options,
        IGeoClusterProvider geoClusterProvider,
        ILoggerFactory loggerFactory,
        IFileStorageService fileStorage
    )
    {
        _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
        _mainDbRepo = mainDbRepo ?? throw new ArgumentNullException(nameof(mainDbRepo));
        _userPermissionService = userPermissionService ?? throw new ArgumentNullException(nameof(userPermissionService));
        _socialSharedService = socialSharedService ?? throw new ArgumentNullException(nameof(socialSharedService));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _geoClusterProvider = geoClusterProvider ?? throw new ArgumentNullException(nameof(geoClusterProvider));
        _fileStorage = fileStorage;
        _log = loggerFactory.CreateLogger("Frever.ProfileService");
    }

    public async Task<Profile[]> GetTopProfiles(string nickname, int skip, int count, bool excludeMinors)
    {
        await _userPermissionService.EnsureCurrentUserActive();

        var result = await _mainDbRepo.GetTopProfiles(
                         _currentUser,
                         nickname,
                         skip,
                         count,
                         excludeMinors
                     );
        if (result.Length == 0)
            return [];

        await _fileStorage.InitUrls<Group>(result);

        return result.Select(ProfileMapper.FromInternal).ToArray();
    }

    public async Task<Profile> GetFreverOfficialProfile()
    {
        await _userPermissionService.EnsureCurrentUserActive();

        var groupId = await _mainDbRepo.GetGroupIdByEmail(_options.FreverOfficialEmail);
        if (groupId == 0)
            return null;

        return await GetProfileAsync(groupId);
    }

    public async Task<Profile[]> GetStartFollowRecommendations()
    {
        await _userPermissionService.EnsureCurrentUserActive();

        var currentGroup = await _mainDbRepo.GetGroup(_currentUser).FirstOrDefaultAsync();
        if (currentGroup == null)
            throw AppErrorWithStatusCodeException.NotAuthorized("Group is not available", "NotAuthorized");

        using var scope = _log.BeginScope(
            "[{Wid}] GetStartFollowRecommendations(groupId={GroupId}): ",
            Guid.NewGuid().ToString("N"),
            _currentUser.UserMainGroupId
        );

        _log.LogInformation("Start giving recommendations");

        var clusterForUser = await _geoClusterProvider.DetectGeoClustersForGroup(_currentUser);

        _log.LogInformation("Geo clusters: {Cluster}", string.Join("; ", clusterForUser.Select(gc => $"{gc.Id}:{gc.Title}")));

        var list = new List<Profile>();
        const int numOfRecommendations = 5;

        foreach (var c in clusterForUser)
        {
            var existing = list.Select(a => a.MainGroupId).ToHashSet();
            var internalProfiles = await _mainDbRepo.GetStartFollowRecommendations(currentGroup, c, existing, numOfRecommendations);
            await _fileStorage.InitUrls<Group>(internalProfiles);

            var profiles = internalProfiles.Select(p => ProfileMapper.FromInternal(p)).ToArray();

            _log.LogInformation(
                "Cluster {Id}: {Length} profiles to recommends: {MainGroupIds}",
                c.Id,
                profiles.Length,
                string.Join(",", profiles.Select(p => p.MainGroupId.ToString()))
            );

            list.AddRange(profiles);

            _log.LogInformation("Total {Count} profiles", list.Count);

            if (list.Count >= numOfRecommendations)
            {
                _log.LogInformation("There are {Count} profiles, enough", list.Count);
                return list.Take(numOfRecommendations).ToArray();
            }
        }

        if (list.Count == 0)
            _log.LogError("No profiles found for recommendations");

        return list.Take(numOfRecommendations).ToArray();
    }

    public async Task<GroupShortInfo[]> GetGroupsShortInfo(long[] groupIds)
    {
        await _userPermissionService.EnsureCurrentUserActive();

        var groups = await _socialSharedService.GetGroupShortInfo(groupIds);

        return groups.Values.ToArray();
    }

    public async Task<Profile> GetProfileAsync(long userMainGroupId)
    {
        await _userPermissionService.EnsureCurrentUserActive();

        if (await _socialSharedService.IsBlocked(userMainGroupId, _currentUser))
            return null;

        var profile = await _mainDbRepo.GetProfile(_currentUser, userMainGroupId);
        if (profile == null)
            return null;

        profile.BioLinks = await _mainDbRepo.GetGroupBioLinks(profile.MainGroupId).ToDictionaryAsync(l => l.LinkType, l => l.Link);

        var followStatCount = await _socialSharedService.GetFollowStatCount(userMainGroupId, _currentUser);
        profile.KPI.FollowersCount = followStatCount.FollowersCount;
        profile.KPI.FollowingCount = followStatCount.FollowingsCount;
        profile.KPI.FriendsCount = followStatCount.FriendsCount;

        var profileKpi = await _mainDbRepo.GetProfileVideoKpi(userMainGroupId, _currentUser);
        profile.KPI.PublishedVideoCount = profileKpi.PublishedVideoCount;
        profile.KPI.TotalVideoCount = profileKpi.TotalVideoCount;
        profile.KPI.TaggedInVideoCount = profileKpi.TaggedInVideoCount;

        if (userMainGroupId != _currentUser.UserMainGroupId)
            profile.KPI.MutualFriendsCount = await _mainDbRepo.GetMutualFriendCount(userMainGroupId, _currentUser);

        await _fileStorage.InitUrls<Group>([profile]);

        return ProfileMapper.FromInternal(profile);
    }
}