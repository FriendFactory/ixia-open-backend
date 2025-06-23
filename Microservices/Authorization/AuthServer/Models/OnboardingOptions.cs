using FluentValidation;

namespace AuthServer.Models;

public class OnboardingOptions
{
    public string FreverOfficialEmail { get; set; }

    public void Validate()
    {
        var inlineValidator = new InlineValidator<OnboardingOptions>();
        inlineValidator.RuleFor(a => a.FreverOfficialEmail).NotEmpty().MinimumLength(1);

        inlineValidator.ValidateAndThrow(this);
    }
}