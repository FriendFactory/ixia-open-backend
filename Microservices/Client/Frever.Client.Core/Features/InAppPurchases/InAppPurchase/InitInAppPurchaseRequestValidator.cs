using FluentValidation;
using Frever.ClientService.Contract.InAppPurchases;

namespace Frever.Client.Core.Features.InAppPurchases.InAppPurchase;

public class InitInAppPurchaseRequestValidator : AbstractValidator<InitInAppPurchaseRequest>
{
    public InitInAppPurchaseRequestValidator()
    {
        RuleFor(e => e.InAppProductOfferKey).NotEmpty().MinimumLength(1);
    }
}