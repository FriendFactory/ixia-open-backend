using FluentValidation;

namespace AssetServer.Services;

public class AssetServiceOptions
{
    public string AssetCdnHost { get; set; }

    public string CloudFrontCertificatePrivateKey { get; set; }

    public string CloudFrontCertificateKeyPairId { get; set; }

    public int AssetUrlLifetimeMinutes { get; set; }
}

public class AssetServiceOptionsValidator : AbstractValidator<AssetServiceOptions>
{
    public AssetServiceOptionsValidator()
    {
        RuleFor(x => x.AssetCdnHost).NotNull().NotEmpty().MinimumLength(1);
        RuleFor(x => x.CloudFrontCertificatePrivateKey).NotNull().NotEmpty().MinimumLength(1);
        RuleFor(x => x.CloudFrontCertificateKeyPairId).NotNull().NotEmpty().MinimumLength(1);
        RuleFor(x => x.AssetUrlLifetimeMinutes).GreaterThan(0);
    }
}