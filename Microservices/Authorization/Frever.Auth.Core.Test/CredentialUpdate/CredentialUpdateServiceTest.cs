using AuthServer.Contracts;
using AuthServer.Features.CredentialUpdate;
using AuthServer.Features.CredentialUpdate.Contracts;
using AuthServer.Features.CredentialUpdate.Handlers;
using AuthServer.Features.CredentialUpdate.Models;
using AuthServer.Permissions.Services;
using AuthServer.Repositories;
using AuthServer.Services.EmailAuth;
using AuthServer.Services.PhoneNumberAuth;
using AuthServer.Services.UserManaging;
using AuthServerShared;
using Common.Infrastructure;
using Common.Models;
using FluentAssertions;
using Frever.Auth.Core.Test.Utils;
using Frever.Shared.MainDb.Entities;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MockQueryable.Moq;
using Moq;
using Xunit;
using Xunit.Abstractions;

namespace Frever.Auth.Core.Test.CredentialUpdate;

[Collection("Credential Update Service")]
public class CredentialUpdateServiceTest(ITestOutputHelper testOut)
{
    [Fact(DisplayName = "👍👍Check get credentials status")]
    public async Task GetCredentialStatus()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddTestAuthServices(testOut);

        await using var provider = services.BuildServiceProvider();

        var users = new List<User>
                    {
                        new()
                        {
                            Email = "test123@email",
                            PhoneNumber = "xxxxxxxxx",
                            AppleId = "test_apple_id",
                            GoogleId = "test_google_id",
                            MainGroup = new Group()
                        }
                    };

        var userRepo = new Mock<IUserRepository>(MockBehavior.Strict);
        userRepo.Setup(s => s.GetUserByGroupId(It.IsAny<long>())).Returns(users.BuildMock());
        userRepo.Setup(s => s.AuthUserHasPassword(It.IsAny<string>())).Returns(Task.FromResult(true));

        var testInstance = CreateTestService(provider, userRepo);

        // Act
        var result = await testInstance.GetCredentialStatus();

        // Assert
        result.Email.Should().Be("t***3@email");
        result.HasPassword.Should().BeTrue();
        result.HasAppleId.Should().BeTrue();
        result.HasGoogleId.Should().BeTrue();
    }

    [Fact(DisplayName = "👍👍Check get credentials status: user not found error")]
    public async Task GetCredentialStatus_UserNotFound()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddTestAuthServices(testOut);

        await using var provider = services.BuildServiceProvider();

        var userRepo = new Mock<IUserRepository>(MockBehavior.Strict);
        userRepo.Setup(s => s.GetUserByGroupId(It.IsAny<long>())).Returns(new List<User>().BuildMock());
        userRepo.Setup(s => s.AuthUserHasPassword(It.IsAny<string>())).Returns(Task.FromResult(true));

        var testInstance = CreateTestService(provider, userRepo);

        // Act
        var act = () => testInstance.GetCredentialStatus();

        // Assert
        await act.Should().ThrowAsync<AppErrorWithStatusCodeException>().Where(e => e.ErrorCode.Equals(ErrorCodes.Auth.UserNotFound));
    }

    [Fact(DisplayName = "👍👍Check verify credentials: for new credentials")]
    public async Task VerifyCredentials_ForNewCredentials()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddTestAuthServices(testOut);

        await using var provider = services.BuildServiceProvider();

        var emailService = new Mock<IEmailAuthService>(MockBehavior.Strict);
        emailService.Setup(s => s.SendEmailVerification(It.IsAny<VerifyEmailRequest>())).Returns(Task.CompletedTask);

        var phoneNumberService = new Mock<IPhoneNumberAuthService>(MockBehavior.Strict);
        phoneNumberService.Setup(s => s.FormatPhoneNumber(It.IsAny<string>())).ReturnsAsync(string.Empty);
        phoneNumberService.Setup(s => s.SendPhoneNumberVerification(It.IsAny<VerifyPhoneNumberRequest>()))
                          .Returns(Task.FromResult(new VerifyPhoneNumberResponse()));

        var testInstance = CreateTestService(provider, emailAuthService: emailService, phoneNumberAuthService: phoneNumberService);

        // Act
        await testInstance.VerifyCredentials(new VerifyCredentialRequest { Email = "test@email", PhoneNumber = "+12345", IsNew = true });

        // Assert
        emailService.Verify(c => c.SendEmailVerification(It.IsAny<VerifyEmailRequest>()), Times.Once);
        phoneNumberService.Verify(c => c.SendPhoneNumberVerification(It.IsAny<VerifyPhoneNumberRequest>()), Times.Once);
    }

    [Theory(DisplayName = "👍👍Check verify credentials: invalid credentials")]
    [InlineData("test@email", null, ErrorCodes.Auth.EmailAlreadyUsed)]
    [InlineData(null, "+12345", ErrorCodes.Auth.PhoneNumberAlreadyUsed)]
    public async Task VerifyCredentials_CredentialAlreadyUsed(string email, string phoneNumber, string errorCode)
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddTestAuthServices(testOut);

        await using var provider = services.BuildServiceProvider();

        var userRepo = new Mock<IUserRepository>(MockBehavior.Strict);
        var user = new User { MainGroup = new Group() };
        userRepo.Setup(s => s.GetUserByGroupId(It.IsAny<long>())).Returns(new List<User> { user }.BuildMock());
        userRepo.Setup(s => s.IsEmailRegistered(It.IsAny<string>())).Returns(Task.FromResult(true));
        userRepo.Setup(s => s.IsPhoneRegistered(It.IsAny<string>())).Returns(Task.FromResult(true));

        var emailService = new Mock<IEmailAuthService>(MockBehavior.Strict);
        emailService.Setup(s => s.SendEmailVerification(It.IsAny<VerifyEmailRequest>())).Returns(Task.CompletedTask);

        var phoneNumberService = new Mock<IPhoneNumberAuthService>(MockBehavior.Strict);
        phoneNumberService.Setup(s => s.FormatPhoneNumber(It.IsAny<string>())).ReturnsAsync(string.Empty);
        phoneNumberService.Setup(s => s.SendPhoneNumberVerification(It.IsAny<VerifyPhoneNumberRequest>()))
                          .Returns(Task.FromResult(new VerifyPhoneNumberResponse()));

        var testInstance = CreateTestService(provider, userRepo, emailService, phoneNumberService);

        // Act
        var request = new VerifyCredentialRequest { Email = email, PhoneNumber = phoneNumber, IsNew = true };
        var act = () => testInstance.VerifyCredentials(request);

        // Assert
        await act.Should().ThrowAsync<AppErrorWithStatusCodeException>().Where(e => e.ErrorCode.Equals(errorCode));
    }

    [Fact(DisplayName = "👍👍Check verify credentials: for old credentials")]
    public async Task VerifyCredentials_ForOldCredentials()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddTestAuthServices(testOut);

        await using var provider = services.BuildServiceProvider();

        var user = new User { Email = "test@email", PhoneNumber = "+12345", MainGroup = new Group() };

        var userRepo = new Mock<IUserRepository>(MockBehavior.Strict);
        userRepo.Setup(s => s.GetUserByGroupId(It.IsAny<long>())).Returns(new List<User> { user }.BuildMock());

        var emailService = new Mock<IEmailAuthService>(MockBehavior.Strict);
        emailService.Setup(s => s.SendEmailVerification(It.IsAny<VerifyEmailRequest>())).Returns(Task.CompletedTask);

        var phoneNumberService = new Mock<IPhoneNumberAuthService>(MockBehavior.Strict);
        phoneNumberService.Setup(s => s.FormatPhoneNumber(It.IsAny<string>())).ReturnsAsync(user.PhoneNumber);
        phoneNumberService.Setup(s => s.SendPhoneNumberVerification(It.IsAny<VerifyPhoneNumberRequest>()))
                          .Returns(Task.FromResult(new VerifyPhoneNumberResponse()));

        var testInstance = CreateTestService(provider, userRepo, emailService, phoneNumberService);

        // Act
        await testInstance.VerifyCredentials(new VerifyCredentialRequest { Email = user.Email, PhoneNumber = user.PhoneNumber });

        // Assert
        emailService.Verify(c => c.SendEmailVerification(It.IsAny<VerifyEmailRequest>()), Times.Once);
        phoneNumberService.Verify(c => c.SendPhoneNumberVerification(It.IsAny<VerifyPhoneNumberRequest>()), Times.Once);
    }

    [Theory(DisplayName = "👍👍Check verify credentials: invalid credentials")]
    [InlineData("test@email", null, ErrorCodes.Auth.EmailInvalid)]
    [InlineData(null, "+12345", ErrorCodes.Auth.PhoneNumberInvalid)]
    public async Task VerifyCredentials_InvalidCredentials(string email, string phoneNumber, string errorCode)
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddTestAuthServices(testOut);

        await using var provider = services.BuildServiceProvider();

        var user = new User { Email = email, PhoneNumber = phoneNumber, MainGroup = new Group() };

        var userRepo = new Mock<IUserRepository>(MockBehavior.Strict);
        userRepo.Setup(s => s.GetUserByGroupId(It.IsAny<long>())).Returns(new List<User> { user }.BuildMock());

        var emailService = new Mock<IEmailAuthService>(MockBehavior.Strict);
        emailService.Setup(s => s.SendEmailVerification(It.IsAny<VerifyEmailRequest>())).Returns(Task.CompletedTask);

        var phoneNumberService = new Mock<IPhoneNumberAuthService>(MockBehavior.Strict);
        phoneNumberService.Setup(s => s.FormatPhoneNumber(It.IsAny<string>())).ReturnsAsync(string.Empty);
        phoneNumberService.Setup(s => s.SendPhoneNumberVerification(It.IsAny<VerifyPhoneNumberRequest>()))
                          .Returns(Task.FromResult(new VerifyPhoneNumberResponse()));

        var testInstance = CreateTestService(provider, userRepo, emailService, phoneNumberService);

        // Act
        var request = new VerifyCredentialRequest { Email = email?[1..], PhoneNumber = phoneNumber?[1..] };
        var act = () => testInstance.VerifyCredentials(request);

        // Assert
        await act.Should().ThrowAsync<AppErrorWithStatusCodeException>().Where(e => e.ErrorCode.Equals(errorCode));
    }

    [Theory(DisplayName = "👍👍Check add credentials")]
    [InlineData(CredentialType.Email)]
    [InlineData(CredentialType.PhoneNumber)]
    [InlineData(CredentialType.Password)]
    [InlineData(CredentialType.AppleId)]
    [InlineData(CredentialType.GoogleId)]
    public async Task AddCredentials(CredentialType type)
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddTestAuthServices(testOut);

        await using var provider = services.BuildServiceProvider();

        var handlers = CreateCredentialHandlers();

        var testInstance = CreateTestService(provider, handlers: handlers);

        // Act
        var request = new AddCredentialsRequest
        {
            Type = type,
            Email = "test@email",
            PhoneNumber = "xxxxxxxxx",
            VerificationCode = "12345",
            AppleId = "apple_id",
            AppleIdentityToken = "apple_token",
            GoogleId = "apple_id",
            IdentityToken = "apple_token",
            Password = "password"
        };

        await testInstance.AddCredentials(request);

        // Assert
        handlers[type]
           .Verify(e => e.AddCredentials(It.IsAny<AddCredentialsRequest>(), It.IsAny<long>(), It.IsAny<CredentialStatus>()), Times.Once);
    }

    [Theory(DisplayName = "👍👍Check add credentials: only password type available for minor users")]
    [InlineData(CredentialType.Email)]
    [InlineData(CredentialType.PhoneNumber)]
    [InlineData(CredentialType.AppleId)]
    [InlineData(CredentialType.GoogleId)]
    public async Task AddCredentials_OnlyPasswordTypeAvailableForMinorUser(CredentialType type)
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddTestAuthServices(testOut);

        await using var provider = services.BuildServiceProvider();

        var userRepo = new Mock<IUserRepository>(MockBehavior.Strict);
        userRepo.Setup(s => s.GetGroupById(It.IsAny<long>())).Returns(new List<Group> { new() { IsMinor = true } }.BuildMock());

        var testInstance = CreateTestService(provider, userRepo);

        // Act
        var request = new AddCredentialsRequest
        {
            Type = type,
            Email = "test@email",
            PhoneNumber = "xxxxxxxxx",
            VerificationCode = "12345",
            AppleId = "apple_id",
            AppleIdentityToken = "apple_token",
            GoogleId = "apple_id",
            IdentityToken = "apple_token",
        };
        var act = () => testInstance.AddCredentials(request);

        // Assert
        await act.Should()
                 .ThrowAsync<AppErrorWithStatusCodeException>()
                 .Where(e => e.ErrorCode.Equals(ErrorCodes.Auth.MinorCredentialsInvalid));
    }

    [Theory(DisplayName = "👍👍Check verify user")]
    [InlineData(CredentialType.Email)]
    [InlineData(CredentialType.PhoneNumber)]
    [InlineData(CredentialType.AppleId)]
    [InlineData(CredentialType.GoogleId)]
    [InlineData(CredentialType.Password)]
    public async Task VerifyUser(CredentialType type)
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddTestAuthServices(testOut);

        await using var provider = services.BuildServiceProvider();

        var tokenProvider = new Mock<ITokenProvider>(MockBehavior.Strict);
        tokenProvider.Setup(s => s.GenerateToken(It.IsAny<long>())).Returns("token");

        var testInstance = CreateTestService(provider, tokenProvider: tokenProvider);

        // Act
        var request = new VerifyUserRequest
        {
            Type = type,
            VerificationCode = "12345",
            AppleIdentityToken = "apple_token",
            IdentityToken = "google_token",
            Password = "password"
        };
        var act = await testInstance.VerifyUser(request);

        // Assert
        act.IsSuccessful.Should().BeTrue();
        act.VerificationToken.Should().NotBeNullOrWhiteSpace();
        act.ErrorCode.Should().BeNull();
        act.ErrorMessage.Should().BeNull();
    }

    [Theory(DisplayName = "👍👍Check verify user: validation failed with error")]
    [InlineData(CredentialType.Email, ErrorCodes.Auth.VerificationCodeInvalid)]
    [InlineData(CredentialType.PhoneNumber, ErrorCodes.Auth.VerificationCodeInvalid)]
    [InlineData(CredentialType.AppleId, ErrorCodes.Auth.AppleTokenInvalid)]
    [InlineData(CredentialType.GoogleId, ErrorCodes.Auth.GoogleTokenInvalid)]
    [InlineData(CredentialType.Password, ErrorCodes.Auth.PasswordInvalid)]
    public async Task VerifyUser_ValidationFailed(CredentialType type, string errorCode)
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddTestAuthServices(testOut);

        await using var provider = services.BuildServiceProvider();

        var testInstance = CreateTestService(provider, validateCredentialErrorCode: errorCode);

        // Act
        var request = new VerifyUserRequest
        {
            Type = type,
            VerificationCode = "12345",
            AppleIdentityToken = "apple_token",
            IdentityToken = "google_token",
            Password = "password"
        };
        var act = await testInstance.VerifyUser(request);

        // Assert
        act.IsSuccessful.Should().BeFalse();
        act.VerificationToken.Should().BeNull();
        act.ErrorCode.Should().Be(errorCode);
        act.ErrorMessage.Should().NotBeNull();
    }

    [Theory(DisplayName = "👍👍Check update credentials")]
    [InlineData(CredentialType.Email)]
    [InlineData(CredentialType.PhoneNumber)]
    [InlineData(CredentialType.Password)]
    public async Task UpdateCredentials(CredentialType type)
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddTestAuthServices(testOut);

        await using var provider = services.BuildServiceProvider();

        var handlers = CreateCredentialHandlers();

        var testInstance = CreateTestService(provider, handlers: handlers);

        // Act
        var request = new UpdateCredentialsRequest
        {
            Type = type,
            VerificationToken = "token",
            Email = "test@email",
            PhoneNumber = "xxxxxxxxx",
            Password = "password"
        };
        await testInstance.UpdateCredentials(request);

        // Assert
        handlers[type]
           .Verify(
                e => e.UpdateCredentials(It.IsAny<UpdateCredentialsRequest>(), It.IsAny<long>(), It.IsAny<CredentialStatus>()),
                Times.Once
            );
    }

    [Theory(DisplayName = "👍👍Check update credentials: ignore apple and google type")]
    [InlineData(CredentialType.AppleId)]
    [InlineData(CredentialType.GoogleId)]
    public async Task UpdateCredentials_IgnoreAppleIdAndGoogleIdType(CredentialType type)
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddTestAuthServices(testOut);

        await using var provider = services.BuildServiceProvider();

        var handlers = CreateCredentialHandlers();

        var testInstance = CreateTestService(provider, handlers: handlers);

        // Act
        var request = new UpdateCredentialsRequest
        {
            Type = type,
            VerificationToken = "token",
            Email = "test@email",
            PhoneNumber = "xxxxxxxxx",
            Password = "password"
        };
        await testInstance.UpdateCredentials(request);

        // Assert
        handlers[type]
           .Verify(
                e => e.UpdateCredentials(It.IsAny<UpdateCredentialsRequest>(), It.IsAny<long>(), It.IsAny<CredentialStatus>()),
                Times.Never
            );
    }

    [Fact(DisplayName = "👍👍Check update credentials: invalid verification token: expired")]
    public async Task UpdateCredentials_InvalidVerificationToken_TokenExpired()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddTestAuthServices(testOut);

        await using var provider = services.BuildServiceProvider();

        var payload = new TokenPayload { ExpiredAt = DateTime.UtcNow.AddMinutes(-5).Ticks };
        var tokenProvider = new Mock<ITokenProvider>(MockBehavior.Strict);
        tokenProvider.Setup(s => s.ParseToken(It.IsAny<string>())).Returns(payload);

        var testInstance = CreateTestService(provider, tokenProvider: tokenProvider);

        // Act
        var request = new UpdateCredentialsRequest { VerificationToken = "token" };
        var act = () => testInstance.UpdateCredentials(request);

        // Assert
        await act.Should()
                 .ThrowAsync<AppErrorWithStatusCodeException>()
                 .Where(e => e.ErrorCode.Equals(ErrorCodes.Auth.VerificationTokenInvalid));
    }

    [Fact(DisplayName = "👍👍Check update credentials: invalid verification token: expired")]
    public async Task UpdateCredentials_InvalidVerificationToken_NotCurrentGroup()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddTestAuthServices(testOut);

        await using var provider = services.BuildServiceProvider();

        var payload = new TokenPayload { ExpiredAt = DateTime.UtcNow.AddMinutes(5).Ticks, GroupId = 16 };
        var tokenProvider = new Mock<ITokenProvider>(MockBehavior.Strict);
        tokenProvider.Setup(s => s.ParseToken(It.IsAny<string>())).Returns(payload);

        var testInstance = CreateTestService(provider, tokenProvider: tokenProvider);

        // Act
        var request = new UpdateCredentialsRequest { VerificationToken = "token" };
        var act = () => testInstance.UpdateCredentials(request);

        // Assert
        await act.Should()
                 .ThrowAsync<AppErrorWithStatusCodeException>()
                 .Where(e => e.ErrorCode.Equals(ErrorCodes.Auth.VerificationTokenInvalid));
    }

    [Theory(DisplayName = "👍👍Check update credentials: only password type available for minor users")]
    [InlineData(CredentialType.Email)]
    [InlineData(CredentialType.PhoneNumber)]
    [InlineData(CredentialType.AppleId)]
    [InlineData(CredentialType.GoogleId)]
    public async Task UpdateCredentials_OnlyPasswordTypeAvailableForMinorUser(CredentialType type)
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddTestAuthServices(testOut);

        await using var provider = services.BuildServiceProvider();

        var userRepo = new Mock<IUserRepository>(MockBehavior.Strict);
        userRepo.Setup(s => s.GetGroupById(It.IsAny<long>())).Returns(new List<Group> { new() { IsMinor = true } }.BuildMock());

        var testInstance = CreateTestService(provider, userRepo);

        // Act
        var request = new UpdateCredentialsRequest
        {
            Type = type,
            VerificationToken = "token",
            Email = "test@email",
            PhoneNumber = "xxxxxxxxx",
            Password = "password"
        };
        var act = () => testInstance.UpdateCredentials(request);

        // Assert
        await act.Should()
                 .ThrowAsync<AppErrorWithStatusCodeException>()
                 .Where(e => e.ErrorCode.Equals(ErrorCodes.Auth.MinorCredentialsInvalid));
    }

    [Fact(DisplayName = "👍👍Check update username")]
    public async Task UpdateUserName()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddTestAuthServices(testOut);

        await using var provider = services.BuildServiceProvider();

        var request = new UpdateUserNameRequest { UserName = "test_user_name" };

        var testInstance = CreateTestService(provider);

        // Act
        var result = await testInstance.UpdateUserName(request);

        // Assert
        result.Ok.Should().BeTrue();
        result.ErrorCode.Should().BeNull();
        result.ErrorDetails.Should().BeNull();
        result.UsernameUpdateAvailableOn.Should().NotBeNull();
    }

    [Fact(DisplayName = "👍👍Check update username: user not found")]
    public async Task UpdateUserName_UserNotFound()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddTestAuthServices(testOut);

        await using var provider = services.BuildServiceProvider();

        var request = new UpdateUserNameRequest { UserName = "test_user_name" };

        var userRepo = new Mock<IUserRepository>(MockBehavior.Strict);
        userRepo.Setup(s => s.GetGroupById(It.IsAny<long>())).Returns(Enumerable.Empty<Group>().BuildMock());

        var testInstance = CreateTestService(provider, userRepo);

        // Act
        var act = () => testInstance.UpdateUserName(request);

        // Assert
        await act.Should().ThrowAsync<AppErrorWithStatusCodeException>().Where(e => e.ErrorCode.Equals(ErrorCodes.Auth.UserNotFound));
    }

    [Fact(DisplayName = "👍👍Check update username: username update limit")]
    public async Task UpdateUserName_UsernameUpdateLimit()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddTestAuthServices(testOut);

        await using var provider = services.BuildServiceProvider();

        var request = new UpdateUserNameRequest { UserName = "test_user_name" };
        var group = new Group { NickNameUpdatedAt = DateTime.UtcNow };

        var userRepo = new Mock<IUserRepository>(MockBehavior.Strict);
        userRepo.Setup(s => s.GetGroupById(It.IsAny<long>())).Returns(new List<Group> { group }.BuildMock());

        var testInstance = CreateTestService(provider, userRepo);

        // Act
        var result = await testInstance.UpdateUserName(request);

        // Assert
        result.Ok.Should().BeFalse();
        result.ErrorCode.Should().Be(ErrorCodes.Auth.UsernameUpdateLimit);
    }

    [Fact(DisplayName = "👍👍Check update username: validation failed")]
    public async Task UpdateUserName_ValidationFailed()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddTestAuthServices(testOut);

        await using var provider = services.BuildServiceProvider();

        var request = new UpdateUserNameRequest { UserName = "test_user_name" };
        var response = new UserAccountRegistrationResult { Ok = false, ErrorCode = ErrorCodes.Auth.UserNameAlreadyUsed };

        var credentialValidateService = new Mock<ICredentialValidateService>();
        credentialValidateService.Setup(e => e.ValidateUserName(request.UserName)).Returns(Task.FromResult(response));

        var testInstance = CreateTestService(provider, credentialValidateService: credentialValidateService);

        // Act
        var result = await testInstance.UpdateUserName(request);

        // Assert
        result.Ok.Should().Be(response.Ok);
        result.ErrorCode.Should().Be(response.ErrorCode);
    }

    [Fact(DisplayName = "👍👍Check token provider")]
    public void TokenProvider()
    {
        // Arrange
        const long groupId = 16;

        var testInstance = new TokenProvider();

        // Act
        var token = testInstance.GenerateToken(groupId);

        var result = testInstance.ParseToken(token);

        // Assert
        result.GroupId.Should().Be(groupId);
        result.ExpiredAt.Should().BeGreaterThan(DateTime.UtcNow.Ticks);
    }

    private static CredentialUpdateService CreateTestService(
        IServiceProvider provider,
        Mock<IUserRepository> userRepo = null,
        Mock<IEmailAuthService> emailAuthService = null,
        Mock<IPhoneNumberAuthService> phoneNumberAuthService = null,
        Mock<ITokenProvider> tokenProvider = null,
        Mock<IUserPermissionService> userPermissionService = null,
        Mock<ICredentialValidateService> credentialValidateService = null,
        Dictionary<CredentialType, Mock<ICredentialHandler>> handlers = null,
        string validateCredentialErrorCode = null
    )
    {
        if (userRepo is null)
        {
            var dbTransactionMock = new Mock<IDbContextTransaction>();
            var user = new User { MainGroup = new Group() };

            userRepo = new Mock<IUserRepository>(MockBehavior.Strict);
            userRepo.Setup(s => s.GetGroupById(It.IsAny<long>())).Returns(new List<Group> { new() }.BuildMock());
            userRepo.Setup(s => s.GetUserByGroupId(It.IsAny<long>())).Returns(new List<User> { user }.BuildMock());
            userRepo.Setup(s => s.AuthUserHasPassword(It.IsAny<string>())).ReturnsAsync(false);
            userRepo.Setup(s => s.IsEmailRegistered(It.IsAny<string>())).ReturnsAsync(false);
            userRepo.Setup(s => s.IsPhoneRegistered(It.IsAny<string>())).ReturnsAsync(false);
            userRepo.Setup(s => s.BeginMainDbTransactionAsync()).ReturnsAsync(dbTransactionMock.Object);
            userRepo.Setup(s => s.BeginAuthDbTransactionAsync()).ReturnsAsync(dbTransactionMock.Object);
            userRepo.Setup(s => s.SaveChanges()).Returns(Task.CompletedTask);
        }

        emailAuthService ??= new Mock<IEmailAuthService>(MockBehavior.Strict);

        if (phoneNumberAuthService == null)
        {
            phoneNumberAuthService = new Mock<IPhoneNumberAuthService>(MockBehavior.Strict);
            phoneNumberAuthService.Setup(s => s.FormatPhoneNumber(It.IsAny<string>())).ReturnsAsync(string.Empty);
        }

        if (tokenProvider is null)
        {
            var payload = new TokenPayload { ExpiredAt = DateTime.UtcNow.AddMinutes(5).Ticks };
            tokenProvider = new Mock<ITokenProvider>(MockBehavior.Strict);
            tokenProvider.Setup(s => s.ParseToken(It.IsAny<string>())).Returns(payload);
        }

        if (userPermissionService == null)
        {
            userPermissionService = new Mock<IUserPermissionService>(MockBehavior.Strict);
            userPermissionService.Setup(s => s.EnsureCurrentUserActive()).Returns(Task.CompletedTask);
        }

        if (credentialValidateService == null)
        {
            credentialValidateService = new Mock<ICredentialValidateService>();
            credentialValidateService.Setup(e => e.ValidateUserName(It.IsAny<string>()))
                                     .ReturnsAsync(new UserAccountRegistrationResult { Ok = true });
        }

        var userInfo = new UserInfo(
            0,
            0,
            false,
            false,
            [],
            []
        );

        var credentialHandlers = handlers ?? CreateCredentialHandlers(validateCredentialErrorCode);
        var userManager = TestServiceConfiguration.CreateUserManager(provider);

        var testInstance = new CredentialUpdateService(
            userInfo,
            provider.GetRequiredService<ILoggerFactory>(),
            userRepo.Object,
            emailAuthService.Object,
            phoneNumberAuthService.Object,
            provider.GetRequiredService<IUserPermissionService>(),
            tokenProvider.Object,
            userManager.Object,
            credentialHandlers.Select(e => e.Value.Object),
            credentialValidateService.Object,
            new VerifyUserRequestValidator(),
            new AddCredentialsRequestValidator(),
            new UpdateCredentialsRequestValidator()
        );

        return testInstance;
    }

    private static Dictionary<CredentialType, Mock<ICredentialHandler>> CreateCredentialHandlers(string validateCredentialErrorCode = null)
    {
        var validateCredentialResponse = string.IsNullOrWhiteSpace(validateCredentialErrorCode)
                                             ? (IsValid: true, ErrorMessage: string.Empty, ErrorCode: string.Empty)
                                             : (IsValid: false, ErrorMessage: string.Empty, ErrorCode: validateCredentialErrorCode);

        var handlers = new Dictionary<CredentialType, Mock<ICredentialHandler>>
                       {
                           {CredentialType.AppleId, new Mock<ICredentialHandler>(MockBehavior.Strict)},
                           {CredentialType.GoogleId, new Mock<ICredentialHandler>(MockBehavior.Strict)},
                           {CredentialType.Email, new Mock<ICredentialHandler>(MockBehavior.Strict)},
                           {CredentialType.PhoneNumber, new Mock<ICredentialHandler>(MockBehavior.Strict)},
                           {CredentialType.Password, new Mock<ICredentialHandler>(MockBehavior.Strict)}
                       };

        foreach (var item in handlers)
        {
            item.Value.Setup(e => e.HandlerType).Returns(item.Key);
            item.Value.Setup(e => e.AddCredentials(It.IsAny<AddCredentialsRequest>(), It.IsAny<long>(), It.IsAny<CredentialStatus>()))
                .Returns(Task.CompletedTask);
            item.Value.Setup(e => e.ValidateCurrentCredential(It.IsAny<VerifyUserRequest>(), It.IsAny<ShortUserInfo>()))
                .Returns(Task.FromResult(validateCredentialResponse));
            item.Value.Setup(e => e.UpdateCredentials(It.IsAny<UpdateCredentialsRequest>(), It.IsAny<long>(), It.IsAny<CredentialStatus>()))
                .Returns(Task.CompletedTask);
        }

        return handlers;
    }
}