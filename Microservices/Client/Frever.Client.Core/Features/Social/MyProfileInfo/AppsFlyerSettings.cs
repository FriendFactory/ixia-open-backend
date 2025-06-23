using FluentValidation;

namespace Frever.Client.Core.Features.Social.MyProfileInfo;

public class AppsFlyerSettings
{
    public const string Url = "https://hq1.appsflyer.com/api/gdpr/v1/opendsr_requests";

    public string AppleAppId { get; set; }
    public string AndroidAppId { get; set; }
    public string Token { get; set; }

    public void Validate()
    {
        var inlineValidator = new InlineValidator<AppsFlyerSettings>();

        inlineValidator.RuleFor(a => a.AndroidAppId).NotEmpty().MinimumLength(1);
        inlineValidator.RuleFor(a => a.AppleAppId).NotEmpty().MinimumLength(1);
        inlineValidator.RuleFor(a => a.Token).NotEmpty().MinimumLength(1);

        inlineValidator.ValidateAndThrow(this);
    }
}