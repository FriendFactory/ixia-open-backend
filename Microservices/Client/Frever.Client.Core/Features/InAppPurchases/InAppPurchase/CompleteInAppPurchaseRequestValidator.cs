using System;
using FluentValidation;
using Frever.ClientService.Contract.Common;
using Frever.ClientService.Contract.InAppPurchases;

namespace Frever.Client.Core.Features.InAppPurchases.InAppPurchase;

public class CompleteInAppPurchaseRequestValidator : AbstractValidator<CompleteInAppPurchaseRequest>
{
    public CompleteInAppPurchaseRequestValidator()
    {
        RuleFor(e => e.PendingOrderId).NotEmpty();
        RuleFor(e => e.TransactionData).NotEmpty().MinimumLength(1);
        RuleFor(e => e.Platform).Must(v => Enum.IsDefined(typeof(Platform), v)).WithMessage("Platform has invalid value");
    }
}