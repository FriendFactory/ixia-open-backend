using System;
using System.Threading.Tasks;
using AuthServer.Permissions.DataAccess;
using AuthServerShared;
using Common.Infrastructure;
using Common.Infrastructure.Caching;
using Common.Infrastructure.Caching.CacheKeys;

namespace AuthServer.Permissions.Services;

public class FreverUserPermissionManagementService(
    IUserPermissionService userPermissionService,
    ICache cache,
    UserInfo currentUser,
    IMainGroupRepository mainGroupRepository
) : IUserPermissionManagementService
{
    private readonly ICache _cache = cache ?? throw new ArgumentNullException(nameof(cache));
    private readonly UserInfo _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
    private readonly IMainGroupRepository _mainGroupRepository = mainGroupRepository ?? throw new ArgumentNullException(nameof(mainGroupRepository));
    private readonly IUserPermissionService _userPermissionService = userPermissionService ?? throw new ArgumentNullException(nameof(userPermissionService));

    public async Task SetGroupBlocked(long groupId, bool isBlocked)
    {
        await _userPermissionService.EnsureCurrentUserEmployee();

        if (groupId == _currentUser?.UserMainGroupId)
            throw AppErrorWithStatusCodeException.BadRequest("You can't block yourself", "GroupBlockYourself");

        await _mainGroupRepository.SetGroupBlocked(groupId, isBlocked);

        await ResetGroupCache(groupId);
    }

    public async Task SoftDeleteSelf()
    {
        await _userPermissionService.EnsureCurrentUserActive();

        var deletedAt = DateTime.UtcNow;
        await _mainGroupRepository.SetGroupDeleted(_currentUser.UserMainGroupId, deletedAt);

        await ResetGroupCache(_currentUser.UserMainGroupId);
    }

    public async Task SoftDeleteGroup(long groupId)
    {
        await _userPermissionService.EnsureCurrentUserEmployee();
        await _userPermissionService.EnsureNotCurrentGroup(groupId);

        var deletedAt = DateTime.UtcNow;
        await _mainGroupRepository.SetGroupDeleted(groupId, deletedAt);

        await ResetGroupCache(groupId);
    }

    public async Task UndeleteGroup(long groupId)
    {
        await _userPermissionService.EnsureCurrentUserEmployee();
        await _userPermissionService.EnsureNotCurrentGroup(groupId);

        await _mainGroupRepository.SetGroupDeleted(groupId, null);

        await ResetGroupCache(groupId);
    }

    private async Task ResetGroupCache(long groupId)
    {
        await _cache.DeleteKeys(FreverUserPermissionService.GroupCacheKey(groupId));

        var publicInfix = VideoCacheKeys.PublicPrefix.GetKeyWithoutVersion();
        await _cache.DeleteKeysWithInfix(publicInfix);
    }
}