using AuthServer.Quickstart.Account;
using AuthServer.Repositories;
using AuthServer.Services.AppleAuth;
using AuthServer.Services.EmailAuth;
using AuthServer.Services.GoogleAuth;
using AuthServer.Services.PhoneNumberAuth;
using Common.Models;
using FluentAssertions;
using Frever.Auth.Core.Test.Utils;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace Frever.Auth.Core.Test.CredentialValidate;

public partial class CredentialValidateServiceTest
{
    [Theory(DisplayName = "👍👍Check validate new account data: minor credentials invalid")]
    [InlineData("test@email.com", null, null, null)]
    [InlineData(null, "+3123455555", null, null)]
    [InlineData(null, null, "test_apple_id", null)]
    [InlineData(null, null, null, "test_google_id")]
    public async Task ValidateNewAccountData_MinorCredentialsInvalid(string email, string phoneNumber, string appleId, string googleId)
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddTestAuthServices(testOut);

        await using var provider = services.BuildServiceProvider();

        var testInstance = CreateTestService(provider);

        var model = new RegisterUserViewModel
                    {
                        Email = email,
                        PhoneNumber = phoneNumber,
                        AppleId = appleId,
                        GoogleId = googleId
                    };

        // Act
        var result = await testInstance.ValidateNewAccountData(model, true);

        // Assert
        result.Ok.Should().BeFalse();
        result.ErrorCode.Should().Be(ErrorCodes.Auth.MinorCredentialsInvalid);
    }

    [Fact(DisplayName = "👍👍Check validate new account data: password empty: for minor user")]
    public async Task ValidateNewAccountData_PasswordEmpty_ForMinorUser()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddTestAuthServices(testOut);

        await using var provider = services.BuildServiceProvider();

        var testInstance = CreateTestService(provider);

        // Act
        var result = await testInstance.ValidateNewAccountData(new RegisterUserViewModel(), true);

        // Assert
        result.Ok.Should().BeFalse();
        result.ErrorCode.Should().Be(ErrorCodes.Auth.PasswordEmpty);
    }

    [Fact(DisplayName = "👍👍Check validate new account data: password matches username: for minor user")]
    public async Task ValidateNewAccountData_PasswordMatchesUsername_ForMinorUser()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddTestAuthServices(testOut);

        await using var provider = services.BuildServiceProvider();

        var model = new RegisterUserViewModel {UserName = "test", Password = "test"};

        var testInstance = CreateTestService(provider);

        // Act
        var result = await testInstance.ValidateNewAccountData(model, true);

        // Assert
        result.Ok.Should().BeFalse();
        result.ErrorCode.Should().Be(ErrorCodes.Auth.PasswordMatchesUsername);
    }

    [Fact(DisplayName = "👍👍Check validate new account data: password or token required: for email")]
    public async Task ValidateNewAccountData_PasswordOrTokenRequired_ForEmail()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddTestAuthServices(testOut);

        await using var provider = services.BuildServiceProvider();

        var model = new RegisterUserViewModel {Email = "test@email.com"};

        var testInstance = CreateTestService(provider);

        // Act
        var result = await testInstance.ValidateNewAccountData(model, false);

        // Assert
        result.Ok.Should().BeFalse();
        result.ErrorCode.Should().Be(ErrorCodes.Auth.PasswordOrTokenRequired);
    }

    [Fact(DisplayName = "👍👍Check validate new account data: email already used: for email")]
    public async Task ValidateNewAccountData_EmailAlreadyUsed_ForEmail()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddTestAuthServices(testOut);

        await using var provider = services.BuildServiceProvider();

        var model = new RegisterUserViewModel {Email = "test@email.com", VerificationCode = "12345"};

        var userRepo = new Mock<IUserRepository>(MockBehavior.Strict);
        userRepo.Setup(s => s.IsEmailRegistered(It.IsAny<string>())).Returns(Task.FromResult(true));

        var testInstance = CreateTestService(provider, userRepo);

        // Act
        var result = await testInstance.ValidateNewAccountData(model, false);

        // Assert
        result.Ok.Should().BeFalse();
        result.ErrorCode.Should().Be(ErrorCodes.Auth.EmailAlreadyUsed);
    }

    [Fact(DisplayName = "👍👍Check validate new account data: verification code invalid: for email")]
    public async Task ValidateNewAccountData_VerificationCodeInvalid_ForEmail()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddTestAuthServices(testOut);

        await using var provider = services.BuildServiceProvider();

        var model = new RegisterUserViewModel {Email = "test@email.com", VerificationCode = "12345"};

        var emailAuthService = new Mock<IEmailAuthService>(MockBehavior.Strict);
        emailAuthService.Setup(s => s.ValidateVerificationCode(It.IsAny<string>(), It.IsAny<string>())).Returns(Task.FromResult(false));

        var testInstance = CreateTestService(provider, emailAuthService: emailAuthService);

        // Act
        var result = await testInstance.ValidateNewAccountData(model, false);

        // Assert
        result.Ok.Should().BeFalse();
        result.ErrorCode.Should().Be(ErrorCodes.Auth.VerificationCodeInvalid);
    }

    [Fact(DisplayName = "👍👍Check validate new account data: verification code empty: for phone number")]
    public async Task ValidateNewAccountData_VerificationCodeEmpty_ForPhoneNumber()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddTestAuthServices(testOut);

        await using var provider = services.BuildServiceProvider();

        var model = new RegisterUserViewModel {PhoneNumber = "+3123455555"};

        var testInstance = CreateTestService(provider);

        // Act
        var result = await testInstance.ValidateNewAccountData(model, false);

        // Assert
        result.Ok.Should().BeFalse();
        result.ErrorCode.Should().Be(ErrorCodes.Auth.VerificationCodeEmpty);
    }

    [Fact(DisplayName = "👍👍Check validate new account data: phone number format invalid: for phone number")]
    public async Task ValidateNewAccountData_PhoneNumberFormatInvalid_ForPhoneNumber()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddTestAuthServices(testOut);

        await using var provider = services.BuildServiceProvider();

        var model = new RegisterUserViewModel {PhoneNumber = "+3123455555", VerificationCode = "12345"};

        var phoneNumberAuthService = new Mock<IPhoneNumberAuthService>(MockBehavior.Strict);
        phoneNumberAuthService.Setup(s => s.FormatPhoneNumber(model.PhoneNumber)).Returns(Task.FromResult<string>(null));

        var testInstance = CreateTestService(provider, phoneNumberAuthService: phoneNumberAuthService);

        // Act
        var result = await testInstance.ValidateNewAccountData(model, false);

        // Assert
        result.Ok.Should().BeFalse();
        result.ErrorCode.Should().Be(ErrorCodes.Auth.PhoneNumberFormatInvalid);
    }

    [Fact(DisplayName = "👍👍Check validate new account data: phone number already used: for phone number")]
    public async Task ValidateNewAccountData_PhoneNumberAlreadyUsed_ForPhoneNumber()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddTestAuthServices(testOut);

        await using var provider = services.BuildServiceProvider();

        var model = new RegisterUserViewModel {PhoneNumber = "+3123455555", VerificationCode = "12345"};

        var userRepo = new Mock<IUserRepository>(MockBehavior.Strict);
        userRepo.Setup(s => s.IsPhoneRegistered(It.IsAny<string>())).Returns(Task.FromResult(true));

        var testInstance = CreateTestService(provider, userRepo);

        // Act
        var result = await testInstance.ValidateNewAccountData(model, false);

        // Assert
        result.Ok.Should().BeFalse();
        result.ErrorCode.Should().Be(ErrorCodes.Auth.PhoneNumberAlreadyUsed);
    }

    [Fact(DisplayName = "👍👍Check validate new account data: verification code invalid: for phone number")]
    public async Task ValidateNewAccountData_VerificationCodeInvalid_ForPhoneNumber()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddTestAuthServices(testOut);

        await using var provider = services.BuildServiceProvider();

        var model = new RegisterUserViewModel {PhoneNumber = "+3123455555", VerificationCode = "12345"};

        var emailAuthService = new Mock<IEmailAuthService>(MockBehavior.Strict);
        emailAuthService.Setup(s => s.ValidateVerificationCode(It.IsAny<string>(), It.IsAny<string>())).Returns(Task.FromResult(false));

        var testInstance = CreateTestService(provider, emailAuthService: emailAuthService);

        // Act
        var result = await testInstance.ValidateNewAccountData(model, false);

        // Assert
        result.Ok.Should().BeFalse();
        result.ErrorCode.Should().Be(ErrorCodes.Auth.VerificationCodeInvalid);
    }

    [Fact(DisplayName = "👍👍Check validate new account data: apple id already used: for apple id")]
    public async Task ValidateNewAccountData_AppleIdAlreadyUsed_ForAppleId()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddTestAuthServices(testOut);

        await using var provider = services.BuildServiceProvider();

        var model = new RegisterUserViewModel {AppleId = "test_apple_id", AppleIdentityToken = "test_token"};

        var userRepo = new Mock<IUserRepository>(MockBehavior.Strict);
        userRepo.Setup(s => s.IsAppleIdRegistered(It.IsAny<string>())).Returns(Task.FromResult(true));

        var testInstance = CreateTestService(provider, userRepo);

        // Act
        var result = await testInstance.ValidateNewAccountData(model, false);

        // Assert
        result.Ok.Should().BeFalse();
        result.ErrorCode.Should().Be(ErrorCodes.Auth.AppleIdAlreadyUsed);
    }

    [Fact(DisplayName = "👍👍Check validate new account data: apple token invalid: for apple id")]
    public async Task ValidateNewAccountData_AppleTokenInvalid_ForAppleId()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddTestAuthServices(testOut);

        await using var provider = services.BuildServiceProvider();

        var model = new RegisterUserViewModel {AppleId = "test_apple_id", AppleIdentityToken = "test_token"};

        var appleAuthService = new Mock<IAppleAuthService>(MockBehavior.Strict);
        appleAuthService.Setup(s => s.ValidateAuthTokenAsync(It.IsAny<string>())).Returns(Task.FromResult<string>(null));

        var testInstance = CreateTestService(provider, appleAuthService: appleAuthService);

        // Act
        var result = await testInstance.ValidateNewAccountData(model, false);

        // Assert
        result.Ok.Should().BeFalse();
        result.ErrorCode.Should().Be(ErrorCodes.Auth.AppleTokenInvalid);
    }

    [Fact(DisplayName = "👍👍Check validate new account data: google id already used: for google id")]
    public async Task ValidateNewAccountData_GoogleIdAlreadyUsed_ForGoogleId()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddTestAuthServices(testOut);

        await using var provider = services.BuildServiceProvider();

        var model = new RegisterUserViewModel {GoogleId = "test_google_id", AppleIdentityToken = "test_token"};

        var userRepo = new Mock<IUserRepository>(MockBehavior.Strict);
        userRepo.Setup(s => s.IsGoogleIdRegistered(It.IsAny<string>())).Returns(Task.FromResult(true));

        var testInstance = CreateTestService(provider, userRepo);

        // Act
        var result = await testInstance.ValidateNewAccountData(model, false);

        // Assert
        result.Ok.Should().BeFalse();
        result.ErrorCode.Should().Be(ErrorCodes.Auth.GoogleIdAlreadyUsed);
    }

    [Fact(DisplayName = "👍👍Check validate new account data: google token invalid: for google id")]
    public async Task ValidateNewAccountData_GoogleTokenInvalid_ForGoogleId()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddTestAuthServices(testOut);

        await using var provider = services.BuildServiceProvider();

        var model = new RegisterUserViewModel {GoogleId = "test_google_id", AppleIdentityToken = "test_token"};

        var googleAuthService = new Mock<IGoogleAuthService>(MockBehavior.Strict);
        googleAuthService.Setup(s => s.ValidateAuthTokenAsync(It.IsAny<string>())).Returns(Task.FromResult<string>(null));

        var testInstance = CreateTestService(provider, googleAuthService: googleAuthService);

        // Act
        var result = await testInstance.ValidateNewAccountData(model, false);

        // Assert
        result.Ok.Should().BeFalse();
        result.ErrorCode.Should().Be(ErrorCodes.Auth.GoogleTokenInvalid);
    }
}