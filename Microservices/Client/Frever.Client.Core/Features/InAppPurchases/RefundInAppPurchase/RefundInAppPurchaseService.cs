using System;
using System.Linq;
using System.Threading.Tasks;
using Common.Infrastructure;
using FluentValidation;
using Frever.Client.Core.Features.InAppPurchases.DataAccess;
using Frever.ClientService.Contract.Common;
using Frever.ClientService.Contract.InAppPurchases;
using Frever.Shared.AssetStore.OfferKeyCodec;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Frever.Client.Core.Features.InAppPurchases.RefundInAppPurchase;

public class RefundInAppPurchaseService : IRefundInAppPurchaseService
{
    private readonly ILogger _log;
    private readonly IInAppProductOfferKeyCodec _offerKeyCodec;
    private readonly IInAppAssetPurchaseManager _purchaseManager;
    private readonly IPurchaseOrderRepository _purchaseOrderRepository;
    private readonly IRefundRepository _refundRepository;
    private readonly IValidator<RefundInAppPurchaseRequest> _refundRequestValidator;

    public RefundInAppPurchaseService(
        IValidator<RefundInAppPurchaseRequest> refundRequestValidator,
        IPurchaseOrderRepository purchaseOrderRepository,
        ILoggerFactory loggerFactory,
        IInAppProductOfferKeyCodec offerKeyCodec,
        IRefundRepository refundRepository,
        IInAppAssetPurchaseManager purchaseManager
    )
    {
        ArgumentNullException.ThrowIfNull(loggerFactory);

        _refundRequestValidator = refundRequestValidator ?? throw new ArgumentNullException(nameof(refundRequestValidator));
        _purchaseOrderRepository = purchaseOrderRepository ?? throw new ArgumentNullException(nameof(purchaseOrderRepository));
        _offerKeyCodec = offerKeyCodec ?? throw new ArgumentNullException(nameof(offerKeyCodec));
        _refundRepository = refundRepository ?? throw new ArgumentNullException(nameof(refundRepository));
        _purchaseManager = purchaseManager ?? throw new ArgumentNullException(nameof(purchaseManager));
        _log = loggerFactory.CreateLogger("Frever.InAppPurchase.Refund");
    }

    public async Task RefundInAppPurchase(RefundInAppPurchaseRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        using var _ = _log.BeginScope("Refund in-app purchase {p} {oid}: ", request.Platform, request.StoreOrderIdentifier);

        await _refundRequestValidator.ValidateAndThrowAsync(request);

        var order = await _purchaseOrderRepository.GetCompletedOrderByStoreOrderIdentifier(request.StoreOrderIdentifier)
                                                  .FirstOrDefaultAsync();
        if (order == null)
        {
            _log.LogError("Order not found");
            throw AppErrorWithStatusCodeException.BadRequest($"Order {request.StoreOrderIdentifier} not found", "OrderNotFound");
        }

        if (order.WasRefund)
        {
            _log.LogWarning("Order has been refund before");
            return;
        }

        _log.LogInformation("InAppPurchase Order {oid}, offer key {ok}", order.Id, order.InAppProductOfferKey);

        var offer = await _offerKeyCodec.DecodeUnsafe(order.InAppProductOfferKey);

        _log.LogInformation(
            "Order includes in-app product ID={pid}, details={d}",
            offer.InAppProductId,
            string.Join(",", offer.InAppProductDetailIds ?? Enumerable.Empty<long>())
        );

        await using var transaction = await _refundRepository.BeginTransaction();

        var inAppProduct = await _refundRepository.GetAllInAppProducts().Where(p => p.Id == offer.InAppProductId).FirstOrDefaultAsync();
        if (inAppProduct == null)
            throw AppErrorWithStatusCodeException.BadRequest($"In-App Product {offer.InAppProductId} not found", "InAppProductNotFound");

        var details = await _refundRepository.GetAllInAppProductDetails(inAppProduct.Id)
                                             .Where(d => offer.InAppProductDetailIds.Contains(d.Id))
                                             .ToArrayAsync();

        await _purchaseManager.RefundInAppPurchase(order.GroupId, inAppProduct, details, request.StoreOrderIdentifier);

        order.WasRefund = true;
        await _purchaseOrderRepository.SaveChanges();

        await transaction.CommitAsync();
    }

    public IQueryable<InAppPurchaseOrderInfo> GetNotRefundOrders(Platform platform, string[] storeOrderIdentifiers)
    {
        var platformName = platform.ToString("G");
        return _refundRepository.GetNonRefundOrders(storeOrderIdentifiers)
                                .Where(o => o.Platform == platformName)
                                .Select(
                                     o => new InAppPurchaseOrderInfo
                                          {
                                              Id = o.Id, Platform = o.Platform, StoreOrderIdentifier = o.StoreOrderIdentifier
                                          }
                                 );
    }
}