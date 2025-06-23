using FluentValidation;

namespace AuthServer.Contracts;

public class AppleEmailInfoRequest
{
    public string AppleId { get; set; }

    public string Email { get; set; }
}

public class AppleEmailInfoRequestValidator : AbstractValidator<AppleEmailInfoRequest>
{
    public AppleEmailInfoRequestValidator()
    {
        RuleFor(e => e.AppleId).NotEmpty().MinimumLength(1);
        RuleFor(e => e.Email).NotEmpty().MinimumLength(1);
    }
}