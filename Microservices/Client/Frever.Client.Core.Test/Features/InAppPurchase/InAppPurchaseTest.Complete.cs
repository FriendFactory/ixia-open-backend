using Common.Infrastructure;
using FluentAssertions;
using Frever.Client.Core.Features.InAppPurchases.InAppPurchase;
using Frever.Client.Core.Test.Utils;
using Frever.ClientService.Contract.InAppPurchases;
using Frever.Shared.MainDb.Entities;
using Microsoft.Extensions.DependencyInjection;
using MockQueryable.Moq;
using Moq;
using Xunit;
using Platform = Frever.ClientService.Contract.Common.Platform;

namespace Frever.Client.Core.Test.Features.InAppPurchase;

public partial class InAppPurchaseTest
{
    [Fact(DisplayName = "👍👎👎Complete in-app purchase should work")]
    public async Task InAppPurchase_Complete_HappyPath()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddTestClientServices(testOut);

        // Act
        await using var provider = services.BuildServiceProvider();
        var testInstance = CreateTestService(provider);

        var request = new CompleteInAppPurchaseRequest
                      {
                          Platform = Platform.iOS, TransactionData = "testid", PendingOrderId = Guid.NewGuid()
                      };
        var result = await testInstance.CompleteInAppPurchase(request);

        // Assert
        result.Should().NotBeNull();
        result.Ok.Should().BeTrue();
        result.ErrorCode.Should().BeNull();
        result.ErrorMessage.Should().BeNull();

        pendingOrderManager.Verify(
            s => s.CompletePendingOrder(
                order.Id,
                Platform.iOS,
                request.TransactionData,
                It.IsAny<string>(),
                It.IsAny<string>()
            ),
            Times.Once
        );

        assetPurchaseManager.Verify(
            s => s.CompleteTransferOwnership(
                It.IsAny<long>(),
                product,
                It.IsAny<IEnumerable<InAppProductDetails>>(),
                request.TransactionData
            ),
            Times.Once
        );

        offerService.Verify(s => s.MarkOfferAsPurchased(order.InAppProductOfferKey));
    }

    [Fact(DisplayName = "👍👍👍Complete in-app purchase should fail if there is no pending order")]
    public async Task InAppPurchase_Complete_ShouldFailIfNoPendingOrder()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddTestClientServices(testOut);

        pendingOrderManager.Setup(s => s.GetPendingOrder(It.IsAny<Guid>())).ReturnsAsync(default(InAppPurchaseOrder));

        // Act
        await using var provider = services.BuildServiceProvider();
        var testInstance = CreateTestService(provider);

        // Assert
        await testInstance
             .Invoking(
                  s => s.CompleteInAppPurchase(
                      new CompleteInAppPurchaseRequest
                      {
                          Platform = Platform.iOS, TransactionData = "testreceipt", PendingOrderId = Guid.NewGuid()
                      }
                  )
              )
             .Should()
             .ThrowAsync<AppErrorWithStatusCodeException>()
             .WithMessage("Pending order ID is invalid");
    }

    [Fact(DisplayName = "👍👍👍Complete in-app purchase should fail if order has been completed")]
    public async Task InAppPurchase_Complete_ShouldFailIfOrderIsNotPending()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddTestClientServices(testOut);

        pendingOrderManager.Setup(s => s.GetPendingOrder(It.IsAny<Guid>()))
                           .ReturnsAsync(new InAppPurchaseOrder {Id = Guid.NewGuid(), IsPending = false});

        // Act
        await using var provider = services.BuildServiceProvider();
        var testInstance = CreateTestService(provider);

        // Assert
        await testInstance
             .Invoking(
                  s => s.CompleteInAppPurchase(
                      new CompleteInAppPurchaseRequest
                      {
                          Platform = Platform.iOS, TransactionData = "testreceipt", PendingOrderId = Guid.NewGuid()
                      }
                  )
              )
             .Should()
             .ThrowAsync<AppErrorWithStatusCodeException>()
             .WithMessage("Pending order ID already completed");
    }

    [Fact(DisplayName = "👍👍👍Complete in-app purchase should fail if no in-app product")]
    public async Task InAppPurchase_Complete_ShouldFailIfNoInAppProduct()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddTestClientServices(testOut);

        inAppProductRepository.Setup(s => s.GetActiveInAppProducts()).Returns(new List<InAppProduct>().BuildMock());

        // Act
        await using var provider = services.BuildServiceProvider();
        var testInstance = CreateTestService(provider);

        // Assert
        await testInstance
             .Invoking(
                  s => s.CompleteInAppPurchase(
                      new CompleteInAppPurchaseRequest
                      {
                          Platform = Platform.iOS, TransactionData = "testreceipt", PendingOrderId = Guid.NewGuid()
                      }
                  )
              )
             .Should()
             .ThrowAsync<AppErrorWithStatusCodeException>()
             .WithMessage("Invalid in-app product");
    }

    [Fact(DisplayName = "👍👍👍Complete in-app purchase should fail if receipt validation failed")]
    public async Task InAppPurchase_Complete_ShouldFailIfReceiptValidationFailed()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddTestClientServices(testOut);

        receiptValidator.Setup(s => s.ValidateStoreTransaction(It.IsAny<ValidateStoreTransactionRequest>()))
                        .ReturnsAsync(new StoreTransactionValidationResult {Error = "Test error", IsValid = false});

        // Act
        await using var provider = services.BuildServiceProvider();
        var testInstance = CreateTestService(provider);

        var request = new CompleteInAppPurchaseRequest
                      {
                          Platform = Platform.iOS, TransactionData = "testreceipt", PendingOrderId = Guid.NewGuid()
                      };

        // Assert
        await testInstance.Invoking(s => s.CompleteInAppPurchase(request))
                          .Should()
                          .ThrowAsync<AppErrorWithStatusCodeException>()
                          .WithMessage("Invalid receipt data");

        pendingOrderManager.Verify(
            s => s.SetPendingOrderError(order.Id, request.Platform, request.TransactionData, It.IsAny<string>()),
            Times.Once
        );
    }

    [Theory(DisplayName = "👍👍👍Complete in-app purchase should fail if receipt contains different product SKU")]
    [InlineData(Platform.Android)]
    [InlineData(Platform.iOS)]
    public async Task InAppPurchase_Complete_ShouldFailIfReceiptHasDifferentProductSKU(Platform platform)
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddTestClientServices(testOut);

        _storeTransactionValidationResult.AppProductSku = "i don't match";

        // Act
        await using var provider = services.BuildServiceProvider();
        var testInstance = CreateTestService(provider);

        var request = new CompleteInAppPurchaseRequest
                      {
                          Platform = platform, TransactionData = "testreceipt", PendingOrderId = Guid.NewGuid()
                      };

        // Assert
        await testInstance.Invoking(s => s.CompleteInAppPurchase(request))
                          .Should()
                          .ThrowAsync<AppErrorWithStatusCodeException>()
                          .WithMessage("Transaction data is from another product");
    }
}