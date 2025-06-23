using System;
using System.Linq;
using System.Threading.Tasks;
using AuthServer.Permissions.Services;
using AuthServerShared;
using Common.Infrastructure;
using Common.Infrastructure.RequestId;
using Frever.Client.Core.Features.Social.DataAccess;
using Frever.Client.Core.Features.Social.Profiles;
using Frever.Client.Shared.Files;
using Frever.Client.Shared.Social.Services;
using Frever.ClientService.Contract.Social;
using Frever.Shared.MainDb.Entities;
using Microsoft.Extensions.Logging;
using NotificationService;
using NotificationService.Client.Messages;

namespace Frever.Client.Core.Features.Social.Followers;

internal sealed partial class FollowingService : IFollowingService
{
    private readonly UserInfo _currentUser;
    private readonly IFollowRecommendationClient _followRecommendationClient;
    private readonly IHeaderAccessor _headerAccessor;
    private readonly IMainDbRepository _mainDbRepository;
    private readonly INotificationAddingService _notificationAddingService;
    private readonly IProfileService _profileService;
    private readonly IFileStorageService _fileStorage;
    private readonly ISocialSharedService _socialSharedService;
    private readonly IUserPermissionService _userPermissionService;

    public FollowingService(
        UserInfo currentUser,
        IMainDbRepository mainDbRepository,
        INotificationAddingService notificationAddingService,
        IUserPermissionService userPermissionService,
        ISocialSharedService socialSharedService,
        IProfileService profileService,
        IFollowRecommendationClient followRecommendationClient,
        ILoggerFactory loggerFactory,
        IHeaderAccessor headerAccessor,
        IFileStorageService fileStorage
    )
    {
        ArgumentNullException.ThrowIfNull(loggerFactory);

        _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
        _mainDbRepository = mainDbRepository ?? throw new ArgumentNullException(nameof(mainDbRepository));
        _notificationAddingService = notificationAddingService ?? throw new ArgumentNullException(nameof(notificationAddingService));
        _userPermissionService = userPermissionService ?? throw new ArgumentNullException(nameof(userPermissionService));
        _socialSharedService = socialSharedService ?? throw new ArgumentNullException(nameof(socialSharedService));
        _profileService = profileService ?? throw new ArgumentNullException(nameof(profileService));
        _followRecommendationClient = followRecommendationClient ?? throw new ArgumentNullException(nameof(followRecommendationClient));
        _headerAccessor = headerAccessor ?? throw new ArgumentNullException(nameof(headerAccessor));
        _fileStorage = fileStorage ?? throw new ArgumentNullException(nameof(fileStorage));

        loggerFactory.CreateLogger("Frever.Client.FollowingService");
    }

    public async Task<Profile[]> GetFollowersProfilesAsync(long userMainGroupId, string nickname, int skip, int take)
    {
        await _userPermissionService.EnsureCurrentUserActive();

        var result = await _mainDbRepository.GetFollowersProfilesAsync(
                         _currentUser,
                         userMainGroupId,
                         nickname,
                         skip,
                         take
                     );
        if (result.Length == 0)
            return [];

        await _fileStorage.InitUrls<Group>(result);

        return result.Select(ProfileMapper.FromInternal).ToArray();
    }

    public async Task<Profile[]> GetFollowedProfilesAsync(long userMainGroupId, string nickname, int skip, int take)
    {
        await _userPermissionService.EnsureCurrentUserActive();

        var result = await _mainDbRepository.GetFollowedProfilesAsync(
                         _currentUser,
                         userMainGroupId,
                         nickname,
                         skip,
                         take
                     );
        if (result.Length == 0)
            return [];

        await _fileStorage.InitUrls<Group>(result);

        return result.Select(ProfileMapper.FromInternal).ToArray();
    }

    public async Task<Profile[]> GetFriendProfilesAsync(
        long userMainGroupId,
        string nickname,
        bool canStartChatOnly,
        int skip,
        int take
    )
    {
        await _userPermissionService.EnsureCurrentUserActive();

        var result = await _mainDbRepository.GetFriends(
                         _currentUser,
                         userMainGroupId,
                         nickname,
                         canStartChatOnly,
                         skip,
                         take
                     );
        if (result.Length == 0)
            return [];

        await _fileStorage.InitUrls<Group>(result);

        return result.Select(ProfileMapper.FromInternal).ToArray();
    }

    public async Task<Profile> FollowGroupAsync(long groupId)
    {
        await _userPermissionService.EnsureCurrentUserActive();

        var blocked = await _socialSharedService.GetBlocked(_currentUser.UserMainGroupId);
        if (blocked.Contains(groupId))
            throw AppErrorWithStatusCodeException.BadRequest("Can't follow user due to privacy and security settings", "BlockedGroup");

        if (_currentUser.UserMainGroupId == groupId)
            throw AppErrorWithStatusCodeException.BadRequest("User can't follow himself", "FollowSelfGroup");

        if (!await _userPermissionService.IsAccountActive(groupId))
            throw AppErrorWithStatusCodeException.BadRequest("Can't follow blocked account", "FollowBlockedGroup");

        await using var transaction = await _mainDbRepository.BeginTransaction();

        await _mainDbRepository.FollowAsync(_currentUser.UserMainGroupId, groupId);

        await transaction.Commit();

        await _notificationAddingService.NotifyNewFollower(
            new NotifyNewFollowerMessage {FollowerGroupId = groupId, CurrentGroupId = _currentUser.UserMainGroupId}
        );

        return await _profileService.GetProfileAsync(groupId);
    }

    public async Task UnFollowGroupAsync(long groupId)
    {
        await _userPermissionService.EnsureCurrentUserActive();

        await using var transaction = await _mainDbRepository.BeginTransaction();

        await _mainDbRepository.UnFollowAsync(_currentUser.UserMainGroupId, groupId);

        await transaction.Commit();
    }
}