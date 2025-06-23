using System;
using System.Threading.Tasks;
using AuthServer.Features.CredentialUpdate.Contracts;
using AuthServer.Features.CredentialUpdate.Models;
using AuthServer.Models;
using AuthServer.Repositories;
using AuthServer.Services.UserManaging;
using Common.Infrastructure;
using Common.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace AuthServer.Features.CredentialUpdate.Handlers;

public class PasswordHandler(IUserRepository repo, UserManager<ApplicationUser> userManager, ICredentialValidateService credentialValidateService) : ICredentialHandler
{
    public CredentialType HandlerType => CredentialType.Password;

    public async Task AddCredentials(AddCredentialsRequest request, long groupId, CredentialStatus status)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Password);

        if (status.HasPassword)
            throw AppErrorWithStatusCodeException.BadRequest("Account has such login method", ErrorCodes.Auth.AccountHasLoginMethod);

        await UpdateUserCredentials(request.Password, groupId);
    }

    public async Task<(bool IsValid, string ErrorMessage, string ErrorCode)> ValidateCurrentCredential(
        VerifyUserRequest request,
        ShortUserInfo userInfo
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Password);

        var authUser = await userManager.FindByIdAsync(userInfo.IdentityServerId.ToString());
        if (authUser is null)
            throw AppErrorWithStatusCodeException.NotFound("Auth user not found", ErrorCodes.Auth.UserNotFound);

        if (!await userManager.CheckPasswordAsync(authUser, request.Password))
            return (false, "Invalid password", ErrorCodes.Auth.PasswordInvalid);

        return (true, null, null);
    }

    public async Task UpdateCredentials(UpdateCredentialsRequest request, long groupId, CredentialStatus status)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Password);

        await UpdateUserCredentials(request.Password, groupId);
    }

    private async Task UpdateUserCredentials(string password, long groupId)
    {
        var validationResult = await credentialValidateService.ValidatePassword(password, null);
        if (!validationResult.Ok)
            throw AppErrorWithStatusCodeException.BadRequest(validationResult.Error ?? "Invalid password", ErrorCodes.Auth.PasswordInvalid);

        var mainDbUser = await repo.GetUserByGroupId(groupId).Include(e => e.MainGroup).FirstOrDefaultAsync();
        if (mainDbUser is null)
            throw AppErrorWithStatusCodeException.NotFound("User not found", ErrorCodes.Auth.UserNotFound);

        var authDbUser = await userManager.FindByIdAsync(mainDbUser.IdentityServerId.ToString());
        if (authDbUser is null)
            throw AppErrorWithStatusCodeException.NotFound("Auth user not found", ErrorCodes.Auth.UserNotFound);

        if (await userManager.CheckPasswordAsync(authDbUser, password))
            throw AppErrorWithStatusCodeException.BadRequest("New password matches the current one", ErrorCodes.Auth.PasswordAlreadyExist);

        var resetToken = await userManager.GeneratePasswordResetTokenAsync(authDbUser);
        await userManager.ResetPasswordAsync(authDbUser, resetToken, password);

        if (mainDbUser.MainGroup.IsTemporary)
            mainDbUser.MainGroup.IsTemporary = false;

        mainDbUser.HasPassword = mainDbUser.HasPassword == false;
        await repo.SaveChanges();
    }
}