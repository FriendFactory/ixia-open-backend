using AuthServer.Features.CredentialUpdate.Contracts;
using AuthServer.Features.CredentialUpdate.Handlers;
using AuthServer.Features.CredentialUpdate.Models;
using AuthServer.Repositories;
using AuthServer.Services.GoogleAuth;
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
public class GoogleIdHandlerTest(ITestOutputHelper testOut)
{
    [Fact(DisplayName = "👍👍Check add credentials")]
    public async Task AddCredentials()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddTestAuthServices(testOut);

        const long groupId = 1;
        var request = new AddCredentialsRequest {GoogleId = "test", IdentityToken = "test"};

        var testInstance = CreateTestService(request.GoogleId);

        // Act
        var act = () => testInstance.AddCredentials(request, groupId, new CredentialStatus());

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact(DisplayName = "👍👍Check add credentials: account has google login method linked")]
    public async Task AddCredentials_AccountHasLoginMethod()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddTestAuthServices(testOut);

        const long groupId = 1;
        var request = new AddCredentialsRequest {GoogleId = "test", IdentityToken = "test"};
        var status = new CredentialStatus {HasGoogleId = true};

        var testInstance = CreateTestService();

        // Act
        var act = () => testInstance.AddCredentials(request, groupId, status);

        // Assert
        await act.Should()
                 .ThrowAsync<AppErrorWithStatusCodeException>()
                 .Where(e => e.ErrorCode.Equals(ErrorCodes.Auth.AccountHasLoginMethod));
    }

    [Fact(DisplayName = "👍👍Check add credentials: google token invalid")]
    public async Task AddCredentials_GoogleTokenInvalid()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddTestAuthServices(testOut);

        const long groupId = 1;
        var request = new AddCredentialsRequest {GoogleId = "test", IdentityToken = "test"};

        var testInstance = CreateTestService();

        // Act
        var act = () => testInstance.AddCredentials(request, groupId, new CredentialStatus());

        // Assert
        await act.Should().ThrowAsync<AppErrorWithStatusCodeException>().Where(e => e.ErrorCode.Equals(ErrorCodes.Auth.GoogleTokenInvalid));
    }

    [Fact(DisplayName = "👍👍Check add credentials: google is already used")]
    public async Task AddCredentials_GoogleIdAlreadyUsed()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddTestAuthServices(testOut);

        const long groupId = 1;
        var request = new AddCredentialsRequest {GoogleId = "test", IdentityToken = "test"};

        var testInstance = CreateTestService(request.GoogleId, true);

        // Act
        var act = () => testInstance.AddCredentials(request, groupId, new CredentialStatus());

        // Assert
        await act.Should()
                 .ThrowAsync<AppErrorWithStatusCodeException>()
                 .Where(e => e.ErrorCode.Equals(ErrorCodes.Auth.GoogleIdAlreadyUsed));
    }

    [Fact(DisplayName = "👍👍Check validate credentials")]
    public async Task ValidateCredential()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddTestAuthServices(testOut);

        var userInfo = new ShortUserInfo {GoogleId = "test"};
        var request = new VerifyUserRequest {IdentityToken = "test"};

        var testInstance = CreateTestService(userInfo.GoogleId);

        // Act
        var act = await testInstance.ValidateCurrentCredential(request, userInfo);

        // Assert
        act.IsValid.Should().BeTrue();
        act.ErrorCode.Should().BeNull();
    }

    [Fact(DisplayName = "👍👍Check validate credentials: google token invalid")]
    public async Task ValidateCredential_GoogleTokenInvalid()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddTestAuthServices(testOut);

        var userInfo = new ShortUserInfo {GoogleId = "test"};
        var request = new VerifyUserRequest {IdentityToken = "test"};

        var testInstance = CreateTestService();

        // Act
        var act = await testInstance.ValidateCurrentCredential(request, userInfo);

        // Assert
        act.IsValid.Should().BeFalse();
        act.ErrorCode.Should().Be(ErrorCodes.Auth.GoogleTokenInvalid);
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

    private static GoogleIdHandler CreateTestService(string googleId = null, bool isRegistered = false)
    {
        var googleAuthService = new Mock<IGoogleAuthService>(MockBehavior.Strict);
        googleAuthService.Setup(e => e.ValidateAuthTokenAsync(It.IsAny<string>())).ReturnsAsync(googleId);

        var userRepo = new Mock<IUserRepository>(MockBehavior.Strict);
        var user = new User {MainGroup = new Group()};
        userRepo.Setup(s => s.GetUserByGroupId(It.IsAny<long>())).Returns(new List<User> {user}.BuildMock());
        userRepo.Setup(s => s.IsGoogleIdRegistered(It.IsAny<string>())).ReturnsAsync(isRegistered);
        userRepo.Setup(s => s.SaveChanges()).Returns(Task.CompletedTask);

        var testInstance = new GoogleIdHandler(userRepo.Object, googleAuthService.Object);

        return testInstance;
    }
}