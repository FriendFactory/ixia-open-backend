using FluentValidation;

namespace Frever.Client.Core.Features.CommercialMusic;

public class SpotifyPopularitySettings
{
    public string Bucket { get; set; }

    public string Prefix { get; set; }

    public string FullDataCsvFileName { get; set; }

    public void Validate()
    {
        var v = new InlineValidator<SpotifyPopularitySettings>();
        v.RuleFor(e => e.Bucket).NotEmpty().MinimumLength(1);
        v.RuleFor(e => e.Prefix).NotEmpty().MinimumLength(1);
        v.RuleFor(e => e.FullDataCsvFileName).NotEmpty().MinimumLength(1);

        v.ValidateAndThrow(this);
    }
}