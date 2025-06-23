using FluentValidation;

namespace NotificationService;

public class AppConfig
{
    public AppServicesConfig Services { get; set; }

    public AuthConfig Auth { get; set; }

    public void Validate()
    {
        new AppConfigValidator().ValidateAndThrow(this);
    }
}

public class AppServicesConfig
{
    public string Video { get; set; }

    public string Main { get; set; }
}

public class AuthConfig
{
    public string AuthServer { get; set; }

    public string ApiName { get; set; }
}

internal class AppConfigValidator : AbstractValidator<AppConfig>
{
    public AppConfigValidator()
    {
        RuleFor(e => e.Services).SetValidator(new AppServicesConfigValidator());
        RuleFor(e => e.Auth).SetValidator(new AuthConfigValidator());
    }
}

internal class AppServicesConfigValidator : AbstractValidator<AppServicesConfig>
{
    public AppServicesConfigValidator()
    {
        RuleFor(e => e.Video).NotEmpty().MinimumLength(1);
        RuleFor(e => e.Main).NotEmpty().MinimumLength(1);
    }
}

internal class AuthConfigValidator : AbstractValidator<AuthConfig>
{
    public AuthConfigValidator()
    {
        RuleFor(e => e.AuthServer).NotEmpty().MinimumLength(1);
        RuleFor(e => e.ApiName).NotEmpty().MinimumLength(1);
    }
}