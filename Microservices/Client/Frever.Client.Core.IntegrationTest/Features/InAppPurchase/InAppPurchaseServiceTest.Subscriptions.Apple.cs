using FluentAssertions;
using Frever.Client.Core.Features.AppStoreApi;
using Frever.Client.Core.Features.InAppPurchases;
using Frever.Client.Core.IntegrationTest.Features.InAppPurchase.Data;
using Frever.Client.Core.IntegrationTest.Utils;
using Frever.ClientService.Contract.Common;
using Frever.ClientService.Contract.InAppPurchases;
using Frever.Common.IntegrationTesting;
using Frever.Common.IntegrationTesting.Data;
using Frever.Common.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;
using Xunit.Abstractions;

namespace Frever.Client.Core.IntegrationTest.Features.InAppPurchase;

public partial class InAppPurchaseServiceTest(ITestOutputHelper testOut) : IntegrationTestBase
{
    [Fact]
    public async Task InAppPurchase_Subscription_Apple_ShouldBuyNewSubscription()
    {
        var services = new ServiceCollection();
        services.AddClientIntegrationTests(testOut);

        var appleProductRef = "ixia_subscription";
        var transactionId = "2200220202020202202";

        var appStoreClient = new Mock<IAppStoreApiClient>();
        RegisterAppStoreMockForSubscription(appStoreClient, appleProductRef, transactionId);
        services.AddSingleton(appStoreClient.Object);

        await using var provider = services.BuildServiceProvider();

        var dataEnv = provider.GetRequiredService<DataEnvironment>();

        await dataEnv.WithSystemUserAndGroup();
        var user = await dataEnv.WithUserAndGroup();

        provider.SetCurrentUser(user);


        var subscription = await dataEnv.WithInAppProducts(
                               new InAppProductInput
                               {
                                   Title = "Subscription",
                                   IsSubscription = true,
                                   AppleProductRef = appleProductRef,
                                   DailyHardCurrency = 30,
                                   GoogleProductRef = appleProductRef,
                                   MonthlyHardCurrency = 1500
                               }
                           );

        var assetOfferService = provider.GetRequiredService<IInAppProductOfferService>();
        var inAppPurchaseService = provider.GetRequiredService<IInAppPurchaseService>();

        var offers = await assetOfferService.GetOffers();
        offers.SubscriptionOffers.Should().HaveCount(1);

        var order = await inAppPurchaseService.InitInAppPurchase(
                        new InitInAppPurchaseRequest
                        {
                            ClientCurrency = "EUR",
                            ClientCurrencyPrice = 19.99M,
                            InAppProductOfferKey = offers.SubscriptionOffers[0].OfferKey
                        }
                    );
        order.Ok.Should().BeTrue();
        order.ErrorCode.Should().BeNullOrEmpty();
        order.ErrorMessage.Should().BeNullOrEmpty();
        order.PendingOrderId.Should().NotBeEmpty();

        var result = await inAppPurchaseService.CompleteInAppPurchase(
                         new CompleteInAppPurchaseRequest
                         {
                             Platform = Platform.iOS, PendingOrderId = order.PendingOrderId, TransactionData = transactionId
                         }
                     );

        result.Ok.Should().BeTrue();
        result.ErrorCode.Should().BeNull();
        result.ErrorMessage.Should().BeNull();

        var balanceService = provider.GetRequiredService<IBalanceService>();
        await balanceService.CheckBalance(user, 0, 1500, 0);

        var groupSubscriptions = await dataEnv.Db.InAppUserSubscription.Where(s => s.GroupId == user.MainGroupId).ToArrayAsync();
        groupSubscriptions.Should().HaveCount(1);
        groupSubscriptions[0].CompletedAt.Should().BeNull();
        groupSubscriptions[0].StartedAt.Should().BeBefore(DateTime.Now);
        groupSubscriptions[0].MonthlyHardCurrency.Should().Be(subscription[0].MonthlyHardCurrency);
        groupSubscriptions[0].DailyHardCurrency.Should().Be(subscription[0].DailyHardCurrency);
        groupSubscriptions[0].RefInAppProductId.Should().Be(subscription[0].Id);
    }

    [Fact]
    public async Task InAppPurchase_Subscription_Apple_ShouldUpgradeSubscription()
    {
        var services = new ServiceCollection();
        services.AddClientIntegrationTests(testOut);

        var appleProductRefSmall = "ixia_subscription_s";
        var purchaseTransactionId = "2200220202020202202";

        var appleProductRefLarge = "ixia_subscription_m";
        var upgradeTransactionId = "2200220202020202203";

        var appStoreClient = new Mock<IAppStoreApiClient>();
        RegisterAppStoreMockForSubscription(appStoreClient, appleProductRefSmall, purchaseTransactionId);
        RegisterAppStoreMockForSubscription(appStoreClient, appleProductRefLarge, upgradeTransactionId);

        services.AddSingleton(appStoreClient.Object);

        await using var provider = services.BuildServiceProvider();

        var dataEnv = provider.GetRequiredService<DataEnvironment>();

        await dataEnv.WithSystemUserAndGroup();
        var user = await dataEnv.WithUserAndGroup();

        provider.SetCurrentUser(user);


        var subscriptionSmall = (await dataEnv.WithInAppProducts(
                                     new InAppProductInput
                                     {
                                         Title = "Subscription Small",
                                         IsSubscription = true,
                                         AppleProductRef = appleProductRefSmall,
                                         DailyHardCurrency = 30,
                                         GoogleProductRef = appleProductRefSmall,
                                         MonthlyHardCurrency = 1500
                                     }
                                 )).First();

        var subscriptionLarge = (await dataEnv.WithInAppProducts(
                                     new InAppProductInput
                                     {
                                         Title = "Subscription Large",
                                         IsSubscription = true,
                                         AppleProductRef = appleProductRefLarge,
                                         DailyHardCurrency = 30,
                                         GoogleProductRef = appleProductRefLarge,
                                         MonthlyHardCurrency = 3000
                                     }
                                 )).First();

        var assetOfferService = provider.GetRequiredService<IInAppProductOfferService>();
        var inAppPurchaseService = provider.GetRequiredService<IInAppPurchaseService>();

        var offers = await assetOfferService.GetOffers();
        offers.SubscriptionOffers.Should().HaveCount(2);

        var smallOffer = offers.SubscriptionOffers.First(o => o.AppStoreProductRef == appleProductRefSmall);
        var largeOffer = offers.SubscriptionOffers.First(o => o.AppStoreProductRef == appleProductRefLarge);

        // Buy Small Subscription
        var order = await inAppPurchaseService.InitInAppPurchase(
                        new InitInAppPurchaseRequest
                        {
                            ClientCurrency = "EUR", ClientCurrencyPrice = 19.99M, InAppProductOfferKey = smallOffer.OfferKey
                        }
                    );

        order.Ok.Should().BeTrue();
        order.ErrorCode.Should().BeNullOrEmpty();
        order.ErrorMessage.Should().BeNullOrEmpty();
        order.PendingOrderId.Should().NotBeEmpty();

        var result = await inAppPurchaseService.CompleteInAppPurchase(
                         new CompleteInAppPurchaseRequest
                         {
                             Platform = Platform.iOS, PendingOrderId = order.PendingOrderId, TransactionData = purchaseTransactionId
                         }
                     );

        result.Ok.Should().BeTrue();
        result.ErrorCode.Should().BeNull();
        result.ErrorMessage.Should().BeNull();

        var balanceService = provider.GetRequiredService<IBalanceService>();
        await balanceService.CheckBalance(user, 0, 1500, 0);

        var groupSubscriptions = await dataEnv.Db.InAppUserSubscription.Where(s => s.GroupId == user.MainGroupId).ToArrayAsync();
        groupSubscriptions.Should().HaveCount(1);
        groupSubscriptions[0].CompletedAt.Should().BeNull();
        groupSubscriptions[0].StartedAt.Should().BeBefore(DateTime.Now);
        groupSubscriptions[0].MonthlyHardCurrency.Should().Be(subscriptionSmall.MonthlyHardCurrency);
        groupSubscriptions[0].DailyHardCurrency.Should().Be(subscriptionSmall.DailyHardCurrency);
        groupSubscriptions[0].RefInAppProductId.Should().Be(subscriptionSmall.Id);


        // Upgrade to large
        var orderUpgrade = await inAppPurchaseService.InitInAppPurchase(
                               new InitInAppPurchaseRequest
                               {
                                   ClientCurrency = "EUR", ClientCurrencyPrice = 29.99M, InAppProductOfferKey = largeOffer.OfferKey
                               }
                           );

        orderUpgrade.Ok.Should().BeTrue();
        orderUpgrade.ErrorCode.Should().BeNullOrEmpty();
        orderUpgrade.ErrorMessage.Should().BeNullOrEmpty();
        orderUpgrade.PendingOrderId.Should().NotBeEmpty();

        var resultUpgrade = await inAppPurchaseService.CompleteInAppPurchase(
                                new CompleteInAppPurchaseRequest
                                {
                                    Platform = Platform.iOS,
                                    PendingOrderId = orderUpgrade.PendingOrderId,
                                    TransactionData = upgradeTransactionId
                                }
                            );

        resultUpgrade.Ok.Should().BeTrue();
        resultUpgrade.ErrorCode.Should().BeNull();
        resultUpgrade.ErrorMessage.Should().BeNull();

        groupSubscriptions = await dataEnv.Db.InAppUserSubscription.Where(s => s.GroupId == user.MainGroupId)
                                          .OrderByDescending(s => s.Id)
                                          .ToArrayAsync();
        groupSubscriptions.Should().HaveCount(2);
        groupSubscriptions[0].CompletedAt.Should().BeNull();
        groupSubscriptions[0].StartedAt.Should().BeBefore(DateTime.Now);
        groupSubscriptions[0].MonthlyHardCurrency.Should().Be(subscriptionLarge.MonthlyHardCurrency);
        groupSubscriptions[0].DailyHardCurrency.Should().Be(subscriptionLarge.DailyHardCurrency);
        groupSubscriptions[0].RefInAppProductId.Should().Be(subscriptionLarge.Id);

        groupSubscriptions[1].CompletedAt.Should().BeBefore(DateTime.Now);
        groupSubscriptions[1].StartedAt.Should().BeBefore(DateTime.Now);
        groupSubscriptions[1].MonthlyHardCurrency.Should().Be(subscriptionSmall.MonthlyHardCurrency);
        groupSubscriptions[1].DailyHardCurrency.Should().Be(subscriptionSmall.DailyHardCurrency);
        groupSubscriptions[1].RefInAppProductId.Should().Be(subscriptionSmall.Id);
    }

    [Fact]
    public async Task InAppPurchase_Subscription_Apple_ShouldDowngradeSubscription()
    {
        var services = new ServiceCollection();
        services.AddClientIntegrationTests(testOut);

        var appleProductRefSmall = "ixia_subscription_s";
        var purchaseTransactionId = "2200220202020202202";

        var appleProductRefLarge = "ixia_subscription_m";
        var downgradeTransactionId = "2200220202020202203";

        var appStoreClient = new Mock<IAppStoreApiClient>();
        RegisterAppStoreMockForSubscription(appStoreClient, appleProductRefLarge, purchaseTransactionId);
        RegisterAppStoreMockForSubscription(appStoreClient, appleProductRefSmall, downgradeTransactionId);

        services.AddSingleton(appStoreClient.Object);

        await using var provider = services.BuildServiceProvider();

        var dataEnv = provider.GetRequiredService<DataEnvironment>();

        await dataEnv.WithSystemUserAndGroup();
        var user = await dataEnv.WithUserAndGroup();

        provider.SetCurrentUser(user);


        var subscriptionSmall = (await dataEnv.WithInAppProducts(
                                     new InAppProductInput
                                     {
                                         Title = "Subscription Small",
                                         IsSubscription = true,
                                         AppleProductRef = appleProductRefSmall,
                                         DailyHardCurrency = 30,
                                         GoogleProductRef = appleProductRefSmall,
                                         MonthlyHardCurrency = 1500
                                     }
                                 )).First();

        var subscriptionLarge = (await dataEnv.WithInAppProducts(
                                     new InAppProductInput
                                     {
                                         Title = "Subscription Large",
                                         IsSubscription = true,
                                         AppleProductRef = appleProductRefLarge,
                                         DailyHardCurrency = 30,
                                         GoogleProductRef = appleProductRefLarge,
                                         MonthlyHardCurrency = 3000
                                     }
                                 )).First();

        var assetOfferService = provider.GetRequiredService<IInAppProductOfferService>();
        var inAppPurchaseService = provider.GetRequiredService<IInAppPurchaseService>();

        var offers = await assetOfferService.GetOffers();
        offers.SubscriptionOffers.Should().HaveCount(2);

        var smallOffer = offers.SubscriptionOffers.First(o => o.AppStoreProductRef == appleProductRefSmall);
        var largeOffer = offers.SubscriptionOffers.First(o => o.AppStoreProductRef == appleProductRefLarge);

        // Buy Large Subscription
        var largeOrder = await inAppPurchaseService.InitInAppPurchase(
                             new InitInAppPurchaseRequest
                             {
                                 ClientCurrency = "EUR", ClientCurrencyPrice = 29.99M, InAppProductOfferKey = largeOffer.OfferKey
                             }
                         );

        largeOrder.Ok.Should().BeTrue();
        largeOrder.ErrorCode.Should().BeNullOrEmpty();
        largeOrder.ErrorMessage.Should().BeNullOrEmpty();
        largeOrder.PendingOrderId.Should().NotBeEmpty();

        var resultLarge = await inAppPurchaseService.CompleteInAppPurchase(
                              new CompleteInAppPurchaseRequest
                              {
                                  Platform = Platform.iOS,
                                  PendingOrderId = largeOrder.PendingOrderId,
                                  TransactionData = purchaseTransactionId
                              }
                          );

        resultLarge.Ok.Should().BeTrue();
        resultLarge.ErrorCode.Should().BeNull();
        resultLarge.ErrorMessage.Should().BeNull();

        var balanceService = provider.GetRequiredService<IBalanceService>();
        await balanceService.CheckBalance(user, 0, 3000, 0);

        var groupSubscriptions = await dataEnv.Db.InAppUserSubscription.Where(s => s.GroupId == user.MainGroupId).ToArrayAsync();
        groupSubscriptions.Should().HaveCount(1);
        groupSubscriptions[0].CompletedAt.Should().BeNull();
        groupSubscriptions[0].StartedAt.Should().BeBefore(DateTime.Now);
        groupSubscriptions[0].MonthlyHardCurrency.Should().Be(subscriptionLarge.MonthlyHardCurrency);
        groupSubscriptions[0].DailyHardCurrency.Should().Be(subscriptionLarge.DailyHardCurrency);
        groupSubscriptions[0].RefInAppProductId.Should().Be(subscriptionLarge.Id);


        // Upgrade to large
        var orderDowngrade = await inAppPurchaseService.InitInAppPurchase(
                                 new InitInAppPurchaseRequest
                                 {
                                     ClientCurrency = "EUR", ClientCurrencyPrice = 19.99M, InAppProductOfferKey = smallOffer.OfferKey
                                 }
                             );

        orderDowngrade.Ok.Should().BeTrue();
        orderDowngrade.ErrorCode.Should().BeNullOrEmpty();
        orderDowngrade.ErrorMessage.Should().BeNullOrEmpty();
        orderDowngrade.PendingOrderId.Should().NotBeEmpty();

        var resultDowngrade = await inAppPurchaseService.CompleteInAppPurchase(
                                  new CompleteInAppPurchaseRequest
                                  {
                                      Platform = Platform.iOS,
                                      PendingOrderId = orderDowngrade.PendingOrderId,
                                      TransactionData = downgradeTransactionId
                                  }
                              );

        resultDowngrade.Ok.Should().BeTrue();
        resultDowngrade.ErrorCode.Should().BeNull();
        resultDowngrade.ErrorMessage.Should().BeNull();

        groupSubscriptions = await dataEnv.Db.InAppUserSubscription.Where(s => s.GroupId == user.MainGroupId)
                                          .OrderByDescending(s => s.Id)
                                          .ToArrayAsync();
        groupSubscriptions.Should().HaveCount(2);
        groupSubscriptions[0].StartedAt.Should().BeOnOrAfter(DateTime.Now.Date);
        groupSubscriptions[0].CompletedAt.Should().BeNull();
        groupSubscriptions[0].MonthlyHardCurrency.Should().Be(subscriptionSmall.MonthlyHardCurrency);
        groupSubscriptions[0].DailyHardCurrency.Should().Be(subscriptionSmall.DailyHardCurrency);
        groupSubscriptions[0].RefInAppProductId.Should().Be(subscriptionSmall.Id);

        groupSubscriptions[1].StartedAt.Should().BeOnOrBefore(DateTime.Now);
        groupSubscriptions[1].CompletedAt.Should().BeOnOrBefore(DateTime.Now.Date);
        groupSubscriptions[1].MonthlyHardCurrency.Should().Be(subscriptionLarge.MonthlyHardCurrency);
        groupSubscriptions[1].DailyHardCurrency.Should().Be(subscriptionLarge.DailyHardCurrency);
        groupSubscriptions[1].RefInAppProductId.Should().Be(subscriptionLarge.Id);
    }


    [Fact]
    public async Task InAppPurchase_Subscription_Apple_ShouldActivateActualSubscription()
    {
        var services = new ServiceCollection();
        services.AddClientIntegrationTests(testOut);

        var appleProductRefSmall = "ixia_subscription_s";
        var purchaseTransactionId = "2200220202020202202";

        var appleProductRefLarge = "ixia_subscription_m";
        var downgradeTransactionId = "2200220202020202203";

        var appStoreClient = new Mock<IAppStoreApiClient>();
        // On downgrading the subscription isn't activated on Apple until current subscription exipiration
        RegisterAppStoreMockForSubscription(appStoreClient, appleProductRefLarge, purchaseTransactionId);
        RegisterAppStoreMockForSubscription(appStoreClient, appleProductRefLarge, downgradeTransactionId);

        services.AddSingleton(appStoreClient.Object);

        await using var provider = services.BuildServiceProvider();

        var dataEnv = provider.GetRequiredService<DataEnvironment>();

        await dataEnv.WithSystemUserAndGroup();
        var user = await dataEnv.WithUserAndGroup();

        provider.SetCurrentUser(user);


        var subscriptionSmall = (await dataEnv.WithInAppProducts(
                                     new InAppProductInput
                                     {
                                         Title = "Subscription Small",
                                         IsSubscription = true,
                                         AppleProductRef = appleProductRefSmall,
                                         DailyHardCurrency = 30,
                                         GoogleProductRef = appleProductRefSmall,
                                         MonthlyHardCurrency = 1500
                                     }
                                 )).First();

        var subscriptionLarge = (await dataEnv.WithInAppProducts(
                                     new InAppProductInput
                                     {
                                         Title = "Subscription Large",
                                         IsSubscription = true,
                                         AppleProductRef = appleProductRefLarge,
                                         DailyHardCurrency = 30,
                                         GoogleProductRef = appleProductRefLarge,
                                         MonthlyHardCurrency = 3000
                                     }
                                 )).First();

        var assetOfferService = provider.GetRequiredService<IInAppProductOfferService>();
        var inAppPurchaseService = provider.GetRequiredService<IInAppPurchaseService>();

        var offers = await assetOfferService.GetOffers();
        offers.SubscriptionOffers.Should().HaveCount(2);

        var smallOffer = offers.SubscriptionOffers.First(o => o.AppStoreProductRef == appleProductRefSmall);
        var largeOffer = offers.SubscriptionOffers.First(o => o.AppStoreProductRef == appleProductRefLarge);

        // Buy Large Subscription
        var largeOrder = await inAppPurchaseService.InitInAppPurchase(
                             new InitInAppPurchaseRequest
                             {
                                 ClientCurrency = "EUR", ClientCurrencyPrice = 29.99M, InAppProductOfferKey = largeOffer.OfferKey
                             }
                         );

        largeOrder.Ok.Should().BeTrue();
        largeOrder.ErrorCode.Should().BeNullOrEmpty();
        largeOrder.ErrorMessage.Should().BeNullOrEmpty();
        largeOrder.PendingOrderId.Should().NotBeEmpty();

        var resultLarge = await inAppPurchaseService.CompleteInAppPurchase(
                              new CompleteInAppPurchaseRequest
                              {
                                  Platform = Platform.iOS,
                                  PendingOrderId = largeOrder.PendingOrderId,
                                  TransactionData = purchaseTransactionId
                              }
                          );

        resultLarge.Ok.Should().BeTrue();
        resultLarge.ErrorCode.Should().BeNull();
        resultLarge.ErrorMessage.Should().BeNull();

        var balanceService = provider.GetRequiredService<IBalanceService>();
        await balanceService.CheckBalance(user, 0, 3000, 0);

        var groupSubscriptions = await dataEnv.Db.InAppUserSubscription.Where(s => s.GroupId == user.MainGroupId).ToArrayAsync();
        groupSubscriptions.Should().HaveCount(1);
        groupSubscriptions[0].CompletedAt.Should().BeNull();
        groupSubscriptions[0].StartedAt.Should().BeBefore(DateTime.Now);
        groupSubscriptions[0].MonthlyHardCurrency.Should().Be(subscriptionLarge.MonthlyHardCurrency);
        groupSubscriptions[0].DailyHardCurrency.Should().Be(subscriptionLarge.DailyHardCurrency);
        groupSubscriptions[0].RefInAppProductId.Should().Be(subscriptionLarge.Id);


        // Upgrade to large
        var orderDowngrade = await inAppPurchaseService.InitInAppPurchase(
                                 new InitInAppPurchaseRequest
                                 {
                                     ClientCurrency = "EUR", ClientCurrencyPrice = 19.99M, InAppProductOfferKey = smallOffer.OfferKey
                                 }
                             );

        orderDowngrade.Ok.Should().BeTrue();
        orderDowngrade.ErrorCode.Should().BeNullOrEmpty();
        orderDowngrade.ErrorMessage.Should().BeNullOrEmpty();
        orderDowngrade.PendingOrderId.Should().NotBeEmpty();

        var resultDowngrade = await inAppPurchaseService.CompleteInAppPurchase(
                                  new CompleteInAppPurchaseRequest
                                  {
                                      Platform = Platform.iOS,
                                      PendingOrderId = orderDowngrade.PendingOrderId,
                                      TransactionData = downgradeTransactionId
                                  }
                              );

        resultDowngrade.Ok.Should().BeTrue();
        resultDowngrade.ErrorCode.Should().BeNull();
        resultDowngrade.ErrorMessage.Should().BeNull();

        // Large subscription should still be active
        groupSubscriptions = await dataEnv.Db.InAppUserSubscription.Where(s => s.GroupId == user.MainGroupId)
                                          .OrderByDescending(s => s.Id)
                                          .ToArrayAsync();
        groupSubscriptions.Should().HaveCount(1);
        groupSubscriptions[0].StartedAt.Should().BeOnOrAfter(DateTime.Now.Date);
        groupSubscriptions[0].CompletedAt.Should().BeNull();
        groupSubscriptions[0].MonthlyHardCurrency.Should().Be(subscriptionLarge.MonthlyHardCurrency);
        groupSubscriptions[0].DailyHardCurrency.Should().Be(subscriptionLarge.DailyHardCurrency);
        groupSubscriptions[0].RefInAppProductId.Should().Be(subscriptionLarge.Id);
    }


    private void RegisterAppStoreMockForSubscription(
        Mock<IAppStoreApiClient> mock,
        string inAppProductId,
        string transactionId,
        bool isSubscriptionActive = true,
        string downgradedToSubscriptionId = null
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(inAppProductId);
        ArgumentException.ThrowIfNullOrWhiteSpace(transactionId);

        downgradedToSubscriptionId ??= inAppProductId;

        var appStoreTransactionStatus = new AppStoreTransactionStatus
                                        {
                                            Environment = "Sandbox",
                                            BundleId = "com.frever.client-ios",
                                            IsSubscription = true,
                                            IsRefunded = false,
                                            IsValid = true,
                                            TransactionId = transactionId,
                                            InAppProductId = inAppProductId
                                        };

        var subscriptionStatus = new SubscriptionStatus
                                 {
                                     IsSubscriptionActive = isSubscriptionActive,
                                     LastTransactions =
                                     [
                                         new SubscriptionTransactionData
                                         {
                                             Status = 1,
                                             IsActive = true,
                                             TransactionInfo = appStoreTransactionStatus,
                                             RenewalInfo = new SubscriptionRenewalInfo
                                                           {
                                                               Environment = "Sandbox",
                                                               ProductId = downgradedToSubscriptionId,
                                                               RenewalDate = DateTimeOffset.Now.AddDays(1),
                                                               OriginalTransactionId = transactionId,
                                                               AutoRenewalProductId = downgradedToSubscriptionId,
                                                               RecentSubscriptionStartDate = DateTimeOffset.Now.AddHours(-1),
                                                               IsInBillingRetryPeriod = false
                                                           }
                                         }
                                     ]
                                 };

        mock.Setup(s => s.CheckAppStoreTransactionStatus(transactionId)).ReturnsAsync(appStoreTransactionStatus);
        mock.Setup(s => s.CheckSubscriptionStatus(transactionId)).ReturnsAsync(subscriptionStatus);
    }
}