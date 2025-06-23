using FluentAssertions;
using Frever.Client.Core.Features.InAppPurchases;
using Frever.Client.Core.Features.InAppPurchases.InAppPurchase;
using Frever.Client.Core.Features.Social.MyProfileInfo;
using Frever.Client.Core.IntegrationTest.Features.InAppPurchase.Data;
using Frever.Client.Core.IntegrationTest.Utils;
using Frever.ClientService.Contract.InAppPurchases;
using Frever.Common.IntegrationTesting;
using Frever.Common.IntegrationTesting.Data;
using Frever.Common.Testing;
using Frever.Shared.AssetStore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;
using Xunit.Abstractions;
using Platform = Frever.ClientService.Contract.Common.Platform;

namespace Frever.Client.Core.IntegrationTest.Features.InAppPurchase;

public class InAppPurchaseIntegrationTest(ITestOutputHelper testOut) : IntegrationTestBase
{
    [Theory]
    [InlineData(Platform.iOS, "iosreceipt")]
    [InlineData(Platform.Android, "androidreceipt")]
    public async Task InAppPurchase_PurchaseHardCurrency(Platform platform, string receipt)
    {
        var services = new ServiceCollection();

        var (p, dataEnv) = await Prepare(platform, services, false);
        await using var provider = p;

        var user = await dataEnv.WithUserAndGroup(new UserAndGroupCreateParams {CountryIso3 = "swe", LanguageIso3 = "swe"});
        provider.SetCurrentUser(user);

        await dataEnv.WithInAppProducts(
            new InAppProductInput
            {
                Title = "Tests offer",
                AppleProductRef = "apple_wardrobe",
                GoogleProductRef = "google_wardrobe",
                Details =
                [
                    new InAppProductDetailsInput {HardCurrency = 20}
                ]
            }
        );

        // Act
        var myProfileService = provider.GetRequiredService<IMyProfileService>();
        var assetOfferService = provider.GetRequiredService<IInAppProductOfferService>();
        var testService = provider.GetRequiredService<IInAppPurchaseService>();

        var originalBalance = await myProfileService.GetMyBalance();

        var offers = await assetOfferService.GetOffers();
        offers.HardCurrencyOffers.Should().HaveCountGreaterThan(0);

        var purchaseInfo = await testService.InitInAppPurchase(
                               new InitInAppPurchaseRequest
                               {
                                   ClientCurrency = "USD",
                                   ClientCurrencyPrice = 10.0M,
                                   InAppProductOfferKey = offers.HardCurrencyOffers.First().OfferKey
                               }
                           );

        purchaseInfo.Ok.Should().BeTrue();
        purchaseInfo.ErrorCode.Should().BeNullOrEmpty();
        purchaseInfo.ErrorMessage.Should().BeNullOrEmpty();
        purchaseInfo.PendingOrderId.Should().NotBeEmpty();

        var result = await testService.CompleteInAppPurchase(
                         new CompleteInAppPurchaseRequest
                         {
                             Platform = platform, PendingOrderId = purchaseInfo.PendingOrderId, TransactionData = receipt
                         }
                     );

        result.Ok.Should().BeTrue();
        result.ErrorCode.Should().BeNullOrEmpty();
        result.ErrorMessage.Should().BeNullOrEmpty();

        var updatedBalance = await myProfileService.GetMyBalance();
        updatedBalance.Should().NotBeNull();
        updatedBalance.HardCurrencyAmount.Should()
                      .Be((originalBalance?.HardCurrencyAmount ?? 0) + 20, "20 of hard currency should be purchased");
    }

    [Theory]
    [InlineData(Platform.iOS, "iosreceipt")]
    public async Task InAppPurchase_PurchaseSubscription(Platform platform, string receipt)
    {
        var services = new ServiceCollection();

        var (p, dataEnv) = await Prepare(platform, services, true, "apple_subscription");
        await using var provider = p;

        var user = await dataEnv.WithUserAndGroup(new UserAndGroupCreateParams {CountryIso3 = "swe", LanguageIso3 = "swe"});
        provider.SetCurrentUser(user);

        var product = (await dataEnv.WithInAppProducts(
                           new InAppProductInput
                           {
                               Title = "Tests offer",
                               AppleProductRef = "apple_subscription",
                               GoogleProductRef = "google_subscription",
                               IsSubscription = true,
                               DailyHardCurrency = 40,
                               MonthlyHardCurrency = 1600
                           }
                       )).First();

        // Act
        var myProfileService = provider.GetRequiredService<IMyProfileService>();
        var assetOfferService = provider.GetRequiredService<IInAppProductOfferService>();
        var testService = provider.GetRequiredService<IInAppPurchaseService>();

        var originalBalance = await myProfileService.GetMyBalance();
        originalBalance.DailyTokens.Should().Be(0);
        originalBalance.SubscriptionTokens.Should().Be(0);
        originalBalance.Subscription.Should().BeNull();
        originalBalance.PermanentTokens.Should().Be(0);

        var offers = await assetOfferService.GetOffers();
        offers.SubscriptionOffers.Should().HaveCount(1);

        var purchaseInfo = await testService.InitInAppPurchase(
                               new InitInAppPurchaseRequest
                               {
                                   ClientCurrency = "USD",
                                   ClientCurrencyPrice = 10.0M,
                                   InAppProductOfferKey = offers.SubscriptionOffers.First().OfferKey
                               }
                           );

        purchaseInfo.Ok.Should().BeTrue();
        purchaseInfo.ErrorCode.Should().BeNullOrEmpty();
        purchaseInfo.ErrorMessage.Should().BeNullOrEmpty();
        purchaseInfo.PendingOrderId.Should().NotBeEmpty();

        var result = await testService.CompleteInAppPurchase(
                         new CompleteInAppPurchaseRequest
                         {
                             Platform = platform, PendingOrderId = purchaseInfo.PendingOrderId, TransactionData = receipt
                         }
                     );

        result.Ok.Should().BeTrue();
        result.ErrorCode.Should().BeNullOrEmpty();
        result.ErrorMessage.Should().BeNullOrEmpty();

        var updatedBalance = await myProfileService.GetMyBalance();
        updatedBalance.Should().NotBeNull();
        updatedBalance.HardCurrencyAmount.Should().Be(1600);

        updatedBalance.Subscription.Should().Be(product.Title);
    }

    private async Task<(ServiceProvider provider, DataEnvironment dataEnv)> Prepare(
        Platform platform,
        ServiceCollection services,
        bool isSubscription,
        string expectedProductSKU = null
    )
    {
        expectedProductSKU ??= platform == Platform.iOS ? "apple_wardrobe" : "google_wardrobe";
        services.AddClientIntegrationTests(testOut);
        services.AddSingleton(
            new AssetStoreOptions
            {
                OfferKeySecret = "ABC",
                SystemUserEmail = "test@frever.test",
                CustomerSupportUserEmail = "test@frever.test",
                RealMoneyUserEmail = "test@frever.test"
            }
        );

        var receiptValidator = new Mock<IStoreTransactionDataValidator>();
        receiptValidator.Setup(s => s.ValidateStoreTransaction(It.IsAny<ValidateStoreTransactionRequest>()))
                        .ReturnsAsync(
                             new StoreTransactionValidationResult
                             {
                                 IsValid = true,
                                 Environment = "integration-test",
                                 AppProductSku = expectedProductSKU,
                                 StoreOrderIdentifier = Guid.NewGuid().ToString("N"),
                                 IsSubscription = isSubscription,
                                 ActiveSubscriptionProductSku = isSubscription ? expectedProductSKU : null
                             }
                         );

        receiptValidator.Setup(s => s.ValidateSubscription(It.IsAny<Platform>(), It.IsAny<string>()))
                        .ReturnsAsync(new SubscriptionValidationResult {IsActive = true});

        services.AddSingleton(receiptValidator.Object);

        var provider = services.BuildServiceProvider();

        var dataEnv = provider.GetRequiredService<DataEnvironment>();

        await dataEnv.WithUserAndGroup(
            new UserAndGroupCreateParams {Email = "test@frever.test", CountryIso3 = "swe", LanguageIso3 = "swe"}
        );

        return (provider, dataEnv);
    }
}