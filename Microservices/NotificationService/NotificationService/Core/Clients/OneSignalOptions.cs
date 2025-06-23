using FluentValidation;

namespace NotificationService.Core;

public class OneSignalOptions
{
    public string AppId { get; set; }
    public string ApiKey { get; set; }
    public string AndroidChannelId { get; set; }
}

public class OneSignalOptionsValidator : AbstractValidator<OneSignalOptions>
{
    public OneSignalOptionsValidator()
    {
        RuleFor(n => n.ApiKey).NotEmpty().MinimumLength(1);
        RuleFor(n => n.AppId).NotEmpty().MinimumLength(1);
    }
}