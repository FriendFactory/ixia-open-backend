using System;
using System.Linq;
using System.Threading.Tasks;
using AuthServer.Permissions.Services;
using AuthServerShared;
using FluentValidation;
using Frever.Client.Core.Features.Social.DataAccess;
using Frever.Client.Core.Features.Social.Profiles;
using Frever.Client.Shared.Files;
using Frever.ClientService.Contract.Social;
using Frever.Shared.MainDb.Entities;

namespace Frever.Client.Core.Features.Social.GroupBlocking;

public class BlockUserService(
    UserInfo currentUser,
    IMainDbRepository mainDbRepository,
    IFileStorageService fileStorage,
    IUserPermissionService userPermissionService
) : IBlockUserService
{
    private readonly UserInfo _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));

    //TODO: add pagination in 1.9 version
    public async Task<Profile[]> GetBlockedProfiles()
    {
        await userPermissionService.EnsureCurrentUserActive();

        var result = await mainDbRepository.GetBlockedUsersProfiles(_currentUser);
        if (result.Length == 0)
            return [];

        await fileStorage.InitUrls<Group>(result);

        return result.Select(p => ProfileMapper.FromInternal(p)).ToArray();
    }

    public async Task BlockUser(long blockedGroupId)
    {
        await userPermissionService.EnsureCurrentUserActive();

        if (_currentUser.UserMainGroupId == blockedGroupId)
            throw new ValidationException("User can't block himself");

        await mainDbRepository.BlockUser(blockedGroupId, _currentUser);

        await mainDbRepository.UnFollowAsync(blockedGroupId, _currentUser);
        await mainDbRepository.UnFollowAsync(_currentUser, blockedGroupId);
    }

    public async Task UnBlockUser(long blockedGroupId)
    {
        await userPermissionService.EnsureCurrentUserActive();

        await mainDbRepository.UnBlockUser(blockedGroupId, _currentUser);
    }
}