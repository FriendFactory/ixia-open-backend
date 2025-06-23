using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using AuthServer.Permissions.DataAccess;
using AuthServerShared;
using Common.Infrastructure;
using Common.Infrastructure.Caching;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AuthServer.Permissions.Services;

public class FreverUserPermissionService(UserInfo currentUser, IMainGroupRepository repo, ICache cache) : IUserPermissionService
{
    private readonly ICache _cache = cache ?? throw new ArgumentNullException(nameof(cache));
    private readonly IMainGroupRepository _repo = repo ?? throw new ArgumentNullException(nameof(repo));

    public Task<bool> IsStarCreator(long groupId)
    {
        return _repo.FindGroupById(groupId).AnyAsync(e => e.IsStarCreator);
    }

    public async Task<bool> IsCurrentUserStarCreator()
    {
        return currentUser != null && await IsStarCreator(currentUser);
    }

    public Task EnsureNotCurrentGroup(long groupId)
    {
        if (currentUser != null && currentUser.UserMainGroupId == groupId)
            throw AppErrorWithStatusCodeException.BadRequest("You can't perform this action on your account", "CantHarmYourself");

        return Task.CompletedTask;
    }

    public async Task<bool> IsCurrentUserEmployee()
    {
        var accountInfo = await GetAccountInfo(currentUser?.UserMainGroupId);

        return accountInfo != null && accountInfo.IsExisting && !accountInfo.IsRestrictedAccess && accountInfo.AccessScopes.Length != 0;
    }

    public async Task EnsureCurrentUserEmployee()
    {
        var isEmployee = await IsCurrentUserEmployee();
        if (!isEmployee)
            throw AppErrorWithStatusCodeException.NotEmployee();
    }

    public async Task<bool> IsAccountActive(long groupId)
    {
        var accountInfo = await GetAccountInfo(groupId);

        return accountInfo != null && accountInfo.IsExisting && !accountInfo.IsRestrictedAccess;
    }

    public async Task EnsureCurrentUserActive()
    {
        var account = await GetAccountInfo(currentUser?.UserMainGroupId);
        if (account == null || !account.IsExisting || account.IsRestrictedAccess)
            throw AppErrorWithStatusCodeException.AccountBlocked(LogLevel.Error);
    }

    public async Task<string[]> GetUserReadinessAccessScopes(long groupId)
    {
        var account = await GetAccountInfo(groupId);
        if (account == null || !account.IsExisting || account.IsRestrictedAccess)
            throw AppErrorWithStatusCodeException.AccountBlocked(LogLevel.Error);

        return account.AccessScopes.Where(e => e is KnownAccessScopes.ReadinessFull or KnownAccessScopes.ReadinessArtists).ToArray();
    }

    public async Task EnsureHasAssetReadAccess()
    {
        await EnsureUserHasAccessToScope(KnownAccessScopes.AssetRead);
    }

    public async Task EnsureHasAssetFullAccess()
    {
        await EnsureUserHasAccessToScope(KnownAccessScopes.AssetFull);
    }

    public async Task EnsureHasCategoryReadAccess()
    {
        await EnsureUserHasAccessToScope(KnownAccessScopes.CategoriesRead);
    }

    public async Task EnsureHasCategoryFullAccess()
    {
        await EnsureUserHasAccessToScope(KnownAccessScopes.CategoriesFull);
    }

    public async Task EnsureHasBankingAccess()
    {
        await EnsureUserHasAccessToScope(KnownAccessScopes.Banking);
    }

    public async Task EnsureHasSeasonsAccess()
    {
        await EnsureUserHasAccessToScope(KnownAccessScopes.Seasons);
    }

    public async Task EnsureHasSettingsAccess()
    {
        await EnsureUserHasAccessToScope(KnownAccessScopes.Settings);
    }

    public async Task EnsureHasSocialAccess()
    {
        await EnsureUserHasAccessToScope(KnownAccessScopes.Social);
    }

    public async Task EnsureHasVideoModerationAccess()
    {
        await EnsureUserHasAccessToScope(KnownAccessScopes.VideoModeration);
    }

    public async Task EnsureHasChatMessageSendingAccess()
    {
        await EnsureUserHasAccessToScope(KnownAccessScopes.ChatMessageSending);
    }

    private async Task EnsureUserHasAccessToScope(string accessScope)
    {
        var account = await GetAccountInfo(currentUser?.UserMainGroupId);
        if (account == null || !account.IsExisting || account.IsRestrictedAccess)
            throw AppErrorWithStatusCodeException.AccountBlocked(LogLevel.Error);

        if (!account.AccessScopes.Contains(accessScope))
            throw new AppErrorWithStatusCodeException("User doesn't have required access", HttpStatusCode.Forbidden);
    }

    private async Task<AccountInfo> GetAccountInfo(long? groupId)
    {
        if (!groupId.HasValue)
            return new AccountInfo();

        return await _cache.GetOrCache(GroupCacheKey(groupId.Value), IsAccountPermanent, TimeSpan.FromMinutes(30));

        async Task<AccountInfo> IsAccountPermanent()
        {
            var group = await _repo.FindGroupById(groupId.Value).Select(e => new {e.IsBlocked, e.DeletedAt}).FirstOrDefaultAsync();
            if (group == null)
                return new AccountInfo();

            var roles = await _repo.GetUserRoles(groupId.Value);

            return new AccountInfo
                   {
                       GroupId = groupId.Value,
                       IsExisting = true,
                       IsRestrictedAccess = group.IsBlocked || group.DeletedAt != null,
                       AccessScopes = roles.SelectMany(e => e.RoleAccessScope).Select(e => e.AccessScope).Distinct().ToArray()
                   };
        }
    }

    public static string GroupCacheKey(long groupId)
    {
        return $"account::group::{groupId}";
    }

    private class AccountInfo
    {
        public long GroupId { get; init; }
        public bool IsExisting { get; init; }
        public bool IsRestrictedAccess { get; init; }
        public string[] AccessScopes { get; init; } = [];
    }
}