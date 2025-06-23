using FluentValidation;
using Frever.AdminService.Core.Services.InAppPurchases.Contracts;

namespace Frever.AdminService.Core.Services.InAppPurchases.Validators;

public class InAppProductPriceTierDtoValidator : AbstractValidator<InAppProductPriceTierDto>
{
    public InAppProductPriceTierDtoValidator()
    {
        RuleFor(e => e.Id).GreaterThanOrEqualTo(0);
        RuleFor(e => e.Title).NotEmpty();
        RuleFor(e => e.AppStoreProductRef).NotEmpty();
        RuleFor(e => e.PlayMarketProductRef).NotEmpty();
        RuleFor(e => e.RefPriceUsdCents).GreaterThanOrEqualTo(0);
    }
}