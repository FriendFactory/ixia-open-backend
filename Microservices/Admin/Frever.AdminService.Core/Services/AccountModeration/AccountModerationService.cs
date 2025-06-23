using System;
using System.Threading.Tasks;
using AuthServer.Permissions.Services;
using AuthServerShared;
using Common.Infrastructure;

namespace Frever.AdminService.Core.Services.AccountModeration;

public class AccountModerationService(
    IAccountModerationRepository repo,
    IUserPermissionService permissionService,
    IUserPermissionManagementService permissionManagementService,
    IAccountHardDeletionService accountHardDeletionService
) : IAccountModerationService
{
    private readonly IAccountModerationRepository _repo = repo ?? throw new ArgumentNullException(nameof(repo));

    private readonly IAccountHardDeletionService _accountHardDeletionService =
        accountHardDeletionService ?? throw new ArgumentNullException(nameof(accountHardDeletionService));

    private readonly IUserPermissionService _permissionService =
        permissionService ?? throw new ArgumentNullException(nameof(permissionService));

    private readonly IUserPermissionManagementService _permissionManagementService =
        permissionManagementService ?? throw new ArgumentNullException(nameof(permissionManagementService));

    public async Task UpdateUserAuthData(UserAuthData model)
    {
        await _permissionService.EnsureHasSocialAccess();

        if (string.IsNullOrWhiteSpace(model.IdentityServerId))
            throw AppErrorWithStatusCodeException.BadRequest("User id can not be null or empty", "UserIdNull");

        var authDbUser = await _repo.GetAuthUser(model.IdentityServerId);
        if (authDbUser == null)
            throw AppErrorWithStatusCodeException.NotFound("Auth user is not found", "UserNotFound");

        var mainDbUser = await _repo.GetUserByIdentityServerId(model.IdentityServerId);
        if (mainDbUser == null)
            throw AppErrorWithStatusCodeException.NotFound("User is not found", "UserNotFound");

        var transactions = await _repo.BeginTransaction();
        try
        {
            model.Email = model.Email.ToLower();

            await UpdateUserClaim(model.IdentityServerId, authDbUser.Email, model.Email, Claims.Email);
            await UpdateUserClaim(model.IdentityServerId, authDbUser.PhoneNumber, model.PhoneNumber, Claims.PhoneNumber);

            authDbUser.Email = model.Email;
            authDbUser.NormalizedEmail = model.Email?.ToUpper();
            authDbUser.PhoneNumber = model.PhoneNumber;
            await _repo.UpdateAuthUser(authDbUser);

            mainDbUser.Email = model.Email;
            mainDbUser.PhoneNumber = model.PhoneNumber;
            await _repo.UpdateUser(mainDbUser);

            await transactions.Item1.CommitAsync();
            await transactions.Item2.CommitAsync();
        }
        catch (Exception ex)
        {
            await transactions.Item1.RollbackAsync();
            await transactions.Item2.RollbackAsync();

            var message = $"{ex.Message}  {ex.InnerException?.Message}";

            throw AppErrorWithStatusCodeException.BadRequest(message, "UpdateUserFail");
        }
        finally
        {
            await transactions.Item1.DisposeAsync();
            await transactions.Item2.DisposeAsync();
        }
    }

    public async Task SoftDeleteGroup(long groupId)
    {
        await _permissionService.EnsureHasSocialAccess();

        await using var transaction = await _repo.BeginMainDbTransaction();

        await _permissionManagementService.SoftDeleteGroup(groupId);

        await transaction.CommitAsync();
    }

    public async Task HardDeleteGroup(long groupId)
    {
        await _permissionService.EnsureHasSocialAccess();

        await SoftDeleteGroup(groupId);

        await _accountHardDeletionService.HardDeleteUserData(groupId);
    }

    private async Task UpdateUserClaim(string identityServerId, string oldClaimValue, string newClaimValue, string claimType)
    {
        if (oldClaimValue != newClaimValue)
        {
            var claim = await _repo.GetAuthUserClaim(identityServerId, claimType);
            if (claim == null)
            {
                await _repo.CreateAuthUserClaim(identityServerId, claimType, newClaimValue);
                return;
            }

            await _repo.UpdateAuthUserClaim(identityServerId, claimType, newClaimValue);
        }
    }
}