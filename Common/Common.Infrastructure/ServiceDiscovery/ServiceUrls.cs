using FluentValidation;

namespace Common.Infrastructure.ServiceDiscovery;

public class ServiceUrls
{
    public readonly string AuthApiName = "friends_factory.creators_api";

    public string Asset { get; set; }
    public string Auth { get; set; }
    public string Chat { get; set; }
    public string Client { get; set; }
    public string Main { get; set; }
    public string Notification { get; set; }
    public string Video { get; set; }
    public string VideoFeed { get; set; }
    public string MachineLearning { get; set; }


    public void Validate()
    {
        var validator = new InlineValidator<ServiceUrls>();
        validator.RuleFor(a => a.Asset).NotEmpty().MinimumLength(2);
        validator.RuleFor(a => a.Auth).NotEmpty().MinimumLength(2);
        validator.RuleFor(a => a.Chat).NotEmpty().MinimumLength(2);
        validator.RuleFor(a => a.Client).NotEmpty().MinimumLength(2);
        validator.RuleFor(a => a.Main).NotEmpty().MinimumLength(2);
        validator.RuleFor(a => a.Notification).NotEmpty().MinimumLength(2);
        validator.RuleFor(a => a.Video).NotEmpty().MinimumLength(2);
        validator.RuleFor(a => a.VideoFeed).NotEmpty().MinimumLength(2);
        validator.RuleFor(a => a.MachineLearning).NotEmpty().MinimumLength(2);

        validator.ValidateAndThrow(this);
    }
}