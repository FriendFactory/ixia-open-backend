using System;
using System.Threading.Tasks;
using AuthServer.Models;
using AuthServer.Repositories;
using IdentityModel;
using IdentityServer4.Events;
using IdentityServer4.Models;
using IdentityServer4.Services;
using IdentityServer4.Validation;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace AuthServer.Services.AppleAuth;

public class AppleAuthExtensionGrantValidator(
    IEventService events,
    ILogger<AppleAuthExtensionGrantValidator> logger,
    IAppleAuthService appleAuthService,
    SignInManager<ApplicationUser> signInManager,
    UserManager<ApplicationUser> userManager,
    IUserRepository userRepository
) : IExtensionGrantValidator
{
    private readonly IAppleAuthService _appleAuthService = appleAuthService ?? throw new ArgumentNullException(nameof(appleAuthService));
    private readonly IEventService _events = events ?? throw new ArgumentNullException(nameof(events));
    private readonly ILogger<AppleAuthExtensionGrantValidator> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly SignInManager<ApplicationUser> _signInManager = signInManager ?? throw new ArgumentNullException(nameof(signInManager));
    private readonly UserManager<ApplicationUser> _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
    private readonly IUserRepository _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));

    public string GrantType => AuthConstants.GrantType.AppleAuthToken;

    public async Task ValidateAsync(ExtensionGrantValidationContext context)
    {
        var raw = context.Request.Raw;
        var credential = raw.Get(OidcConstants.TokenRequest.GrantType);
        if (credential is not AuthConstants.GrantType.AppleAuthToken)
        {
            context.Result = new GrantValidationResult(TokenRequestErrors.InvalidGrant, "invalid apple_auth_token credential");
            return;
        }

        var appleId = raw.Get(AuthConstants.TokenRequest.AppleId);
        var appleAuthToken = raw.Get(AuthConstants.TokenRequest.IdentityToken);

        var mainDbUser = await _userRepository.GetUserByAppleId(appleId);
        if (mainDbUser is null)
        {
            _logger.LogInformation("Authentication failed {AppleId}, reason: no user found in main-db", appleId);
            await _events.RaiseAsync(new UserLoginFailureEvent(appleId, "User is not found", false));
            return;
        }

        var authDbUser = await _userManager.FindByIdAsync(mainDbUser.IdentityServerId.ToString());
        if (authDbUser is null)
        {
            _logger.LogInformation("Authentication failed {AppleId}, reason: no user found in auth-db", appleId);
            await _events.RaiseAsync(new UserLoginFailureEvent(appleId, "User is not found", false));
            return;
        }

        var isBlocked = await _userRepository.IsGroupBlockedForAuthUser(authDbUser.Id);
        if (isBlocked)
        {
            _logger.LogInformation("Authentication failed {AppleId}, reason: account is blocked", appleId);
            await _events.RaiseAsync(new UserLoginFailureEvent(appleId, "User is not blocked or deleted", false));
            return;
        }

        var tokenAppleId = await _appleAuthService.ValidateAuthTokenAsync(appleAuthToken);
        if (tokenAppleId != mainDbUser.AppleId)
        {
            _logger.LogInformation(
                "Authentication failed, reason: invalid token, tokenAppleId: {TokenAppleId}, appleId: {AppleId}, appleAuthToken: {AppleAuthToken}",
                tokenAppleId,
                appleId,
                appleAuthToken
            );
            await _events.RaiseAsync(new UserLoginFailureEvent(appleAuthToken, "invalid token or verification id", false));
            return;
        }

        _logger.LogInformation("Credentials validated for appleId: {AppleId}", appleId);
        await _events.RaiseAsync(new UserLoginSuccessEvent(appleId, authDbUser.Id, appleId, false));
        await _signInManager.SignInAsync(authDbUser, true);
        context.Result = new GrantValidationResult(authDbUser.Id, OidcConstants.AuthenticationMethods.ProofOfPossessionSoftwareSecuredKey);
    }
}