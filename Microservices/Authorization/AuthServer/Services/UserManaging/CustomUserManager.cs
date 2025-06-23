using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AuthServer.Models;
using AuthServer.Repositories;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AuthServer.Services.UserManaging;

public class CustomUserManager(
    IUserStore<ApplicationUser> store,
    IOptions<IdentityOptions> optionsAccessor,
    IPasswordHasher<ApplicationUser> passwordHasher,
    IEnumerable<IUserValidator<ApplicationUser>> userValidators,
    IEnumerable<IPasswordValidator<ApplicationUser>> passwordValidators,
    ILookupNormalizer keyNormalizer,
    IdentityErrorDescriber errors,
    IServiceProvider services,
    ILogger<UserManager<ApplicationUser>> logger,
    IUserRepository userRepository
) : UserManager<ApplicationUser>(
    store,
    optionsAccessor,
    passwordHasher,
    userValidators,
    passwordValidators,
    keyNormalizer,
    errors,
    services,
    logger
)
{
    private readonly IUserRepository _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));

    public override async Task<ApplicationUser> FindByNameAsync(string userName)
    {
        var identityUser = await base.FindByNameAsync(userName);
        if (identityUser == null)
            return null;

        var isBlocked = await _userRepository.IsGroupBlockedForAuthUser(identityUser.Id);

        return isBlocked ? null : identityUser;
    }
}