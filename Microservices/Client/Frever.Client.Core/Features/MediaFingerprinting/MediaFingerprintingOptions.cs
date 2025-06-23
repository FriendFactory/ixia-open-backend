using FluentValidation;

namespace Frever.Client.Core.Features.MediaFingerprinting;

public class MediaFingerprintingOptions
{
    public string Host { get; set; }

    public string AccessKey { get; set; }

    public string AccessSecret { get; set; }

    public string LogBucket { get; set; }

    public void Validate()
    {
        var validator = new InlineValidator<MediaFingerprintingOptions>();
        validator.RuleFor(e => e.Host).NotEmpty().MinimumLength(1);
        validator.RuleFor(e => e.AccessKey).NotEmpty().MinimumLength(1);
        validator.RuleFor(e => e.AccessSecret).NotEmpty().MinimumLength(1);
        validator.RuleFor(e => e.LogBucket).NotEmpty().MinimumLength(1);

        validator.ValidateAndThrow(this);
    }
}