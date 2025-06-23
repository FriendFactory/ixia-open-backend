using System.Text.RegularExpressions;
using FluentValidation;

namespace AuthServer.Services.PhoneNumberAuth;

public class VerifyPhoneNumberRequest
{
    public string PhoneNumber { get; set; }
}

public class VerifyPhoneNumberRequestValidator : AbstractValidator<VerifyPhoneNumberRequest>
{
    public const string PhoneNumberRegex = @"\+\d{9,15}";

    private readonly Regex _phoneNumberVerificationRegex = new(PhoneNumberRegex);

    public VerifyPhoneNumberRequestValidator()
    {
        RuleFor(e => e.PhoneNumber).NotEmpty().Matches(_phoneNumberVerificationRegex);
    }
}