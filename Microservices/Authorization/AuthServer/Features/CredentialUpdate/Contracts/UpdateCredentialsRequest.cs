using AuthServer.Features.CredentialUpdate.Models;
using AuthServer.Services.PhoneNumberAuth;
using FluentValidation;

namespace AuthServer.Features.CredentialUpdate.Contracts;

public class UpdateCredentialsRequest
{
    public CredentialType Type { get; set; }
    public string VerificationToken { get; set; }
    public string VerificationCode { get; set; }
    public string Email { get; set; }
    public string PhoneNumber { get; set; }
    public string Password { get; set; }
}

public class UpdateCredentialsRequestValidator : AbstractValidator<UpdateCredentialsRequest>
{
    public UpdateCredentialsRequestValidator()
    {
        RuleFor(e => e.Type).IsInEnum();

        RuleFor(e => e.VerificationToken).NotEmpty();

        RuleFor(e => e.Password).NotEmpty().When(e => e.Type == CredentialType.Password);

        RuleFor(e => e.Email).EmailAddress().When(e => !string.IsNullOrWhiteSpace(e.Email));

        RuleFor(e => e.PhoneNumber)
           .Matches(VerifyPhoneNumberRequestValidator.PhoneNumberRegex)
           .When(e => !string.IsNullOrWhiteSpace(e.PhoneNumber));
    }
}