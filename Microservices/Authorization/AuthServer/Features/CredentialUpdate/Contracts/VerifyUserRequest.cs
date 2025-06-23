using AuthServer.Features.CredentialUpdate.Models;
using FluentValidation;

namespace AuthServer.Features.CredentialUpdate.Contracts;

public class VerifyUserRequest
{
    public CredentialType Type { get; set; }
    public string AppleIdentityToken { get; set; }

    //TODO: we can drop in 1.9 version and use IdentityToken for both Apple and Google
    public string IdentityToken { get; set; }
    public string Password { get; set; }
    public string VerificationCode { get; set; }
}

public class VerifyUserRequestValidator : AbstractValidator<VerifyUserRequest>
{
    public VerifyUserRequestValidator()
    {
        RuleFor(e => e.Type).IsInEnum();
        RuleFor(e => e.Password).NotEmpty().When(e => e.Type == CredentialType.Password);
        RuleFor(e => e.AppleIdentityToken).NotEmpty().When(e => e.Type == CredentialType.AppleId);
        RuleFor(e => e.IdentityToken).NotEmpty().When(e => e.Type == CredentialType.GoogleId);
        RuleFor(e => e.VerificationCode).NotEmpty().When(e => e.Type is CredentialType.Email or CredentialType.PhoneNumber);
    }
}