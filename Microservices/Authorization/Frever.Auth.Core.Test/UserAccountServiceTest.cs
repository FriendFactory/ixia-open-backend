using AuthServer.Contracts;
using AuthServer.Models;
using AuthServer.Permissions.Services;
using AuthServer.Permissions.Sub13;
using AuthServer.Repositories;
using AuthServer.Services.AppleAuth;
using AuthServer.Services.EmailAuth;
using AuthServer.Services.GoogleAuth;
using AuthServer.Services.PhoneNumberAuth;
using AuthServer.Services.UserManaging;
using AuthServer.Services.UserManaging.NicknameSuggestion;
using Common.Infrastructure;
using Common.Infrastructure.EmailSending;
using Common.Infrastructure.ModerationProvider;
using Common.Models;
using FluentAssertions;
using Frever.Auth.Core.Test.Utils;
using Frever.Shared.AssetStore.DailyTokenRefill;
using Frever.Shared.MainDb.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MockQueryable.Moq;
using Moq;
using Xunit;
using Xunit.Abstractions;
using RegisterUserViewModel = AuthServer.Quickstart.Account.RegisterUserViewModel;

namespace Frever.Auth.Core.Test;

[Collection("User Account Service")]
public class UserAccountServiceTest(ITestOutputHelper testOut)
{
    [Fact(DisplayName = "👍👎Check register account")]
    public async Task RegisterAccount()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddTestAuthServices(testOut);

        await using var provider = services.BuildServiceProvider();

        var model = new RegisterUserViewModel
                    {
                        UserName = "testusername",
                        Password = "testpassword",
                        BirthDate = DateTime.UtcNow,
                        Country = "swe",
                        DefaultLanguage = "swe"
                    };

        var testInstance = CreateTestService(provider);

        // Act
        var result = await testInstance.RegisterAccount(model);

        // Assert
        result.Ok.Should().BeTrue();
    }

    [Fact(DisplayName = "👍👍Check register account: validation failed")]
    public async Task RegisterAccount_ValidationFailed()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddTestAuthServices(testOut);

        await using var provider = services.BuildServiceProvider();

        var model = new RegisterUserViewModel
                    {
                        Email = "test@email.com",
                        BirthDate = DateTime.UtcNow,
                        Country = "swe",
                        DefaultLanguage = "swe"
                    };
        var validationResult = new UserAccountRegistrationResult {ErrorCode = ErrorCodes.Auth.UserNameAlreadyUsed};

        var credentialValidateService = new Mock<ICredentialValidateService>();
        credentialValidateService.Setup(s => s.ValidateNewAccountData(It.IsAny<RegisterUserViewModel>(), false))
                                 .ReturnsAsync(validationResult);

        var testInstance = CreateTestService(provider, credentialValidateService: credentialValidateService);

        // Act
        var result = await testInstance.RegisterAccount(model);

        // Assert
        result.Should().Be(validationResult);
    }

    [Fact(DisplayName = "👍👎Check register temporary account")]
    public async Task RegisterTemporaryAccount()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddTestAuthServices(testOut);

        await using var provider = services.BuildServiceProvider();

        const string token = "test_token";

        var testInstance = CreateTestService(provider, token: token);

        // Act
        var result = await testInstance.RegisterTemporaryAccount(new TemporaryAccountRequest());

        // Assert
        result.Should().Be(token);
    }

    [Fact(DisplayName = "👍👎Check login with apple")]
    public async Task LoginWithApple()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddTestAuthServices(testOut);

        await using var provider = services.BuildServiceProvider();

        const string token = "test_token";
        var user = new User {AppleId = "test_apple_id"};
        var model = new LoginWithAppleRequest {AppleId = user.AppleId, AppleIdentityToken = "test_apple_token"};

        var userRepo = new Mock<IUserRepository>(MockBehavior.Strict);
        userRepo.Setup(s => s.GetUserByAppleId(It.IsAny<string>())).ReturnsAsync(user);

        var appleAuthService = new Mock<IAppleAuthService>(MockBehavior.Strict);
        appleAuthService.Setup(s => s.ValidateAuthTokenAsync(It.IsAny<string>())).ReturnsAsync(model.AppleId);

        var testInstance = CreateTestService(provider, userRepo, appleAuthService: appleAuthService, token: token);

        // Act
        var result = await testInstance.LoginWithApple(model);

        // Assert
        result.Should().Be(token);
    }

    [Fact(DisplayName = "👍👍Check login with apple: user not found")]
    public async Task LoginWithApple_UserNotFound()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddTestAuthServices(testOut);

        await using var provider = services.BuildServiceProvider();

        var model = new LoginWithAppleRequest {AppleId = "test_apple_id", AppleIdentityToken = "test_apple_token"};

        var userRepo = new Mock<IUserRepository>(MockBehavior.Strict);
        userRepo.Setup(s => s.GetUserByAppleId(It.IsAny<string>())).Returns(Task.FromResult<User>(null));

        var testInstance = CreateTestService(provider, userRepo);

        // Act
        var act = () => testInstance.LoginWithApple(model);

        // Assert
        await act.Should().ThrowAsync<AppErrorWithStatusCodeException>().Where(e => e.ErrorCode.Equals(ErrorCodes.Auth.UserNotFound));
    }

    [Fact(DisplayName = "👍👍Check login with apple: identity token invalid")]
    public async Task LoginWithApple_IdentityTokenInvalid()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddTestAuthServices(testOut);

        await using var provider = services.BuildServiceProvider();

        var model = new LoginWithAppleRequest {AppleId = "test_apple_id", AppleIdentityToken = "test_apple_token"};

        var userRepo = new Mock<IUserRepository>(MockBehavior.Strict);
        userRepo.Setup(s => s.GetUserByAppleId(It.IsAny<string>())).ReturnsAsync(new User());

        var appleAuthService = new Mock<IAppleAuthService>(MockBehavior.Strict);
        appleAuthService.Setup(s => s.ValidateAuthTokenAsync(It.IsAny<string>())).ReturnsAsync(model.AppleId);

        var testInstance = CreateTestService(provider, userRepo, appleAuthService: appleAuthService);

        // Act
        var act = () => testInstance.LoginWithApple(model);

        // Assert
        await act.Should().ThrowAsync<AppErrorWithStatusCodeException>().Where(e => e.ErrorCode.Equals(ErrorCodes.Auth.AppleTokenInvalid));
    }

    [Fact(DisplayName = "👍👎Check login with google")]
    public async Task LoginWithGoogle()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddTestAuthServices(testOut);

        await using var provider = services.BuildServiceProvider();

        const string token = "test_token";
        var user = new User {GoogleId = "test_google_id"};
        var model = new LoginWithGoogleRequest {GoogleId = user.GoogleId, IdentityToken = "test_google_token"};

        var userRepo = new Mock<IUserRepository>(MockBehavior.Strict);
        userRepo.Setup(s => s.GetUserByGoogleId(It.IsAny<string>())).ReturnsAsync(user);

        var googleAuthService = new Mock<IGoogleAuthService>(MockBehavior.Strict);
        googleAuthService.Setup(s => s.ValidateAuthTokenAsync(It.IsAny<string>())).ReturnsAsync(model.GoogleId);

        var testInstance = CreateTestService(provider, userRepo, googleAuthService: googleAuthService, token: token);

        // Act
        var result = await testInstance.LoginWithGoogle(model);

        // Assert
        result.Should().Be(token);
    }

    [Fact(DisplayName = "👍👍Check login with google: user not found")]
    public async Task LoginWithGoogle_UserNotFound()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddTestAuthServices(testOut);

        await using var provider = services.BuildServiceProvider();

        var model = new LoginWithGoogleRequest {GoogleId = "test_google_id", IdentityToken = "test_google_token"};

        var userRepo = new Mock<IUserRepository>(MockBehavior.Strict);
        userRepo.Setup(s => s.GetUserByGoogleId(It.IsAny<string>())).Returns(Task.FromResult<User>(null));

        var testInstance = CreateTestService(provider, userRepo);

        // Act
        var act = () => testInstance.LoginWithGoogle(model);

        // Assert
        await act.Should().ThrowAsync<AppErrorWithStatusCodeException>().Where(e => e.ErrorCode.Equals(ErrorCodes.Auth.UserNotFound));
    }

    [Fact(DisplayName = "👍👍Check login with google: identity token invalid")]
    public async Task LoginWithGoogle_IdentityTokenInvalid()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddTestAuthServices(testOut);

        await using var provider = services.BuildServiceProvider();

        var model = new LoginWithGoogleRequest {GoogleId = "test_google_id", IdentityToken = "test_google_token"};

        var userRepo = new Mock<IUserRepository>(MockBehavior.Strict);
        userRepo.Setup(s => s.GetUserByGoogleId(It.IsAny<string>())).ReturnsAsync(new User());

        var googleAuthService = new Mock<IGoogleAuthService>(MockBehavior.Strict);
        googleAuthService.Setup(s => s.ValidateAuthTokenAsync(It.IsAny<string>())).ReturnsAsync(model.GoogleId);

        var testInstance = CreateTestService(provider, userRepo, googleAuthService: googleAuthService);

        // Act
        var act = () => testInstance.LoginWithGoogle(model);

        // Assert
        await act.Should().ThrowAsync<AppErrorWithStatusCodeException>().Where(e => e.ErrorCode.Equals(ErrorCodes.Auth.GoogleTokenInvalid));
    }

    [Theory(DisplayName = "👍👎Check login info")]
    [InlineData("test@email.com", null, null)]
    [InlineData(null, "+3123455555", null)]
    [InlineData(null, null, "test")]
    public async Task CheckLoginInfo(string email, string phoneNumber, string username)
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddTestAuthServices(testOut);

        await using var provider = services.BuildServiceProvider();

        var user = new User {Email = email, PhoneNumber = phoneNumber, MainGroup = new Group {NickName = username}};
        var model = new AuthenticationInfo {Email = email, PhoneNumber = phoneNumber, UserName = username};

        var userRepo = new Mock<IUserRepository>(MockBehavior.Strict);
        userRepo.Setup(s => s.AllUsers()).Returns(new List<User> {user}.BuildMock());

        var testInstance = CreateTestService(provider, userRepo);

        // Act
        var result = await testInstance.CheckLoginInfo(model);

        // Assert
        result.IsValid.Should().BeTrue();
        result.ErrorCode.Should().BeNull();
    }

    [Fact(DisplayName = "👍👍Check login info: phone number format invalid")]
    public async Task CheckLoginInfo_PhoneNumberFormatInvalid()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddTestAuthServices(testOut);

        await using var provider = services.BuildServiceProvider();

        var model = new AuthenticationInfo {PhoneNumber = "+3123455555"};

        var phoneNumberAuthService = new Mock<IPhoneNumberAuthService>(MockBehavior.Strict);
        phoneNumberAuthService.Setup(s => s.FormatPhoneNumber(It.IsAny<string>())).Returns(Task.FromResult<string>(null));

        var testInstance = CreateTestService(provider, phoneNumberAuthService: phoneNumberAuthService);

        // Act
        var result = await testInstance.CheckLoginInfo(model);

        // Assert
        result.IsValid.Should().BeFalse();
        result.ErrorCode.Should().Be(ErrorCodes.Auth.PhoneNumberFormatInvalid);
    }

    [Fact(DisplayName = "👍👍Check login info: account not exist")]
    public async Task CheckLoginInfo_AccountNotExist()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddTestAuthServices(testOut);

        await using var provider = services.BuildServiceProvider();

        var model = new AuthenticationInfo {PhoneNumber = "+3123455555"};

        var userRepo = new Mock<IUserRepository>(MockBehavior.Strict);
        userRepo.Setup(s => s.AllUsers()).Returns(Enumerable.Empty<User>().BuildMock());

        var testInstance = CreateTestService(provider, userRepo);

        // Act
        var result = await testInstance.CheckLoginInfo(model);

        // Assert
        result.IsValid.Should().BeFalse();
        result.ErrorCode.Should().Be(ErrorCodes.Auth.AccountNotExist);
    }

    [Theory(DisplayName = "👍👎Check registration info")]
    [InlineData("test@email.com", null, null)]
    [InlineData(null, "+3123455555", null)]
    [InlineData(null, null, "test")]
    public async Task CheckRegistrationInfo(string email, string phoneNumber, string username)
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddTestAuthServices(testOut);

        await using var provider = services.BuildServiceProvider();

        var model = new AuthenticationInfo {Email = email, PhoneNumber = phoneNumber, UserName = username};

        var userRepo = new Mock<IUserRepository>(MockBehavior.Strict);
        userRepo.Setup(s => s.AllUsers()).Returns(Enumerable.Empty<User>().BuildMock());
        userRepo.Setup(s => s.IsNicknameUsed(It.IsAny<string>())).ReturnsAsync(false);
        userRepo.Setup(s => s.GetAuthDbUsersByUsername(It.IsAny<string>())).Returns(Enumerable.Empty<ApplicationUser>().BuildMock());

        var testInstance = CreateTestService(provider, userRepo);

        // Act
        var result = await testInstance.CheckRegistrationInfo(model);

        // Assert
        result.IsValid.Should().BeTrue();
        result.ErrorCode.Should().BeNull();
    }

    [Fact(DisplayName = "👍👍Check registration info: phone number format invalid")]
    public async Task CheckRegistrationInfo_PhoneNumberFormatInvalid()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddTestAuthServices(testOut);

        await using var provider = services.BuildServiceProvider();

        var model = new AuthenticationInfo {PhoneNumber = "+3123455555"};

        var phoneNumberAuthService = new Mock<IPhoneNumberAuthService>(MockBehavior.Strict);
        phoneNumberAuthService.Setup(s => s.FormatPhoneNumber(It.IsAny<string>())).Returns(Task.FromResult<string>(null));

        var testInstance = CreateTestService(provider, phoneNumberAuthService: phoneNumberAuthService);

        // Act
        var result = await testInstance.CheckRegistrationInfo(model);

        // Assert
        result.IsValid.Should().BeFalse();
        result.ErrorCode.Should().Be(ErrorCodes.Auth.PhoneNumberFormatInvalid);
    }

    [Theory(DisplayName = "👍👍Check login info: account already exist")]
    [InlineData("test@email.com", null, null)]
    [InlineData(null, "+3123455555", null)]
    public async Task CheckLoginInfo_AccountAlreadyExist(string email, string phoneNumber, string username)
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddTestAuthServices(testOut);

        await using var provider = services.BuildServiceProvider();

        var user = new User {Email = email, PhoneNumber = phoneNumber, MainGroup = new Group {NickName = username}};
        var model = new AuthenticationInfo {Email = email, PhoneNumber = phoneNumber, UserName = username};

        var userRepo = new Mock<IUserRepository>(MockBehavior.Strict);
        userRepo.Setup(s => s.AllUsers()).Returns(new List<User> {user}.BuildMock());

        var testInstance = CreateTestService(provider, userRepo);

        // Act
        var result = await testInstance.CheckRegistrationInfo(model);

        // Assert
        result.IsValid.Should().BeFalse();
        result.ErrorCode.Should().Be(ErrorCodes.Auth.AccountAlreadyExist);
        result.UserRegistrationErrors.Should().NotBeNull();
        result.UserRegistrationErrors.UsernameTaken.Should().BeTrue();
        result.UserRegistrationErrors.UsernameLengthIncorrect.Should().BeFalse();
        result.UserRegistrationErrors.UsernameModerationFailed.Should().BeFalse();
        result.UserRegistrationErrors.UsernameContainsForbiddenSymbols.Should().BeFalse();
    }

    [Fact(DisplayName = "👍👍Check login info: account already exist: for username")]
    public async Task CheckLoginInfo_AccountAlreadyExist_ForUserName()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddTestAuthServices(testOut);

        await using var provider = services.BuildServiceProvider();

        var model = new AuthenticationInfo {UserName = "test"};

        var userRepo = new Mock<IUserRepository>(MockBehavior.Strict);
        userRepo.Setup(s => s.GetAuthDbUsersByUsername(It.IsAny<string>())).Returns(new List<ApplicationUser> {new()}.BuildMock());
        userRepo.Setup(s => s.IsNicknameUsed(It.IsAny<string>())).ReturnsAsync(true);


        var testInstance = CreateTestService(provider, userRepo);

        // Act
        var result = await testInstance.CheckRegistrationInfo(model);

        // Assert
        result.IsValid.Should().BeFalse();
        result.ErrorCode.Should().Be(ErrorCodes.Auth.AccountAlreadyExist);
        result.UserRegistrationErrors.Should().NotBeNull();
        result.UserRegistrationErrors.UsernameTaken.Should().BeTrue();
        result.UserRegistrationErrors.UsernameLengthIncorrect.Should().BeFalse();
        result.UserRegistrationErrors.UsernameModerationFailed.Should().BeFalse();
        result.UserRegistrationErrors.UsernameContainsForbiddenSymbols.Should().BeFalse();
    }

    private IUserAccountService CreateTestService(
        IServiceProvider provider,
        Mock<IUserRepository> userRepo = null,
        Mock<IPhoneNumberAuthService> phoneNumberAuthService = null,
        Mock<IAppleAuthService> appleAuthService = null,
        Mock<IEmailAuthService> emailAuthService = null,
        Mock<IMinorUserService> minorService = null,
        Mock<IEmailSendingService> emailSendingService = null,
        Mock<IModerationProviderApi> moderationProviderApi = null,
        Mock<IJwtTokenProvider> jwtTokenProvider = null,
        Mock<IGoogleAuthService> googleAuthService = null,
        Mock<ICredentialValidateService> credentialValidateService = null,
        Mock<UserManager<ApplicationUser>> userManager = null,
        string token = null
    )
    {
        userRepo ??= GetUserRepoMock();

        if (phoneNumberAuthService == null)
        {
            phoneNumberAuthService = new Mock<IPhoneNumberAuthService>(MockBehavior.Strict);
            phoneNumberAuthService.Setup(s => s.FormatPhoneNumber(It.IsAny<string>())).ReturnsAsync((string val) => val);
        }

        appleAuthService ??= new Mock<IAppleAuthService>(MockBehavior.Strict);
        googleAuthService ??= new Mock<IGoogleAuthService>(MockBehavior.Strict);
        emailAuthService ??= new Mock<IEmailAuthService>(MockBehavior.Strict);
        emailSendingService ??= new Mock<IEmailSendingService>(MockBehavior.Strict);

        if (minorService == null)
        {
            minorService = new Mock<IMinorUserService>(MockBehavior.Strict);
            minorService.Setup(s => s.IsMinorAge(It.IsAny<string>(), It.IsAny<DateTime>())).ReturnsAsync(false);
        }

        if (moderationProviderApi == null)
        {
            moderationProviderApi = new Mock<IModerationProviderApi>(MockBehavior.Strict);
            moderationProviderApi.Setup(s => s.CallModerationProviderApiText(It.IsAny<string>()))
                                 .ReturnsAsync(ModerationResult.DummyPassed);
        }

        var nicknameSuggestionService = new Mock<INicknameSuggestionService>(MockBehavior.Strict);
        nicknameSuggestionService.Setup(s => s.SuggestNickname(It.IsAny<string>(), It.IsAny<int>())).ReturnsAsync(["test"]);

        jwtTokenProvider ??= new Mock<IJwtTokenProvider>();
        if (!string.IsNullOrWhiteSpace(token))
            jwtTokenProvider.Setup(
                                 s => s.GetJwtToken(
                                     It.IsAny<string>(),
                                     It.IsAny<string>(),
                                     It.IsAny<string>(),
                                     It.IsAny<string>(),
                                     It.IsAny<string>(),
                                     It.IsAny<string>(),
                                     It.IsAny<string>(),
                                     It.IsAny<string>()
                                 )
                             )
                            .ReturnsAsync(token);

        var permissionService = new Mock<IUserPermissionService>(MockBehavior.Strict);
        permissionService.Setup(s => s.GetUserReadinessAccessScopes(It.IsAny<long>())).ReturnsAsync([]);

        if (credentialValidateService == null)
        {
            credentialValidateService = new Mock<ICredentialValidateService>();
            credentialValidateService.Setup(s => s.ValidateNewAccountData(It.IsAny<RegisterUserViewModel>(), false))
                                     .ReturnsAsync(new UserAccountRegistrationResult {Ok = true});
            credentialValidateService.Setup(s => s.ValidateUserName(It.IsAny<string>()))
                                     .ReturnsAsync(new UserAccountRegistrationResult {Ok = true});
        }

        userManager ??= TestServiceConfiguration.CreateUserManager(provider);

        var dailyTokenRefillService = new Mock<IDailyTokenRefillService>();

        var testInstance = new UserAccountService(
            userRepo.Object,
            provider.GetRequiredService<ILoggerFactory>(),
            userManager.Object,
            phoneNumberAuthService.Object,
            appleAuthService.Object,
            emailAuthService.Object,
            minorService.Object,
            new OnboardingOptions {FreverOfficialEmail = "frever@com"},
            emailSendingService.Object,
            jwtTokenProvider.Object,
            permissionService.Object,
            googleAuthService.Object,
            nicknameSuggestionService.Object,
            credentialValidateService.Object,
            new AppleEmailInfoRequestValidator(),
            new TemporaryAccountRequestValidator(),
            new UpdateAccountRequestValidator(),
            dailyTokenRefillService.Object
        );

        return testInstance;
    }

    private static Mock<IUserRepository> GetUserRepoMock()
    {
        var userRepo = new Mock<IUserRepository>(MockBehavior.Strict);

        var dbTransactionMock = new Mock<IDbContextTransaction>();

        userRepo.Setup(s => s.BeginMainDbTransactionAsync()).ReturnsAsync(dbTransactionMock.Object);
        userRepo.Setup(s => s.BeginAuthDbTransactionAsync()).ReturnsAsync(dbTransactionMock.Object);
        userRepo.Setup(s => s.SaveChanges()).Returns(Task.CompletedTask);
        userRepo.Setup(s => s.AllUsers()).Returns(Enumerable.Empty<User>().BuildMock());
        userRepo.Setup(s => s.GetCountries()).Returns(Enumerable.Empty<Country>().BuildMock());
        userRepo.Setup(s => s.CreateUserAsync(It.IsAny<UserCreateModel>())).Returns(Task.CompletedTask);
        userRepo.Setup(s => s.UpdateUser(It.IsAny<UserUpdateModel>())).Returns(Task.CompletedTask);
        userRepo.Setup(s => s.GetClaimsDataAsync(It.IsAny<string>())).ReturnsAsync(new MainDbClaimData());
        userRepo.Setup(s => s.AddInitialFriend(It.IsAny<long>(), It.IsAny<string>())).Returns(Task.CompletedTask);

        return userRepo;
    }
}