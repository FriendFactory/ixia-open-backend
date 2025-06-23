using System;
using System.Threading.Tasks;
using AuthServer.Models;
using Common.Infrastructure.EmailSending;
using FluentValidation;
using IdentityServer4.Models;
using Microsoft.AspNetCore.Identity;

namespace AuthServer.Services.EmailAuth;

public class EmailAuthService(
    EmailTokenProvider<ApplicationUser> emailTokenProvider,
    UserManager<ApplicationUser> userManager,
    IEmailSendingService emailSendingService,
    IValidator<VerifyEmailRequest> validator
) : IEmailAuthService
{
    private readonly IValidator<VerifyEmailRequest> _validator = validator ?? throw new ArgumentNullException(nameof(validator));
    private readonly UserManager<ApplicationUser> _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));

    private readonly IEmailSendingService _emailSendingService =
        emailSendingService ?? throw new ArgumentNullException(nameof(emailSendingService));

    private readonly EmailTokenProvider<ApplicationUser> _emailTokenProvider =
        emailTokenProvider ?? throw new ArgumentNullException(nameof(emailTokenProvider));

    public async Task SendEmailVerification(VerifyEmailRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        await _validator.ValidateAndThrowAsync(request);

        var code = await GenerateVerificationCode(request.Email);

        var emailMessage = string.Join(
            Environment.NewLine,
            $"Your Frever verification code is: {code}",
            Environment.NewLine,
            "Best Regards,",
            "Team Frever"
        );

        var emailParams = new SendEmailParams {Body = emailMessage, Subject = "Frever Verification Code", To = [request.Email]};
        await _emailSendingService.SendEmail(emailParams);
    }

    public async Task<bool> ValidateVerificationCode(string email, string verificationCode)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException($"{nameof(email)} cannot be null or whitespace.");

        if (string.IsNullOrWhiteSpace(verificationCode))
            return false;

        var user = await GetUser(email);

        var result = await _emailTokenProvider.ValidateAsync("verify_email", verificationCode, _userManager, user);

        return result;
    }

    public async Task<string> GenerateVerificationCode(string email)
    {
        var user = await GetUser(email);

        return await _emailTokenProvider.GenerateAsync("verify_email", _userManager, user);
    }

    public async Task SendParentEmailVerification(VerifyEmailRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        await _validator.ValidateAndThrowAsync(request);

        var user = new ApplicationUser {Email = request.Email, SecurityStamp = GetSecurityStamp(request.Email)};

        var code = await _emailTokenProvider.GenerateAsync("verify_parent_email", _userManager, user);

        var emailMessage = string.Join(
            Environment.NewLine,
            $"Your Frever verification code is: {code}",
            Environment.NewLine,
            "Best Regards,",
            "Team Frever"
        );

        var emailParams = new SendEmailParams {Body = emailMessage, Subject = "Frever Verification Code", To = [request.Email]};
        await _emailSendingService.SendEmail(emailParams);
    }

    public async Task<bool> ValidateParentEmailCode(string email, string verificationCode)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException($"{nameof(email)} cannot be null or whitespace.");

        if (string.IsNullOrWhiteSpace(verificationCode))
            return false;

        var user = new ApplicationUser {Email = email, SecurityStamp = GetSecurityStamp(email)};

        var result = await _emailTokenProvider.ValidateAsync("verify_parent_email", verificationCode, _userManager, user);

        return result;
    }

    private async Task<ApplicationUser> GetUser(string email)
    {
        return await _userManager.FindByEmailAsync(email) ?? new ApplicationUser {Email = email, SecurityStamp = GetSecurityStamp(email)};
    }

    private static string GetSecurityStamp(string email)
    {
        return new Secret("your_secret_key").Value + email.Sha256();
    }
}