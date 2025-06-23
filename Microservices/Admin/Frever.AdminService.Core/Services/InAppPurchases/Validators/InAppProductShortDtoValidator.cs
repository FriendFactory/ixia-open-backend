using AssetStoragePathProviding;
using FluentValidation;
using Frever.AdminService.Core.Services.InAppPurchases.Contracts;
using Frever.AdminService.Core.Validation;
using Frever.Shared.MainDb.Entities;

namespace Frever.AdminService.Core.Services.InAppPurchases.Validators;

public class InAppProductShortDtoValidator : AbstractValidator<InAppProductShortDto>
{
    public InAppProductShortDtoValidator(IAssetFilesConfigs assetFilesConfigs)
    {
        RuleFor(e => e.Id).GreaterThanOrEqualTo(0);
        RuleFor(e => e.Title).NotEmpty();
        RuleFor(e => e.AppStoreProductRef).NotEmpty();
        RuleFor(e => e.PlayMarketProductRef).NotEmpty();
        RuleFor(e => e.SortOrder).GreaterThanOrEqualTo(0);
        RuleFor(e => e.Files).EntityFiles(assetFilesConfigs, typeof(InAppProduct)).When(e => e.Files != null);
        RuleFor(e => new {e.PublicationDate, e.DepublicationDate})
           .Must(
                e => !e.PublicationDate.HasValue || (e.DepublicationDate.HasValue && e.DepublicationDate.Value >= e.PublicationDate.Value)
            );
    }
}