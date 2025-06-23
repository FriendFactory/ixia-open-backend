using FluentValidation;

namespace Common.Infrastructure.MusicProvider.Validators;

public class MusicProviderOAuthSettingsValidator : AbstractValidator<MusicProviderOAuthSettings>
{
    public MusicProviderOAuthSettingsValidator()
    {
        RuleFor(e => e.OAuthVersion).NotEmpty().MinimumLength(1);
        RuleFor(e => e.OAuthConsumerKey).NotEmpty().MinimumLength(1);
        RuleFor(e => e.OAuthConsumerSecret).NotEmpty().MinimumLength(1);
        RuleFor(e => e.OAuthSignatureMethod).NotEmpty().MinimumLength(1);
    }
}