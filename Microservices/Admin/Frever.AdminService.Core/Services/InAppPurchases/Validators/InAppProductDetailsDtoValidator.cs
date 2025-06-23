using AssetStoragePathProviding;
using FluentValidation;
using Frever.AdminService.Core.Services.InAppPurchases.Contracts;
using Frever.AdminService.Core.Validation;
using Frever.Shared.MainDb.Entities;

namespace Frever.AdminService.Core.Services.InAppPurchases.Validators;

public class InAppProductDetailsDtoValidator : AbstractValidator<InAppProductDetailsDto>
{
    public InAppProductDetailsDtoValidator(IAssetFilesConfigs assetFilesConfigs)
    {
        RuleFor(e => e.Id).GreaterThanOrEqualTo(0);
        RuleFor(e => e.InAppProductId).GreaterThan(0);
        RuleFor(e => e.Files).EntityFiles(assetFilesConfigs, typeof(InAppProductDetails));
        RuleFor(e => e.SortOrder).GreaterThanOrEqualTo(0);
        RuleFor(e => e.UniqueOfferGroup).GreaterThanOrEqualTo(0);
        RuleFor(e => e.HardCurrency)
           .Must(e => e == null)
           .When(e => e.SoftCurrency.HasValue || (e.AssetId.HasValue && e.AssetType.HasValue));
        RuleFor(e => e.SoftCurrency)
           .Must(e => e == null)
           .When(e => e.HardCurrency.HasValue || (e.AssetId.HasValue && e.AssetType.HasValue));
        RuleFor(e => new {e.AssetId, e.AssetType})
           .Must(e => e.AssetId == null && e.AssetType == null)
           .When(e => e.SoftCurrency.HasValue || e.HardCurrency.HasValue);

        RuleFor(
                e => new
                     {
                         e.AssetId,
                         e.AssetType,
                         e.HardCurrency,
                         e.SoftCurrency
                     }
            )
           .Must(e => e.HardCurrency.HasValue || e.SoftCurrency.HasValue || (e.AssetId.HasValue && e.AssetType.HasValue));
    }
}