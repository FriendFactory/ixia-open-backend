using FluentValidation;

namespace AuthServer.Services.SmsSender;

public class TwilioSmsSettings
{
    public string Sid { get; set; }

    public string Secret { get; set; }

    public string MessagingServiceSid { get; set; }

    public string VerifyServiceSid { get; set; }
}

public class TwilioSmsSettingsValidator : AbstractValidator<TwilioSmsSettings>
{
    public TwilioSmsSettingsValidator()
    {
        RuleFor(e => e.Sid).NotEmpty().MinimumLength(1);
        RuleFor(e => e.Secret).NotEmpty().MinimumLength(1);
        RuleFor(e => e.MessagingServiceSid).NotEmpty().MinimumLength(1);
        RuleFor(e => e.VerifyServiceSid).NotEmpty().MinimumLength(1);
    }
}