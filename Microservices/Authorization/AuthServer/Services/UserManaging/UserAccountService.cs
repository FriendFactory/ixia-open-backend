using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Threading.Tasks;
using AuthServer.Contracts;
using AuthServer.Models;
using AuthServer.Permissions.Services;
using AuthServer.Permissions.Sub13;
using AuthServer.Quickstart.Account;
using AuthServer.Repositories;
using AuthServer.Services.AppleAuth;
using AuthServer.Services.EmailAuth;
using AuthServer.Services.GoogleAuth;
using AuthServer.Services.PhoneNumberAuth;
using AuthServer.Services.UserManaging.NicknameSuggestion;
using AuthServerShared;
using Common.Infrastructure;
using Common.Infrastructure.EmailSending;
using Common.Models;
using FluentValidation;
using Frever.Shared.AssetStore.DailyTokenRefill;
using Frever.Shared.MainDb.Entities;
using IdentityModel;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace AuthServer.Services.UserManaging;

public class UserAccountService : IUserAccountService
{
    private const int MinDataCollectionAge = 13;

    private readonly IAppleAuthService _appleAuthService;
    private readonly IValidator<AppleEmailInfoRequest> _appleEmailValidator;
    private readonly ICredentialValidateService _credentialValidate;
    private readonly IDailyTokenRefillService _dailyTokenRefillService;
    private readonly IEmailAuthService _emailAuthService;
    private readonly IEmailSendingService _emailSendingService;
    private readonly IGoogleAuthService _googleAuthService;
    private readonly IJwtTokenProvider _jwtTokenProvider;
    private readonly ILogger _logger;
    private readonly IMinorUserService _minorUserService;
    private readonly INicknameSuggestionService _nicknameSuggestionService;
    private readonly OnboardingOptions _options;
    private readonly IUserPermissionService _permissionService;
    private readonly IPhoneNumberAuthService _phoneNumberAuthService;
    private readonly IUserRepository _repo;
    private readonly IValidator<TemporaryAccountRequest> _temporaryAccountValidator;
    private readonly IValidator<UpdateAccountRequest> _updateAccountValidator;
    private readonly UserManager<ApplicationUser> _userManager;

    public UserAccountService(
        IUserRepository repo,
        ILoggerFactory loggerFactory,
        UserManager<ApplicationUser> userManager,
        IPhoneNumberAuthService phoneNumberAuthService,
        IAppleAuthService appleAuthService,
        IEmailAuthService emailAuthService,
        IMinorUserService minorUserService,
        OnboardingOptions options,
        IEmailSendingService emailSendingService,
        IJwtTokenProvider jwtTokenProvider,
        IUserPermissionService permissionService,
        IGoogleAuthService googleAuthService,
        INicknameSuggestionService nicknameSuggestionService,
        ICredentialValidateService credentialValidate,
        IValidator<AppleEmailInfoRequest> appleEmailValidator,
        IValidator<TemporaryAccountRequest> temporaryAccountValidator,
        IValidator<UpdateAccountRequest> updateAccountValidator,
        IDailyTokenRefillService dailyTokenRefillService
    )
    {
        _appleAuthService = appleAuthService ?? throw new ArgumentNullException(nameof(appleAuthService));
        _emailAuthService = emailAuthService ?? throw new ArgumentNullException(nameof(emailAuthService));
        _minorUserService = minorUserService ?? throw new ArgumentNullException(nameof(minorUserService));
        _phoneNumberAuthService = phoneNumberAuthService ?? throw new ArgumentNullException(nameof(phoneNumberAuthService));
        _repo = repo ?? throw new ArgumentNullException(nameof(repo));
        _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _emailSendingService = emailSendingService ?? throw new ArgumentNullException(nameof(emailSendingService));
        _permissionService = permissionService ?? throw new ArgumentNullException(nameof(permissionService));
        _googleAuthService = googleAuthService ?? throw new ArgumentNullException(nameof(googleAuthService));
        _credentialValidate = credentialValidate ?? throw new ArgumentNullException(nameof(credentialValidate));
        _jwtTokenProvider = jwtTokenProvider ?? throw new ArgumentNullException(nameof(jwtTokenProvider));
        _nicknameSuggestionService = nicknameSuggestionService ?? throw new ArgumentNullException(nameof(nicknameSuggestionService));
        _appleEmailValidator = appleEmailValidator ?? throw new ArgumentNullException(nameof(appleEmailValidator));
        _temporaryAccountValidator = temporaryAccountValidator ?? throw new ArgumentNullException(nameof(temporaryAccountValidator));
        _updateAccountValidator = updateAccountValidator ?? throw new ArgumentNullException(nameof(updateAccountValidator));
        _dailyTokenRefillService = dailyTokenRefillService ?? throw new ArgumentNullException(nameof(dailyTokenRefillService));

        _logger = loggerFactory.CreateLogger("Frever.Auth.MainDbUserManager");
    }

    public async Task<UserAccountRegistrationResult> RegisterAccount(RegisterUserViewModel model)
    {
        using var _ = _logger.BeginScope("{Scopeid}: Registering account {UserName}", Guid.NewGuid().ToString("N"), model.UserName);

        model.Validate();

        var isMinor = await _minorUserService.IsMinorAge(model.Country, model.BirthDate);

        model.UserName = (model.UserName ?? string.Empty).Trim();

        _logger.LogInformation(
            "User {Minor}, birth date {BirthDate}, country code is {Country}",
            isMinor ? "is minor" : "not a minor",
            model.BirthDate,
            model.Country
        );

        var result = await _credentialValidate.ValidateNewAccountData(model, isMinor);
        if (!result.Ok)
            return result;

        await CreateAccount(model, false, isMinor);

        var token = await _jwtTokenProvider.GetJwtToken(
                        model.AppleId,
                        model.AppleIdentityToken,
                        model.Email,
                        model.PhoneNumber,
                        model.UserName,
                        model.Password,
                        model.GoogleId,
                        model.IdentityToken
                    );

        return new UserAccountRegistrationResult {Ok = true, Jwt = token};
    }

    public async Task UpdateAccount(long groupId, UpdateAccountRequest request)
    {
        using var _ = _logger.BeginScope("{Scopeid}: Update account {GroupId}", Guid.NewGuid().ToString("N"), groupId);

        await _updateAccountValidator.ValidateAndThrowAsync(request);

        var user = await _repo.GetUserByGroupId(groupId)
                              .Include(e => e.MainGroup)
                              .FirstOrDefaultAsync(u => u.MainGroup.DeletedAt == null && !u.MainGroup.IsBlocked);
        if (user is null)
            throw AppErrorWithStatusCodeException.NotFound("Account is not found or not accessible", ErrorCodes.Auth.UserNotFound);

        user.MainGroup.BirthDate = request.BirthDate;

        var isMinor = await _minorUserService.IsMinorAge(request.Country, request.BirthDate);

        await _repo.UpdateUser(user, request.Country, request.DefaultLanguage, isMinor);
    }

    //TODO: remove input model in 1.9 version if not required
    public async Task<string> RegisterTemporaryAccount(TemporaryAccountRequest request)
    {
        using var _ = _logger.BeginScope("{Scopeid}: Register temporary account", Guid.NewGuid().ToString("N"));

        if (string.IsNullOrWhiteSpace(request.Country))
            request.Country = Constants.FallbackCountryCode;
        if (string.IsNullOrWhiteSpace(request.DefaultLanguage))
            request.DefaultLanguage = Constants.FallbackLanguageCode;

        await _temporaryAccountValidator.ValidateAndThrowAsync(request);

        var userName = await _nicknameSuggestionService.SuggestNickname(string.Empty, 1);
        if (userName is null || userName.Length == 0)
            throw AppErrorWithStatusCodeException.BadRequest("No suggested nickname", ErrorCodes.Auth.UserNameEmpty);

        var model = new RegisterUserViewModel
                    {
                        Country = request.Country,
                        DefaultLanguage = request.DefaultLanguage,
                        UserName = userName.FirstOrDefault()?.ToLowerInvariant(),
                        Password = Guid.NewGuid().ToString(),
                        BirthDate = DateTime.UtcNow
                    };

        await CreateAccount(model, true, false);

        _logger.LogInformation("Temporary account registered, username={UserName}, country={Country}", model.UserName, model.Country);

        var token = await _jwtTokenProvider.GetJwtToken(userName: model.UserName, password: model.Password);

        var authDbUser = await _userManager.FindByNameAsync(model.UserName);
        if (authDbUser != null)
            await _userManager.RemovePasswordAsync(authDbUser);

        return token;
    }

    public async Task<string> LoginWithApple(LoginWithAppleRequest model)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(model.AppleId);
        ArgumentException.ThrowIfNullOrWhiteSpace(model.AppleIdentityToken);

        var user = await _repo.GetUserByAppleId(model.AppleId);
        if (user is null)
            throw AppErrorWithStatusCodeException.NotFound("User not found", ErrorCodes.Auth.UserNotFound);

        var appleId = await _appleAuthService.ValidateAuthTokenAsync(model.AppleIdentityToken);
        if (appleId is null || appleId != user.AppleId)
            throw AppErrorWithStatusCodeException.BadRequest("Invalid appleIdentity token", ErrorCodes.Auth.AppleTokenInvalid);

        return await _jwtTokenProvider.GetJwtToken(model.AppleId, model.AppleIdentityToken);
    }

    public async Task<string> LoginWithGoogle(LoginWithGoogleRequest model)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(model.GoogleId);
        ArgumentException.ThrowIfNullOrWhiteSpace(model.IdentityToken);

        var user = await _repo.GetUserByGoogleId(model.GoogleId);
        if (user is null)
            throw AppErrorWithStatusCodeException.NotFound("User not found", ErrorCodes.Auth.UserNotFound);

        var googleId = await _googleAuthService.ValidateAuthTokenAsync(model.IdentityToken);
        if (googleId is null || googleId != user.GoogleId)
            throw AppErrorWithStatusCodeException.BadRequest("Invalid google identity token", ErrorCodes.Auth.GoogleTokenInvalid);

        return await _jwtTokenProvider.GetJwtToken(googleId: model.GoogleId, googleIdentityToken: model.IdentityToken);
    }

    public async Task<AuthenticationInfoStatus> CheckLoginInfo(AuthenticationInfo request)
    {
        if (!string.IsNullOrWhiteSpace(request.PhoneNumber) && !await FormatAndValidatePhoneNumber(request))
            return new AuthenticationInfoStatus("Incorrect phone number format", ErrorCodes.Auth.PhoneNumberFormatInvalid);

        var isUserRegistered = await GetRegisteredUser(request).AnyAsync(e => !e.MainGroup.IsBlocked && e.MainGroup.DeletedAt == null);

        return isUserRegistered
                   ? AuthenticationInfoStatus.Valid()
                   : new AuthenticationInfoStatus("Such account does not exist", ErrorCodes.Auth.AccountNotExist);
    }

    public async Task<AuthenticationInfoStatus> CheckRegistrationInfo(AuthenticationInfo request)
    {
        if (!string.IsNullOrWhiteSpace(request.PhoneNumber) && !await FormatAndValidatePhoneNumber(request))
            return new AuthenticationInfoStatus("Incorrect phone number format", ErrorCodes.Auth.PhoneNumberFormatInvalid);

        if (string.IsNullOrWhiteSpace(request.UserName))
        {
            if (await GetRegisteredUser(request).AnyAsync())
                return new AuthenticationInfoStatus("Such account has been already registered", ErrorCodes.Auth.AccountAlreadyExist)
                       {
                           UserRegistrationErrors = new UserAccountRegistrationErrors {UsernameTaken = true}
                       };

            return AuthenticationInfoStatus.Valid();
        }

        if (await _repo.GetAuthDbUsersByUsername(request.UserName).AnyAsync() || await _repo.IsNicknameUsed(request.UserName))
            return new AuthenticationInfoStatus("Such account has been already registered", ErrorCodes.Auth.AccountAlreadyExist)
                   {
                       UserRegistrationErrors = new UserAccountRegistrationErrors {UsernameTaken = true}
                   };

        var validationResult = await _credentialValidate.ValidateUserName(request.UserName);
        if (!validationResult.Ok)
            return new AuthenticationInfoStatus(validationResult.ErrorDetails, validationResult.ErrorCode)
                   {
                       UserRegistrationErrors = validationResult.RegistrationErrorDetails
                   };

        return AuthenticationInfoStatus.Valid();
    }

    public async Task<bool> IsLoginByEmailAvailable(string userName)
    {
        var group = await _repo.GetGroupByName(userName).FirstOrDefaultAsync();
        if (group == null)
            return false;

        var user = await _repo.GetUserByGroupId(group.Id).FirstOrDefaultAsync();
        if (user == null)
            return false;

        if (string.IsNullOrWhiteSpace(user.Email))
            return false;

        return !group.IsMinor || group.IsParentalConsentValidated;
    }

    public async Task ConfigureParentalConsent(long groupId, ParentalConsent consent)
    {
        using var scope = _logger.BeginScope("ConfigureParentalConsent(groupId={}): ", groupId);

        _logger.LogInformation("Consent: {}", JsonConvert.SerializeObject(consent));

        var group = await _repo.GetGroupById(groupId).FirstOrDefaultAsync();
        var user = await _repo.GetUserByGroupId(groupId).FirstOrDefaultAsync();

        if (group == null || user == null)
            throw AppErrorWithStatusCodeException.NotFound("Group is not found", ErrorCodes.Auth.GroupNotFound);

        if (!group.IsMinor)
            throw AppErrorWithStatusCodeException.BadRequest("Group is not minor", ErrorCodes.Auth.GroupNotMinor);
        if (!group.IsParentalConsentValidated)
            throw AppErrorWithStatusCodeException.BadRequest("Parent age is not confirmed", "ParentAgeNotConfirmed");

        group.ParentalConsent = consent;

        await _repo.SaveChanges();

        _logger.LogInformation("Parental consent updated");
    }

    public async Task SendVerificationCodeToParentEmail(long groupId)
    {
        var group = await _repo.GetGroupById(groupId).FirstOrDefaultAsync();
        if (group == null || !group.IsMinor || !group.IsParentalConsentValidated)
            throw AppErrorWithStatusCodeException.BadRequest("Group is not minor or don't have parent email bound", "InvalidGroup");

        var user = await _repo.GetUserByGroupId(groupId).FirstOrDefaultAsync();
        if (user == null || string.IsNullOrWhiteSpace(user.Email))
            throw AppErrorWithStatusCodeException.BadRequest(
                "User is not found or don't have email assigned",
                ErrorCodes.Auth.UserWithEmailNotFound
            );

        await _emailAuthService.SendParentEmailVerification(new VerifyEmailRequest {Email = user.Email});

        _logger.LogInformation("GroupId={}: Sent parent email verification code", groupId);
    }

    public async Task<bool> VerifyParentEmailCode(long groupId, string code)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(code));

        var user = await _repo.GetUserByGroupId(groupId).FirstOrDefaultAsync();
        if (user == null || string.IsNullOrWhiteSpace(user.Email))
            throw AppErrorWithStatusCodeException.BadRequest(
                "User is not found or don't have email assigned",
                ErrorCodes.Auth.UserWithEmailNotFound
            );

        var isValid = await _emailAuthService.ValidateParentEmailCode(user.Email, code);

        _logger.LogInformation("Validating parent email code for group: {}, isCodeValid={}", groupId, isValid);

        return isValid;
    }

    public async Task<IList<Claim>> GetClaimsByIdAsync(string userId)
    {
        var claimData = await _repo.GetClaimsDataAsync(userId);

        var readinessScopes = await _permissionService.GetUserReadinessAccessScopes(claimData.PrimaryGroupId);

        var result = GetClaims(claimData, readinessScopes);

        return result;
    }

    public Task StoreEmailForAppleId(AppleEmailInfoRequest request)
    {
        _appleEmailValidator.ValidateAndThrow(request);

        request.Email = request.Email.ToLower();

        return _repo.StoreEmailForAppleId(request.AppleId, request.Email);
    }

    public async Task AssignParentEmail(long groupId, string parentEmail)
    {
        if (string.IsNullOrWhiteSpace(parentEmail))
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(parentEmail));

        using var scope = _logger.BeginScope("AssignParentEmail(groupId={}, email={}): ", groupId, parentEmail);

        var user = await _repo.GetUserByGroupId(groupId).SingleOrDefaultAsync();
        if (user == null)
            throw AppErrorWithStatusCodeException.BadRequest("User is not found", ErrorCodes.Auth.UserNotFound);

        var identityServerUserId = user.IdentityServerId.ToString();
        var authUser = await _userManager.Users.SingleOrDefaultAsync(u => u.Id == identityServerUserId);
        if (authUser == null)
            throw AppErrorWithStatusCodeException.BadRequest("Auth User is not found", ErrorCodes.Auth.UserNotFound);
        var changeEmailToken = await _userManager.GenerateChangeEmailTokenAsync(authUser, parentEmail);
        await _userManager.ChangeEmailAsync(authUser, parentEmail, changeEmailToken);

        await using var transaction = await _repo.BeginMainDbTransactionAsync();

        var isNonUnique = await _repo.IsParentEmailNonUnique(groupId, parentEmail);
        if (isNonUnique)
            throw AppErrorWithStatusCodeException.BadRequest("Parent email already used", ErrorCodes.Auth.EmailAlreadyUsed);

        var groupCountry = await _repo.GetGroupById(groupId).Select(g => new {g.TaxationCountryId}).FirstOrDefaultAsync();
        if (groupCountry == null || groupCountry.TaxationCountryId == null)
            throw AppErrorWithStatusCodeException.NotFound("Group is not found", ErrorCodes.Auth.GroupNotFound);

        var needsExtendedAgeVerification = await _minorUserService.NeedsExtendedEmailVerification(groupCountry.TaxationCountryId.Value);

        await _repo.SetParentEmail(groupId, parentEmail, !needsExtendedAgeVerification);

        await transaction.CommitAsync();

        _logger.LogInformation("Parent email set. Needs extended age verification={}", needsExtendedAgeVerification);

        var country = await _repo.GetCountries().FirstOrDefaultAsync(c => c.Id == groupCountry.TaxationCountryId);
        if (country != null)
        {
            _logger.LogInformation(
                "Country {}, needs extended parent age verification: {}",
                country.ISOName,
                country.ExtendedParentAgeValidation
            );

            if (country.StrictCoppaRules && country.ExtendedParentAgeValidation)
            {
                await _emailSendingService.SendEmail(
                    new SendEmailParams
                    {
                        To = [parentEmail],
                        Subject = "Frever email added",
                        Body = "Your email has been added as parent email to Frever account"
                    }
                );

                _logger.LogInformation("Parental notice sent to {}", parentEmail);
            }
            else
            {
                _logger.LogInformation("Sending parental notice is not required");
            }
        }
    }

    public async Task RemoveParentEmail(long groupId)
    {
        await _repo.SetParentEmail(groupId, null, false);
        _logger.LogInformation("Parent email removed from account groupId={}", groupId);
    }

    private async Task<bool> FormatAndValidatePhoneNumber(AuthenticationInfo request)
    {
        if (string.IsNullOrWhiteSpace(request.PhoneNumber))
            return true;

        request.PhoneNumber = await _phoneNumberAuthService.FormatPhoneNumber(request.PhoneNumber);

        return request.PhoneNumber != null;
    }

    private IQueryable<User> GetRegisteredUser(AuthenticationInfo request)
    {
        var user = _repo.AllUsers();

        if (!string.IsNullOrWhiteSpace(request.Email))
            return user.Where(e => e.Email.ToLower() == request.Email.ToLower());

        if (!string.IsNullOrWhiteSpace(request.UserName))
            return user.Where(e => e.MainGroup.NickName.ToLower() == request.UserName.ToLower());

        if (!string.IsNullOrWhiteSpace(request.PhoneNumber))
            return user.Where(e => e.PhoneNumber == request.PhoneNumber);

        throw new InvalidOperationException("Authentication info is invalid");
    }

    private async Task CreateAccount(RegisterUserViewModel model, bool isTempAccount, bool isMinor)
    {
        IList<Claim> userMainDbClaims;

        using var scope = _logger.BeginScope(
            "Start account creation: username={} email={}, phone={}, appleId={}, googleId={}, country={}, language={}",
            model.UserName,
            model.Email,
            model.PhoneNumber,
            model.AppleId,
            model.GoogleId,
            model.Country,
            model.DefaultLanguage
        );

        model.Email = model.Email?.ToLowerInvariant();
        model.UserName = model.UserName?.ToLowerInvariant();

        var identityServer4User = new ApplicationUser
                                  {
                                      UserName = model.UserName,
                                      Email = model.Email,
                                      PhoneNumber = model.PhoneNumber,
                                      PhoneNumberConfirmed = !string.IsNullOrWhiteSpace(model.PhoneNumber),
                                      EmailConfirmed = !string.IsNullOrWhiteSpace(model.Email)
                                  };

        await using var authDbTransaction = await _repo.BeginAuthDbTransactionAsync();
        await using var mainDbTransaction = await _repo.BeginMainDbTransactionAsync();

        try
        {
            var createUserResult = string.IsNullOrWhiteSpace(model.Password)
                                       ? await _userManager.CreateAsync(identityServer4User)
                                       : await _userManager.CreateAsync(identityServer4User, model.Password);

            if (!createUserResult.Succeeded)
            {
                _logger.LogError(
                    "Failed create user UserName:{UserName} Email:{Email} PhoneNumber:{PhoneNumber} :{ErrorMessages}",
                    identityServer4User.UserName,
                    identityServer4User.Email,
                    identityServer4User.PhoneNumber,
                    string.Join(";", createUserResult.Errors.Select(x => x.Description))
                );

                throw new AppErrorWithStatusCodeException("Failed to create user", HttpStatusCode.InternalServerError);
            }

            var mappedUserModel = new UserCreateModel
                                  {
                                      IdentityServerId = Guid.Parse(identityServer4User.Id),
                                      Email = model.Email,
                                      PhoneNumber = model.PhoneNumber,
                                      AppleId = model.AppleId,
                                      GoogleId = model.GoogleId,
                                      NickName = model.UserName,
                                      BirthDate = model.BirthDate == default ? null : model.BirthDate,
                                      AnalyticsEnabled = model.AnalyticsEnabled,
                                      Country = model.Country,
                                      DefaultLanguage = model.DefaultLanguage,
                                      AllowDataCollection = await IsAllowedDataCollection(model),
                                      IsTemporary = isTempAccount,
                                      IsMinor = isMinor
                                  };

            await _repo.CreateUserAsync(mappedUserModel);

            var claims = new List<Claim> {new(JwtClaimTypes.Name, identityServer4User.UserName)};

            if (!string.IsNullOrWhiteSpace(identityServer4User.Email))
                claims.Add(new Claim(JwtClaimTypes.Email, identityServer4User.Email));

            if (!string.IsNullOrWhiteSpace(identityServer4User.PhoneNumber))
                claims.Add(new Claim(JwtClaimTypes.PhoneNumber, identityServer4User.PhoneNumber));

            userMainDbClaims = await GetClaimsByIdAsync(identityServer4User.Id);

            claims.AddRange(userMainDbClaims);

            var result = await _userManager.AddClaimsAsync(identityServer4User, claims);
            if (!result.Succeeded)
                throw new AppErrorWithStatusCodeException(
                    "Failed to add claims. " + string.Join(";", result.Errors.Select(x => x.Description)),
                    HttpStatusCode.InternalServerError
                );

            await mainDbTransaction.CommitAsync();
            await authDbTransaction.CommitAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create account");
            throw new AppErrorWithStatusCodeException("Failed to create account", HttpStatusCode.InternalServerError);
        }

        var group = userMainDbClaims.First(e => e.Type == Claims.PrimaryGroupId);
        var groupId = long.Parse(group.Value);

        await _repo.AddInitialFriend(groupId, _options.FreverOfficialEmail);
        await _dailyTokenRefillService.RefillDailyTokens(groupId);

        var oldPassword = model.Password;
        model.Password = null;
        _logger.LogInformation("Account created, group ID={}, registration info {}", groupId, JsonConvert.SerializeObject(model));
        model.Password = oldPassword;
    }

    private async Task UpdateAccount(RegisterUserViewModel model, Guid identityServerId, bool isMinor)
    {
        var authDbUser = await _userManager.FindByIdAsync(identityServerId.ToString());
        if (authDbUser is null)
            throw AppErrorWithStatusCodeException.NotFound("Account is not found or not accessible", ErrorCodes.Auth.UserNotFound);

        model.Email = model.Email?.ToLower();

        await _userManager.RemoveClaimAsync(authDbUser, new Claim(JwtClaimTypes.Name, authDbUser.UserName!));
        await _userManager.SetUserNameAsync(authDbUser, model.UserName);
        await _userManager.SetEmailAsync(authDbUser, model.Email);
        await _userManager.SetPhoneNumberAsync(authDbUser, model.PhoneNumber);

        if (!string.IsNullOrWhiteSpace(model.Password))
        {
            var token = await _userManager.GeneratePasswordResetTokenAsync(authDbUser);
            await _userManager.ResetPasswordAsync(authDbUser, token, model.Password);
        }
        else
        {
            await _userManager.RemovePasswordAsync(authDbUser);
        }

        var claims = new List<Claim> {new(JwtClaimTypes.Name, authDbUser.UserName)};
        if (!string.IsNullOrWhiteSpace(authDbUser.Email))
            claims.Add(new Claim(JwtClaimTypes.Email, authDbUser.Email));
        if (!string.IsNullOrWhiteSpace(authDbUser.PhoneNumber))
            claims.Add(new Claim(JwtClaimTypes.PhoneNumber, authDbUser.PhoneNumber));

        await _userManager.AddClaimsAsync(authDbUser, claims);

        await _repo.UpdateUser(
            new UserUpdateModel(
                identityServerId,
                model.UserName,
                model.BirthDate,
                model.AppleId,
                model.GoogleId,
                model.Email,
                model.PhoneNumber,
                model.DefaultLanguage,
                model.Country,
                isMinor,
                !string.IsNullOrWhiteSpace(model.Password)
            )
        );
    }

    private async Task<bool> IsAllowedDataCollection(RegisterUserViewModel model)
    {
        var location = model.Country;
        var locationCountry = await _repo.GetCountries().Where(c => c.ISOName == location || c.ISO2Code == location).FirstOrDefaultAsync();

        var minDataCollectionAge = locationCountry?.AgeOfConsent ?? MinDataCollectionAge;
        var age = (DateTime.UtcNow - model.BirthDate).Days / 365;

        if (age < minDataCollectionAge)
            return false;

        return model.AllowDataCollection ?? true;
    }

    private static List<Claim> GetClaims(MainDbClaimData data, IEnumerable<string> readinessScopes)
    {
        var result = new List<Claim>
                     {
                         new(Claims.UserId, data.UserId.ToString()), new(Claims.PrimaryGroupId, data.PrimaryGroupId.ToString())
                     };

        if (data.MainCharacterId.HasValue)
            result.Add(new Claim(Claims.MainCharacterId, data.MainCharacterId.ToString()));

        result.AddRange(from cpl in data.CreatorPermissionLevels ?? [] select new Claim(Claims.CreatorPermissionLevels, cpl.ToString()));
        result.AddRange(from s in readinessScopes select new Claim(Claims.AccessScopes, s));

        if (data.IsQA)
            result.Add(new Claim(Claims.IsQA, data.IsQA.ToString()));
        if (data.IsModerator)
            result.Add(new Claim(Claims.IsModerator, data.IsModerator.ToString()));
        if (data.IsEmployee)
            result.Add(new Claim(Claims.IsEmployee, data.IsEmployee.ToString()));
        if (data.IsFeatured)
            result.Add(new Claim(Claims.IsFeatured, data.IsFeatured.ToString()));
        if (data.RegisteredWithAppleId)
            result.Add(new Claim(Claims.RegisteredWithAppleId, data.RegisteredWithAppleId.ToString()));
        if (data.IsStarCreator)
            result.Add(new Claim(Claims.IsStarCreator, data.IsStarCreator.ToString()));
        if (data.IsOnboardingCompleted)
            result.Add(new Claim(Claims.IsOnboardingCompleted, data.IsOnboardingCompleted.ToString()));

        return result;
    }
}