using System;
using System.Linq;
using System.Threading.Tasks;
using FluentValidation;
using Frever.Client.Core.Features.InAppPurchases.Contract;
using Frever.Shared.MainDb.Entities;
using Platform = Frever.ClientService.Contract.Common.Platform;

namespace Frever.Client.Core.Features.InAppPurchases.InAppPurchase;

public interface IPendingOrderManager
{
    Task<InAppPurchaseOrder> PlacePendingOrder(InAppProductOffer offer, string clientCurrency, decimal clientCurrencyPrice);

    Task CompletePendingOrder(
        Guid orderId,
        Platform platform,
        string receipt,
        string storeOrderIdentifier,
        string environment
    );

    Task SetPendingOrderError(Guid orderId, Platform platform, string receipt, string error);

    Task<InAppPurchaseOrder> GetPendingOrder(Guid orderId);

    IQueryable<InAppPurchaseOrder> GetExistingPendingOrder(long inAppProductId);

    Task DiscardPendingOrder(long currentGroupId, Guid orderId);

    Task<InAppPurchaseOrder> CreateRestoreOrder(RestoreOrderParams parameters);
}

public class RestoreOrderParams
{
    public required Platform Platform { get; set; }
    public required string InAppProductIdentifier { get; set; }
    public required DateTime PurchaseTime { get; set; }
    public required string StoreOrderIdentifier { get; set; }
    public required string Environment { get; set; }
    public required string RefClientCurrency { get; set; }
    public required decimal RefClientCurrencyPrice { get; set; }

    public void Validate()
    {
        var validator = new InlineValidator<RestoreOrderParams>();
        validator.RuleFor(s => s.InAppProductIdentifier).NotNull().NotEmpty().MinimumLength(1);
        validator.RuleFor(s => s.StoreOrderIdentifier).NotNull().NotEmpty().MinimumLength(1);
        validator.RuleFor(s => s.Environment).NotNull().NotEmpty().MinimumLength(1);
        validator.RuleFor(s => s.RefClientCurrency).NotNull().NotEmpty().MinimumLength(1);

        validator.ValidateAndThrow(this);
    }
}