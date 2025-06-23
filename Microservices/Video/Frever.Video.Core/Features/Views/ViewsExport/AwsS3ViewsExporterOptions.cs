using FluentValidation;

namespace Frever.Video.Core.Features.Views.ViewsExport;

public class AwsS3ViewsExporterOptions
{
    public string S3Bucket { get; set; }

    public void Validate()
    {
        var validator = new InlineValidator<AwsS3ViewsExporterOptions>();
        validator.RuleFor(e => e.S3Bucket).NotEmpty().MinimumLength(1);

        validator.ValidateAndThrow(this);
    }
}