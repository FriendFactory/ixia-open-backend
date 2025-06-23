using FluentAssertions;
using Frever.Client.Core.Features.InAppPurchases;
using Frever.Client.Core.IntegrationTest.Features.InAppPurchase.Data;
using Frever.Client.Core.IntegrationTest.Utils;
using Frever.Common.IntegrationTesting;
using Frever.Common.IntegrationTesting.Data;
using Frever.Shared.MainDb.Entities;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;

namespace Frever.Client.Core.IntegrationTest.Features.InAppPurchase;

public partial class BalanceTest : IntegrationTestBase
{
    private readonly IServiceCollection _services;

    public BalanceTest(ITestOutputHelper testOut)
    {
        _services = new ServiceCollection();
        _services.AddClientIntegrationTests(testOut);
    }

    [Fact]
    public async Task Balance_ShouldWork()
    {
        await using var provider = _services.BuildServiceProvider();
        var dataEnv = provider.GetRequiredService<DataEnvironment>();

        var user = await dataEnv.WithUserAndGroup(new UserAndGroupCreateParams());

        const int dailyTokens = 30;
        const int monthlyTokens = 1500;

        var initDate = new DateTime(2025, 02, 01);


        await dataEnv.WithAssetStoreTransaction(
            user.MainGroupId,
            AssetStoreTransactionType.InitialAccountBalance,
            100,
            initDate.AddDays(10)
        );
        await dataEnv.WithAssetStoreTransaction(
            user.MainGroupId,
            AssetStoreTransactionType.DailyTokenRefill,
            dailyTokens,
            initDate.AddDays(11)
        );
        await dataEnv.WithAssetStoreTransaction(user.MainGroupId, AssetStoreTransactionType.AiWorkflowRun, -70, initDate.AddDays(11));
        await dataEnv.WithAssetStoreTransaction(user.MainGroupId, AssetStoreTransactionType.InAppPurchase, 1000, initDate.AddDays(12));
        await dataEnv.WithAssetStoreTransaction(user.MainGroupId, AssetStoreTransactionType.AiWorkflowRun, -200, initDate.AddDays(13));


        await dataEnv.WithAssetStoreTransaction(
            user.MainGroupId,
            AssetStoreTransactionType.MonthlyTokenRefill,
            monthlyTokens,
            initDate.AddDays(14)
        );
        await dataEnv.WithAssetStoreTransaction(user.MainGroupId, AssetStoreTransactionType.AiWorkflowRun, -700, initDate.AddDays(15));
        await dataEnv.WithAssetStoreTransaction(user.MainGroupId, AssetStoreTransactionType.AiWorkflowRun, -200, initDate.AddDays(15));

        var service = provider.GetRequiredService<IBalanceService>();
        var balance = await service.GetBalance(user.MainGroupId);

        balance.Should().NotBeNull();
        balance.DailyTokens.Should().Be(0);
        balance.SubscriptionTokens.Should().Be(monthlyTokens - 700 - 200);
        balance.PermanentTokens.Should().Be(100 + dailyTokens - 70 + 1000 - 200);
    }

    [Fact]
    public async Task Balance_ShouldCorrectlyCalculateDailyTokens()
    {
        await using var provider = _services.BuildServiceProvider();
        var dataEnv = provider.GetRequiredService<DataEnvironment>();

        var user = await dataEnv.WithUserAndGroup(new UserAndGroupCreateParams());
        var service = provider.GetRequiredService<IBalanceService>();

        const int dailyTokens = 30;

        var initDate = new DateTime(2025, 02, 01);

        await dataEnv.WithAssetStoreTransaction(
            user.MainGroupId,
            AssetStoreTransactionType.InitialAccountBalance,
            100,
            initDate.AddDays(10)
        );

        await dataEnv.WithAssetStoreTransaction(
            user.MainGroupId,
            AssetStoreTransactionType.DailyTokenRefill,
            dailyTokens,
            initDate.AddDays(11)
        );
        await dataEnv.WithAssetStoreTransaction(user.MainGroupId, AssetStoreTransactionType.AiWorkflowRun, -10, initDate.AddDays(12));

        var balance = await service.GetBalance(user.MainGroupId);

        balance.Should().NotBeNull();
        balance.DailyTokens.Should().Be(dailyTokens - 10);
        balance.SubscriptionTokens.Should().Be(0);
        balance.PermanentTokens.Should().Be(100);

        await dataEnv.WithAssetStoreTransaction(user.MainGroupId, AssetStoreTransactionType.AiWorkflowRun, -20, initDate.AddDays(13));

        var balance2 = await service.GetBalance(user.MainGroupId);

        balance2.Should().NotBeNull();
        balance2.DailyTokens.Should().Be(0);
        balance2.SubscriptionTokens.Should().Be(0);
        balance2.PermanentTokens.Should().Be(100);

        await dataEnv.WithAssetStoreTransaction(user.MainGroupId, AssetStoreTransactionType.AiWorkflowRun, -30, initDate.AddDays(14));

        var balance3 = await service.GetBalance(user.MainGroupId);

        balance3.Should().NotBeNull();
        balance3.DailyTokens.Should().Be(0);
        balance3.SubscriptionTokens.Should().Be(0);
        balance3.PermanentTokens.Should().Be(70);
    }

    [Fact]
    public async Task Balance_ShouldCorrectlyCalculateMonthly()
    {
        await using var provider = _services.BuildServiceProvider();
        var dataEnv = provider.GetRequiredService<DataEnvironment>();

        var user = await dataEnv.WithUserAndGroup(new UserAndGroupCreateParams());
        var service = provider.GetRequiredService<IBalanceService>();

        const int initialBalance = 200;
        const int dailyTokens = 30;
        const int monthlyTokens = 1500;

        var date = new DateSequence(new DateTime(2025, 02, 01));
        var t = new AssetStoreTransactionGenerator(dataEnv, user.MainGroupId);

        await t.PurchaseTokens(initialBalance, date.NextDay());

        await t.RefillDaily(dailyTokens, date.NextDay());
        await t.RefillMonthly(monthlyTokens, date.NextMoment());

        await t.RunWorkflow(-10, date.NextDay());

        await service.CheckBalance(user, dailyTokens - 10, monthlyTokens, initialBalance);

        await t.RunWorkflow(-20, date.NextDay());
        await service.CheckBalance(user, dailyTokens - 30, monthlyTokens, initialBalance);

        await t.RunWorkflow(-30, date.NextDay());
        await service.CheckBalance(user, 0, monthlyTokens - 30, initialBalance);


        await t.BurnoutMonthly(-(monthlyTokens - 30), date.NextDay());
        await t.RefillMonthly(monthlyTokens, date.NextMoment());
        await t.RefillDaily(dailyTokens, date.NextDay());
        await service.CheckBalance(user, dailyTokens, monthlyTokens, initialBalance);
    }

    [Fact]
    public async Task Balance_ShouldCorrectlyUsePermanentTokens()
    {
        await using var provider = _services.BuildServiceProvider();
        var dataEnv = provider.GetRequiredService<DataEnvironment>();

        var user = await dataEnv.WithUserAndGroup(new UserAndGroupCreateParams());
        var service = provider.GetRequiredService<IBalanceService>();

        const int dailyTokens = 30;
        const int monthlyTokens = 1500;
        const int initialBalance = 1000;

        var initDate = new DateTime(2025, 02, 01);

        await dataEnv.WithAssetStoreTransaction(
            user.MainGroupId,
            AssetStoreTransactionType.InitialAccountBalance,
            initialBalance,
            initDate.AddDays(10)
        );

        await dataEnv.WithAssetStoreTransaction(
            user.MainGroupId,
            AssetStoreTransactionType.DailyTokenRefill,
            dailyTokens,
            initDate.AddDays(11)
        );

        await dataEnv.WithAssetStoreTransaction(user.MainGroupId, AssetStoreTransactionType.AiWorkflowRun, -100, initDate.AddDays(11));

        var balance1 = await service.GetBalance(user.MainGroupId);

        balance1.Should().NotBeNull();
        balance1.DailyTokens.Should().Be(0);
        balance1.SubscriptionTokens.Should().Be(0);
        balance1.PermanentTokens.Should().Be(initialBalance - 100 + dailyTokens);

        await dataEnv.WithAssetStoreTransaction(
            user.MainGroupId,
            AssetStoreTransactionType.MonthlyTokenRefill,
            monthlyTokens,
            initDate.AddDays(12)
        );

        var balance2 = await service.GetBalance(user.MainGroupId);

        balance2.Should().NotBeNull();
        balance2.DailyTokens.Should().Be(0);
        balance2.SubscriptionTokens.Should().Be(monthlyTokens);
        balance2.PermanentTokens.Should().Be(initialBalance - 100 + dailyTokens);
    }

    [Fact]
    public async Task Balance_ShouldCorrectlyUseAllTypesOfTokens()
    {
        await using var provider = _services.BuildServiceProvider();
        var dataEnv = provider.GetRequiredService<DataEnvironment>();

        var user = await dataEnv.WithUserAndGroup(new UserAndGroupCreateParams());
        var service = provider.GetRequiredService<IBalanceService>();

        var date = new DateSequence(new DateTime(2025, 02, 01));
        var t = new AssetStoreTransactionGenerator(dataEnv, user.MainGroupId);

        await service.CheckBalance(user, 0, 0, 0);

        // Initial
        await t.PurchaseTokens(1000, date.NextDay());
        await service.CheckBalance(user, 0, 0, 1000);

        // D0
        await t.RefillDaily(30, date.NextDay());
        await t.RunWorkflow(-100, date.NextMoment());
        await service.CheckBalance(user, 0, 0, 930);

        // D1
        await t.RefillDaily(30, date.NextDay());
        await t.RunWorkflow(-10, date.NextMoment());
        await service.CheckBalance(user, 20, 0, 930 /*1000 + 30 - 100*/);

        // D2
        await t.BurnoutDaily(-(30 - 10), date.NextDay());
        await t.RefillDaily(30, date.NextMoment());
        await service.CheckBalance(user, 30, 0, 930 /*1000 + 30 - 100*/);

        // D3
        await t.RunWorkflow(-100, date.NextDay());
        await service.CheckBalance(user, 0, 0, 860 /*1000 + 30 - 100 - 70*/);


        // D4
        await t.RefillDaily(30, date.NextDay());
        await t.RunWorkflow(-15, date.NextMoment());
        await service.CheckBalance(user, 15, 0, 860 /*1000 + 30 - 100 - 70*/);

        // D5
        await t.BurnoutDaily(-(30 - 15), date.NextDay());
        await t.RefillDaily(30, date.NextMoment());
        await service.CheckBalance(user, 30, 0, 860);

        await t.RefillMonthly(1500, date.NextMoment());
        await service.CheckBalance(user, 30, 1500, 860);

        // D6 
        await t.RunWorkflow(-10, date.NextDay());
        await service.CheckBalance(user, 20, 1500, 860);

        // D7
        await t.BurnoutDaily(-(30 - 10), date.NextDay());
        await t.RefillDaily(30, date.NextMoment());
        await service.CheckBalance(user, 30, 1500, 860);

        // D8
        await t.RunWorkflow(-1700, date.NextDay());
        await service.CheckBalance(user, 0, 0, 690);

        // D9
        await t.RefillDaily(30, date.NextMoment());
        await service.CheckBalance(user, 30, 0, 690);

        // D10
        await t.RunWorkflow(-20, date.NextMoment());
        await service.CheckBalance(user, 10, 0, 690);

        // D11
        await t.BurnoutDaily(-10, date.NextDay());
        await t.RefillDaily(30, date.NextMoment());
        await service.CheckBalance(user, 30, 0, 690);

        // D12 
        await t.RefillMonthly(1500, date.NextDay());
        await t.RunWorkflow(-100, date.NextMoment());
        await service.CheckBalance(user, 0, 1430, 690);

        // D13
        await t.RefillDaily(30, date.NextDay());
        await service.CheckBalance(user, 30, 1430, 690);

        // D14
        await t.BurnoutMonthly(-1430, date.NextDay());
        await t.RefillMonthly(1500, date.NextMoment());
        await service.CheckBalance(user, 30, 1500, 690);

        // D15 
        await t.RunWorkflow(-100, date.NextDay());
        await service.CheckBalance(user, 0, 1430, 690);

        // D16
        await t.RefillDaily(30, date.NextDay());
        await t.RunWorkflow(-10, date.NextMoment());
        await service.CheckBalance(user, 20, 1430, 690);
    }
}

public static class BalanceExtensions
{
    public static async Task CheckBalance(
        this IBalanceService service,
        User user,
        int dailyTokens,
        int subscriptionTokens,
        int permanentTokens
    )
    {
        var balance = await service.GetBalance(user.MainGroupId);
        balance.Should().NotBeNull();
        balance.DailyTokens.Should().Be(dailyTokens);
        balance.SubscriptionTokens.Should().Be(subscriptionTokens);
        balance.PermanentTokens.Should().Be(permanentTokens);
    }
}