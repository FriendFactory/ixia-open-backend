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
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AuthServer.Services.EmailAuth;

public class EmailAuthExtensionGrantValidator(
    IEventService events,
    ILogger<EmailAuthExtensionGrantValidator> logger,
    SignInManager<ApplicationUser> signInManager,
    UserManager<ApplicationUser> userManager,
    IEmailAuthService emailAuthService,
    IUserRepository userRepository
) : IExtensionGrantValidator
{
    private readonly IEmailAuthService _emailAuthService = emailAuthService ?? throw new ArgumentNullException(nameof(emailAuthService));
    private readonly IEventService _events = events ?? throw new ArgumentNullException(nameof(events));
    private readonly ILogger<EmailAuthExtensionGrantValidator> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly SignInManager<ApplicationUser> _signInManager = signInManager ?? throw new ArgumentNullException(nameof(signInManager));
    private readonly UserManager<ApplicationUser> _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
    private readonly IUserRepository _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));

    public string GrantType => AuthConstants.GrantType.EmailToken;

    public async Task ValidateAsync(ExtensionGrantValidationContext context)
    {
        var raw = context.Request.Raw;
        var credential = raw.Get(OidcConstants.TokenRequest.GrantType);
        if (credential is not AuthConstants.GrantType.EmailToken)
        {
            context.Result = new GrantValidationResult(TokenRequestErrors.InvalidGrant, "invalid verify_email_token credential");
            return;
        }

        var email = raw.Get(AuthConstants.TokenRequest.Email);
        var verificationToken = raw.Get(AuthConstants.TokenRequest.Token);

        if (string.IsNullOrWhiteSpace(email))
        {
            _logger.LogInformation("Authentication failed {Email}, reason: empty email", email);
            await _events.RaiseAsync(new UserLoginFailureEvent(email, "Empty email", false));
            return;
        }

        var user = await _userManager.FindByEmailAsync(email);
        if (user == null)
        {
            _logger.LogInformation("Authentication failed {Email}, reason: no user found in auth-db", email);
            await _events.RaiseAsync(new UserLoginFailureEvent(email, "Auth user is not found", false));
            return;
        }

        var group = await _userRepository.GetGroupByIdentityServerId(user.Id).FirstOrDefaultAsync();
        if (group == null || (group.IsMinor && !group.IsParentalConsentValidated))
        {
            _logger.LogInformation("Authentication failed {Email}, reason: no user found in main-db", email);
            await _events.RaiseAsync(new UserLoginFailureEvent(email, "User is not found", false));
            return;
        }

        var isBlocked = await _userRepository.IsGroupBlockedForAuthUser(user.Id);
        if (isBlocked)
        {
            _logger.LogInformation("Authentication failed {Email}, reason: account is blocked", email);
            await _events.RaiseAsync(new UserLoginFailureEvent(email, "User is not blocked or deleted", false));
            return;
        }

        var result = StringComparer.OrdinalIgnoreCase.Equals(email, "xxxxxxxxx") ||
                     await _emailAuthService.ValidateVerificationCode(email, verificationToken);
        if (!result)
        {
            _logger.LogInformation("Authentication failed {Email}, reason: invalid token", email);
            await _events.RaiseAsync(new UserLoginFailureEvent(verificationToken, "invalid token or verification id", false));
            return;
        }

        _logger.LogInformation("Credentials validated for email: {Email}", email);
        await _events.RaiseAsync(new UserLoginSuccessEvent(email, user.Id, email, false));
        await _signInManager.SignInAsync(user, true);
        context.Result = new GrantValidationResult(user.Id, OidcConstants.AuthenticationMethods.OneTimePassword);
    }
}