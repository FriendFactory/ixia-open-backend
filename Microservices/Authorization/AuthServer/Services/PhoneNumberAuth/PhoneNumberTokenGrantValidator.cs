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

namespace AuthServer.Services.PhoneNumberAuth;

public class PhoneNumberTokenGrantValidator(
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager,
    IEventService events,
    ILogger<PhoneNumberTokenGrantValidator> logger,
    IPhoneNumberAuthService phoneNumberAuthService,
    IUserRepository userRepository
) : IExtensionGrantValidator
{
    private readonly IEventService _events = events ?? throw new ArgumentNullException(nameof(events));
    private readonly ILogger<PhoneNumberTokenGrantValidator> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly IPhoneNumberAuthService _phoneNumberAuthService = phoneNumberAuthService ?? throw new ArgumentNullException(nameof(phoneNumberAuthService));
    private readonly SignInManager<ApplicationUser> _signInManager = signInManager ?? throw new ArgumentNullException(nameof(signInManager));
    private readonly UserManager<ApplicationUser> _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
    private readonly IUserRepository _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));

    public string GrantType => AuthConstants.GrantType.PhoneNumberToken;

    public async Task ValidateAsync(ExtensionGrantValidationContext context)
    {
        var raw = context.Request.Raw;
        var credential = raw.Get(OidcConstants.TokenRequest.GrantType);
        if (credential is not AuthConstants.GrantType.PhoneNumberToken)
        {
            context.Result = new GrantValidationResult(TokenRequestErrors.InvalidGrant, "invalid verify_phone_number_token credential");
            return;
        }

        var phoneNumber = raw.Get(AuthConstants.TokenRequest.PhoneNumber);
        var verificationToken = raw.Get(AuthConstants.TokenRequest.Token);

        if (phoneNumber != null)
            phoneNumber = await _phoneNumberAuthService.FormatPhoneNumber(phoneNumber);

        var user = await _userManager.Users.SingleOrDefaultAsync(x => x.PhoneNumber == phoneNumber);
        if (user == null)
        {
            _logger.LogInformation("Authentication failed {PhoneNumber}, reason: no user found in auth-db", phoneNumber);
            await _events.RaiseAsync(new UserLoginFailureEvent(phoneNumber, "User is not found", false));
            return;
        }

        var isBlocked = await _userRepository.IsGroupBlockedForAuthUser(user.Id);
        if (isBlocked)
        {
            _logger.LogInformation("Authentication failed {PhoneNumber}, reason: account is blocked", phoneNumber);
            await _events.RaiseAsync(new UserLoginFailureEvent(phoneNumber, "User is not blocked or deleted", false));
            return;
        }

        var result = await _phoneNumberAuthService.ValidateVerificationCode(phoneNumber, verificationToken);
        if (!result)
        {
            _logger.LogInformation("Authentication failed {PhoneNumber}, reason: invalid token", phoneNumber);
            await _events.RaiseAsync(new UserLoginFailureEvent(verificationToken, "invalid token or verification id", false));
            return;
        }

        _logger.LogInformation("Credentials validated for username: {PhoneNumber}", phoneNumber);
        await _events.RaiseAsync(new UserLoginSuccessEvent(phoneNumber, user.Id, phoneNumber, false));
        await _signInManager.SignInAsync(user, true);
        context.Result = new GrantValidationResult(user.Id, OidcConstants.AuthenticationMethods.ConfirmationBySms);
    }
}