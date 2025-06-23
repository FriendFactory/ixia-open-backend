using FluentValidation;

namespace AssetStoragePathProviding;

public class VideoNamingHelperOptions
{
    public string IngestVideoBucket { get; set; }

    public string DestinationVideoBucket { get; set; }

    public string CloudFrontHost { get; set; }

    public void Validate()
    {
        var validator = new InlineValidator<VideoNamingHelperOptions>();
        validator.RuleFor(e => e.IngestVideoBucket).NotEmpty().MinimumLength(1);
        validator.RuleFor(e => e.DestinationVideoBucket).NotEmpty().MinimumLength(1);
        validator.RuleFor(e => e.CloudFrontHost).NotEmpty().MinimumLength(1);
    }
}