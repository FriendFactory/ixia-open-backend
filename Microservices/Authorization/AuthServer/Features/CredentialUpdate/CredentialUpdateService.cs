using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using AuthServer.Contracts;
using AuthServer.Features.CredentialUpdate.Contracts;
using AuthServer.Features.CredentialUpdate.Handlers;
using AuthServer.Features.CredentialUpdate.Models;
using AuthServer.Models;
using AuthServer.Permissions.Services;
using AuthServer.Repositories;
using AuthServer.Services.EmailAuth;
using AuthServer.Services.PhoneNumberAuth;
using AuthServer.Services.UserManaging;
using AuthServerShared;
using Common.Infrastructure;
using Common.Models;
using FluentValidation;
using IdentityModel;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AuthServer.Features.CredentialUpdate;

public class CredentialUpdateService(
    UserInfo currentUser,
    ILoggerFactory loggerFactory,
    IUserRepository repo,
    IEmailAuthService emailAuthService,
    IPhoneNumberAuthService phoneNumberAuthService,
    IUserPermissionService permissionService,
    ITokenProvider tokenProvider,
    UserManager<ApplicationUser> userManager,
    IEnumerable<ICredentialHandler> credentialHandlers,
    ICredentialValidateService credentialValidateService,
    IValidator<VerifyUserRequest> verifyUserRequestValidator,
    IValidator<AddCredentialsRequest> addCredentialsRequestValidator,
    IValidator<UpdateCredentialsRequest> updateCredentialsRequestValidator
) : ICredentialUpdateService
{
    private readonly ILogger _logger = loggerFactory.CreateLogger("Frever.Auth.CredentialUpdateService");
    private readonly UserInfo _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));

    public async Task<CredentialStatus> GetCredentialStatus()
    {
        var user = await GetShortUserInfo();
        if (user is null)
            throw AppErrorWithStatusCodeException.NotFound("User not found", ErrorCodes.Auth.UserNotFound);

        return new CredentialStatus
               {
                   Email = CredentialsFormatter.MaskEmail(user.Email),
                   PhoneNumber = CredentialsFormatter.MaskPhoneNumber(user.PhoneNumber),
                   HasAppleId = user.AppleId != null,
                   HasGoogleId = user.GoogleId != null,
                   HasPassword = !user.IsTemporary && await repo.AuthUserHasPassword(user.IdentityServerId.ToString())
               };
    }

    public async Task VerifyCredentials(VerifyCredentialRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        await permissionService.EnsureCurrentUserActive();

        _logger.LogInformation("VerifyCredentials(email={}, phoneNumber={}, isNew={})", request.Email, request.PhoneNumber, request.IsNew);

        var user = await GetShortUserInfo();
        if (user is null)
            throw AppErrorWithStatusCodeException.NotFound("User not found", ErrorCodes.Auth.UserNotFound);

        if (!string.IsNullOrWhiteSpace(request.Email))
        {
            if (request.IsNew && await repo.IsEmailRegistered(request.Email))
                throw AppErrorWithStatusCodeException.BadRequest("Credential is already in use", ErrorCodes.Auth.EmailAlreadyUsed);
            if (!request.IsNew && request.Email != user.Email)
                throw AppErrorWithStatusCodeException.BadRequest("Invalid credentials", ErrorCodes.Auth.EmailInvalid);

            await emailAuthService.SendEmailVerification(new VerifyEmailRequest {Email = request.Email});
        }

        if (!string.IsNullOrWhiteSpace(request.PhoneNumber))
        {
            request.PhoneNumber = await phoneNumberAuthService.FormatPhoneNumber(request.PhoneNumber);
            if (request.PhoneNumber == null)
                throw AppErrorWithStatusCodeException.BadRequest("Invalid phone number format", ErrorCodes.Auth.PhoneNumberFormatInvalid);

            if (request.IsNew && await repo.IsPhoneRegistered(request.PhoneNumber))
                throw AppErrorWithStatusCodeException.BadRequest("Credential is already in use", ErrorCodes.Auth.PhoneNumberAlreadyUsed);
            if (!request.IsNew && request.PhoneNumber != user.PhoneNumber)
                throw AppErrorWithStatusCodeException.BadRequest("Invalid credentials", ErrorCodes.Auth.PhoneNumberInvalid);

            await phoneNumberAuthService.SendPhoneNumberVerification(new VerifyPhoneNumberRequest {PhoneNumber = request.PhoneNumber});
        }
    }

    public async Task AddCredentials(AddCredentialsRequest request)
    {
        await permissionService.EnsureCurrentUserActive();
        await addCredentialsRequestValidator.ValidateAndThrowAsync(request);

        _logger.LogInformation(
            "AddCredentials(type={}, appleId={}, googleId={}, email={}, phoneNumber={}, hasPassword={})",
            request.Type,
            request.AppleId,
            request.GoogleId,
            request.Email,
            request.PhoneNumber,
            !string.IsNullOrWhiteSpace(request.Password)
        );

        if (request.Type != CredentialType.Password && await repo.GetGroupById(_currentUser).AnyAsync(e => e.IsMinor))
            throw AppErrorWithStatusCodeException.BadRequest("Minor can add only password", ErrorCodes.Auth.MinorCredentialsInvalid);

        request.Email = request.Email?.ToLower();

        var handler = GetHandler(request.Type);
        _logger.LogInformation("{Handler} handler selected for {Type} type", handler.GetType().Name, request.Type.ToString());

        var status = await GetCredentialStatus();

        await handler.AddCredentials(request, _currentUser, status);
    }

    public async Task<VerifyUserResponse> VerifyUser(VerifyUserRequest request)
    {
        await permissionService.EnsureCurrentUserActive();
        await verifyUserRequestValidator.ValidateAndThrowAsync(request);

        var user = await GetShortUserInfo();
        if (user is null)
            throw AppErrorWithStatusCodeException.NotFound("User not found", ErrorCodes.Auth.UserNotFound);

        var handler = GetHandler(request.Type);
        _logger.LogInformation("{Handler} handler selected for {Type} type", handler.GetType().Name, request.Type.ToString());

        var result = await handler.ValidateCurrentCredential(request, user);
        if (!result.IsValid)
            return new VerifyUserResponse(result.ErrorMessage, result.ErrorCode);

        var token = tokenProvider.GenerateToken(_currentUser);
        return new VerifyUserResponse(token);
    }

    public async Task UpdateCredentials(UpdateCredentialsRequest request)
    {
        await permissionService.EnsureCurrentUserActive();
        await updateCredentialsRequestValidator.ValidateAndThrowAsync(request);

        _logger.LogInformation(
            "UpdateCredentials(type={}, email={}, phoneNumber={}, hasPassword={})",
            request.Type,
            request.Email,
            request.PhoneNumber,
            !string.IsNullOrWhiteSpace(request.Password)
        );

        var payload = tokenProvider.ParseToken(request.VerificationToken);
        if (payload is null || payload.ExpiredAt <= DateTime.UtcNow.Ticks || payload.GroupId != _currentUser)
            throw AppErrorWithStatusCodeException.BadRequest("Invalid verification token", ErrorCodes.Auth.VerificationTokenInvalid);

        if (request.Type != CredentialType.Password && await repo.GetGroupById(_currentUser).AnyAsync(e => e.IsMinor))
            throw AppErrorWithStatusCodeException.BadRequest("Minor can add only password", ErrorCodes.Auth.MinorCredentialsInvalid);

        if (request.Type is CredentialType.AppleId or CredentialType.GoogleId)
            return;

        var status = await GetCredentialStatus();

        var handler = GetHandler(request.Type);
        _logger.LogInformation("{Handler} handler selected for {Type} type", handler.GetType().Name, request.Type.ToString());

        await handler.UpdateCredentials(request, _currentUser, status);
    }

    public async Task<UpdateUserNameResponse> UpdateUserName(UpdateUserNameRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.UserName);

        var group = await repo.GetGroupById(_currentUser).Select(e => new {e.NickNameUpdatedAt}).FirstOrDefaultAsync();
        if (group is null)
            throw AppErrorWithStatusCodeException.NotFound("Account is not found or not accessible", ErrorCodes.Auth.UserNotFound);

        if (group.NickNameUpdatedAt != null && group.NickNameUpdatedAt > DateTime.UtcNow.AddDays(-Constants.UsernameUpdateIntervalDays))
            return new UpdateUserNameResponse
                   {
                       ErrorCode = ErrorCodes.Auth.UsernameUpdateLimit, ErrorDetails = "Username update frequency exceeded"
                   };

        request.UserName = request.UserName.Trim().ToLower();

        var validationResult = await credentialValidateService.ValidateUserName(request.UserName);
        if (!validationResult.Ok)
            return new UpdateUserNameResponse
                   {
                       Ok = validationResult.Ok,
                       ErrorCode = validationResult.ErrorCode,
                       ErrorDetails = validationResult.ErrorDetails,
                       UpdateErrorDetails = validationResult.RegistrationErrorDetails
                   };

        await UpdateUserNameInDb(request.UserName);

        var usernameUpdateAvailableOn = DateTime.UtcNow.AddDays(Constants.UsernameUpdateIntervalDays);
        return new UpdateUserNameResponse {Ok = true, UsernameUpdateAvailableOn = usernameUpdateAvailableOn};
    }

    private async Task UpdateUserNameInDb(string userName)
    {
        await using var authDbTransaction = await repo.BeginAuthDbTransactionAsync();
        await using var mainDbTransaction = await repo.BeginMainDbTransactionAsync();

        var mainDbUser = await repo.GetUserByGroupId(_currentUser).Include(e => e.MainGroup).FirstOrDefaultAsync();
        if (mainDbUser is null)
            throw AppErrorWithStatusCodeException.NotFound("Account is not found or not accessible", ErrorCodes.Auth.UserNotFound);

        var authDbUser = await userManager.FindByIdAsync(mainDbUser.IdentityServerId.ToString());
        if (authDbUser is null)
            throw AppErrorWithStatusCodeException.NotFound("Account is not found or not accessible", ErrorCodes.Auth.UserNotFound);

        await userManager.ReplaceClaimAsync(
            authDbUser,
            new Claim(JwtClaimTypes.Name, authDbUser.UserName!),
            new Claim(JwtClaimTypes.Name, userName)
        );
        await userManager.SetUserNameAsync(authDbUser, userName);

        mainDbUser.MainGroup.NickName = userName;
        mainDbUser.MainGroup.NickNameUpdatedAt = DateTime.UtcNow;
        await repo.SaveChanges();

        await mainDbTransaction.CommitAsync();
        await authDbTransaction.CommitAsync();
    }

    private async Task<ShortUserInfo> GetShortUserInfo()
    {
        var user = await repo.GetUserByGroupId(_currentUser)
                             .Select(
                                  e => new ShortUserInfo
                                       {
                                           IdentityServerId = e.IdentityServerId,
                                           Email = e.Email,
                                           PhoneNumber = e.PhoneNumber,
                                           AppleId = e.AppleId,
                                           GoogleId = e.GoogleId,
                                           IsTemporary = e.MainGroup.IsTemporary
                                       }
                              )
                             .FirstOrDefaultAsync();
        return user;
    }

    private ICredentialHandler GetHandler(CredentialType credentialType)
    {
        var handler = credentialHandlers.FirstOrDefault(e => e.HandlerType == credentialType);

        if (handler is null)
            throw AppErrorWithStatusCodeException.BadRequest("Handler for this credential type not found", "MissingHandler");

        return handler;
    }
}