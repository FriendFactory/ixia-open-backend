using System;
using System.Threading.Tasks;
using AuthServer.Features.CredentialUpdate.Contracts;
using AuthServer.Features.CredentialUpdate.Models;
using AuthServer.Repositories;
using AuthServer.Services.AppleAuth;
using Common.Infrastructure;
using Common.Models;
using Microsoft.EntityFrameworkCore;

namespace AuthServer.Features.CredentialUpdate.Handlers;

public class AppleIdHandler(IUserRepository repo, IAppleAuthService appleAuthService) : ICredentialHandler
{
    public CredentialType HandlerType => CredentialType.AppleId;

    public async Task AddCredentials(AddCredentialsRequest request, long groupId, CredentialStatus status)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(request.AppleId);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.AppleIdentityToken);

        if (status.HasAppleId)
            throw AppErrorWithStatusCodeException.BadRequest("Account has such login method", ErrorCodes.Auth.AccountHasLoginMethod);

        var validation = await ValidateCredential(request.AppleId, request.AppleIdentityToken);
        if (!validation.IsValid)
            throw AppErrorWithStatusCodeException.BadRequest(validation.ErrorMessage, validation.ErrorCode);

        if (await repo.IsAppleIdRegistered(request.AppleId))
            throw AppErrorWithStatusCodeException.BadRequest("AppleId is already in use", ErrorCodes.Auth.AppleIdAlreadyUsed);

        var mainDbUser = await repo.GetUserByGroupId(groupId).Include(e => e.MainGroup).FirstOrDefaultAsync();
        if (mainDbUser is null)
            throw AppErrorWithStatusCodeException.NotFound("User not found", ErrorCodes.Auth.UserNotFound);

        mainDbUser.AppleId = request.AppleId;

        if (mainDbUser.MainGroup.IsTemporary)
            mainDbUser.MainGroup.IsTemporary = false;

        await repo.SaveChanges();
    }

    public Task<(bool IsValid, string ErrorMessage, string ErrorCode)> ValidateCurrentCredential(VerifyUserRequest request, ShortUserInfo userInfo)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userInfo.AppleId);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.AppleIdentityToken);

        return ValidateCredential(userInfo.AppleId, request.AppleIdentityToken);
    }

    public Task UpdateCredentials(UpdateCredentialsRequest request, long groupId, CredentialStatus status)
    {
        throw new NotImplementedException();
    }

    private async Task<(bool IsValid, string ErrorMessage, string ErrorCode)> ValidateCredential(string appleId, string identityToken)
    {
        var tokenAppleId = await appleAuthService.ValidateAuthTokenAsync(identityToken);

        return tokenAppleId is null || tokenAppleId != appleId
                   ? (false, "Invalid identity token", ErrorCodes.Auth.AppleTokenInvalid)
                   : (true, null, null);
    }
}