using AuthServer.Features.CredentialUpdate.Contracts;
using AuthServer.Features.CredentialUpdate.Handlers;
using AuthServer.Features.CredentialUpdate.Models;
using AuthServer.Repositories;
using AuthServer.Services.PhoneNumberAuth;
using Common.Infrastructure;
using Common.Models;
using FluentAssertions;
using Frever.Auth.Core.Test.Utils;
using Frever.Shared.MainDb.Entities;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using MockQueryable.Moq;
using Moq;
using Xunit;
using Xunit.Abstractions;

namespace Frever.Auth.Core.Test.CredentialUpdate;

[Collection("Credential Update Service")]
public class PhoneNumberHandlerTest(ITestOutputHelper testOut)
{
    [Fact(DisplayName = "👍👍Check add credentials")]
    public async Task AddCredentials()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddTestAuthServices(testOut);

        const long groupId = 1;
        var request = new AddCredentialsRequest {PhoneNumber = "+12345", VerificationCode = "test_code"};

        await using var provider = services.BuildServiceProvider();
        var testInstance = CreateTestService(provider, user: new User());

        // Act
        var act = () => testInstance.AddCredentials(request, groupId, new CredentialStatus());

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact(DisplayName = "👍👍Check add credentials: account has phone number login method linked")]
    public async Task AddCredentials_AccountHasLoginMethod()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddTestAuthServices(testOut);

        const long groupId = 1;
        var request = new AddCredentialsRequest {PhoneNumber = "+12345", VerificationCode = "test_code"};
        var status = new CredentialStatus {PhoneNumber = request.PhoneNumber};

        await using var provider = services.BuildServiceProvider();
        var testInstance = CreateTestService(provider);

        // Act
        var act = () => testInstance.AddCredentials(request, groupId, status);

        // Assert
        await act.Should()
                 .ThrowAsync<AppErrorWithStatusCodeException>()
                 .Where(e => e.ErrorCode.Equals(ErrorCodes.Auth.AccountHasLoginMethod));
    }

    [Fact(DisplayName = "👍👍Check add credentials: phone number format invalid")]
    public async Task AddCredentials_PhoneNumberFormatInvalid()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddTestAuthServices(testOut);

        const long groupId = 1;
        var request = new AddCredentialsRequest {PhoneNumber = "+12345", VerificationCode = "test_code"};

        await using var provider = services.BuildServiceProvider();
        var testInstance = CreateTestService(provider, correctFormat: false);

        // Act
        var act = () => testInstance.AddCredentials(request, groupId, new CredentialStatus());

        // Assert
        await act.Should()
                 .ThrowAsync<AppErrorWithStatusCodeException>()
                 .Where(e => e.ErrorCode.Equals(ErrorCodes.Auth.PhoneNumberFormatInvalid));
    }

    [Fact(DisplayName = "👍👍Check add credentials: verification code invalid")]
    public async Task AddCredentials_VerificationCodeInvalid()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddTestAuthServices(testOut);

        const long groupId = 1;
        var request = new AddCredentialsRequest {PhoneNumber = "+12345", VerificationCode = "test_code"};

        await using var provider = services.BuildServiceProvider();
        var testInstance = CreateTestService(provider, false);

        // Act
        var act = () => testInstance.AddCredentials(request, groupId, new CredentialStatus());

        // Assert
        await act.Should()
                 .ThrowAsync<AppErrorWithStatusCodeException>()
                 .Where(e => e.ErrorCode.Equals(ErrorCodes.Auth.VerificationCodeInvalid));
    }

    [Fact(DisplayName = "👍👍Check add credentials: phone number is already used")]
    public async Task AddCredentials_PhoneNumberAlreadyUsed()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddTestAuthServices(testOut);

        const long groupId = 1;
        var request = new AddCredentialsRequest {PhoneNumber = "+12345", VerificationCode = "test_code"};

        await using var provider = services.BuildServiceProvider();
        var testInstance = CreateTestService(provider, isRegistered: true);

        // Act
        var act = () => testInstance.AddCredentials(request, groupId, new CredentialStatus());

        // Assert
        await act.Should()
                 .ThrowAsync<AppErrorWithStatusCodeException>()
                 .Where(e => e.ErrorCode.Equals(ErrorCodes.Auth.PhoneNumberAlreadyUsed));
    }

    [Fact(DisplayName = "👍👍Check validate credentials")]
    public async Task ValidateCredential()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddTestAuthServices(testOut);

        var userInfo = new ShortUserInfo {PhoneNumber = "+12345"};
        var request = new VerifyUserRequest {VerificationCode = "test_code"};

        await using var provider = services.BuildServiceProvider();
        var testInstance = CreateTestService(provider);

        // Act
        var act = await testInstance.ValidateCurrentCredential(request, userInfo);

        // Assert
        act.IsValid.Should().BeTrue();
        act.ErrorCode.Should().BeNull();
    }

    [Fact(DisplayName = "👍👍Check validate credentials: verification code invalid")]
    public async Task ValidateCredential_VerificationCodeInvalid()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddTestAuthServices(testOut);

        var userInfo = new ShortUserInfo {PhoneNumber = "+12345"};
        var request = new VerifyUserRequest {VerificationCode = "test_code"};

        await using var provider = services.BuildServiceProvider();
        var testInstance = CreateTestService(provider, false);

        // Act
        var act = await testInstance.ValidateCurrentCredential(request, userInfo);

        // Assert
        act.IsValid.Should().BeFalse();
        act.ErrorCode.Should().Be(ErrorCodes.Auth.VerificationCodeInvalid);
    }

    [Fact(DisplayName = "👍👍Check update credentials")]
    public async Task UpdateCredentials()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddTestAuthServices(testOut);

        const long groupId = 1;
        var request = new UpdateCredentialsRequest {PhoneNumber = "+12345", VerificationCode = "test_code"};

        await using var provider = services.BuildServiceProvider();
        var testInstance = CreateTestService(provider, user: new User());

        // Act
        var act = () => testInstance.UpdateCredentials(request, groupId, new CredentialStatus());

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact(DisplayName = "👍👍Check update credentials: account last login method")]
    public async Task UpdateCredentials_AccountLastLoginMethod()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddTestAuthServices(testOut);

        const long groupId = 1;
        var request = new UpdateCredentialsRequest {VerificationCode = "test_code"};

        await using var provider = services.BuildServiceProvider();
        var testInstance = CreateTestService(provider);

        // Act
        var act = () => testInstance.UpdateCredentials(request, groupId, new CredentialStatus());

        // Assert
        await act.Should()
                 .ThrowAsync<AppErrorWithStatusCodeException>()
                 .Where(e => e.ErrorCode.Equals(ErrorCodes.Auth.AccountLastLoginMethod));
    }

    [Fact(DisplayName = "👍👍Check update credentials: phone number format invalid")]
    public async Task UpdateCredentials_PhoneNumberFormatInvalid()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddTestAuthServices(testOut);

        const long groupId = 1;
        var request = new UpdateCredentialsRequest {PhoneNumber = "+12345", VerificationCode = "test_code"};

        await using var provider = services.BuildServiceProvider();
        var testInstance = CreateTestService(provider, correctFormat: false);

        // Act
        var act = () => testInstance.UpdateCredentials(request, groupId, new CredentialStatus());

        // Assert
        await act.Should()
                 .ThrowAsync<AppErrorWithStatusCodeException>()
                 .Where(e => e.ErrorCode.Equals(ErrorCodes.Auth.PhoneNumberFormatInvalid));
    }

    [Fact(DisplayName = "👍👍Check update credentials: verification code invalid")]
    public async Task UpdateCredentials_VerificationCodeInvalid()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddTestAuthServices(testOut);

        const long groupId = 1;
        var request = new UpdateCredentialsRequest {PhoneNumber = "+12345", VerificationCode = "test_code"};

        await using var provider = services.BuildServiceProvider();
        var testInstance = CreateTestService(provider, false);

        // Act
        var act = () => testInstance.UpdateCredentials(request, groupId, new CredentialStatus());

        // Assert
        await act.Should()
                 .ThrowAsync<AppErrorWithStatusCodeException>()
                 .Where(e => e.ErrorCode.Equals(ErrorCodes.Auth.VerificationCodeInvalid));
    }

    [Fact(DisplayName = "👍👍Check update credentials: phone number already used")]
    public async Task UpdateCredentials_PhoneNumberAlreadyUsed()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddTestAuthServices(testOut);

        const long groupId = 1;
        var request = new UpdateCredentialsRequest {PhoneNumber = "+12345", VerificationCode = "test_code"};

        await using var provider = services.BuildServiceProvider();
        var testInstance = CreateTestService(provider, isRegistered: true);

        // Act
        var act = () => testInstance.UpdateCredentials(request, groupId, new CredentialStatus());

        // Assert
        await act.Should()
                 .ThrowAsync<AppErrorWithStatusCodeException>()
                 .Where(e => e.ErrorCode.Equals(ErrorCodes.Auth.PhoneNumberAlreadyUsed));
    }

    private static PhoneNumberHandler CreateTestService(
        IServiceProvider provider,
        bool verificationCodeValid = true,
        bool isRegistered = false,
        bool correctFormat = true,
        User user = null
    )
    {
        var phoneNumberAuthService = new Mock<IPhoneNumberAuthService>(MockBehavior.Strict);
        phoneNumberAuthService.Setup(s => s.FormatPhoneNumber(It.IsAny<string>())).ReturnsAsync(correctFormat ? "" : null);
        phoneNumberAuthService.Setup(s => s.ValidateVerificationCode(It.IsAny<string>(), It.IsAny<string>()))
                              .ReturnsAsync(verificationCodeValid);

        var dbTransactionMock = new Mock<IDbContextTransaction>();

        if (user != null)
            user.MainGroup = new Group();

        var userRepo = new Mock<IUserRepository>(MockBehavior.Strict);
        userRepo.Setup(s => s.GetUserByGroupId(It.IsAny<long>())).Returns(new List<User> {user}.BuildMock());
        userRepo.Setup(s => s.IsPhoneRegistered(It.IsAny<string>())).ReturnsAsync(isRegistered);
        userRepo.Setup(s => s.SaveChanges()).Returns(Task.CompletedTask);
        userRepo.Setup(s => s.BeginMainDbTransactionAsync()).ReturnsAsync(dbTransactionMock.Object);
        userRepo.Setup(s => s.BeginAuthDbTransactionAsync()).ReturnsAsync(dbTransactionMock.Object);

        var userManager = TestServiceConfiguration.CreateUserManager(provider);

        var testInstance = new PhoneNumberHandler(userRepo.Object, phoneNumberAuthService.Object, userManager.Object);

        return testInstance;
    }
}