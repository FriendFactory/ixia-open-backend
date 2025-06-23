using FluentValidation;

namespace Frever.Videos.Shared.MusicGeoFiltering.AbstractApi;

public class AbstractApiConfiguration
{
    public string ApiKey { get; set; }

    public void Validate()
    {
        var validator = new InlineValidator<AbstractApiConfiguration>();
        validator.RuleFor(e => e.ApiKey).NotNull().NotEmpty().MinimumLength(1);

        validator.Validate(this);
    }
}