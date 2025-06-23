using System;
using System.Threading.Tasks;
using AuthServer.Features.CredentialUpdate.Contracts;
using AuthServer.Features.CredentialUpdate.Models;
using AuthServer.Repositories;
using AuthServer.Services.GoogleAuth;
using Common.Infrastructure;
using Common.Models;
using Microsoft.EntityFrameworkCore;

namespace AuthServer.Features.CredentialUpdate.Handlers;

public class GoogleIdHandler(IUserRepository repo, IGoogleAuthService googleAuthService) : ICredentialHandler
{
    public CredentialType HandlerType => CredentialType.GoogleId;

    public async Task AddCredentials(AddCredentialsRequest request, long groupId, CredentialStatus status)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(request.GoogleId);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.IdentityToken);

        if (status.HasGoogleId)
            throw AppErrorWithStatusCodeException.BadRequest("Account has such login method", ErrorCodes.Auth.AccountHasLoginMethod);

        var validation = await ValidateCredential(request.GoogleId, request.IdentityToken);
        if (!validation.IsValid)
            throw AppErrorWithStatusCodeException.BadRequest(validation.ErrorMessage, validation.ErrorCode);

        if (await repo.IsGoogleIdRegistered(request.GoogleId))
            throw AppErrorWithStatusCodeException.BadRequest("GoogleId is already in use", ErrorCodes.Auth.GoogleIdAlreadyUsed);

        var mainDbUser = await repo.GetUserByGroupId(groupId).Include(e => e.MainGroup).FirstOrDefaultAsync();
        if (mainDbUser is null)
            throw AppErrorWithStatusCodeException.NotFound("User not found", ErrorCodes.Auth.UserNotFound);

        mainDbUser.GoogleId = request.GoogleId;

        if (mainDbUser.MainGroup.IsTemporary)
            mainDbUser.MainGroup.IsTemporary = false;

        await repo.SaveChanges();
    }

    public Task<(bool IsValid, string ErrorMessage, string ErrorCode)> ValidateCurrentCredential(VerifyUserRequest request, ShortUserInfo userInfo)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userInfo.GoogleId);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.IdentityToken);

        return ValidateCredential(userInfo.GoogleId, request.IdentityToken);
    }

    public Task UpdateCredentials(UpdateCredentialsRequest request, long groupId, CredentialStatus status)
    {
        throw new NotImplementedException();
    }

    private async Task<(bool IsValid, string ErrorMessage, string ErrorCode)> ValidateCredential(string googleId, string identityToken)
    {
        var tokenGoogleId = await googleAuthService.ValidateAuthTokenAsync(identityToken);

        return tokenGoogleId is null || tokenGoogleId != googleId
                   ? (false, "Invalid identity token", ErrorCodes.Auth.GoogleTokenInvalid)
                   : (true, null, null);
    }
}