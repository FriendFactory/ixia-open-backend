using AuthServer.Features.CredentialUpdate.Models;
using AuthServer.Services.PhoneNumberAuth;
using FluentValidation;

namespace AuthServer.Features.CredentialUpdate.Contracts;

public class AddCredentialsRequest
{
    public CredentialType Type { get; set; }
    public string VerificationCode { get; set; }
    public string AppleId { get; set; }

    //TODO: we can drop in 1.9 version and use IdentityToken for both Apple and Google
    public string AppleIdentityToken { get; set; }
    public string GoogleId { get; set; }
    public string IdentityToken { get; set; }
    public string Email { get; set; }
    public string PhoneNumber { get; set; }
    public string Password { get; set; }
}

public class AddCredentialsRequestValidator : AbstractValidator<AddCredentialsRequest>
{
    public AddCredentialsRequestValidator()
    {
        RuleFor(e => e.Type).IsInEnum();

        RuleFor(e => e.Password).NotEmpty().When(e => e.Type == CredentialType.Password);

        RuleFor(e => e.Email).EmailAddress().When(e => !string.IsNullOrWhiteSpace(e.Email));
        RuleFor(e => e.PhoneNumber)
           .Matches(VerifyPhoneNumberRequestValidator.PhoneNumberRegex)
           .When(e => !string.IsNullOrWhiteSpace(e.PhoneNumber));

        RuleFor(e => new {e.Email, e.VerificationCode})
           .Must(e => !string.IsNullOrWhiteSpace(e.Email) && !string.IsNullOrWhiteSpace(e.VerificationCode))
           .When(e => e.Type == CredentialType.Email);

        RuleFor(e => new {e.PhoneNumber, e.VerificationCode})
           .Must(e => !string.IsNullOrWhiteSpace(e.PhoneNumber) && !string.IsNullOrWhiteSpace(e.VerificationCode))
           .When(e => e.Type == CredentialType.PhoneNumber);

        RuleFor(e => new {e.AppleId, e.AppleIdentityToken})
           .Must(e => !string.IsNullOrWhiteSpace(e.AppleId) && !string.IsNullOrWhiteSpace(e.AppleIdentityToken))
           .When(e => e.Type == CredentialType.AppleId);

        RuleFor(e => new {e.GoogleId, GoogleIdentityToken = e.IdentityToken})
           .Must(e => !string.IsNullOrWhiteSpace(e.GoogleId) && !string.IsNullOrWhiteSpace(e.GoogleIdentityToken))
           .When(e => e.Type == CredentialType.GoogleId);
    }
}