using Common.Infrastructure;
using FluentAssertions;
using Frever.Client.Core.Features.InAppPurchases.Contract;
using Frever.Client.Core.Test.Utils;
using Frever.ClientService.Contract.InAppPurchases;
using Frever.Shared.MainDb.Entities;
using Microsoft.Extensions.DependencyInjection;
using MockQueryable.Moq;
using Moq;
using Xunit;

namespace Frever.Client.Core.Test.Features.InAppPurchase;

public partial class InAppPurchaseTest
{
    [Fact(DisplayName = "👍👎👎Init in-app purchase should work")]
    public async Task InAppPurchase_Init_HappyPath()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddTestClientServices(testOut);

        var order = new InAppPurchaseOrder
                    {
                        Id = Guid.NewGuid(),
                        Platform = "iOS",
                        Receipt = "test_offer_receipt",
                        CreatedTime = DateTime.Now,
                        GroupId = 11,
                        IsPending = true,
                        InAppProductId = 111
                    };

        pendingOrderManager.Setup(s => s.GetExistingPendingOrder(It.IsAny<long>())).Returns(new List<InAppPurchaseOrder>().BuildMock());
        pendingOrderManager.Setup(s => s.PlacePendingOrder(It.IsAny<InAppProductOffer>(), It.IsAny<string>(), It.IsAny<decimal>()))
                           .ReturnsAsync(order);

        var inAppProductOffer = new InAppProductOffer
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
                                            HardCurrency = 1,
                                            Title = "test offer details",
                                            Description = "test offer details description"
                                        }
                                    ]
                                };
        offerService.Setup(s => s.GetInAppProductOfferLimited(It.IsAny<string>())).ReturnsAsync(inAppProductOffer);

        // Act
        await using var provider = services.BuildServiceProvider();
        var testInstance = CreateTestService(provider);

        var request = new InitInAppPurchaseRequest
                      {
                          ClientCurrency = "USD", ClientCurrencyPrice = 122.0M, InAppProductOfferKey = "testoffer"
                      };
        var result = await testInstance.InitInAppPurchase(request);

        // Assert
        result.Should().NotBeNull();
        result.Ok.Should().BeTrue();
        result.PendingOrderId.Should().Be(order.Id);
        result.ErrorCode.Should().BeNull();
        result.ErrorMessage.Should().BeNull();
        pendingOrderManager.Verify(
            s => s.PlacePendingOrder(It.IsAny<InAppProductOffer>(), request.ClientCurrency, request.ClientCurrencyPrice),
            Times.Once
        );
        offerService.Verify(s => s.GetInAppProductOfferLimited(request.InAppProductOfferKey), Times.Once);
    }

    [Fact(DisplayName = "👍👍👍Init in-app purchase: should fail on buying season pass if no season")]
    public async Task InAppPurchase_Init_ShouldFailIfNoInAppProduct()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddTestClientServices(testOut);

        var order = new InAppPurchaseOrder
                    {
                        Id = Guid.NewGuid(),
                        Platform = "iOS",
                        Receipt = "test_offer_receipt",
                        CreatedTime = DateTime.Now,
                        GroupId = 11,
                        IsPending = true,
                        InAppProductId = 111
                    };
        pendingOrderManager.Setup(s => s.GetExistingPendingOrder(It.IsAny<long>())).Returns(new List<InAppPurchaseOrder>().BuildMock());
        pendingOrderManager.Setup(s => s.PlacePendingOrder(It.IsAny<InAppProductOffer>(), It.IsAny<string>(), It.IsAny<decimal>()))
                           .ReturnsAsync(order);

        offerService.Setup(s => s.GetInAppProductOfferLimited(It.IsAny<string>())).ReturnsAsync(default(InAppProductOffer));

        // Act
        await using var provider = services.BuildServiceProvider();
        var testInstance = CreateTestService(provider);

        var request = new InitInAppPurchaseRequest
                      {
                          ClientCurrency = "USD", ClientCurrencyPrice = 122.0M, InAppProductOfferKey = "testoffer"
                      };
        await testInstance.Invoking(s => s.InitInAppPurchase(request))
                          .Should()
                          .ThrowAsync<AppErrorWithStatusCodeException>()
                          .WithMessage("Invalid in-app product");
    }
}