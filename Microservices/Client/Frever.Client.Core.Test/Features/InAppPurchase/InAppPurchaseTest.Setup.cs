using AuthServer.Permissions.Services;
using AuthServer.Permissions.Sub13;
using AuthServerShared;
using Common.Infrastructure.Database;
using Frever.Client.Core.Features.InAppPurchases;
using Frever.Client.Core.Features.InAppPurchases.Contract;
using Frever.Client.Core.Features.InAppPurchases.DataAccess;
using Frever.Client.Core.Features.InAppPurchases.InAppPurchase;
using Frever.Client.Core.Features.InAppPurchases.Subscriptions;
using Frever.Common.Testing;
using Frever.Shared.MainDb.Entities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MockQueryable.Moq;
using Moq;
using Xunit.Abstractions;

namespace Frever.Client.Core.Test.Features.InAppPurchase;

public partial class InAppPurchaseTest
{
    private readonly StoreTransactionValidationResult _storeTransactionValidationResult = new()
                                                                                          {
                                                                                              Environment = "unit_test",
                                                                                              AppProductSku = "test_product_ios",
                                                                                              IsValid = true,
                                                                                              StoreOrderIdentifier = "xx22xx44"
                                                                                          };

    private readonly Mock<IInAppAssetPurchaseManager> assetPurchaseManager = new();

    private readonly InAppProductDetails inAppProductDetails = new()
                                                               {
                                                                   Id = 1111,
                                                                   Title = "test detail",
                                                                   AssetId = 11,
                                                                   AssetType = AssetStoreAssetType.Wardrobe,
                                                                   IsActive = true,
                                                                   UniqueOfferGroup = 1,
                                                                   InAppProductId = 111
                                                               };

    private readonly Mock<IInAppProductRepository> inAppProductRepository = new();
    private readonly Mock<IInAppProductOfferService> offerService = new();

    private readonly InAppPurchaseOrder order = new()
                                                {
                                                    Id = Guid.NewGuid(),
                                                    Platform = "iOS",
                                                    Receipt = "test_offer_receipt",
                                                    CreatedTime = DateTime.Now,
                                                    GroupId = 11,
                                                    IsPending = true,
                                                    InAppProductId = 111,
                                                    InAppProductOfferKey = "TOFF"
                                                };

    private readonly Mock<IParentalConsentValidationService> parentalConsentValidator = new();
    private readonly Mock<IPendingOrderManager> pendingOrderManager = new();

    private readonly InAppProduct product = new()
                                            {
                                                Id = 111,
                                                Title = "test product",
                                                IsActive = true,
                                                AppStoreProductRef = "test_product_ios",
                                                PlayMarketProductRef = "test_product_google"
                                            };

    private readonly Mock<IPurchaseOrderRepository> purchaseOrderRepo = new Mock<IPurchaseOrderRepository>();

    private readonly Mock<IStoreTransactionDataValidator> receiptValidator = new();
    private readonly ITestOutputHelper testOut;

    public InAppPurchaseTest(ITestOutputHelper testOut)
    {
        this.testOut = testOut;

        inAppProductRepository.Setup(s => s.GetActiveInAppProducts()).Returns(new List<InAppProduct> {product}.BuildMock());
        inAppProductRepository.Setup(s => s.GetActiveInAppProductDetails(It.IsAny<long>()))
                              .Returns(new List<InAppProductDetails> {inAppProductDetails}.BuildMock());

        pendingOrderManager.Setup(s => s.GetExistingPendingOrder(It.IsAny<long>())).Returns(new List<InAppPurchaseOrder>().BuildMock());
        pendingOrderManager.Setup(s => s.PlacePendingOrder(It.IsAny<InAppProductOffer>(), It.IsAny<string>(), It.IsAny<decimal>()))
                           .ReturnsAsync(order);
        pendingOrderManager.Setup(s => s.GetPendingOrder(It.IsAny<Guid>())).ReturnsAsync(order);

        receiptValidator.Setup(s => s.ValidateStoreTransaction(It.IsAny<ValidateStoreTransactionRequest>()))
                        .ReturnsAsync(_storeTransactionValidationResult);

        offerService.Setup(s => s.GetInAppProductOfferLimited(It.IsAny<string>()))
                    .ReturnsAsync(
                         new InAppProductOffer
                         {
                             Id = 11,
                             Description = "test offer description",
                             Title = "test offer",
                             OfferKey = "test_offer",
                             AppStoreProductRef = "test_offer_apple",
                             PlayMarketProductRef = "test_offer_google",
                             Details =
                             [
                                 new InAppProductOfferDetails
                                 {
                                     Id = 111,
                                     Title = "test offer details",
                                     Description = "test offer details description",
                                     HardCurrency = 1
                                 }
                             ]
                         }
                     );
    }

    private IInAppPurchaseService CreateTestService(IServiceProvider provider)
    {
        ArgumentNullException.ThrowIfNull(provider);

        var transaction = provider.GetRequiredService<TransactionMockManager>();
        inAppProductRepository.Setup(s => s.BeginTransaction()).ReturnsAsync(new NestedTransaction());

        var subscriptionManager = new Mock<IInAppSubscriptionManager>();

        return new InAppPurchaseService(
            provider.GetRequiredService<ILoggerFactory>(),
            provider.GetRequiredService<UserInfo>(),
            provider.GetRequiredService<IUserPermissionService>(),
            new InitInAppPurchaseRequestValidator(),
            new CompleteInAppPurchaseRequestValidator(),
            inAppProductRepository.Object,
            pendingOrderManager.Object,
            receiptValidator.Object,
            assetPurchaseManager.Object,
            offerService.Object,
            parentalConsentValidator.Object,
            subscriptionManager.Object,
            purchaseOrderRepo.Object
        );
    }
}