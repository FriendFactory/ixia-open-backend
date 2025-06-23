using System;
using System.Threading.Tasks;
using AuthServer.Models;
using IdentityServer4.Models;
using IdentityServer4.Validation;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace AuthServer.Services.PasswordAuth;

public class ResourceOwnerPasswordValidator(UserManager<ApplicationUser> userManager, ILogger<ResourceOwnerPasswordValidator> logger)
    : IResourceOwnerPasswordValidator
{
    private static readonly MemoryCache UserNameToPasswordCache = new(
        new MemoryCacheOptions {SizeLimit = 4096, ExpirationScanFrequency = TimeSpan.FromMinutes(30)}
    );

    public virtual async Task ValidateAsync(ResourceOwnerPasswordValidationContext context)
    {
        var user = await userManager.FindByNameAsync(context.UserName);
        if (user == null)
        {
            logger.LogInformation("No user found matching username: {Username}", context.UserName);
            context.Result = new GrantValidationResult(TokenRequestErrors.InvalidGrant, SharedErrorTypes.AuthenticateLogin.ToString());
            return;
        }

        if (string.IsNullOrEmpty(context.Password))
        {
            logger.LogInformation("Authentication failed for username: {Username}, reason: empty password", context.UserName);
            context.Result = new GrantValidationResult(TokenRequestErrors.InvalidGrant, SharedErrorTypes.AuthenticatePassword.ToString());
            return;
        }

        bool checkPasswordResult;
        if (UserNameToPasswordCache.TryGetValue(context.UserName, out var cachedPassword))
        {
            if (!context.Password.Equals((string) cachedPassword))
            {
                UserNameToPasswordCache.Remove(user.Id);
                checkPasswordResult = await userManager.CheckPasswordAsync(user, context.Password);
            }
            else
            {
                checkPasswordResult = true;
            }
        }
        else
        {
            checkPasswordResult = await userManager.CheckPasswordAsync(user, context.Password);
        }

        if (!checkPasswordResult)
        {
            logger.LogInformation("Authentication failed for username: {Username}, reason: invalid credentials", context.UserName);
            context.Result = new GrantValidationResult(TokenRequestErrors.InvalidGrant, SharedErrorTypes.AuthenticatePassword.ToString());
            return;
        }

        var memoryCacheEntryOptions = new MemoryCacheEntryOptions();
        memoryCacheEntryOptions.SetSize(1);
        memoryCacheEntryOptions.SetAbsoluteExpiration(TimeSpan.FromMinutes(120));
        UserNameToPasswordCache.Set(context.UserName, context.Password, memoryCacheEntryOptions);

        logger.LogInformation("Credentials validated for username: {Username}", context.UserName);

        var claims = await userManager.GetClaimsAsync(user);

        context.Result = new GrantValidationResult(user.Id, context.Request.GrantType, claims) {CustomResponse = []};
    }
}