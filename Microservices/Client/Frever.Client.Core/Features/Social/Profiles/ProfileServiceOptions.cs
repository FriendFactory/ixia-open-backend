using FluentValidation;

namespace Frever.Client.Core.Features.Social.Profiles;

public class ProfileServiceOptions
{
    public string DeleteAccountEmail { get; set; }

    public string FreverOfficialEmail { get; set; }

    public void Validate()
    {
        var inlineValidator = new InlineValidator<ProfileServiceOptions>();
        inlineValidator.RuleFor(a => a.DeleteAccountEmail).NotEmpty().MinimumLength(1);
        inlineValidator.RuleFor(a => a.FreverOfficialEmail).NotEmpty().MinimumLength(1);

        inlineValidator.ValidateAndThrow(this);
    }
}