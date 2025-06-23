using FluentValidation;
using Frever.AdminService.Core.Services.InAppPurchases.Contracts;

namespace Frever.AdminService.Core.Services.InAppPurchases.Validators;

public class HardCurrencyExchangeOfferDtoValidator : AbstractValidator<HardCurrencyExchangeOfferDto>
{
    public HardCurrencyExchangeOfferDtoValidator()
    {
        RuleFor(e => e.Id).GreaterThanOrEqualTo(0);
        RuleFor(e => e.Title).NotEmpty();
        RuleFor(e => e.HardCurrencyRequired).GreaterThan(0);
        RuleFor(e => e.SoftCurrencyGiven).GreaterThan(0);
        RuleFor(e => e.SortOrder).GreaterThanOrEqualTo(0);
    }
}