using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using AuthServer.Features.CredentialUpdate.Contracts;
using AuthServer.Features.CredentialUpdate.Models;
using AuthServer.Models;
using AuthServer.Repositories;
using AuthServer.Services.EmailAuth;
using Common.Infrastructure;
using Common.Models;
using IdentityModel;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace AuthServer.Features.CredentialUpdate.Handlers;

public class EmailHandler(IUserRepository repo, IEmailAuthService emailAuthService, UserManager<ApplicationUser> userManager)
    : ICredentialHandler
{
    public CredentialType HandlerType => CredentialType.Email;

    public async Task AddCredentials(AddCredentialsRequest request, long groupId, CredentialStatus status)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Email);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.VerificationCode);

        if (status.Email != null)
            throw AppErrorWithStatusCodeException.BadRequest("Account has such login method", ErrorCodes.Auth.AccountHasLoginMethod);

        var validation = await ValidateCredential(request.Email, request.VerificationCode);
        if (!validation.IsValid)
            throw AppErrorWithStatusCodeException.BadRequest(validation.ErrorMessage, validation.ErrorCode);

        if (await repo.IsEmailRegistered(request.Email))
            throw AppErrorWithStatusCodeException.BadRequest("Email is already in use", ErrorCodes.Auth.EmailAlreadyUsed);

        await UpdateUserCredentials(groupId, request.Email);
    }

    public Task<(bool IsValid, string ErrorMessage, string ErrorCode)> ValidateCurrentCredential(VerifyUserRequest request, ShortUserInfo userInfo)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userInfo.Email);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.VerificationCode);

        return ValidateCredential(userInfo.Email, request.VerificationCode);
    }

    public async Task UpdateCredentials(UpdateCredentialsRequest request, long groupId, CredentialStatus status)
    {
        if (string.IsNullOrWhiteSpace(request.Email))
        {
            if (!status.HasPassword && !status.HasAppleId && !status.HasGoogleId && status.PhoneNumber == null)
                throw AppErrorWithStatusCodeException.BadRequest("User must have login method", ErrorCodes.Auth.AccountLastLoginMethod);
        }
        else
        {
            var validation = await ValidateCredential(request.Email, request.VerificationCode);
            if (!validation.IsValid)
                throw AppErrorWithStatusCodeException.BadRequest(validation.ErrorMessage, validation.ErrorCode);

            if (await repo.IsEmailRegistered(request.Email))
                throw AppErrorWithStatusCodeException.BadRequest("Email is already in use", ErrorCodes.Auth.EmailAlreadyUsed);
        }

        await UpdateUserCredentials(groupId, request.Email);
    }

    private async Task UpdateUserCredentials(long groupId, string email)
    {
        var mainDbUser = await repo.GetUserByGroupId(groupId).Include(e => e.MainGroup).FirstOrDefaultAsync();
        if (mainDbUser is null)
            throw AppErrorWithStatusCodeException.NotFound("User not found", ErrorCodes.Auth.UserNotFound);

        await using var authDbTransaction = await repo.BeginAuthDbTransactionAsync();
        await using var mainDbTransaction = await repo.BeginMainDbTransactionAsync();

        var authDbUser = await userManager.FindByIdAsync(mainDbUser.IdentityServerId.ToString());
        if (authDbUser is null)
            throw AppErrorWithStatusCodeException.NotFound("Auth user not found", ErrorCodes.Auth.UserNotFound);

        await UpdateClaims(JwtClaimTypes.Email, email);
        authDbUser.Email = email;
        mainDbUser.Email = email;

        if (mainDbUser.MainGroup.IsTemporary)
            mainDbUser.MainGroup.IsTemporary = false;

        await userManager.UpdateAsync(authDbUser);
        await repo.SaveChanges();

        await mainDbTransaction.CommitAsync();
        await authDbTransaction.CommitAsync();
        return;

        async Task UpdateClaims(string claimType, string value)
        {
            var allClaims = await userManager.GetClaimsAsync(authDbUser);
            var claim = allClaims.FirstOrDefault(e => e.Type == claimType);

            if (value != null && claim == null)
                await userManager.AddClaimAsync(authDbUser, new Claim(claimType, value));
            else if (value == null && claim != null)
                await userManager.RemoveClaimAsync(authDbUser, claim);
            else if (value != null && claim != null && value != claim.Value)
                await userManager.ReplaceClaimAsync(authDbUser, claim, new Claim(claimType, value));
        }
    }

    private async Task<(bool IsValid, string ErrorMessage, string ErrorCode)> ValidateCredential(string email, string verificationCode)
    {
        var valid = await emailAuthService.ValidateVerificationCode(email, verificationCode);

        return !valid ? (false, "Invalid verification code", ErrorCodes.Auth.VerificationCodeInvalid) : (true, null, null);
    }
}