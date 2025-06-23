using FluentValidation;

namespace AssetServer.Shared.AssetCopying;

public class AssetCopyingOptions
{
    public string AssetCopyingQueueUrl { get; set; }
    public string BucketName { get; set; }

    public void Validate()
    {
        var inlineValidator = new InlineValidator<AssetCopyingOptions>();

        inlineValidator.RuleFor(a => a.AssetCopyingQueueUrl).NotEmpty().MinimumLength(1);
        inlineValidator.RuleFor(a => a.BucketName).NotEmpty().MinimumLength(1);

        inlineValidator.ValidateAndThrow(this);
    }
}