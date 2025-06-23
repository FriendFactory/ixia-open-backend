using Frever.Client.Core.Features.InAppPurchases;
using Frever.Client.Core.IntegrationTest.Features.InAppPurchase.Data;
using Frever.Client.Core.IntegrationTest.Utils;
using Frever.Common.IntegrationTesting;
using Frever.Common.IntegrationTesting.Data;
using Frever.Common.Testing;
using Frever.Shared.AssetStore.DailyTokenRefill;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;

namespace Frever.Client.Core.IntegrationTest.Features.InAppPurchase;

public class DailyTokenRefillTest : IntegrationTestBase
{
    private readonly IServiceCollection _services;

    public DailyTokenRefillTest(ITestOutputHelper testOut)
    {
        _services = new ServiceCollection();
        _services.AddClientIntegrationTests(testOut);
    }

    [Fact]
    public async Task DailyTokenRefill_ShouldWork()
    {
        await using var provider = _services.BuildServiceProvider();
        var dataEnv = provider.GetRequiredService<DataEnvironment>();

        await dataEnv.WithSystemUserAndGroup();

        var user = await dataEnv.WithUserAndGroup(new UserAndGroupCreateParams());
        var balanceService = provider.GetRequiredService<IBalanceService>();
        var dailyTokenRefill = provider.GetRequiredService<IDailyTokenRefillService>();

        var date = new DateSequence(new DateTime(2025, 02, 01));
        var t = new AssetStoreTransactionGenerator(dataEnv, user.MainGroupId);

        // Initial
        await t.PurchaseTokens(1000, date.NextDay());
        await balanceService.CheckBalance(user, 0, 0, 1000);

        // D0
        await t.RefillDaily(30, date.NextDay());
        await t.RunWorkflow(-100, date.NextMoment());
        await balanceService.CheckBalance(user, 0, 0, 930);

        // D1
        await t.RefillDaily(30, date.NextDay());
        await t.RunWorkflow(-12, date.NextMoment());
        await balanceService.CheckBalance(user, 18, 0, 930 /*1000 + 30 - 100*/);

        var generatedBefore = await dataEnv.Db.AssetStoreTransactions.Where(t => t.GroupId == user.MainGroupId)
                                           .OrderByDescending(t => t.Id)
                                           .Take(4)
                                           .ToArrayAsync();

        await dailyTokenRefill.BatchRefillDailyTokens(false);

        var generatedAfter = await dataEnv.Db.AssetStoreTransactions.Where(t => t.GroupId == user.MainGroupId)
                                          .OrderByDescending(t => t.Id)
                                          .Take(4)
                                          .ToArrayAsync();

        await balanceService.CheckBalance(user, 30, 0, 930);
    }

    [Fact]
    public async Task DailyTokenRefill_ShouldNotRefillTwice()
    {
        await using var provider = _services.BuildServiceProvider();
        var dataEnv = provider.GetRequiredService<DataEnvironment>();

        await dataEnv.WithSystemUserAndGroup();

        var user = await dataEnv.WithUserAndGroup(new UserAndGroupCreateParams());
        var balanceService = provider.GetRequiredService<IBalanceService>();
        var dailyTokenRefill = provider.GetRequiredService<IDailyTokenRefillService>();

        var date = new DateSequence(new DateTime(2025, 02, 01));
        var t = new AssetStoreTransactionGenerator(dataEnv, user.MainGroupId);

        // Initial
        await t.PurchaseTokens(1000, date.NextDay());
        await balanceService.CheckBalance(user, 0, 0, 1000);

        // D0
        await t.RefillDaily(30, date.NextDay());
        await t.RunWorkflow(-100, date.NextMoment());
        await balanceService.CheckBalance(user, 0, 0, 930);

        // D1
        await t.RefillDaily(30, date.NextDay());
        await t.RunWorkflow(-10, date.NextMoment());
        await balanceService.CheckBalance(user, 20, 0, 930 /*1000 + 30 - 100*/);

        await dailyTokenRefill.BatchRefillDailyTokens(false);

        await balanceService.CheckBalance(user, 30, 0, 930);

        // Don't refill second time a day even after spending some tokens 
        await t.RunWorkflow(-10, date.NextMoment());
        await dailyTokenRefill.BatchRefillDailyTokens(false);
        await balanceService.CheckBalance(user, 20, 0, 930);
    }

    [Fact]
    public async Task DailyTokenRefill_ShouldRefillForNewUser()
    {
        await using var provider = _services.BuildServiceProvider();
        var dataEnv = provider.GetRequiredService<DataEnvironment>();

        await dataEnv.WithSystemUserAndGroup();

        var user = await dataEnv.WithUserAndGroup(new UserAndGroupCreateParams());
        var balanceService = provider.GetRequiredService<IBalanceService>();
        var dailyTokenRefill = provider.GetRequiredService<IDailyTokenRefillService>();

        await balanceService.CheckBalance(user, 0, 0, 0);

        await dailyTokenRefill.BatchRefillDailyTokens(false);

        await balanceService.CheckBalance(user, 30, 0, 0);
    }

    [Fact]
    public async Task DailyTokenRefill_ShouldRefillForUserWithExpensesOnly()
    {
        await using var provider = _services.BuildServiceProvider();
        var dataEnv = provider.GetRequiredService<DataEnvironment>();

        await dataEnv.WithSystemUserAndGroup();

        var user = await dataEnv.WithUserAndGroup(new UserAndGroupCreateParams());
        var balanceService = provider.GetRequiredService<IBalanceService>();
        var dailyTokenRefill = provider.GetRequiredService<IDailyTokenRefillService>();

        var date = new DateSequence(new DateTime(2025, 02, 01));
        var t = new AssetStoreTransactionGenerator(dataEnv, user.MainGroupId);

        // Initial
        await t.PurchaseTokens(1000, date.NextDay());
        await balanceService.CheckBalance(user, 0, 0, 1000);

        // D0
        await t.RunWorkflow(-100, date.NextMoment());
        await balanceService.CheckBalance(user, 0, 0, 900);

        await dailyTokenRefill.BatchRefillDailyTokens(false);

        await balanceService.CheckBalance(user, 30, 0, 900);
    }

    [Fact]
    public async Task DailyTokenRefillForCertainUser_ShouldWork()
    {
        await using var provider = _services.BuildServiceProvider();
        var dataEnv = provider.GetRequiredService<DataEnvironment>();

        var systemGroup = (await dataEnv.WithSystemUserAndGroup()).First();

        await using var scope1 = provider.CreateAsyncScope();

        var user1 = await dataEnv.WithUserAndGroup(new UserAndGroupCreateParams());
        scope1.ServiceProvider.SetCurrentUser(user1);

        await using var scope2 = provider.CreateAsyncScope();
        var user2 = await dataEnv.WithUserAndGroup(new UserAndGroupCreateParams());
        scope2.ServiceProvider.SetCurrentUser(user2);

        var refillService2 = scope2.ServiceProvider.GetRequiredService<IDailyTokenRefillService>();

        await refillService2.RefillDailyTokens(user2.MainGroupId);

        var balanceService = provider.GetRequiredService<IBalanceService>();

        await balanceService.CheckBalance(user1, 0, 0, 0);
        await balanceService.CheckBalance(user2, 30, 0, 0);
    }

    [Fact]
    public async Task DailyTokenRefill_ShouldCorrectlyHandleDailyTokenOverflow()
    {
        await using var provider = _services.BuildServiceProvider();
        var dataEnv = provider.GetRequiredService<DataEnvironment>();

        var systemGroup = (await dataEnv.WithSystemUserAndGroup()).First();

        await using var scope = provider.CreateAsyncScope();

        var user = await dataEnv.WithUserAndGroup(new UserAndGroupCreateParams());
        scope.ServiceProvider.SetCurrentUser(user);

        var transactions = """
                           148171,2025-05-09 11:19:16.882095 +00:00,DailyTokenRefill,30
                           148250,2025-05-09 14:28:57.902778 +00:00,AiWorkflowRun,-10
                           148398,2025-05-10 01:00:05.780420 +00:00,DailyTokenRefill,30
                           148399,2025-05-10 01:00:05.780420 +00:00,DailyTokenBurnout,-20
                           148610,2025-05-11 01:00:05.534430 +00:00,DailyTokenRefill,30
                           148611,2025-05-11 01:00:05.534430 +00:00,DailyTokenBurnout,-10
                           148805,2025-05-12 01:00:05.793262 +00:00,DailyTokenRefill,30
                           148806,2025-05-12 01:00:05.793262 +00:00,DailyTokenBurnout,-20
                           149307,2025-05-13 01:00:05.124094 +00:00,DailyTokenRefill,30
                           149308,2025-05-13 01:00:05.124094 +00:00,DailyTokenBurnout,-10
                           149941,2025-05-14 01:00:06.280860 +00:00,DailyTokenRefill,30
                           149942,2025-05-14 01:00:06.280860 +00:00,DailyTokenBurnout,-20
                           150641,2025-05-15 01:00:07.449294 +00:00,DailyTokenRefill,30
                           150642,2025-05-15 01:00:07.449294 +00:00,DailyTokenBurnout,-10
                           """;

        await dataEnv.ImportAssetStoreTransactionCsv(user.MainGroupId, transactions);

        var refillService = provider.GetRequiredService<IDailyTokenRefillService>();
        var balanceService = provider.GetRequiredService<IBalanceService>();
        await balanceService.CheckBalance(user, 110, 0, 0);

        await refillService.RefillDailyTokens(user.MainGroupId);

        await balanceService.CheckBalance(user, 30, 0, 0);
    }

    [Fact]
    public async Task DailyTokenRefill_BatchRefillShouldWork()
    {
        await using var provider = _services.BuildServiceProvider();
        var dataEnv = provider.GetRequiredService<DataEnvironment>();

        var systemGroup = (await dataEnv.WithSystemUserAndGroup()).First();

        await using var scope1 = provider.CreateAsyncScope();

        var user1 = await dataEnv.WithUserAndGroup(new UserAndGroupCreateParams());
        scope1.ServiceProvider.SetCurrentUser(user1);

        await using var scope2 = provider.CreateAsyncScope();
        var user2 = await dataEnv.WithUserAndGroup(new UserAndGroupCreateParams());
        scope2.ServiceProvider.SetCurrentUser(user2);

        var refillService2 = scope2.ServiceProvider.GetRequiredService<IDailyTokenRefillService>();

        await refillService2.BatchRefillDailyTokens(false);

        var balanceService = provider.GetRequiredService<IBalanceService>();

        await balanceService.CheckBalance(user1, 30, 0, 0);
        await balanceService.CheckBalance(user2, 30, 0, 0);
    }
}