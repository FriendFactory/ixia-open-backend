using FluentValidation;

namespace Common.Infrastructure.MusicProvider.Validators;

public class MusicProviderApiSettingsValidator : AbstractValidator<MusicProviderApiSettings>
{
    public MusicProviderApiSettingsValidator()
    {
        RuleFor(e => e.CountryCode).NotEmpty().MinimumLength(2);
        RuleFor(e => e.TrackDetailsUrl).NotEmpty().MinimumLength(1);
        RuleFor(e => e.UsageTypes).NotEmpty().MinimumLength(1);
    }
}