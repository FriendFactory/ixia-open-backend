using AuthServer.Features.CredentialUpdate.Contracts;
using AuthServer.Features.CredentialUpdate.Handlers;
using AuthServer.Features.CredentialUpdate.Models;
using AuthServer.Repositories;
using AuthServer.Services.AppleAuth;
using Common.Infrastructure;
using Common.Models;
using FluentAssertions;
using Frever.Auth.Core.Test.Utils;
using Frever.Shared.MainDb.Entities;
using Microsoft.Extensions.DependencyInjection;
using MockQueryable.Moq;
using Moq;
using Xunit;
using Xunit.Abstractions;

namespace Frever.Auth.Core.Test.CredentialUpdate;

[Collection("Credential Update Service")]
public class AppleIdHandlerTest(ITestOutputHelper testOut)
{
    [Fact(DisplayName = "👍👍Check add credentials")]
    public async Task AddCredentials()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddTestAuthServices(testOut);

        const long groupId = 1;
        var request = new AddCredentialsRequest {AppleId = "test", AppleIdentityToken = "test"};

        var testInstance = CreateTestService(request.AppleId);

        // Act
        var act = () => testInstance.AddCredentials(request, groupId, new CredentialStatus());

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact(DisplayName = "👍👍Check add credentials: account has apple login method linked")]
    public async Task AddCredentials_AccountHasLoginMethod()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddTestAuthServices(testOut);

        const long groupId = 1;
        var request = new AddCredentialsRequest {AppleId = "test", AppleIdentityToken = "test"};
        var status = new CredentialStatus {HasAppleId = true};

        var testInstance = CreateTestService();

        // Act
        var act = () => testInstance.AddCredentials(request, groupId, status);

        // Assert
        await act.Should()
                 .ThrowAsync<AppErrorWithStatusCodeException>()
                 .Where(e => e.ErrorCode.Equals(ErrorCodes.Auth.AccountHasLoginMethod));
    }

    [Fact(DisplayName = "👍👍Check add credentials: apple token invalid")]
    public async Task AddCredentials_AppleTokenInvalid()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddTestAuthServices(testOut);

        const long groupId = 1;
        var request = new AddCredentialsRequest {AppleId = "test", AppleIdentityToken = "test"};

        var testInstance = CreateTestService();

        // Act
        var act = () => testInstance.AddCredentials(request, groupId, new CredentialStatus());

        // Assert
        await act.Should().ThrowAsync<AppErrorWithStatusCodeException>().Where(e => e.ErrorCode.Equals(ErrorCodes.Auth.AppleTokenInvalid));
    }

    [Fact(DisplayName = "👍👍Check add credentials: apple is already used")]
    public async Task AddCredentials_AppleIdAlreadyUsed()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddTestAuthServices(testOut);

        const long groupId = 1;
        var request = new AddCredentialsRequest {AppleId = "test", AppleIdentityToken = "test"};

        var testInstance = CreateTestService(request.AppleId, true);

        // Act
        var act = () => testInstance.AddCredentials(request, groupId, new CredentialStatus());

        // Assert
        await act.Should().ThrowAsync<AppErrorWithStatusCodeException>().Where(e => e.ErrorCode.Equals(ErrorCodes.Auth.AppleIdAlreadyUsed));
    }

    [Fact(DisplayName = "👍👍Check validate credentials")]
    public async Task ValidateCredential()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddTestAuthServices(testOut);

        var userInfo = new ShortUserInfo {AppleId = "test"};
        var request = new VerifyUserRequest {AppleIdentityToken = "test"};

        var testInstance = CreateTestService(userInfo.AppleId);

        // Act
        var act = await testInstance.ValidateCurrentCredential(request, userInfo);

        // Assert
        act.IsValid.Should().BeTrue();
        act.ErrorCode.Should().BeNull();
    }

    [Fact(DisplayName = "👍👍Check validate credentials: apple token invalid")]
    public async Task ValidateCredential_AppleTokenInvalid()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddTestAuthServices(testOut);

        var userInfo = new ShortUserInfo {AppleId = "test"};
        var request = new VerifyUserRequest {AppleIdentityToken = "test"};

        var testInstance = CreateTestService();

        // Act
        var act = await testInstance.ValidateCurrentCredential(request, userInfo);

        // Assert
        act.IsValid.Should().BeFalse();
        act.ErrorCode.Should().Be(ErrorCodes.Auth.AppleTokenInvalid);
    }

    [Fact(DisplayName = "👍👍Check update credentials")]
    public async Task UpdateCredentials()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddTestAuthServices(testOut);

        var testInstance = CreateTestService();

        // Act
        var act = () => testInstance.UpdateCredentials(new UpdateCredentialsRequest(), 1, new CredentialStatus());

        // Assert
        await act.Should().ThrowAsync<NotImplementedException>();
    }

    private static AppleIdHandler CreateTestService(string appleId = null, bool isRegistered = false)
    {
        var appleAuthService = new Mock<IAppleAuthService>(MockBehavior.Strict);
        appleAuthService.Setup(e => e.ValidateAuthTokenAsync(It.IsAny<string>())).ReturnsAsync(appleId);

        var userRepo = new Mock<IUserRepository>(MockBehavior.Strict);
        var user = new User {MainGroup = new Group()};
        userRepo.Setup(s => s.GetUserByGroupId(It.IsAny<long>())).Returns(new List<User> {user}.BuildMock());
        userRepo.Setup(s => s.IsAppleIdRegistered(It.IsAny<string>())).ReturnsAsync(isRegistered);
        userRepo.Setup(s => s.SaveChanges()).Returns(Task.CompletedTask);

        var testInstance = new AppleIdHandler(userRepo.Object, appleAuthService.Object);

        return testInstance;
    }
}