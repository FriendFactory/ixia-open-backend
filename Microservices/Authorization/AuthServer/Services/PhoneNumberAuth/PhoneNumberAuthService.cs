using System;
using System.Threading.Tasks;
using AuthServer.Models;
using AuthServer.Services.SmsSender;
using FluentValidation;
using IdentityServer4.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace AuthServer.Services.PhoneNumberAuth;

public class PhoneNumberAuthService(
    ISmsSender smsSender,
    PhoneNumberTokenProvider<ApplicationUser> phoneNumberTokenProvider,
    UserManager<ApplicationUser> userManager,
    PhoneNumberTokenGrantValidatorSettings settings,
    IValidator<VerifyPhoneNumberRequest> validator
) : IPhoneNumberAuthService
{

    private readonly ISmsSender _smsSender = smsSender ?? throw new ArgumentNullException(nameof(smsSender));
    private readonly IValidator<VerifyPhoneNumberRequest> _validator = validator ?? throw new ArgumentNullException(nameof(validator));
    private readonly PhoneNumberTokenGrantValidatorSettings _settings = settings ?? throw new ArgumentNullException(nameof(settings));
    private readonly UserManager<ApplicationUser> _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));

    private readonly PhoneNumberTokenProvider<ApplicationUser> _phoneNumberTokenProvider =
        phoneNumberTokenProvider ?? throw new ArgumentNullException(nameof(phoneNumberTokenProvider));

    public async Task<VerifyPhoneNumberResponse> SendPhoneNumberVerification(VerifyPhoneNumberRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        await _validator.ValidateAndThrowAsync(request);

        request.PhoneNumber = await FormatPhoneNumber(request.PhoneNumber);

        var user = await GetUser(request.PhoneNumber);
        await SendSmsRequest(request.PhoneNumber, user);

        return new VerifyPhoneNumberResponse {IsSuccessful = true};
    }

    public async Task<bool> ValidateVerificationCode(string phoneNumber, string verificationCode)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber))
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(phoneNumber));

        if (string.IsNullOrWhiteSpace(verificationCode))
            return false;

        var user = await GetUser(phoneNumber);

        if (_settings.AllowUniversalOTP && StringComparer.OrdinalIgnoreCase.Equals("unicode", verificationCode))
            return true;

        var result = await _phoneNumberTokenProvider.ValidateAsync("verify_number", verificationCode, _userManager, user);

        return result;
    }

    public async Task<string> GenerateVerificationCode(string phoneNumber)
    {
        var user = await GetUser(phoneNumber);
        var verificationCode = await _phoneNumberTokenProvider.GenerateAsync("verify_number", _userManager, user);

        return verificationCode;
    }

    public Task<string> FormatPhoneNumber(string phoneNumber)
    {
        return _smsSender.FormatPhoneNumber(phoneNumber);
    }

    private async Task<ApplicationUser> GetUser(string phoneNumber)
    {
        var user = await _userManager.Users.SingleOrDefaultAsync(x => x.PhoneNumber == phoneNumber) ??
                   new ApplicationUser
                   {
                       PhoneNumber = phoneNumber, SecurityStamp = new Secret("your_secret_key").Value + phoneNumber.Sha256()
                   };

        return user;
    }

    private async Task SendSmsRequest(string phoneNumber, ApplicationUser user)
    {
        var verificationCode = await _phoneNumberTokenProvider.GenerateAsync("verify_number", _userManager, user);
        await _smsSender.SendMessage(phoneNumber, $"Your Frever security code is {verificationCode}");
    }
}