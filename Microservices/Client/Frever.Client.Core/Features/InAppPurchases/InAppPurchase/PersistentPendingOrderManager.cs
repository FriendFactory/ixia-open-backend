using System;
using System.Linq;
using System.Threading.Tasks;
using AuthServerShared;
using Common.Infrastructure;
using Frever.Client.Core.Features.InAppPurchases.Contract;
using Frever.Client.Core.Features.InAppPurchases.DataAccess;
using Frever.Shared.AssetStore.OfferKeyCodec;
using Frever.Shared.MainDb;
using Frever.Shared.MainDb.Entities;
using Microsoft.EntityFrameworkCore;
using Platform = Frever.ClientService.Contract.Common.Platform;

namespace Frever.Client.Core.Features.InAppPurchases.InAppPurchase;

public class PersistentPendingOrderManager(
    IWriteDb mainDb,
    UserInfo currentUser,
    IInAppProductOfferKeyCodec offerCodec,
    IInAppProductRepository productRepo
) : IPendingOrderManager
{
    private readonly UserInfo _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));

    public async Task<InAppPurchaseOrder> PlacePendingOrder(InAppProductOffer offer, string clientCurrency, decimal clientCurrencyPrice)
    {
        ArgumentNullException.ThrowIfNull(offer);

        var productInfo = await offerCodec.DecodeAndValidate(_currentUser, offer.OfferKey);

        var refHardCurrency = offer.Details.OrderByDescending(a => a.HardCurrency ?? 0).Select(a => a.HardCurrency).FirstOrDefault();

        var product = await productRepo.GetActiveInAppProducts()
                                       .Select(p => new {p.Id, p.InAppProductPriceTierId})
                                       .FirstOrDefaultAsync(p => p.Id == productInfo.InAppProductId);
        var refPriceUsd = product == null
                              ? 0
                              : await productRepo.GetPriceTiers()
                                                 .Where(t => t.Id == product.InAppProductPriceTierId)
                                                 .Select(t => t.RefPriceUsdCents)
                                                 .FirstOrDefaultAsync();

        var pendingOrder = new InAppPurchaseOrder
                           {
                               Id = Guid.NewGuid(),
                               CreatedTime = DateTime.UtcNow,
                               GroupId = _currentUser,
                               IsPending = true,
                               InAppProductId = productInfo.InAppProductId,
                               InAppProductOfferKey = offer.OfferKey,
                               RefHardCurrencyAmount = refHardCurrency,
                               RefPriceUsdCents = refPriceUsd,
                               RefClientCurrency = clientCurrency,
                               RefClientCurrencyPrice = clientCurrencyPrice,
                               RefInAppProductId = product?.Id
                           };

        await mainDb.InAppPurchaseOrder.AddAsync(pendingOrder);
        await mainDb.SaveChangesAsync();

        return pendingOrder;
    }

    public async Task CompletePendingOrder(
        Guid orderId,
        Platform platform,
        string receipt,
        string storeOrderIdentifier,
        string environment
    )
    {
        if (string.IsNullOrWhiteSpace(storeOrderIdentifier))
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(storeOrderIdentifier));

        var order = await GetPendingOrder(orderId);
        if (order == null)
            throw new InvalidOperationException("Order is not valid");

        order.IsPending = false;
        order.CompletedTime = DateTime.UtcNow;
        order.Receipt = receipt;
        order.Platform = platform.ToString("G");
        order.StoreOrderIdentifier = storeOrderIdentifier;
        order.Environment = environment;

        await mainDb.SaveChangesAsync();
    }

    public async Task<InAppPurchaseOrder> CreateRestoreOrder(RestoreOrderParams parameters)
    {
        if (parameters == null)
            throw new ArgumentNullException(nameof(parameters));

        parameters.Validate();

        var appStoreProductRef = parameters.Platform == Platform.iOS ? parameters.InAppProductIdentifier ?? String.Empty : String.Empty;
        var playMarketProductRef =
            parameters.Platform == Platform.Android ? parameters.InAppProductIdentifier ?? String.Empty : String.Empty;


        var product = await productRepo.GetActiveInAppProducts()
                                       .Where(
                                            p => (p.AppStoreProductRef == appStoreProductRef && appStoreProductRef != String.Empty) ||
                                                 (p.PlayMarketProductRef == playMarketProductRef && playMarketProductRef != String.Empty)
                                        )
                                       .FirstOrDefaultAsync();

        if (product == null)
            throw AppErrorWithStatusCodeException.BadRequest("Product identifier is not found", "PRODUCT_NOT_FOUND");

        var refPriceUsd = await productRepo.GetPriceTiers()
                                           .Where(t => t.Id == product.InAppProductPriceTierId)
                                           .Select(t => t.RefPriceUsdCents)
                                           .FirstOrDefaultAsync();

        var restoredOrder = new InAppPurchaseOrder
                            {
                                Id = Guid.NewGuid(),
                                CreatedTime = DateTime.UtcNow,
                                CompletedTime = parameters.PurchaseTime.ToUniversalTime(),
                                Receipt = parameters.StoreOrderIdentifier,
                                StoreOrderIdentifier = parameters.StoreOrderIdentifier,
                                GroupId = _currentUser,
                                InAppProductId = product.Id,
                                InAppProductOfferKey = parameters.StoreOrderIdentifier,
                                RefPriceUsdCents = refPriceUsd,
                                RefClientCurrency = parameters.RefClientCurrency,
                                RefClientCurrencyPrice = parameters.RefClientCurrencyPrice,
                                RefInAppProductId = product.Id,
                                Platform = parameters.Platform.ToString("G"),
                                Environment = parameters.Environment
                            };

        await mainDb.InAppPurchaseOrder.AddAsync(restoredOrder);
        await mainDb.SaveChangesAsync();

        return restoredOrder;
    }

    public async Task SetPendingOrderError(Guid orderId, Platform platform, string transactionId, string error)
    {
        if (string.IsNullOrWhiteSpace(transactionId))
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(transactionId));
        var order = await GetPendingOrder(orderId);
        if (order == null)
            throw new InvalidOperationException("Order is not valid");

        order.CompletedTime = DateTime.UtcNow;
        order.StoreOrderIdentifier = transactionId;
        order.ErrorCode = error;
        order.Platform = platform.ToString("G");

        await mainDb.SaveChangesAsync();
    }

    public Task<InAppPurchaseOrder> GetPendingOrder(Guid orderId)
    {
        return mainDb.InAppPurchaseOrder.SingleOrDefaultAsync(o => o.Id == orderId && o.GroupId == _currentUser.UserMainGroupId);
    }

    public IQueryable<InAppPurchaseOrder> GetExistingPendingOrder(long inAppProductId)
    {
        return mainDb.InAppPurchaseOrder.Where(o => o.InAppProductId == inAppProductId && o.GroupId == _currentUser && o.IsPending);
    }

    public async Task DiscardPendingOrder(long currentGroupId, Guid orderId)
    {
        var order = await mainDb.InAppPurchaseOrder.FirstOrDefaultAsync(o => o.Id == orderId && o.GroupId == currentGroupId && o.IsPending);

        if (order != null)
        {
            order.IsPending = false;
        }

        await mainDb.SaveChangesAsync();
    }
}