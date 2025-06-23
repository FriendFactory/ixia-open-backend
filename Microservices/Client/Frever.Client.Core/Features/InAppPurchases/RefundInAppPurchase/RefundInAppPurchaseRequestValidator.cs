using System;
using FluentValidation;
using Frever.ClientService.Contract.Common;
using Frever.ClientService.Contract.InAppPurchases;

namespace Frever.Client.Core.Features.InAppPurchases.RefundInAppPurchase;

public class RefundInAppPurchaseRequestValidator : AbstractValidator<RefundInAppPurchaseRequest>
{
    public RefundInAppPurchaseRequestValidator()
    {
        RuleFor(e => e.Platform).Must(v => Enum.IsDefined(typeof(Platform), v)).WithMessage("Platform has invalid value");
        RuleFor(e => e.StoreOrderIdentifier).NotEmpty().MinimumLength(1);
    }
}