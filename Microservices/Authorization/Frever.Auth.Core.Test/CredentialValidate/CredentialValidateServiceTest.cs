using AuthServer.Repositories;
using AuthServer.Services.AppleAuth;
using AuthServer.Services.EmailAuth;
using AuthServer.Services.GoogleAuth;
using AuthServer.Services.PhoneNumberAuth;
using AuthServer.Services.UserManaging;
using Common.Infrastructure.ModerationProvider;
using Common.Models;
using FluentAssertions;
using Frever.Auth.Core.Test.Utils;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using Xunit.Abstractions;

namespace Frever.Auth.Core.Test.CredentialValidate;

[Collection("Credential Validate Service")]
public partial class CredentialValidateServiceTest(ITestOutputHelper testOut)
{
    [Fact(DisplayName = "👍👎Check validate password strength")]
    public async Task ValidatePasswordStrength()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddTestAuthServices(testOut);

        await using var provider = services.BuildServiceProvider();

        const string password = "testpassword12345";
        const string username = "testusername12345";

        var testInstance = CreateTestService(provider);

        // Act
        var result = await testInstance.ValidatePassword(password, username);

        // Assert
        result.Ok.Should().BeTrue();
        result.IsStrong.Should().BeTrue();
        result.IsLongEnough.Should().BeTrue();
        result.IsTooSimple.Should().BeFalse();
    }

    [Fact(DisplayName = "👍👍Check validate password strength: password matches username")]
    public async Task ValidatePasswordStrength_PasswordMatchesUsername()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddTestAuthServices(testOut);

        await using var provider = services.BuildServiceProvider();

        const string password = "test12345";
        const string username = "test12345";

        var testInstance = CreateTestService(provider);

        // Act
        var result = await testInstance.ValidatePassword(password, username);

        // Assert
        result.Ok.Should().BeFalse();
        result.ErrorCode.Should().Be(ErrorCodes.Auth.PasswordMatchesUsername);
    }

    [Fact(DisplayName = "👍👍Check validate password strength: is not long enough")]
    public async Task ValidatePasswordStrength_IsNotLongEnough()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddTestAuthServices(testOut);

        await using var provider = services.BuildServiceProvider();

        const string password = "12345";
        const string username = "testusername12345";

        var testInstance = CreateTestService(provider);

        // Act
        var result = await testInstance.ValidatePassword(password, username);

        // Assert
        result.Ok.Should().BeFalse();
        result.IsStrong.Should().BeFalse();
        result.IsLongEnough.Should().BeFalse();
        result.IsTooSimple.Should().BeTrue();
    }

    [Fact(DisplayName = "👍👍Check validate password strength: is too simple")]
    public async Task ValidatePasswordStrength_IsTooSimple()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddTestAuthServices(testOut);

        await using var provider = services.BuildServiceProvider();

        const string password = "123456";
        const string username = "testusername12345";

        var testInstance = CreateTestService(provider);

        // Act
        var result = await testInstance.ValidatePassword(password, username);

        // Assert
        result.Ok.Should().BeFalse();
        result.IsStrong.Should().BeFalse();
        result.IsLongEnough.Should().BeTrue();
        result.IsTooSimple.Should().BeTrue();
    }

    [Fact(DisplayName = "👍👎Check validate username")]
    public async Task ValidateUserName()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddTestAuthServices(testOut);

        await using var provider = services.BuildServiceProvider();

        const string username = "1234567890";

        var testInstance = CreateTestService(provider);

        // Act
        var result = await testInstance.ValidateUserName(username);

        // Assert
        result.Ok.Should().BeTrue();
    }

    [Fact(DisplayName = "👍👍Check validate username: username already used")]
    public async Task ValidateUserName_UserNameAlreadyUsed()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddTestAuthServices(testOut);

        await using var provider = services.BuildServiceProvider();

        const string username = "test";

        var userRepo = new Mock<IUserRepository>(MockBehavior.Strict);
        userRepo.Setup(s => s.IsNicknameUsed(It.IsAny<string>())).Returns(Task.FromResult(true));

        var testInstance = CreateTestService(provider, userRepo);

        // Act
        var result = await testInstance.ValidateUserName(username);

        // Assert
        result.Ok.Should().BeFalse();
        result.ErrorCode.Should().Be(ErrorCodes.Auth.UserNameAlreadyUsed);
        result.RegistrationErrorDetails.Should().NotBeNull();
        result.RegistrationErrorDetails.UsernameTaken.Should().BeTrue();
        result.RegistrationErrorDetails.UsernameLengthIncorrect.Should().BeFalse();
        result.RegistrationErrorDetails.UsernameModerationFailed.Should().BeFalse();
        result.RegistrationErrorDetails.UsernameContainsForbiddenSymbols.Should().BeFalse();
    }

    [Fact(DisplayName = "👍👍Check validate username: username contains invalid symbols")]
    public async Task ValidateUserName_UserNameContainsInvalidSymbols()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddTestAuthServices(testOut);

        await using var provider = services.BuildServiceProvider();

        const string username = "test/+$%&";

        var testInstance = CreateTestService(provider);

        // Act
        var result = await testInstance.ValidateUserName(username);

        // Assert
        result.Ok.Should().BeFalse();
        result.ErrorCode.Should().Be(ErrorCodes.Auth.UserNameContainsInvalidSymbols);
        result.RegistrationErrorDetails.Should().NotBeNull();
        result.RegistrationErrorDetails.UsernameTaken.Should().BeFalse();
        result.RegistrationErrorDetails.UsernameLengthIncorrect.Should().BeFalse();
        result.RegistrationErrorDetails.UsernameModerationFailed.Should().BeFalse();
        result.RegistrationErrorDetails.UsernameContainsForbiddenSymbols.Should().BeTrue();
    }

    [Theory(DisplayName = "👍👍Check validate username: username length invalid")]
    [InlineData("ABCDEFGHIJKLMNOPQRSTUVWXYZ")]
    [InlineData("AB")]
    public async Task ValidateUserName_UserNameLengthInvalid(string username)
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddTestAuthServices(testOut);

        await using var provider = services.BuildServiceProvider();

        var testInstance = CreateTestService(provider);

        // Act
        var result = await testInstance.ValidateUserName(username);

        // Assert
        result.Ok.Should().BeFalse();
        result.ErrorCode.Should().Be(ErrorCodes.Auth.UserNameLengthInvalid);
        result.RegistrationErrorDetails.Should().NotBeNull();
        result.RegistrationErrorDetails.UsernameTaken.Should().BeFalse();
        result.RegistrationErrorDetails.UsernameLengthIncorrect.Should().BeTrue();
        result.RegistrationErrorDetails.UsernameModerationFailed.Should().BeFalse();
        result.RegistrationErrorDetails.UsernameContainsForbiddenSymbols.Should().BeFalse();
    }

    [Fact(DisplayName = "👍👍Check validate username: username moderation failed")]
    public async Task ValidateUserName_UsernameModerationFailed()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddTestAuthServices(testOut);

        await using var provider = services.BuildServiceProvider();

        const string username = "1234567890";

        var moderationProviderApi = new Mock<IModerationProviderApi>(MockBehavior.Strict);
        moderationProviderApi.Setup(s => s.CallModerationProviderApiText(It.IsAny<string>()))
                             .Returns(Task.FromResult(new ModerationResult()));

        var testInstance = CreateTestService(provider, moderationProviderApi: moderationProviderApi);

        // Act
        var result = await testInstance.ValidateUserName(username);

        // Assert
        result.Ok.Should().BeFalse();
        result.ErrorCode.Should().Be(ErrorCodes.Auth.UsernameModerationFailed);
        result.RegistrationErrorDetails.Should().NotBeNull();
        result.RegistrationErrorDetails.UsernameTaken.Should().BeFalse();
        result.RegistrationErrorDetails.UsernameLengthIncorrect.Should().BeFalse();
        result.RegistrationErrorDetails.UsernameModerationFailed.Should().BeTrue();
        result.RegistrationErrorDetails.UsernameContainsForbiddenSymbols.Should().BeFalse();
    }

    private static CredentialValidateService CreateTestService(
        IServiceProvider provider,
        Mock<IUserRepository> userRepo = null,
        Mock<IPhoneNumberAuthService> phoneNumberAuthService = null,
        Mock<IAppleAuthService> appleAuthService = null,
        Mock<IEmailAuthService> emailAuthService = null,
        Mock<IModerationProviderApi> moderationProviderApi = null,
        Mock<IGoogleAuthService> googleAuthService = null
    )
    {
        if (userRepo == null)
        {
            userRepo = new Mock<IUserRepository>(MockBehavior.Strict);
            userRepo.Setup(s => s.IsEmailRegistered(It.IsAny<string>())).Returns(Task.FromResult(false));
            userRepo.Setup(s => s.IsPhoneRegistered(It.IsAny<string>())).Returns(Task.FromResult(false));
            userRepo.Setup(s => s.IsAppleIdRegistered(It.IsAny<string>())).Returns(Task.FromResult(false));
            userRepo.Setup(s => s.IsGoogleIdRegistered(It.IsAny<string>())).Returns(Task.FromResult(false));
            userRepo.Setup(s => s.IsNicknameUsed(It.IsAny<string>())).Returns(Task.FromResult(false));
        }

        if (phoneNumberAuthService == null)
        {
            phoneNumberAuthService = new Mock<IPhoneNumberAuthService>(MockBehavior.Strict);
            phoneNumberAuthService.Setup(s => s.FormatPhoneNumber(It.IsAny<string>())).Returns((string val) => Task.FromResult(val));
            phoneNumberAuthService.Setup(s => s.ValidateVerificationCode(It.IsAny<string>(), It.IsAny<string>()))
                                  .Returns(Task.FromResult(false));
        }

        appleAuthService ??= new Mock<IAppleAuthService>(MockBehavior.Strict);
        googleAuthService ??= new Mock<IGoogleAuthService>(MockBehavior.Strict);
        emailAuthService ??= new Mock<IEmailAuthService>(MockBehavior.Strict);

        if (moderationProviderApi == null)
        {
            moderationProviderApi = new Mock<IModerationProviderApi>(MockBehavior.Strict);
            moderationProviderApi.Setup(s => s.CallModerationProviderApiText(It.IsAny<string>()))
                                 .Returns(Task.FromResult(ModerationResult.DummyPassed));
        }

        var testInstance = new CredentialValidateService(
            appleAuthService.Object,
            emailAuthService.Object,
            googleAuthService.Object,
            phoneNumberAuthService.Object,
            moderationProviderApi.Object,
            userRepo.Object,
            provider.GetRequiredService<ILoggerFactory>()
        );

        return testInstance;
    }
}