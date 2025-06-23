using FluentValidation;

namespace Frever.Client.Core.Features.InAppPurchases;

public class InAppPurchaseOptions
{
    public bool IsProduction { get; set; }

    public string GoogleApiKeyBase64 { get; set; }
    public string AppleSharedSecret { get; set; }
    public string AppStoreBundleIdPrefix { get; set; }
    public string PlayMarketPackageName { get; set; }

    public string OfferKeySecret { get; set; }

    public void Validate()
    {
        var validator = new InlineValidator<InAppPurchaseOptions>();

        validator.RuleFor(e => e.GoogleApiKeyBase64).NotEmpty().MinimumLength(1);
        validator.RuleFor(e => e.PlayMarketPackageName).NotEmpty().MinimumLength(1);
        validator.RuleFor(e => e.AppStoreBundleIdPrefix).NotEmpty().MinimumLength(1);

        validator.ValidateAndThrow(this);
    }
}