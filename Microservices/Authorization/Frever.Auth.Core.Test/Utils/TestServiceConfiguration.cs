using System.Security.Claims;
using AuthServer.Models;
using Frever.Common.Testing;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit.Abstractions;

namespace Frever.Auth.Core.Test.Utils;

public static class TestServiceConfiguration
{
    public static void AddTestAuthServices(this IServiceCollection services, ITestOutputHelper testOut)
    {
        ArgumentNullException.ThrowIfNull(testOut);

        services.AddUnitTestServices(testOut);
        AddUserManager(services);
    }

    private static void AddUserManager(IServiceCollection services)
    {
        var userStore = new Mock<IUserStore<ApplicationUser>>();
        services.AddSingleton(userStore);

        services.AddSingleton(
            provider => new UserManager<ApplicationUser>(
                userStore.Object,
                new OptionsWrapper<IdentityOptions>(new IdentityOptions()),
                new PasswordHasher<ApplicationUser>(new OptionsWrapper<PasswordHasherOptions>(new PasswordHasherOptions())),
                Enumerable.Empty<IUserValidator<ApplicationUser>>(),
                Enumerable.Empty<IPasswordValidator<ApplicationUser>>(),
                new UpperInvariantLookupNormalizer(),
                new IdentityErrorDescriber(),
                provider,
                provider.GetRequiredService<ILogger<UserManager<ApplicationUser>>>()
            )
        );
    }

    public static Mock<UserManager<ApplicationUser>> CreateUserManager(IServiceProvider provider, bool validPassword = true)
    {
        var userStore = new Mock<IUserStore<ApplicationUser>>();

        var userManager = new Mock<UserManager<ApplicationUser>>(
            userStore.Object,
            new OptionsWrapper<IdentityOptions>(new IdentityOptions()),
            new PasswordHasher<ApplicationUser>(new OptionsWrapper<PasswordHasherOptions>(new PasswordHasherOptions())),
            Enumerable.Empty<IUserValidator<ApplicationUser>>(),
            Enumerable.Empty<IPasswordValidator<ApplicationUser>>(),
            new UpperInvariantLookupNormalizer(),
            new IdentityErrorDescriber(),
            provider,
            provider.GetRequiredService<ILogger<UserManager<ApplicationUser>>>()
        );

        userManager.Setup(s => s.CreateAsync(It.IsAny<ApplicationUser>())).ReturnsAsync(IdentityResult.Success);
        userManager.Setup(s => s.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
                   .ReturnsAsync(IdentityResult.Success);
        userManager.Setup(s => s.FindByIdAsync(It.IsAny<string>())).ReturnsAsync(new ApplicationUser {UserName = "UserName"});
        userManager.Setup(s => s.GetClaimsAsync(It.IsAny<ApplicationUser>())).ReturnsAsync(new List<Claim>());
        userManager.Setup(s => s.AddClaimsAsync(It.IsAny<ApplicationUser>(), It.IsAny<List<Claim>>()))
                   .ReturnsAsync(IdentityResult.Success);
        userManager.Setup(s => s.AddClaimAsync(It.IsAny<ApplicationUser>(), It.IsAny<Claim>()))
                   .ReturnsAsync(IdentityResult.Success);
        userManager.Setup(s => s.RemoveClaimAsync(It.IsAny<ApplicationUser>(), It.IsAny<Claim>()))
                   .ReturnsAsync(IdentityResult.Success);
        userManager.Setup(s => s.ReplaceClaimAsync(It.IsAny<ApplicationUser>(), It.IsAny<Claim>(), It.IsAny<Claim>()))
                   .ReturnsAsync(IdentityResult.Success);
        userManager.Setup(s => s.CheckPasswordAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
                   .ReturnsAsync(validPassword);
        userManager.Setup(s => s.SetUserNameAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
                   .ReturnsAsync(IdentityResult.Success);

        return userManager;
    }
}