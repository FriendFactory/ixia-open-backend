using FluentAssertions;
using Frever.Client.Core.Features.InAppPurchases;
using Frever.Client.Core.IntegrationTest.Features.InAppPurchase.Data;
using Frever.Common.IntegrationTesting;
using Frever.Common.IntegrationTesting.Data;
using Frever.Shared.MainDb.Entities;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Frever.Client.Core.IntegrationTest.Features.InAppPurchase;

public partial class BalanceTest
{
    [Fact]
    public async Task Balance_ShouldWorkWithRefund()
    {
        await using var provider = _services.BuildServiceProvider();
        var dataEnv = provider.GetRequiredService<DataEnvironment>();

        var user = await dataEnv.WithUserAndGroup(new UserAndGroupCreateParams());
        var date = new DateSequence(new DateTime(2025, 02, 01));


        // P: 100         M: 0         D: 0
        await dataEnv.WithAssetStoreTransaction(user.MainGroupId, AssetStoreTransactionType.InitialAccountBalance, 100, date);

        // P: 100         M: 0        D: 30
        await dataEnv.WithAssetStoreTransaction(user.MainGroupId, AssetStoreTransactionType.DailyTokenRefill, 30, date.NextMoment());

        // P:  60         M: 0        D: 0
        await dataEnv.WithAssetStoreTransaction(user.MainGroupId, AssetStoreTransactionType.AiWorkflowRun, -70, date.NextDay());
        // P:  1060       M: 0        D: 0
        await dataEnv.WithAssetStoreTransaction(user.MainGroupId, AssetStoreTransactionType.InAppPurchase, 1000, date.NextDay());
        // P:   860       M: 0        D: 0
        await dataEnv.WithAssetStoreTransaction(user.MainGroupId, AssetStoreTransactionType.AiWorkflowRun, -200, date.NextHour());
        // P:   660       M: 0        D: 0
        await dataEnv.WithAssetStoreTransaction(user.MainGroupId, AssetStoreTransactionType.AiWorkflowRun, -200, date.NextHour());

        // P:   660       M: 1500        D: 0
        await dataEnv.WithAssetStoreTransaction(user.MainGroupId, AssetStoreTransactionType.MonthlyTokenRefill, 1500, date.NextDay());

        // P:   660       M: 800        D: 0
        await dataEnv.WithAssetStoreTransaction(user.MainGroupId, AssetStoreTransactionType.AiWorkflowRun, -700, date.NextDay());
        // P:   660       M: 600        D: 0
        await dataEnv.WithAssetStoreTransaction(user.MainGroupId, AssetStoreTransactionType.AiWorkflowRun, -200, date.NextDay());

        // Refund as permanent token
        // P:   1360       M: 600        D: 0
        await dataEnv.WithAssetStoreTransaction(user.MainGroupId, AssetStoreTransactionType.AiWorkflowRunErrorRefund, 700, date.NextDay());

        var service = provider.GetRequiredService<IBalanceService>();
        var balance = await service.GetBalance(user.MainGroupId);

        balance.Should().NotBeNull();
        balance.DailyTokens.Should().Be(0);
        balance.SubscriptionTokens.Should().Be(600);
        balance.PermanentTokens.Should().Be(1360);
    }
}