using System;
using System.Threading.Tasks;
using AuthServer.Permissions.Services;
using AuthServer.Permissions.Sub13;
using AuthServerShared;
using Common.Infrastructure;
using FluentValidation;
using Frever.Client.Core.Features.InAppPurchases.DataAccess;
using Frever.Client.Core.Features.InAppPurchases.Subscriptions;
using Frever.ClientService.Contract.Common;
using Frever.ClientService.Contract.InAppPurchases;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Frever.Client.Core.Features.InAppPurchases.InAppPurchase;

public partial class InAppPurchaseService : IInAppPurchaseService
{
    private readonly IInAppAssetPurchaseManager _assetPurchaseManager;
    private readonly IValidator<CompleteInAppPurchaseRequest> _completeInAppPurchaseRequestValidator;
    private readonly UserInfo _currentUser;
    private readonly IInAppProductRepository _inAppProductRepository;
    private readonly IValidator<InitInAppPurchaseRequest> _initInAppPurchaseRequestValidator;
    private readonly ILogger _log;
    private readonly IInAppProductOfferService _offerService;
    private readonly IParentalConsentValidationService _parentalConsentValidation;
    private readonly IPendingOrderManager _pendingOrderManager;
    private readonly IPurchaseOrderRepository _purchaseOrderRepository;
    private readonly IStoreTransactionDataValidator _storeTransactionDataValidator;
    private readonly IInAppSubscriptionManager _subscriptionManager;
    private readonly IUserPermissionService _userPermissionService;

    public InAppPurchaseService(
        ILoggerFactory loggerFactory,
        UserInfo currentUser,
        IUserPermissionService userPermissionService,
        IValidator<InitInAppPurchaseRequest> initInAppPurchaseRequestValidator,
        IValidator<CompleteInAppPurchaseRequest> completeInAppPurchaseRequestValidator,
        IInAppProductRepository inAppProductRepository,
        IPendingOrderManager pendingOrderManager,
        IStoreTransactionDataValidator storeTransactionDataValidator,
        IInAppAssetPurchaseManager assetPurchaseManager,
        IInAppProductOfferService offerService,
        IParentalConsentValidationService parentalConsentValidation,
        IInAppSubscriptionManager subscriptionManager,
        IPurchaseOrderRepository purchaseOrderRepository
    )
    {
        if (subscriptionManager == null)
            throw new ArgumentNullException(nameof(subscriptionManager));
        if (purchaseOrderRepository == null)
            throw new ArgumentNullException(nameof(purchaseOrderRepository));

        ArgumentNullException.ThrowIfNull(loggerFactory);
        _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
        _userPermissionService = userPermissionService ?? throw new ArgumentNullException(nameof(userPermissionService));
        _initInAppPurchaseRequestValidator = initInAppPurchaseRequestValidator ??
                                             throw new ArgumentNullException(nameof(initInAppPurchaseRequestValidator));
        _inAppProductRepository = inAppProductRepository ?? throw new ArgumentNullException(nameof(inAppProductRepository));
        _pendingOrderManager = pendingOrderManager ?? throw new ArgumentNullException(nameof(pendingOrderManager));
        _completeInAppPurchaseRequestValidator = completeInAppPurchaseRequestValidator ??
                                                 throw new ArgumentNullException(nameof(completeInAppPurchaseRequestValidator));
        _storeTransactionDataValidator =
            storeTransactionDataValidator ?? throw new ArgumentNullException(nameof(storeTransactionDataValidator));
        _assetPurchaseManager = assetPurchaseManager ?? throw new ArgumentNullException(nameof(assetPurchaseManager));
        _offerService = offerService ?? throw new ArgumentNullException(nameof(offerService));
        _parentalConsentValidation = parentalConsentValidation ?? throw new ArgumentNullException(nameof(parentalConsentValidation));
        _subscriptionManager = subscriptionManager;
        _purchaseOrderRepository = purchaseOrderRepository;

        _log = loggerFactory.CreateLogger("Frever.InAppPurchase");
    }

    public async Task<InitInAppPurchaseResponse> InitInAppPurchase(InitInAppPurchaseRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        _log.LogInformation(
            "Init purchasing premium pass for group {GroupId}, product ID {OfferKey}",
            _currentUser.UserMainGroupId,
            request.InAppProductOfferKey
        );

        await _userPermissionService.EnsureCurrentUserActive();
        await _parentalConsentValidation.EnsureInAppPurchasesAllowed();

        await _initInAppPurchaseRequestValidator.ValidateAndThrowAsync(request);

        await _subscriptionManager.RenewSubscriptionTokens();

        var requestedProduct = await _offerService.GetInAppProductOfferLimited(request.InAppProductOfferKey);
        if (requestedProduct == null)
            throw AppErrorWithStatusCodeException.BadRequest("Invalid in-app product", "InvalidInAppProduct");

        await using var transaction = await _inAppProductRepository.BeginTransaction();

        var existingOrder = await _pendingOrderManager.GetExistingPendingOrder(requestedProduct.Id).FirstOrDefaultAsync();
        if (existingOrder != null)
            await _pendingOrderManager.DiscardPendingOrder(_currentUser.UserMainGroupId, existingOrder.Id);

        var pendingOrder = await _pendingOrderManager.PlacePendingOrder(
                               requestedProduct,
                               request.ClientCurrency,
                               request.ClientCurrencyPrice
                           );

        await transaction.Commit();

        _log.LogInformation("In-app purchase init: order ID={}, request={}", pendingOrder.Id, JsonConvert.SerializeObject(request));

        return new InitInAppPurchaseResponse {Ok = true, PendingOrderId = pendingOrder.Id};
    }

    public async Task<CompleteInAppPurchaseResponse> CompleteInAppPurchase(CompleteInAppPurchaseRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        using var scope = _log.BeginScope(
            "CompleteInAppPurchase OrderId={pendingOrderId} Platform={platform} TransactionData={transactionData} :",
            request.PendingOrderId,
            request.Platform,
            request.TransactionData
        );

        _log.LogInformation("Start completing order");

        await _completeInAppPurchaseRequestValidator.ValidateAndThrowAsync(request);

        await using var transaction = await _inAppProductRepository.BeginTransaction();

        var pendingOrder = await _pendingOrderManager.GetPendingOrder(request.PendingOrderId);

        if (pendingOrder == null)
            throw AppErrorWithStatusCodeException.BadRequest("Pending order ID is invalid", "InvalidPendingOrder");
        if (!pendingOrder.IsPending)
            throw AppErrorWithStatusCodeException.BadRequest("Pending order ID already completed", "CompletedPendingOrder");

        if (pendingOrder.PremiumPassPurchase == true)
            throw AppErrorWithStatusCodeException.BadRequest("No season running currently", "NoCurrentSeason");

        var inAppProduct = await _inAppProductRepository.GetActiveInAppProducts()
                                                        .FirstOrDefaultAsync(p => p.Id == pendingOrder.InAppProductId);

        if (inAppProduct == null)
            throw AppErrorWithStatusCodeException.BadRequest("Invalid in-app product", "InvalidInAppProduct");

        if (inAppProduct.IsFreeProduct)
        {
            _log.LogInformation("Product is free product");
            await _pendingOrderManager.CompletePendingOrder(
                pendingOrder.Id,
                request.Platform,
                request.TransactionData,
                $"free_product_{Guid.NewGuid():N}",
                "production"
            );
        }
        else
        {
            var transactionDataValidationResult = await _storeTransactionDataValidator.ValidateStoreTransaction(
                                                      new ValidateStoreTransactionRequest
                                                      {
                                                          AppStoreProductRef = inAppProduct.AppStoreProductRef,
                                                          Platform = request.Platform,
                                                          TransactionData = request.TransactionData,
                                                          IsSubscription = inAppProduct.IsSubscription,
                                                          PlayMarketProductRef = inAppProduct.PlayMarketProductRef
                                                      }
                                                  );

            _log.LogInformation(
                "Transaction data validated: data={transactionData}",
                JsonConvert.SerializeObject(transactionDataValidationResult)
            );

            if (!transactionDataValidationResult.IsValid)
            {
                _log.LogError("Error completing order: {error}", transactionDataValidationResult.Error);

                await _pendingOrderManager.SetPendingOrderError(
                    pendingOrder.Id,
                    request.Platform,
                    request.TransactionData,
                    transactionDataValidationResult.Error
                );

                throw AppErrorWithStatusCodeException.BadRequest("Invalid receipt data", "InvalidStoreReceipt");
            }

            if (transactionDataValidationResult.IsSubscription)
            {
                await _pendingOrderManager.CompletePendingOrder(
                    pendingOrder.Id,
                    request.Platform,
                    request.TransactionData,
                    transactionDataValidationResult.StoreOrderIdentifier,
                    transactionDataValidationResult.Environment
                );

                if (String.IsNullOrWhiteSpace(transactionDataValidationResult.ActiveSubscriptionProductSku))
                {
                    await _subscriptionManager.CancelAllSubscriptions();
                }
                else
                {
                    var activeSubscriptionSku = transactionDataValidationResult.ActiveSubscriptionProductSku;
                    var subscriptionProduct = request.Platform == Platform.iOS
                                                  ? await _inAppProductRepository.GetActiveInAppProducts()
                                                                                 .FirstOrDefaultAsync(
                                                                                      p => p.AppStoreProductRef == activeSubscriptionSku
                                                                                  )
                                                  : await _inAppProductRepository.GetActiveInAppProducts()
                                                                                 .FirstOrDefaultAsync(
                                                                                      p => p.PlayMarketProductRef == activeSubscriptionSku
                                                                                  );

                    if (subscriptionProduct == null)
                    {
                        _log.LogWarning(
                            "No in-app product with SKU={activeSubscriptionSku} found",
                            transactionDataValidationResult.ActiveSubscriptionProductSku
                        );
                    }
                    else
                    {
                        _log.LogInformation("Activating subscription for subscription product ID={inAppProductId}", subscriptionProduct.Id);
                        await _subscriptionManager.ActivateSubscription(pendingOrder.Id, subscriptionProduct.Id);
                    }
                }
            }
            else
            {
                if ((request.Platform == Platform.iOS && !StringComparer.OrdinalIgnoreCase.Equals(
                         inAppProduct.AppStoreProductRef,
                         transactionDataValidationResult.AppProductSku
                     )) || (request.Platform != Platform.iOS && !StringComparer.OrdinalIgnoreCase.Equals(
                                inAppProduct.PlayMarketProductRef,
                                transactionDataValidationResult.AppProductSku
                            )))
                {
                    _log.LogError("Transaction contains data from another product");
                    throw AppErrorWithStatusCodeException.BadRequest(
                        "Transaction data is from another product",
                        "STORE_TRANSACTION_FROM_ANOTHER_PRODUCT"
                    );
                }

                if (await _assetPurchaseManager.IsStoreIdentifierUsed(_currentUser, transactionDataValidationResult.StoreOrderIdentifier))
                {
                    _log.LogError("Store Order ID used in another product from another user");
                    throw AppErrorWithStatusCodeException.BadRequest(
                        "Store order identifier already registered",
                        "STORE_ORDER_ID_ALREADY_USED"
                    );
                }

                await _pendingOrderManager.CompletePendingOrder(
                    pendingOrder.Id,
                    request.Platform,
                    request.TransactionData,
                    transactionDataValidationResult.StoreOrderIdentifier,
                    transactionDataValidationResult.Environment
                );

                _log.LogInformation("Purchase completed successfully");
            }
        }

        var productDetails = await _inAppProductRepository.GetActiveInAppProductDetails(inAppProduct.Id).ToArrayAsync();

        await _assetPurchaseManager.CompleteTransferOwnership(_currentUser, inAppProduct, productDetails, request.TransactionData);

        await transaction.Commit();

        await _offerService.MarkOfferAsPurchased(pendingOrder.InAppProductOfferKey);

        return new CompleteInAppPurchaseResponse {Ok = true};
    }

    public async Task CancelAllSubscriptions()
    {
        await _subscriptionManager.CancelAllSubscriptions();
    }
}