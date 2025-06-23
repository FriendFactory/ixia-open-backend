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

namespace AuthServer.Services.GoogleAuth;

public class GoogleAuthExtensionGrantValidator(
    IEventService events,
    ILogger<GoogleAuthExtensionGrantValidator> logger,
    IGoogleAuthService googleAuthService,
    SignInManager<ApplicationUser> signInManager,
    UserManager<ApplicationUser> userManager,
    IUserRepository userRepository
) : IExtensionGrantValidator
{
    private readonly IGoogleAuthService _googleAuthService = googleAuthService ?? throw new ArgumentNullException(nameof(googleAuthService));
    private readonly IEventService _events = events ?? throw new ArgumentNullException(nameof(events));
    private readonly ILogger<GoogleAuthExtensionGrantValidator> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly SignInManager<ApplicationUser> _signInManager = signInManager ?? throw new ArgumentNullException(nameof(signInManager));
    private readonly UserManager<ApplicationUser> _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
    private readonly IUserRepository _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));

    public string GrantType => AuthConstants.GrantType.GoogleAuthToken;

    public async Task ValidateAsync(ExtensionGrantValidationContext context)
    {
        var raw = context.Request.Raw;
        var credential = raw.Get(OidcConstants.TokenRequest.GrantType);
        if (credential is not AuthConstants.GrantType.GoogleAuthToken)
        {
            context.Result = new GrantValidationResult(TokenRequestErrors.InvalidGrant, "invalid google_auth_token credential");
            return;
        }

        var googleId = raw.Get(AuthConstants.TokenRequest.GoogleId);
        var googleAuthToken = raw.Get(AuthConstants.TokenRequest.IdentityToken);

        var mainDbUser = await _userRepository.GetUserByGoogleId(googleId);
        if (mainDbUser is null)
        {
            _logger.LogInformation("Authentication failed {GoogleId}, reason: no user found in main-db", googleId);
            await _events.RaiseAsync(new UserLoginFailureEvent(googleId, "User is not found", false));
            return;
        }

        var authDbUser = await _userManager.FindByIdAsync(mainDbUser.IdentityServerId.ToString());
        if (authDbUser is null)
        {
            _logger.LogInformation("Authentication failed {GoogleId}, reason: no user found in auth-db", googleId);
            await _events.RaiseAsync(new UserLoginFailureEvent(googleId, "User is not found", false));
            return;
        }

        var isBlocked = await _userRepository.IsGroupBlockedForAuthUser(authDbUser.Id);
        if (isBlocked)
        {
            _logger.LogInformation("Authentication failed {GoogleId}, reason: account is blocked", googleId);
            await _events.RaiseAsync(new UserLoginFailureEvent(googleId, "User is not blocked or deleted", false));
            return;
        }

        var tokenGoogleId = await _googleAuthService.ValidateAuthTokenAsync(googleAuthToken);
        if (tokenGoogleId != mainDbUser.GoogleId)
        {
            _logger.LogInformation(
                "Authentication failed, reason: invalid token, tokenGoogleId: {TokenGoogleId}, googleId: {GoogleId}, googleAuthToken: {GoogleAuthToken}",
                tokenGoogleId,
                googleId,
                googleAuthToken
            );
            await _events.RaiseAsync(new UserLoginFailureEvent(googleAuthToken, "invalid token or verification id", false));
            return;
        }

        _logger.LogInformation("Credentials validated for googleId: {GoogleId}", googleId);
        await _events.RaiseAsync(new UserLoginSuccessEvent(googleId, authDbUser.Id, googleId, false));
        await _signInManager.SignInAsync(authDbUser, true);
        context.Result = new GrantValidationResult(authDbUser.Id, OidcConstants.AuthenticationMethods.ProofOfPossessionSoftwareSecuredKey);
    }
}