using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AuthServer.Contracts;
using AuthServer.Quickstart.Account;
using AuthServer.Repositories;
using AuthServer.Services.AppleAuth;
using AuthServer.Services.EmailAuth;
using AuthServer.Services.GoogleAuth;
using AuthServer.Services.PhoneNumberAuth;
using Common.Infrastructure.ModerationProvider;
using Common.Models;
using Microsoft.Extensions.Logging;

namespace AuthServer.Services.UserManaging;

public interface ICredentialValidateService
{
    Task<UserAccountRegistrationResult> ValidateNewAccountData(RegisterUserViewModel model, bool isMinor);
    Task<ValidatePasswordResult> ValidatePassword(string password, string username);
    Task<UserAccountRegistrationResult> ValidateUserName(string userName);
}

public partial class CredentialValidateService(
    IAppleAuthService appleAuthService,
    IEmailAuthService emailAuthService,
    IGoogleAuthService googleAuthService,
    IPhoneNumberAuthService phoneNumberAuthService,
    IModerationProviderApi moderationProviderApi,
    IUserRepository repo,
    ILoggerFactory loggerFactory
) : ICredentialValidateService
{
    private readonly ILogger _logger = loggerFactory.CreateLogger("Frever.Auth.CredentialValidationService");

    private static readonly Regex UsernameSymbolsRegex = new("^[a-zA-Z0-9]+$");

    public async Task<UserAccountRegistrationResult> ValidateNewAccountData(RegisterUserViewModel model, bool isMinor)
    {
        if (isMinor)
        {
            if (!string.IsNullOrWhiteSpace(model.Email) || !string.IsNullOrWhiteSpace(model.PhoneNumber) ||
                !string.IsNullOrWhiteSpace(model.AppleId) || !string.IsNullOrWhiteSpace(model.GoogleId))
                return new UserAccountRegistrationResult
                       {
                           ErrorCode = ErrorCodes.Auth.MinorCredentialsInvalid,
                           ErrorDetails = "Can't use email, phone number or AppleID for minor user account creation"
                       };

            if (string.IsNullOrWhiteSpace(model.Password))
                return new UserAccountRegistrationResult
                       {
                           ErrorCode = ErrorCodes.Auth.PasswordEmpty, ErrorDetails = "Password is required for minor user account creation"
                       };

            //TODO: add this check for all users in version 1.9
            if (StringComparer.OrdinalIgnoreCase.Equals(model.UserName, model.Password))
                return new UserAccountRegistrationResult
                       {
                           ErrorCode = ErrorCodes.Auth.PasswordMatchesUsername, ErrorDetails = "Password must not match username"
                       };
        }

        if (!string.IsNullOrWhiteSpace(model.Password) && !(await ValidatePassword(model.Password, null)).Ok)
            return new UserAccountRegistrationResult
                   {
                       ErrorCode = ErrorCodes.Auth.PasswordTooSimple, ErrorDetails = "Password too short or too simple"
                   };

        if (!string.IsNullOrWhiteSpace(model.Email) && string.IsNullOrWhiteSpace(model.AppleId) && string.IsNullOrWhiteSpace(model.GoogleId))
        {
            if (string.IsNullOrWhiteSpace(model.Password) && string.IsNullOrWhiteSpace(model.VerificationCode))
                return new UserAccountRegistrationResult
                       {
                           ErrorCode = ErrorCodes.Auth.PasswordOrTokenRequired, ErrorDetails = "Verification Token is required"
                       };

            if (await repo.IsEmailRegistered(model.Email))
                return new UserAccountRegistrationResult
                       {
                           ErrorCode = ErrorCodes.Auth.EmailAlreadyUsed, ErrorDetails = "This email is already in use"
                       };

            if (!string.IsNullOrWhiteSpace(model.VerificationCode) && !await emailAuthService.ValidateVerificationCode(model.Email, model.VerificationCode))
                return new UserAccountRegistrationResult
                       {
                           ErrorCode = ErrorCodes.Auth.VerificationCodeInvalid, ErrorDetails = "Invalid verification code"
                       };
        }

        if (!string.IsNullOrWhiteSpace(model.PhoneNumber))
        {
            if (string.IsNullOrWhiteSpace(model.VerificationCode))
                return new UserAccountRegistrationResult
                       {
                           ErrorCode = ErrorCodes.Auth.VerificationCodeEmpty, ErrorDetails = "Verification code is required"
                       };

            model.PhoneNumber = await phoneNumberAuthService.FormatPhoneNumber(model.PhoneNumber);

            if (model.PhoneNumber == null)
                return new UserAccountRegistrationResult
                       {
                           ErrorCode = ErrorCodes.Auth.PhoneNumberFormatInvalid, ErrorDetails = "Invalid phone number format"
                       };

            if (await repo.IsPhoneRegistered(model.PhoneNumber))
                return new UserAccountRegistrationResult
                       {
                           ErrorCode = ErrorCodes.Auth.PhoneNumberAlreadyUsed, ErrorDetails = "This phone number is already in use"
                       };

            if (!await phoneNumberAuthService.ValidateVerificationCode(model.PhoneNumber, model.VerificationCode))
                return new UserAccountRegistrationResult
                       {
                           ErrorCode = ErrorCodes.Auth.VerificationCodeInvalid, ErrorDetails = "Invalid verification code"
                       };
        }

        if (!string.IsNullOrWhiteSpace(model.AppleId))
        {
            if (await repo.IsAppleIdRegistered(model.AppleId))
                return new UserAccountRegistrationResult
                       {
                           ErrorCode = ErrorCodes.Auth.AppleIdAlreadyUsed, ErrorDetails = "This AppleId is already in use"
                       };

            var tokenAppleId = await appleAuthService.ValidateAuthTokenAsync(model.AppleIdentityToken);
            if (tokenAppleId != model.AppleId)
                return new UserAccountRegistrationResult
                       {
                           ErrorCode = ErrorCodes.Auth.AppleTokenInvalid, ErrorDetails = "Invalid Apple Identity token"
                       };

            if (string.IsNullOrWhiteSpace(model.Email) || model.Email == model.AppleId)
                model.Email = await repo.LookupEmailByAppleToken(model.AppleId);
        }

        if (!string.IsNullOrWhiteSpace(model.GoogleId))
        {
            if (await repo.IsGoogleIdRegistered(model.GoogleId))
                return new UserAccountRegistrationResult
                       {
                           ErrorCode = ErrorCodes.Auth.GoogleIdAlreadyUsed, ErrorDetails = "This GoogleId is already in use"
                       };

            var tokenGoogleId = await googleAuthService.ValidateAuthTokenAsync(model.IdentityToken);
            if (tokenGoogleId != model.GoogleId)
                return new UserAccountRegistrationResult
                       {
                           ErrorCode = ErrorCodes.Auth.GoogleTokenInvalid, ErrorDetails = "Invalid google identity token"
                       };
        }

        var usernameValidationResult = await ValidateUserName(model.UserName);

        return !usernameValidationResult.Ok ? usernameValidationResult : new UserAccountRegistrationResult {Ok = true};
    }

    public Task<ValidatePasswordResult> ValidatePassword(string password, string username)
    {
        if (string.IsNullOrWhiteSpace(password))
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(password));

        //TODO: make it mandatory in version 1.9
        if (!string.IsNullOrWhiteSpace(username) && StringComparer.OrdinalIgnoreCase.Equals(username, password))
            return Task.FromResult(
                new ValidatePasswordResult
                {
                    Ok = false, ErrorCode = ErrorCodes.Auth.PasswordMatchesUsername, Error = "Password must not match username"
                }
            );

        if (password.Length < 6)
            return Task.FromResult(
                new ValidatePasswordResult
                {
                    ErrorCode = ErrorCodes.Auth.PasswordMinLenght,
                    Error = "Use at least 6 characters",
                    Ok = false,
                    IsStrong = false,
                    IsLongEnough = false,
                    IsTooSimple = true
                }
            );


        if (SimplePasswords.Contains(password))
            return Task.FromResult(
                new ValidatePasswordResult
                {
                    ErrorCode = ErrorCodes.Auth.PasswordTooSimple,
                    Error = "This password is too simple",
                    Ok = false,
                    IsStrong = false,
                    IsLongEnough = true,
                    IsTooSimple = true
                }
            );

        return Task.FromResult(
            new ValidatePasswordResult
            {
                Ok = true,
                IsStrong = true,
                IsLongEnough = true,
                IsTooSimple = false
            }
        );
    }

    public async Task<UserAccountRegistrationResult> ValidateUserName(string userName)
    {
        if (await repo.IsNicknameUsed(userName))
            return new UserAccountRegistrationResult
                   {
                       ErrorCode = ErrorCodes.Auth.UserNameAlreadyUsed,
                       ErrorDetails = "This username is already in use. Please try another one",
                       RegistrationErrorDetails = new UserAccountRegistrationErrors {UsernameTaken = true}
                   };

        if (!UsernameSymbolsRegex.IsMatch(userName) || userName.Any(char.IsWhiteSpace))
            return new UserAccountRegistrationResult
                   {
                       ErrorCode = ErrorCodes.Auth.UserNameContainsInvalidSymbols,
                       ErrorDetails = "User name contains invalid symbols",
                       RegistrationErrorDetails = new UserAccountRegistrationErrors {UsernameContainsForbiddenSymbols = true}
                   };

        if (userName.Length is < 3 or > 15)
            return new UserAccountRegistrationResult
                   {
                       ErrorCode = ErrorCodes.Auth.UserNameLengthInvalid,
                       ErrorDetails = "User name should be between 3 and 15 character long",
                       RegistrationErrorDetails = new UserAccountRegistrationErrors {UsernameLengthIncorrect = true}
                   };

        var moderationResult = await moderationProviderApi.CallModerationProviderApiText(userName);
        if (!moderationResult.PassedModeration)
        {
            _logger.LogInformation(
                "Username {Username} doesn't pass moderation: {Reason} {ErrorMessage}",
                userName,
                moderationResult.Reason,
                moderationResult.ErrorMessage
            );

            return new UserAccountRegistrationResult
                   {
                       ErrorCode = ErrorCodes.Auth.UsernameModerationFailed,
                       ErrorDetails = "User name contains personal information or inappropriate words",
                       RegistrationErrorDetails = new UserAccountRegistrationErrors {UsernameModerationFailed = true}
                   };
        }

        return new UserAccountRegistrationResult {Ok = true};
    }
}