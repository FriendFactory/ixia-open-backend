using FluentValidation;

namespace AuthServer.Services.EmailAuth;

public class VerifyEmailRequest
{
    public string Email { get; set; }
}

public class VerifyEmailRequestValidator : AbstractValidator<VerifyEmailRequest>
{
    public VerifyEmailRequestValidator()
    {
        RuleFor(e => e.Email).NotEmpty().EmailAddress();
    }
}