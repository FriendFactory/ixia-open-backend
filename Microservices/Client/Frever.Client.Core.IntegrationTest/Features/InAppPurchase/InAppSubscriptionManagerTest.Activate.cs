using FluentAssertions;
using Frever.Client.Core.Features.InAppPurchases;
using Frever.Client.Core.Features.InAppPurchases.Subscriptions;
using Frever.Client.Core.IntegrationTest.Features.InAppPurchase.Data;
using Frever.Common.IntegrationTesting;
using Frever.Common.IntegrationTesting.Data;
using Frever.Common.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Frever.Client.Core.IntegrationTest.Features.InAppPurchase;

public partial class InAppSubscriptionManagerTest
{
    [Fact]
    public async Task ActivateSubscription_MustWork()
    {
        await using var provider = _services.BuildServiceProvider();
        var dataEnv = provider.GetRequiredService<DataEnvironment>();

        await dataEnv.WithSystemUserAndGroup();

        var user = await dataEnv.WithUserAndGroup(new UserAndGroupCreateParams());

        provider.SetCurrentUser(user);
        var balanceService = provider.GetRequiredService<IBalanceService>();

        var subscriptionManager = provider.GetRequiredService<IInAppSubscriptionManager>();

        var subscription = (await dataEnv.WithInAppProducts(
                                new InAppProductInput
                                {
                                    Title = "Fake subscription",
                                    IsSubscription = true,
                                    AppleProductRef = "apple_ref",
                                    DailyHardCurrency = 30,
                                    GoogleProductRef = "playmarket_ref",
                                    MonthlyHardCurrency = 1500
                                }
                            )).First();

        var purchase = await dataEnv.WithInAppPurchase(user.MainGroupId, subscription.Id, "testreceipt");

        await subscriptionManager.ActivateSubscription(purchase.Id, subscription.Id);

        var myBalance = await balanceService.GetBalance(user.MainGroupId);
        myBalance.SubscriptionTokens.Should().Be(1500);
        myBalance.PermanentTokens.Should().Be(0);
        myBalance.DailyTokens.Should().Be(0);

        var balanceAfterRefill = await subscriptionManager.RenewSubscriptionTokens();
        balanceAfterRefill.SubscriptionTokens.Should().Be(1500);
        balanceAfterRefill.DailyTokens.Should().Be(0);
        balanceAfterRefill.PermanentTokens.Should().Be(0);
        balanceAfterRefill.NextSubscriptionTokenRefresh.Should().Be(DateTime.Now.AddDays(30 + 1).Date);
        balanceAfterRefill.ActiveSubscription.Should().Be("Fake subscription");

        // Second refill should not update monthly token balance
        var balance2AfterRefill = await subscriptionManager.RenewSubscriptionTokens();
        balance2AfterRefill.SubscriptionTokens.Should().Be(1500, "balance should not be changed until next refill period");
        balance2AfterRefill.DailyTokens.Should().Be(0);
        balance2AfterRefill.PermanentTokens.Should().Be(0);
        balance2AfterRefill.NextSubscriptionTokenRefresh.Should().Be(DateTime.Now.AddDays(30 + 1).Date);
        balanceAfterRefill.ActiveSubscription.Should().Be("Fake subscription");
    }

    [Fact]
    public async Task ActivateSubscription_UpgradeSubscriptionMustGrantAdditionalTokens()
    {
        await using var provider = _services.BuildServiceProvider();
        var dataEnv = provider.GetRequiredService<DataEnvironment>();

        await dataEnv.WithSystemUserAndGroup();

        var user = await dataEnv.WithUserAndGroup(new UserAndGroupCreateParams());

        provider.SetCurrentUser(user);
        var balanceService = provider.GetRequiredService<IBalanceService>();

        var subscriptionManager = provider.GetRequiredService<IInAppSubscriptionManager>();

        var subscriptionLow = (await dataEnv.WithInAppProducts(
                                   new InAppProductInput
                                   {
                                       Title = "Low subscription",
                                       IsSubscription = true,
                                       AppleProductRef = "apple_ref1",
                                       DailyHardCurrency = 30,
                                       GoogleProductRef = "playmarket_ref1",
                                       MonthlyHardCurrency = 1500
                                   }
                               )).First();

        var subscriptionHigh = (await dataEnv.WithInAppProducts(
                                    new InAppProductInput
                                    {
                                        Title = "High subscription",
                                        IsSubscription = true,
                                        AppleProductRef = "apple_ref2",
                                        DailyHardCurrency = 30,
                                        GoogleProductRef = "playmarket_ref2",
                                        MonthlyHardCurrency = 3000
                                    }
                                )).First();

        var purchase = await dataEnv.WithInAppPurchase(user.MainGroupId, subscriptionLow.Id, "testreceipt");

        await subscriptionManager.ActivateSubscription(purchase.Id, subscriptionLow.Id);

        await balanceService.CheckBalance(user, 0, 1500, 0);

        var purchase2 = await dataEnv.WithInAppPurchase(user.MainGroupId, subscriptionHigh.Id, "testreceipt2");
        await subscriptionManager.ActivateSubscription(purchase2.Id, subscriptionHigh.Id);
        await balanceService.CheckBalance(user, 0, 3000 + 1500, 0);

        var activeSubscription = await dataEnv.Db.InAppUserSubscription
                                              .Where(
                                                   s => s.GroupId == user.MainGroupId &&
                                                        (s.CompletedAt == null || s.CompletedAt > DateTime.UtcNow)
                                               )
                                              .ToArrayAsync();
        activeSubscription.Should().HaveCount(1);
    }

    [Fact]
    public async Task ActivateSubscription_DowngradeSubscriptionMustNotGrantExtraTokens()
    {
        await using var provider = _services.BuildServiceProvider();
        var dataEnv = provider.GetRequiredService<DataEnvironment>();

        await dataEnv.WithSystemUserAndGroup();

        var user = await dataEnv.WithUserAndGroup(new UserAndGroupCreateParams());

        provider.SetCurrentUser(user);
        var balanceService = provider.GetRequiredService<IBalanceService>();

        var subscriptionManager = provider.GetRequiredService<IInAppSubscriptionManager>();

        var subscriptionLow = (await dataEnv.WithInAppProducts(
                                   new InAppProductInput
                                   {
                                       Title = "Low subscription",
                                       IsSubscription = true,
                                       AppleProductRef = "apple_ref1",
                                       DailyHardCurrency = 30,
                                       GoogleProductRef = "playmarket_ref1",
                                       MonthlyHardCurrency = 1500
                                   }
                               )).First();

        var subscriptionHigh = (await dataEnv.WithInAppProducts(
                                    new InAppProductInput
                                    {
                                        Title = "High subscription",
                                        IsSubscription = true,
                                        AppleProductRef = "apple_ref2",
                                        DailyHardCurrency = 30,
                                        GoogleProductRef = "playmarket_ref2",
                                        MonthlyHardCurrency = 3000
                                    }
                                )).First();

        var purchase = await dataEnv.WithInAppPurchase(user.MainGroupId, subscriptionHigh.Id, "testreceipt");
        await subscriptionManager.ActivateSubscription(purchase.Id, subscriptionHigh.Id);

        await balanceService.CheckBalance(user, 0, 3000, 0);

        var purchase2 = await dataEnv.WithInAppPurchase(user.MainGroupId, subscriptionLow.Id, "testreceipt2");
        await subscriptionManager.ActivateSubscription(purchase2.Id, subscriptionLow.Id);
        await balanceService.CheckBalance(user, 0, 3000, 0);

        var activeSubscription = await dataEnv.Db.InAppUserSubscription
                                              .Where(
                                                   s => s.GroupId == user.MainGroupId &&
                                                        (s.CompletedAt == null || s.CompletedAt > DateTime.UtcNow)
                                               )
                                              .ToArrayAsync();
        activeSubscription.Should().HaveCount(1);
    }

    [Fact]
    public async Task ActivateSubscription_ShouldDoNothingOnSubscribingToTheSameProduct()
    {
        await using var provider = _services.BuildServiceProvider();
        var dataEnv = provider.GetRequiredService<DataEnvironment>();

        await dataEnv.WithSystemUserAndGroup();

        var user = await dataEnv.WithUserAndGroup(new UserAndGroupCreateParams());

        provider.SetCurrentUser(user);
        var balanceService = provider.GetRequiredService<IBalanceService>();

        var subscriptionManager = provider.GetRequiredService<IInAppSubscriptionManager>();

        var subscription = (await dataEnv.WithInAppProducts(
                                new InAppProductInput
                                {
                                    Title = "Low subscription",
                                    IsSubscription = true,
                                    AppleProductRef = "apple_ref1",
                                    DailyHardCurrency = 30,
                                    GoogleProductRef = "playmarket_ref1",
                                    MonthlyHardCurrency = 1500
                                }
                            )).First();

        var purchase = await dataEnv.WithInAppPurchase(user.MainGroupId, subscription.Id, "testreceipt");
        await subscriptionManager.ActivateSubscription(purchase.Id, subscription.Id);

        await balanceService.CheckBalance(user, 0, 1500, 0);

        var subscriptionCountBefore = await dataEnv.Db.InAppUserSubscription.CountAsync();

        var purchase2 = await dataEnv.WithInAppPurchase(user.MainGroupId, subscription.Id, "testreceipt2");
        await subscriptionManager.ActivateSubscription(purchase2.Id, subscription.Id);
        await balanceService.CheckBalance(user, 0, 1500, 0);

        var subscriptionCountAfter = await dataEnv.Db.InAppUserSubscription.CountAsync();

        subscriptionCountBefore.Should().Be(subscriptionCountAfter);
    }
}