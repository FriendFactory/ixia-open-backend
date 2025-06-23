using FluentValidation;

namespace AuthServer.Models;

public class ExternalUrls
{
    public string Main { get; set; }
    public string Asset { get; set; }
    public string Transcoding { get; set; }
    public string Video { get; set; }
    public string Social { get; set; }
    public string Notification { get; set; }
    public string AssetManager { get; set; }
    public string Chat { get; set; }
    public string Client { get; set; }

    public void Validate()
    {
        var validator = new InlineValidator<ExternalUrls>();

        validator.RuleFor(e => e.Asset).NotEmpty().MinimumLength(2);
        validator.RuleFor(e => e.AssetManager).NotEmpty().MinimumLength(2);
        validator.RuleFor(e => e.Chat).NotEmpty().MinimumLength(2);
        validator.RuleFor(e => e.Client).NotEmpty().MinimumLength(2);
        validator.RuleFor(e => e.Main).NotEmpty().MinimumLength(2);
        validator.RuleFor(e => e.Notification).NotEmpty().MinimumLength(2);
        validator.RuleFor(e => e.Social).NotEmpty().MinimumLength(2);
        validator.RuleFor(e => e.Transcoding).NotEmpty().MinimumLength(2);
        validator.RuleFor(e => e.Video).NotEmpty().MinimumLength(2);
    }
}