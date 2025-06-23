using AuthServer.Contracts;
using AuthServer.Features.CredentialUpdate.Contracts;
using AuthServer.Features.CredentialUpdate.Handlers;
using AuthServer.Features.CredentialUpdate.Models;
using AuthServer.Models;
using AuthServer.Repositories;
using AuthServer.Services.UserManaging;
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
public class PasswordHandlerTest(ITestOutputHelper testOut)
{
    [Fact(DisplayName = "👍👍Check add credentials")]
    public async Task AddCredentials()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddTestAuthServices(testOut);

        const long groupId = 1;
        var request = new AddCredentialsRequest {Password = "test"};

        await using var provider = services.BuildServiceProvider();
        var testInstance = CreateTestService(provider);

        // Act
        var act = () => testInstance.AddCredentials(request, groupId, new CredentialStatus());

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact(DisplayName = "👍👍Check add credentials: account has password login method linked")]
    public async Task AddCredentials_AccountHasLoginMethod()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddTestAuthServices(testOut);

        const long groupId = 1;
        var request = new AddCredentialsRequest {Password = "test"};
        var status = new CredentialStatus {HasPassword = true};

        await using var provider = services.BuildServiceProvider();
        var testInstance = CreateTestService(provider);

        // Act
        var act = () => testInstance.AddCredentials(request, groupId, status);

        // Assert
        await act.Should()
                 .ThrowAsync<AppErrorWithStatusCodeException>()
                 .Where(e => e.ErrorCode.Equals(ErrorCodes.Auth.AccountHasLoginMethod));
    }

    [Fact(DisplayName = "👍👍Check add credentials: password invalid")]
    public async Task AddCredentials_PasswordInvalid()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddTestAuthServices(testOut);

        const long groupId = 1;
        var request = new AddCredentialsRequest {Password = "test"};

        await using var provider = services.BuildServiceProvider();
        var testInstance = CreateTestService(provider, passwordInvalid: true);

        // Act
        var act = () => testInstance.AddCredentials(request, groupId, new CredentialStatus());

        // Assert
        await act.Should().ThrowAsync<AppErrorWithStatusCodeException>().Where(e => e.ErrorCode.Equals(ErrorCodes.Auth.PasswordInvalid));
    }

    [Fact(DisplayName = "👍👍Check add credentials: password already exist")]
    public async Task AddCredentials_PasswordAlreadyExist()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddTestAuthServices(testOut);

        const long groupId = 1;
        var request = new AddCredentialsRequest {Password = "test"};

        await using var provider = services.BuildServiceProvider();
        var testInstance = CreateTestService(provider, true);

        // Act
        var act = () => testInstance.AddCredentials(request, groupId, new CredentialStatus());

        // Assert
        await act.Should()
                 .ThrowAsync<AppErrorWithStatusCodeException>()
                 .Where(e => e.ErrorCode.Equals(ErrorCodes.Auth.PasswordAlreadyExist));
    }

    [Fact(DisplayName = "👍👍Check validate credentials")]
    public async Task ValidateCredential()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddTestAuthServices(testOut);

        var request = new VerifyUserRequest {Password = "test"};

        await using var provider = services.BuildServiceProvider();
        var testInstance = CreateTestService(provider, true);

        // Act
        var act = await testInstance.ValidateCurrentCredential(request, new ShortUserInfo());

        // Assert
        act.IsValid.Should().BeTrue();
        act.ErrorCode.Should().BeNull();
    }

    [Fact(DisplayName = "👍👍Check validate credentials: password invalid")]
    public async Task ValidateCredential_PasswordInvalid()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddTestAuthServices(testOut);

        var request = new VerifyUserRequest {Password = "test"};

        await using var provider = services.BuildServiceProvider();
        var testInstance = CreateTestService(provider);

        // Act
        var act = await testInstance.ValidateCurrentCredential(request, new ShortUserInfo());

        // Assert
        act.IsValid.Should().BeFalse();
        act.ErrorCode.Should().Be(ErrorCodes.Auth.PasswordInvalid);
    }

    [Fact(DisplayName = "👍👍Check update credentials")]
    public async Task UpdateCredentials()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddTestAuthServices(testOut);

        var request = new UpdateCredentialsRequest {Password = "test"};

        await using var provider = services.BuildServiceProvider();
        var testInstance = CreateTestService(provider);

        // Act
        var act = () => testInstance.UpdateCredentials(request, 1, new CredentialStatus());

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact(DisplayName = "👍👍Check update credentials: password invalid")]
    public async Task UpdateCredentials_PasswordInvalid()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddTestAuthServices(testOut);

        const long groupId = 1;
        var request = new UpdateCredentialsRequest {Password = "test"};

        await using var provider = services.BuildServiceProvider();
        var testInstance = CreateTestService(provider, passwordInvalid: true);

        // Act
        var act = () => testInstance.UpdateCredentials(request, groupId, new CredentialStatus());

        // Assert
        await act.Should().ThrowAsync<AppErrorWithStatusCodeException>().Where(e => e.ErrorCode.Equals(ErrorCodes.Auth.PasswordInvalid));
    }

    [Fact(DisplayName = "👍👍Check update credentials: password already exist")]
    public async Task UpdateCredentials_PasswordAlreadyExist()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddTestAuthServices(testOut);

        const long groupId = 1;
        var request = new UpdateCredentialsRequest {Password = "test"};

        await using var provider = services.BuildServiceProvider();
        var testInstance = CreateTestService(provider, true);

        // Act
        var act = () => testInstance.UpdateCredentials(request, groupId, new CredentialStatus());

        // Assert
        await act.Should()
                 .ThrowAsync<AppErrorWithStatusCodeException>()
                 .Where(e => e.ErrorCode.Equals(ErrorCodes.Auth.PasswordAlreadyExist));
    }

    private static PasswordHandler CreateTestService(
        IServiceProvider provider,
        bool isPasswordUsed = false,
        bool applicationUserExists = true,
        bool passwordInvalid = false
    )
    {
        var userRepo = new Mock<IUserRepository>(MockBehavior.Strict);
        var dbUser = new User {MainGroup = new Group()};
        userRepo.Setup(s => s.GetUserByGroupId(It.IsAny<long>())).Returns(new List<User> {dbUser}.BuildMock());
        userRepo.Setup(s => s.SaveChanges()).Returns(Task.CompletedTask);

        var user = applicationUserExists ? new ApplicationUser() : null;

        var userManager = TestServiceConfiguration.CreateUserManager(provider);
        userManager.Setup(s => s.FindByIdAsync(It.IsAny<string>())).Returns(Task.FromResult(user));
        userManager.Setup(e => e.CheckPasswordAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
                   .Returns(Task.FromResult(isPasswordUsed));

        var credentialValidateService = new Mock<ICredentialValidateService>();
        var validationResult = new ValidatePasswordResult {Ok = !passwordInvalid};
        credentialValidateService.Setup(s => s.ValidatePassword(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(validationResult);

        var testInstance = new PasswordHandler(userRepo.Object, userManager.Object, credentialValidateService.Object);

        return testInstance;
    }
}