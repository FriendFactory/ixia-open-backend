using FluentAssertions;
using Frever.Client.Core.Features.InAppPurchases;
using Frever.Client.Core.Features.InAppPurchases.InAppPurchase;
using Frever.Client.Core.Features.InAppPurchases.Subscriptions;
using Frever.Client.Core.IntegrationTest.Features.InAppPurchase.Data;
using Frever.Client.Core.IntegrationTest.Utils;
using Frever.Common.IntegrationTesting;
using Frever.Common.IntegrationTesting.Data;
using Frever.Common.Testing;
using Frever.Shared.MainDb.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;
using Xunit.Abstractions;
using Platform = Frever.ClientService.Contract.Common.Platform;

namespace Frever.Client.Core.IntegrationTest.Features.InAppPurchase;

public partial class InAppSubscriptionManagerTest : IntegrationTestBase
{
    private readonly Mock<IStoreTransactionDataValidator> _receiptValidator;
    private readonly IServiceCollection _services;

    public InAppSubscriptionManagerTest(ITestOutputHelper testOut)
    {
        _services = new ServiceCollection();
        _services.AddClientIntegrationTests(testOut);

        _receiptValidator = new Mock<IStoreTransactionDataValidator>();

        _receiptValidator.Setup(v => v.ValidateSubscription(It.IsAny<Platform>(), It.IsAny<string>()))
                         .ReturnsAsync(new SubscriptionValidationResult {IsActive = true,});

        _services.AddScoped(_ => _receiptValidator.Object);
    }

    [Fact]
    public async Task RenewSubscriptionTokens_MustWork()
    {
        await using var provider = _services.BuildServiceProvider();
        var dataEnv = provider.GetRequiredService<DataEnvironment>();

        await dataEnv.WithSystemUserAndGroup();

        var inAppProduct = (await dataEnv.WithInAppProducts(
                                new InAppProductInput
                                {
                                    Title = "test_subscription",
                                    Details = [],
                                    AppleProductRef = "apple_ref",
                                    GoogleProductRef = "play_ref",
                                    IsSubscription = true,
                                    MonthlyHardCurrency = 1500,
                                    DailyHardCurrency = 30
                                }
                            )).First();

        var user = await dataEnv.WithUserAndGroup(new UserAndGroupCreateParams());

        provider.SetCurrentUser(user);
        var balanceService = provider.GetRequiredService<IBalanceService>();

        var subscriptionManager = provider.GetRequiredService<IInAppSubscriptionManager>();

        await dataEnv.WithInAppUserSubscription(
            user.MainGroupId,
            DateTime.Now.AddDays(-10),
            DateTime.Now.AddDays(100),
            30,
            1500,
            inAppProduct.Id
        );

        var myBalance = await balanceService.GetBalance(user.MainGroupId);
        myBalance.SubscriptionTokens.Should().Be(0);
        myBalance.PermanentTokens.Should().Be(0);
        myBalance.DailyTokens.Should().Be(0);

        var balanceAfterRefill = await subscriptionManager.RenewSubscriptionTokens();
        balanceAfterRefill.SubscriptionTokens.Should().Be(1500);
        balanceAfterRefill.DailyTokens.Should().Be(0);
        balanceAfterRefill.PermanentTokens.Should().Be(0);
        balanceAfterRefill.NextSubscriptionTokenRefresh.Should().Be(DateTime.Now.AddDays(20 + 1).Date);
        balanceAfterRefill.MaxDailyTokens.Should().Be(30);
        balanceAfterRefill.MaxSubscriptionTokens.Should().Be(1500);

        // Second refill should not update monthly token balance
        var balance2AfterRefill = await subscriptionManager.RenewSubscriptionTokens();
        balance2AfterRefill.SubscriptionTokens.Should().Be(1500, "balance should not be changed until next refill period");
        balance2AfterRefill.DailyTokens.Should().Be(0);
        balance2AfterRefill.PermanentTokens.Should().Be(0);
        balance2AfterRefill.NextSubscriptionTokenRefresh.Should().Be(DateTime.Now.AddDays(20 + 1).Date);
        balance2AfterRefill.MaxDailyTokens.Should().Be(30);
        balance2AfterRefill.MaxSubscriptionTokens.Should().Be(1500);
    }

    [Fact]
    public async Task RenewSubscriptionTokens_MustNotFailIfNoSubscription()
    {
        await using var provider = _services.BuildServiceProvider();
        var dataEnv = provider.GetRequiredService<DataEnvironment>();

        await dataEnv.WithSystemUserAndGroup();

        var user = await dataEnv.WithUserAndGroup(new UserAndGroupCreateParams());

        provider.SetCurrentUser(user);
        var balanceService = provider.GetRequiredService<IBalanceService>();

        var subscriptionManager = provider.GetRequiredService<IInAppSubscriptionManager>();

        var myBalance = await balanceService.GetBalance(user.MainGroupId);
        myBalance.SubscriptionTokens.Should().Be(0);
        myBalance.PermanentTokens.Should().Be(0);
        myBalance.DailyTokens.Should().Be(0);

        var balanceAfterRefill = await subscriptionManager.RenewSubscriptionTokens();
        balanceAfterRefill.SubscriptionTokens.Should().Be(0);
        balanceAfterRefill.DailyTokens.Should().Be(0);
        balanceAfterRefill.PermanentTokens.Should().Be(0);
        balanceAfterRefill.NextSubscriptionTokenRefresh.Should().Be(null);

        // Second refill should not update monthly token balance
        var balance2AfterRefill = await subscriptionManager.RenewSubscriptionTokens();
        balance2AfterRefill.SubscriptionTokens.Should().Be(0, "should not refill in consequtive calls if no subscription");
        balance2AfterRefill.DailyTokens.Should().Be(0);
        balance2AfterRefill.PermanentTokens.Should().Be(0);
        balance2AfterRefill.NextSubscriptionTokenRefresh.Should().Be(null);
    }


    [Fact]
    public async Task RenewSubscriptionTokens_MustRefillPartialAmount()
    {
        await using var provider = _services.BuildServiceProvider();
        var dataEnv = provider.GetRequiredService<DataEnvironment>();

        await dataEnv.WithSystemUserAndGroup();

        var inAppProduct = (await dataEnv.WithInAppProducts(
                                new InAppProductInput
                                {
                                    Title = "test_subscription",
                                    Details = [],
                                    AppleProductRef = "apple_ref",
                                    GoogleProductRef = "play_ref",
                                    IsSubscription = true,
                                    MonthlyHardCurrency = 1500,
                                    DailyHardCurrency = 30
                                }
                            )).First();

        var user = await dataEnv.WithUserAndGroup(new UserAndGroupCreateParams());

        provider.SetCurrentUser(user);
        var balanceService = provider.GetRequiredService<IBalanceService>();

        var subscriptionManager = provider.GetRequiredService<IInAppSubscriptionManager>();

        await dataEnv.WithInAppUserSubscription(
            user.MainGroupId,
            DateTime.Now.AddDays(-100),
            DateTime.Now.AddDays(20),
            30,
            1500,
            inAppProduct.Id
        );

        var t = new AssetStoreTransactionGenerator(dataEnv, user.MainGroupId);
        await t.RefillMonthly(1500, DateTime.Now.AddDays(-99));
        await t.BurnoutMonthly(-1500, DateTime.Now.AddDays(-55));
        await t.RefillMonthly(1500, DateTime.Now.AddDays(-55));

        await t.RunWorkflow(-800, DateTime.Now.AddDays(-2));

        var myBalance = await balanceService.GetBalance(user.MainGroupId);
        myBalance.SubscriptionTokens.Should().Be(700);
        myBalance.PermanentTokens.Should().Be(0);
        myBalance.DailyTokens.Should().Be(0);

        var balanceAfterRefill = await subscriptionManager.RenewSubscriptionTokens();
        balanceAfterRefill.SubscriptionTokens.Should().Be(1500);
        balanceAfterRefill.DailyTokens.Should().Be(0);
        balanceAfterRefill.PermanentTokens.Should().Be(0);

        // Second refill should not update monthly token balance
        var balance2AfterRefill = await subscriptionManager.RenewSubscriptionTokens();
        balance2AfterRefill.SubscriptionTokens.Should().Be(1500, "balance should not be changed until next refill period");
        balance2AfterRefill.DailyTokens.Should().Be(0);
        balance2AfterRefill.PermanentTokens.Should().Be(0);
    }

    [Fact]
    public async Task RenewSubscriptionTokens_MustNotRefillCompletedSubscription()
    {
        await using var provider = _services.BuildServiceProvider();
        var dataEnv = provider.GetRequiredService<DataEnvironment>();

        await dataEnv.WithSystemUserAndGroup();

        var inAppProduct = (await dataEnv.WithInAppProducts(
                                new InAppProductInput
                                {
                                    Title = "test_subscription",
                                    Details = [],
                                    AppleProductRef = "apple_ref",
                                    GoogleProductRef = "play_ref",
                                    IsSubscription = true,
                                    MonthlyHardCurrency = 1500,
                                    DailyHardCurrency = 30
                                }
                            )).First();

        var user = await dataEnv.WithUserAndGroup(new UserAndGroupCreateParams());

        provider.SetCurrentUser(user);
        var balanceService = provider.GetRequiredService<IBalanceService>();

        var subscriptionManager = provider.GetRequiredService<IInAppSubscriptionManager>();

        var subscription = await dataEnv.WithInAppUserSubscription(
                               user.MainGroupId,
                               DateTime.Now.AddDays(-100),
                               DateTime.Now.AddDays(-10),
                               30,
                               1500,
                               inAppProduct.Id
                           );

        var t = new AssetStoreTransactionGenerator(dataEnv, user.MainGroupId);
        await t.RefillMonthly(1500, DateTime.Now.AddDays(-99));
        await t.BurnoutMonthly(-1500, DateTime.Now.AddDays(-55));
        await t.RefillMonthly(1500, DateTime.Now.AddDays(-55));

        await t.RunWorkflow(-800, DateTime.Now.AddDays(-2));

        var myBalance = await balanceService.GetBalance(user.MainGroupId);
        myBalance.SubscriptionTokens.Should().Be(700);
        myBalance.PermanentTokens.Should().Be(0);
        myBalance.DailyTokens.Should().Be(0);

        var balanceAfterRefill = await subscriptionManager.RenewSubscriptionTokens();
        balanceAfterRefill.SubscriptionTokens.Should().Be(0, "subscription complete");
        balanceAfterRefill.DailyTokens.Should().Be(0);
        balanceAfterRefill.PermanentTokens.Should().Be(0);
        balanceAfterRefill.NextSubscriptionTokenRefresh.Should().BeNull("subscription complete");

        // Second refill should not update monthly token balance
        var balance2AfterRefill = await subscriptionManager.RenewSubscriptionTokens();
        balance2AfterRefill.SubscriptionTokens.Should().Be(0, "subscription is complete");
        balance2AfterRefill.DailyTokens.Should().Be(0);
        balance2AfterRefill.PermanentTokens.Should().Be(0);
        balance2AfterRefill.NextSubscriptionTokenRefresh.Should().BeNull();
    }

    [Fact]
    public async Task RenewSubscriptionTokens_MustNotRefillIfReceiptValidationFailed()
    {
        await using var provider = _services.BuildServiceProvider();
        var dataEnv = provider.GetRequiredService<DataEnvironment>();

        await dataEnv.WithSystemUserAndGroup();

        var inAppProduct = (await dataEnv.WithInAppProducts(
                                new InAppProductInput
                                {
                                    Title = "test_subscription",
                                    Details = [],
                                    AppleProductRef = "apple_ref",
                                    GoogleProductRef = "play_ref",
                                    IsSubscription = true,
                                    MonthlyHardCurrency = 1500,
                                    DailyHardCurrency = 30
                                }
                            )).First();

        var user = await dataEnv.WithUserAndGroup(new UserAndGroupCreateParams());

        provider.SetCurrentUser(user);
        var balanceService = provider.GetRequiredService<IBalanceService>();

        var subscriptionManager = provider.GetRequiredService<IInAppSubscriptionManager>();

        var subscription = await dataEnv.WithInAppUserSubscription(
                               user.MainGroupId,
                               DateTime.Now.AddDays(-100),
                               DateTime.Now.AddDays(30),
                               30,
                               1500,
                               inAppProduct.Id
                           );

        _receiptValidator.Setup(v => v.ValidateSubscription(It.IsAny<Platform>(), It.IsAny<string>()))
                         .ReturnsAsync(new SubscriptionValidationResult {IsActive = false});

        var t = new AssetStoreTransactionGenerator(dataEnv, user.MainGroupId);
        await t.RefillMonthly(1500, DateTime.Now.AddDays(-99));
        await t.BurnoutMonthly(-1500, DateTime.Now.AddDays(-55));
        await t.RefillMonthly(1500, DateTime.Now.AddDays(-55));

        await t.RunWorkflow(-800, DateTime.Now.AddDays(-2));

        var myBalance = await balanceService.GetBalance(user.MainGroupId);
        myBalance.SubscriptionTokens.Should().Be(700);
        myBalance.PermanentTokens.Should().Be(0);
        myBalance.DailyTokens.Should().Be(0);

        var balanceAfterRefill = await subscriptionManager.RenewSubscriptionTokens();
        balanceAfterRefill.SubscriptionTokens.Should().Be(0, "subscription complete");
        balanceAfterRefill.DailyTokens.Should().Be(0);
        balanceAfterRefill.PermanentTokens.Should().Be(0);
        balanceAfterRefill.NextSubscriptionTokenRefresh.Should().BeNull("subscription complete");

        // Second refill should not update monthly token balance
        var balance2AfterRefill = await subscriptionManager.RenewSubscriptionTokens();
        balance2AfterRefill.SubscriptionTokens.Should().Be(0, "subscription is complete");
        balance2AfterRefill.DailyTokens.Should().Be(0);
        balance2AfterRefill.PermanentTokens.Should().Be(0);
        balance2AfterRefill.NextSubscriptionTokenRefresh.Should().BeNull();

        var savedSubscription =
            await dataEnv.Db.InAppUserSubscription.Where(s => s.Id == subscription.Id).AsNoTracking().FirstOrDefaultAsync();

        savedSubscription.CompletedAt.Should().NotBeNull();
        (savedSubscription.CompletedAt.Value < DateTime.UtcNow).Should().BeTrue();
        savedSubscription.Status.Should().Be(InAppUserSubscription.KnownStatusComplete);
    }
}