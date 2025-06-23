using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using AuthServer.Features.CredentialUpdate.Contracts;
using AuthServer.Features.CredentialUpdate.Models;
using AuthServer.Models;
using AuthServer.Repositories;
using AuthServer.Services.PhoneNumberAuth;
using Common.Infrastructure;
using Common.Models;
using IdentityModel;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace AuthServer.Features.CredentialUpdate.Handlers;

public class PhoneNumberHandler(
    IUserRepository repo,
    IPhoneNumberAuthService phoneNumberAuthService,
    UserManager<ApplicationUser> userManager
) : ICredentialHandler
{
    public CredentialType HandlerType => CredentialType.PhoneNumber;

    public async Task AddCredentials(AddCredentialsRequest request, long groupId, CredentialStatus status)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(request.PhoneNumber);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.VerificationCode);

        if (status.PhoneNumber != null)
            throw AppErrorWithStatusCodeException.BadRequest("Account has such login method", ErrorCodes.Auth.AccountHasLoginMethod);

        request.PhoneNumber = await phoneNumberAuthService.FormatPhoneNumber(request.PhoneNumber);
        if (request.PhoneNumber == null)
            throw AppErrorWithStatusCodeException.BadRequest("Invalid phone number format", ErrorCodes.Auth.PhoneNumberFormatInvalid);

        var validation = await ValidateCredential(request.PhoneNumber, request.VerificationCode);
        if (!validation.IsValid)
            throw AppErrorWithStatusCodeException.BadRequest(validation.ErrorMessage, validation.ErrorCode);

        if (await repo.IsPhoneRegistered(request.PhoneNumber))
            throw AppErrorWithStatusCodeException.BadRequest("Phone number is already in use", ErrorCodes.Auth.PhoneNumberAlreadyUsed);

        await UpdateUserCredentials(groupId, request.PhoneNumber);
    }

    public Task<(bool IsValid, string ErrorMessage, string ErrorCode)> ValidateCurrentCredential(VerifyUserRequest request, ShortUserInfo userInfo)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userInfo.PhoneNumber);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.VerificationCode);

        return ValidateCredential(userInfo.PhoneNumber, request.VerificationCode);
    }

    public async Task UpdateCredentials(UpdateCredentialsRequest request, long groupId, CredentialStatus status)
    {
        if (string.IsNullOrWhiteSpace(request.PhoneNumber))
        {
            if (!status.HasPassword && !status.HasAppleId && !status.HasGoogleId && status.Email == null)
                throw AppErrorWithStatusCodeException.BadRequest("User must have login method", ErrorCodes.Auth.AccountLastLoginMethod);
        }
        else
        {
            request.PhoneNumber = await phoneNumberAuthService.FormatPhoneNumber(request.PhoneNumber);
            if (request.PhoneNumber == null)
                throw AppErrorWithStatusCodeException.BadRequest("Invalid phone number format", ErrorCodes.Auth.PhoneNumberFormatInvalid);

            var validation = await ValidateCredential(request.PhoneNumber, request.VerificationCode);
            if (!validation.IsValid)
                throw AppErrorWithStatusCodeException.BadRequest(validation.ErrorMessage, validation.ErrorCode);

            if (await repo.IsPhoneRegistered(request.PhoneNumber))
                throw AppErrorWithStatusCodeException.BadRequest("Phone number is already in use", ErrorCodes.Auth.PhoneNumberAlreadyUsed);
        }

        await UpdateUserCredentials(groupId, request.PhoneNumber);
    }

    private async Task UpdateUserCredentials(long groupId, string phoneNumber)
    {
        var mainDbUser = await repo.GetUserByGroupId(groupId).Include(e => e.MainGroup).FirstOrDefaultAsync();
        if (mainDbUser is null)
            throw AppErrorWithStatusCodeException.NotFound("User not found", ErrorCodes.Auth.UserNotFound);

        await using var authDbTransaction = await repo.BeginAuthDbTransactionAsync();
        await using var mainDbTransaction = await repo.BeginMainDbTransactionAsync();

        var authDbUser = await userManager.FindByIdAsync(mainDbUser.IdentityServerId.ToString());
        if (authDbUser is null)
            throw AppErrorWithStatusCodeException.NotFound("Auth user not found", ErrorCodes.Auth.UserNotFound);

        await UpdateClaims(JwtClaimTypes.PhoneNumber, phoneNumber);
        authDbUser.PhoneNumber = phoneNumber;
        mainDbUser.PhoneNumber = phoneNumber;

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

    private async Task<(bool IsValid, string ErrorMessage, string ErrorCode)> ValidateCredential(
        string phoneNumber,
        string verificationCode
    )
    {
        var valid = await phoneNumberAuthService.ValidateVerificationCode(phoneNumber, verificationCode);

        return !valid ? (false, "Invalid verification code", ErrorCodes.Auth.VerificationCodeInvalid) : (true, null, null);
    }
}