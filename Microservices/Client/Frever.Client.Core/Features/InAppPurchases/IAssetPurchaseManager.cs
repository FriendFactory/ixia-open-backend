using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Frever.Shared.AssetStore.Transactions;
using Frever.Shared.MainDb;
using Frever.Shared.MainDb.Entities;
using Microsoft.EntityFrameworkCore;

namespace Frever.Client.Core.Features.InAppPurchases;

public interface IInAppAssetPurchaseManager
{
    Task CompleteTransferOwnership(long groupId, InAppProduct product, IEnumerable<InAppProductDetails> details, string inAppPurchaseRef);

    Task RefundInAppPurchase(long groupId, InAppProduct product, IEnumerable<InAppProductDetails> details, string inAppPurchaseRef);

    Task<bool> IsStoreIdentifierUsed(long groupId, string storeIdentifier);
}

public class PersistentInAppAssetPurchaseManager(
    IWriteDb mainDb,
    IAssetStoreTransactionGenerationService assetStoreTransactionGenerationService
) : IInAppAssetPurchaseManager
{
    public async Task CompleteTransferOwnership(
        long groupId,
        InAppProduct product,
        IEnumerable<InAppProductDetails> details,
        string inAppPurchaseRef
    )
    {
        var priceTier = await mainDb.InAppProductPriceTier.FirstOrDefaultAsync(pt => pt.Id == product.InAppProductPriceTierId);
        if (priceTier == null && !product.IsFreeProduct)
            throw new InvalidOperationException("Price tier is invalid");

        var hardCurrency = details.Any() ? details.Sum(pd => pd.HardCurrency ?? 0) : 0;
        var softCurrency = details.Any() ? details.Sum(pd => pd.SoftCurrency ?? 0) : 0;

        var transactions = await assetStoreTransactionGenerationService.InAppPurchase(
                               groupId,
                               product.Id,
                               details.Where(pd => pd.AssetId != null && pd.AssetType != null)
                                      .Select(pd => new AssetToPurchase {AssetId = pd.AssetId.Value, AssetType = pd.AssetType.Value})
                                      .ToArray(),
                               product.IsFreeProduct ? 0 : hardCurrency,
                               product.IsFreeProduct ? 0 : softCurrency,
                               product.IsFreeProduct ? 0 : priceTier?.RefPriceUsdCents ?? 0,
                               inAppPurchaseRef
                           );
        foreach (var t in transactions)
            mainDb.AssetStoreTransactions.Add(t);

        await mainDb.SaveChangesAsync();
    }

    public async Task RefundInAppPurchase(
        long groupId,
        InAppProduct product,
        IEnumerable<InAppProductDetails> details,
        string inAppPurchaseRef
    )
    {
        var priceTier = await mainDb.InAppProductPriceTier.FirstOrDefaultAsync(pt => pt.Id == product.InAppProductPriceTierId);
        if (priceTier == null)
            throw new InvalidOperationException("Price tier is invalid");

        var hardCurrency = details.Any() ? details.Sum(pd => pd.HardCurrency ?? 0) : 0;
        var softCurrency = details.Any() ? details.Sum(pd => pd.SoftCurrency ?? 0) : 0;


        var transactions = await assetStoreTransactionGenerationService.InAppPurchaseRefund(
                               groupId,
                               product.Id,
                               details.Where(pd => pd.AssetId != null && pd.AssetType != null)
                                      .Select(pd => new AssetToPurchase {AssetId = pd.AssetId.Value, AssetType = pd.AssetType.Value})
                                      .ToArray(),
                               hardCurrency,
                               softCurrency,
                               priceTier.RefPriceUsdCents
                           );
        foreach (var t in transactions)
        {
            t.InAppPurchaseRef = inAppPurchaseRef;
            mainDb.AssetStoreTransactions.Add(t);
        }

        await mainDb.SaveChangesAsync();
    }

    public Task<bool> IsStoreIdentifierUsed(long groupId, string storeIdentifier)
    {
        return mainDb.InAppPurchaseOrder.AnyAsync(o => o.GroupId != groupId && o.StoreOrderIdentifier == storeIdentifier);
    }
}