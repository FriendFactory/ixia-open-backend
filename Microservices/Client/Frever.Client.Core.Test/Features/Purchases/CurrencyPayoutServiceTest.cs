using Common.Infrastructure;
using Common.Models;
using FluentAssertions;
using Frever.Cache;
using Frever.Client.Shared.Payouts;
using Frever.Common.Testing;
using Frever.Shared.AssetStore;
using Frever.Shared.AssetStore.DataAccess;
using Frever.Shared.AssetStore.Transactions;
using Frever.Shared.MainDb.Entities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using Xunit.Abstractions;

namespace Frever.Client.Core.Test.Features.Purchases;

[Collection("Currency Payout Service")]
public class CurrencyPayoutServiceTest(ITestOutputHelper testOut)
{
    [Theory(DisplayName = "👍👎Check hard currency payout")]
    [InlineData(AssetStoreTransactionType.CreatorLevelUp)]
    [InlineData(AssetStoreTransactionType.CrewReward)]
    [InlineData(AssetStoreTransactionType.DailyQuest)]
    [InlineData(AssetStoreTransactionType.Invitation)]
    [InlineData(AssetStoreTransactionType.LevelUp)]
    [InlineData(AssetStoreTransactionType.OnboardingReward)]
    [InlineData(AssetStoreTransactionType.TaskCompletion)]
    [InlineData(AssetStoreTransactionType.PublishedVideoShare)]
    public async Task HardCurrencyPayout(AssetStoreTransactionType transactionType)
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddUnitTestServices(testOut);

        await using var provider = services.BuildServiceProvider();

        const long groupId = 1;
        const long activityId = 1;
        const int amount = 1;
        AssetStoreTransaction[] transactions = null;

        var repo = new Mock<ICurrencyPayoutRepository>(MockBehavior.Strict);
        repo.Setup(s => s.RecordAssetStoreTransaction(It.IsAny<IEnumerable<AssetStoreTransaction>>()))
            .Callback<IEnumerable<AssetStoreTransaction>>(t => transactions = t.ToArray())
            .Returns(Task.CompletedTask);

        var testInstance = CreateTestService(provider, repo);

        // Act
        await testInstance.HardCurrencyPayout(groupId, amount, transactionType, activityId);

        // Assert
        transactions.Should().HaveCount(2);
        transactions[0].UserActivityId.Should().Be(activityId);
        transactions[0].HardCurrencyAmount.Should().Be(amount);
        transactions[0].SoftCurrencyAmount.Should().Be(0);
        transactions[0].TransactionType.Should().Be(transactionType);

        transactions[1].HardCurrencyAmount.Should().Be(-amount);
        transactions[1].SoftCurrencyAmount.Should().Be(0);
        transactions[1].TransactionType.Should().Be(AssetStoreTransactionType.SystemExpense);

        transactions[0].TransactionGroup.Should().Be(transactions[1].TransactionGroup);
    }

    [Fact(DisplayName = "👍👍Check soft currency payout: amount must be positive")]
    public async Task HardCurrencyPayout_AmountMustBePositive()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddUnitTestServices(testOut);

        await using var provider = services.BuildServiceProvider();

        var testInstance = CreateTestService(provider);

        // Act
        var act = () => testInstance.HardCurrencyPayout(1, 0, AssetStoreTransactionType.Achievement, null);

        // Assert
        await act.Should().ThrowAsync<ArgumentOutOfRangeException>();
    }

    [Fact(DisplayName = "👍👎Check spend hard currency")]
    public async Task SpendHardCurrency()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddUnitTestServices(testOut);

        await using var provider = services.BuildServiceProvider();

        const long groupId = 1;
        const long activityId = 1;
        const int amount = -1;
        var transactionType = AssetStoreTransactionType.PremiumPurchase;
        AssetStoreTransaction[] transactions = null;

        var repo = new Mock<ICurrencyPayoutRepository>(MockBehavior.Strict);
        repo.Setup(s => s.GetHardCurrencyBalance(It.IsAny<long>())).ReturnsAsync(-amount);
        repo.Setup(s => s.RecordAssetStoreTransaction(It.IsAny<IEnumerable<AssetStoreTransaction>>()))
            .Callback<IEnumerable<AssetStoreTransaction>>(t => transactions = t.ToArray())
            .Returns(Task.CompletedTask);

        var testInstance = CreateTestService(provider, repo);

        // Act
        await testInstance.SpendHardCurrency(groupId, amount, transactionType, activityId);

        // Assert
        transactions.Should().HaveCount(2);
        transactions[0].UserActivityId.Should().Be(activityId);
        transactions[0].HardCurrencyAmount.Should().Be(amount);
        transactions[0].SoftCurrencyAmount.Should().Be(0);
        transactions[0].TransactionType.Should().Be(transactionType);

        transactions[1].HardCurrencyAmount.Should().Be(-amount);
        transactions[1].SoftCurrencyAmount.Should().Be(0);
        transactions[1].TransactionType.Should().Be(AssetStoreTransactionType.SystemIncome);

        transactions[0].TransactionGroup.Should().Be(transactions[1].TransactionGroup);
    }

    [Fact(DisplayName = "👍👍Check spend hard currency: amount must be negative or zero")]
    public async Task SpendHardCurrency_AmountMustBePositive()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddUnitTestServices(testOut);

        await using var provider = services.BuildServiceProvider();

        var testInstance = CreateTestService(provider);

        // Act
        var act = () => testInstance.SpendHardCurrency(1, 1, AssetStoreTransactionType.Achievement, null);

        // Assert
        await act.Should().ThrowAsync<ArgumentOutOfRangeException>();
    }

    [Fact(DisplayName = "👍👍Check spend hard currency: not enough currency")]
    public async Task SpendHardCurrency_NotEnoughCurrency()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddUnitTestServices(testOut);

        await using var provider = services.BuildServiceProvider();

        var repo = new Mock<ICurrencyPayoutRepository>(MockBehavior.Strict);
        repo.Setup(s => s.GetHardCurrencyBalance(It.IsAny<long>())).ReturnsAsync(0);

        var testInstance = CreateTestService(provider, repo);

        // Act
        var act = () => testInstance.SpendHardCurrency(1, -1, AssetStoreTransactionType.Achievement, null);

        // Assert
        await act.Should()
                 .ThrowAsync<AppErrorWithStatusCodeException>()
                 .Where(e => e.ErrorCode.Equals(ErrorCodes.Client.PurchaseNotEnoughCurrency));
    }

    [Fact(DisplayName = "👍👎Check exchange hard currency to soft")]
    public async Task ExchangeHardCurrencyToSoft()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddUnitTestServices(testOut);

        await using var provider = services.BuildServiceProvider();

        const long groupId = 1;
        const long offerId = 1;
        const int hardCurrency = 1;
        const int softCurrency = 1;
        AssetStoreTransaction[] transactions = null;

        var repo = new Mock<ICurrencyPayoutRepository>(MockBehavior.Strict);
        repo.Setup(s => s.GetHardCurrencyBalance(It.IsAny<long>())).ReturnsAsync(hardCurrency);
        repo.Setup(s => s.RecordAssetStoreTransaction(It.IsAny<IEnumerable<AssetStoreTransaction>>()))
            .Callback<IEnumerable<AssetStoreTransaction>>(t => transactions = t.ToArray())
            .Returns(Task.CompletedTask);

        var testInstance = CreateTestService(provider, repo);

        // Act
        await testInstance.ExchangeHardCurrencyToSoft(groupId, hardCurrency, softCurrency, offerId);

        // Assert
        transactions.Should().HaveCount(3);
        transactions[0].HardCurrencyExchangeOfferId.Should().Be(offerId);
        transactions[0].HardCurrencyAmount.Should().Be(-hardCurrency);
        transactions[0].SoftCurrencyAmount.Should().Be(softCurrency);
        transactions[0].TransactionType.Should().Be(AssetStoreTransactionType.HardCurrencyExchange);

        transactions[1].HardCurrencyAmount.Should().Be(0);
        transactions[1].SoftCurrencyAmount.Should().Be(-softCurrency);
        transactions[1].TransactionType.Should().Be(AssetStoreTransactionType.SystemExpense);

        transactions[2].HardCurrencyAmount.Should().Be(hardCurrency);
        transactions[2].SoftCurrencyAmount.Should().Be(0);
        transactions[2].TransactionType.Should().Be(AssetStoreTransactionType.SystemIncome);

        transactions[0].TransactionGroup.Should().Be(transactions[1].TransactionGroup);
        transactions[0].TransactionGroup.Should().Be(transactions[2].TransactionGroup);
        transactions[1].TransactionGroup.Should().Be(transactions[2].TransactionGroup);
    }

    [Theory(DisplayName = "👍👍Check exchange hard currency to soft: amount must be negative or zero")]
    [InlineData(0, 1)]
    [InlineData(1, 0)]
    public async Task ExchangeHardCurrencyToSoft_AmountMustBePositive(int hardCurrency, int softCurrency)
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddUnitTestServices(testOut);

        await using var provider = services.BuildServiceProvider();

        var testInstance = CreateTestService(provider);

        // Act
        var act = () => testInstance.ExchangeHardCurrencyToSoft(1, hardCurrency, softCurrency, 1);

        // Assert
        await act.Should().ThrowAsync<ArgumentOutOfRangeException>();
    }

    [Fact(DisplayName = "👍👍Check exchange hard currency to soft: not enough currency")]
    public async Task ExchangeHardCurrencyToSoft_NotEnoughCurrency()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddUnitTestServices(testOut);

        await using var provider = services.BuildServiceProvider();

        var repo = new Mock<ICurrencyPayoutRepository>(MockBehavior.Strict);
        repo.Setup(s => s.GetHardCurrencyBalance(It.IsAny<long>())).ReturnsAsync(0);

        var testInstance = CreateTestService(provider, repo);

        // Act
        var act = () => testInstance.ExchangeHardCurrencyToSoft(1, 1, 1, 1);

        // Assert
        await act.Should()
                 .ThrowAsync<AppErrorWithStatusCodeException>()
                 .Where(e => e.ErrorCode.Equals(ErrorCodes.Client.PurchaseNotEnoughCurrency));
    }

    [Fact(DisplayName = "👍👎Check add initial account balance")]
    public async Task AddInitialAccountBalance()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddUnitTestServices(testOut);

        await using var provider = services.BuildServiceProvider();

        const long groupId = 1;
        const int hardCurrency = 1;
        const int softCurrency = 1;
        AssetStoreTransaction[] transactions = null;

        var repo = new Mock<ICurrencyPayoutRepository>(MockBehavior.Strict);
        repo.Setup(s => s.GroupHasTransaction(It.IsAny<long>(), It.IsAny<AssetStoreTransactionType>())).ReturnsAsync(false);
        repo.Setup(s => s.RecordAssetStoreTransaction(It.IsAny<IEnumerable<AssetStoreTransaction>>()))
            .Callback<IEnumerable<AssetStoreTransaction>>(t => transactions = t.ToArray())
            .Returns(Task.CompletedTask);

        var testInstance = CreateTestService(provider, repo);

        // Act
        await testInstance.AddInitialAccountBalance(groupId, hardCurrency, softCurrency);

        // Assert
        transactions.Should().HaveCount(2);
        transactions[0].HardCurrencyAmount.Should().Be(hardCurrency);
        transactions[0].SoftCurrencyAmount.Should().Be(softCurrency);
        transactions[0].TransactionType.Should().Be(AssetStoreTransactionType.InitialAccountBalance);

        transactions[1].HardCurrencyAmount.Should().Be(-hardCurrency);
        transactions[1].SoftCurrencyAmount.Should().Be(-softCurrency);
        transactions[1].TransactionType.Should().Be(AssetStoreTransactionType.SystemExpense);

        transactions[0].TransactionGroup.Should().Be(transactions[1].TransactionGroup);
    }

    [Fact(DisplayName = "👍👎Check add initial daily tokens")]
    public async Task AddInitialDailyTokens()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddUnitTestServices(testOut);

        await using var provider = services.BuildServiceProvider();

        const long groupId = 1;
        const int tokens = 30;

        AssetStoreTransaction[] transactions = null;

        var repo = new Mock<ICurrencyPayoutRepository>(MockBehavior.Strict);
        repo.Setup(s => s.GroupHasTransaction(It.IsAny<long>(), It.IsAny<AssetStoreTransactionType>())).ReturnsAsync(false);
        repo.Setup(s => s.RecordAssetStoreTransaction(It.IsAny<IEnumerable<AssetStoreTransaction>>()))
            .Callback<IEnumerable<AssetStoreTransaction>>(t => transactions = t.ToArray())
            .Returns(Task.CompletedTask);

        var testInstance = CreateTestService(provider, repo);

        // Act
        await testInstance.AddInitialDailyTokens(groupId, tokens);

        // Assert
        transactions.Should().HaveCount(2);
        transactions[0].HardCurrencyAmount.Should().Be(tokens);
        transactions[0].SoftCurrencyAmount.Should().Be(0);
        transactions[0].TransactionType.Should().Be(AssetStoreTransactionType.DailyTokenRefill);

        transactions[1].HardCurrencyAmount.Should().Be(-tokens);
        transactions[1].SoftCurrencyAmount.Should().Be(-0);
        transactions[1].TransactionType.Should().Be(AssetStoreTransactionType.SystemExpense);

        transactions[0].TransactionGroup.Should().Be(transactions[1].TransactionGroup);
    }

    [Fact(DisplayName = "👍👍Check add initial account balance: transaction already added")]
    public async Task AddInitialAccountBalance_AlreadyAdded()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddUnitTestServices(testOut);

        await using var provider = services.BuildServiceProvider();

        var repo = new Mock<ICurrencyPayoutRepository>(MockBehavior.Strict);
        repo.Setup(s => s.GroupHasTransaction(It.IsAny<long>(), It.IsAny<AssetStoreTransactionType>())).ReturnsAsync(true);

        var testInstance = CreateTestService(provider, repo);

        // Act
        await testInstance.AddInitialAccountBalance(1, 1, 1);

        // Assert
        repo.Verify(v => v.RecordAssetStoreTransaction(It.IsAny<IEnumerable<AssetStoreTransaction>>()), Times.Never);
    }

    [Fact(DisplayName = "👍👍Check add initial daily tokens: transaction already added")]
    public async Task AddInitialDailyTokens_AlreadyAdded()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddUnitTestServices(testOut);

        await using var provider = services.BuildServiceProvider();

        var repo = new Mock<ICurrencyPayoutRepository>(MockBehavior.Strict);
        repo.Setup(s => s.GroupHasTransaction(It.IsAny<long>(), It.IsAny<AssetStoreTransactionType>())).ReturnsAsync(true);

        var testInstance = CreateTestService(provider, repo);

        // Act
        await testInstance.AddInitialDailyTokens(1, 30);

        // Assert
        repo.Verify(v => v.RecordAssetStoreTransaction(It.IsAny<IEnumerable<AssetStoreTransaction>>()), Times.Never);
    }

    [Fact(DisplayName = "👍👍Check add initial account balance: amount must be negative or zero")]
    public async Task AddInitialAccountBalance_AmountMustBePositive()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddUnitTestServices(testOut);

        await using var provider = services.BuildServiceProvider();

        var testInstance = CreateTestService(provider);

        // Act
        var act = () => testInstance.AddInitialAccountBalance(1, 0, 0);

        // Assert
        await act.Should().ThrowAsync<ArgumentOutOfRangeException>();
    }

    private static CurrencyPayoutService CreateTestService(IServiceProvider provider, Mock<ICurrencyPayoutRepository> repo = null)
    {
        repo ??= new Mock<ICurrencyPayoutRepository>(MockBehavior.Strict);

        var transactionRepo = new Mock<IAssetStoreTransactionRepository>(MockBehavior.Strict);

        var serviceGroups = new ServiceGroups {SystemGroupId = 1, CustomerSupportGroupId = 1, RealMoneySystemGroupId = 1};
        var cache = new Mock<IBlobCache<ServiceGroups>>(MockBehavior.Strict);
        cache.Setup(s => s.GetOrCache(It.IsAny<string>(), It.IsAny<Func<Task<ServiceGroups>>>(), It.IsAny<TimeSpan>()))
             .ReturnsAsync(serviceGroups);

        var transactionGenerator = new AssetStoreTransactionGenerator(transactionRepo.Object, cache.Object, new AssetStoreOptions());

        var service = new CurrencyPayoutService(provider.GetRequiredService<ILoggerFactory>(), repo.Object, transactionGenerator);

        return service;
    }
}