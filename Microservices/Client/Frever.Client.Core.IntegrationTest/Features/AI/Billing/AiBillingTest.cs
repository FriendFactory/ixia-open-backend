using FluentAssertions;
using Frever.Client.Core.Features.Social.MyProfileInfo;
using Frever.Client.Core.IntegrationTest.Features.AI.Billing.Data;
using Frever.Client.Core.IntegrationTest.Utils;
using Frever.Client.Shared.AI.Billing;
using Frever.Client.Shared.AI.Metadata;
using Frever.Common.IntegrationTesting;
using Frever.Common.IntegrationTesting.Data;
using Frever.Common.Testing;
using Frever.Shared.MainDb.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;

namespace Frever.Client.Core.IntegrationTest.Features.AI.Billing;

public partial class AiBillingTest(ITestOutputHelper testOut) : IntegrationTestBase
{
    [Fact]
    public async Task GetWorkflowPrices_MustWork()
    {
        var services = new ServiceCollection();
        services.AddClientIntegrationTests(testOut);

        await using var provider = services.BuildServiceProvider();

        var dataEnv = provider.GetRequiredService<DataEnvironment>();

        await dataEnv.WithSystemUserAndGroup();

        var user = await dataEnv.WithUserAndGroup(new UserAndGroupCreateParams {CountryIso3 = "swe", LanguageIso3 = "swe"});
        provider.SetCurrentUser(user);

        var prices = await dataEnv.WithAiWorkflowPrices();

        var service = provider.GetRequiredService<IAiWorkflowMetadataService>();

        var actualPrices = await service.GetInternal();
        var expectedPrices = prices.Where(p => p.IsActive).ToArray();

        actualPrices.Should().HaveSameCount(expectedPrices);
        foreach (var actual in actualPrices)
        {
            var expected = expectedPrices.FirstOrDefault(e => e.Id == actual.Id);
            expected.Should().NotBeNull();

            expected.AiWorkflow.Should().Be(actual.AiWorkflow);
            expected.Description.Should().Be(actual.Description);
            expected.HardCurrencyPrice.Should().Be(actual.HardCurrencyPrice);
            expected.RequireBillingUnits.Should().Be(actual.RequireBillingUnits);
        }
    }

    [Fact]
    public async Task TryPurchaseAiWorkflowRun_MustWorkIfUserHasEnoughBalance()
    {
        var services = new ServiceCollection();
        services.AddClientIntegrationTests(testOut);

        await using var provider = services.BuildServiceProvider();

        var dataEnv = provider.GetRequiredService<DataEnvironment>();
        await dataEnv.WithSystemUserAndGroup();

        var user = await dataEnv.WithUserAndGroup(new UserAndGroupCreateParams {CountryIso3 = "swe", LanguageIso3 = "swe"});
        provider.SetCurrentUser(user);

        const int aiContentId = 1;
        const int initialBalance = 100;

        await dataEnv.WithInitialBalance(user.MainGroupId, initialBalance);
        await dataEnv.WithAiWorkflowPrices();

        var prices = await provider.GetRequiredService<IAiWorkflowMetadataService>().GetInternal();

        var service = provider.GetRequiredService<IAiBillingService>();

        var result = await service.TryPurchaseAiWorkflowRun(
                         prices[0].AiWorkflow,
                         null,
                         aiContentId,
                         prices[0].RequireBillingUnits ? 1 : null
                     );
        result.Should().BeTrue();

        var myProfileService = provider.GetRequiredService<IMyProfileService>();
        var actualBalance = await myProfileService.GetMyBalance();
        actualBalance.HardCurrencyAmount.Should().Be(initialBalance - prices[0].HardCurrencyPrice);

        var transaction = await dataEnv.Db.AssetStoreTransactions.FirstOrDefaultAsync(t => t.EntityRefId == aiContentId);
        transaction.Should().NotBeNull();
        transaction.AiWorkflow.Should().Be(prices[0].AiWorkflow);
        if (prices[0].RequireBillingUnits)
            transaction.AiWorkflowBillingUnits.Should().Be(1);
        else
            transaction.AiWorkflowBillingUnits.Should().BeNull();

        transaction.TransactionType.Should().Be(AssetStoreTransactionType.AiWorkflowRun);
        transaction.GroupId.Should().Be(user.MainGroupId);
        transaction.HardCurrencyAmount.Should().Be(-prices[0].HardCurrencyPrice);

        var group = await dataEnv.Db.AssetStoreTransactions.Where(t => t.TransactionGroup == transaction.TransactionGroup).ToArrayAsync();
        group.Should().HaveCount(2);
        group.Sum(t => t.HardCurrencyAmount).Should().Be(0);
    }

    [Fact]
    public async Task TryPurchaseAiWorkflowRun_MustWorkForPricesWithUnits()
    {
        var services = new ServiceCollection();
        services.AddClientIntegrationTests(testOut);

        await using var provider = services.BuildServiceProvider();

        var dataEnv = provider.GetRequiredService<DataEnvironment>();
        await dataEnv.WithSystemUserAndGroup();

        var user = await dataEnv.WithUserAndGroup(new UserAndGroupCreateParams {CountryIso3 = "swe", LanguageIso3 = "swe"});
        provider.SetCurrentUser(user);

        const int aiContentId = 1;
        const int initialBalance = 100;

        await dataEnv.WithInitialBalance(user.MainGroupId, initialBalance);
        await dataEnv.WithAiWorkflowPrices();

        var prices = await provider.GetRequiredService<IAiWorkflowMetadataService>().GetInternal();
        var priceWithUnits = prices.FirstOrDefault(p => p.RequireBillingUnits);
        priceWithUnits.Should().NotBeNull();
        const int numOfUnits = 3;

        var service = provider.GetRequiredService<IAiBillingService>();

        var result = await service.TryPurchaseAiWorkflowRun(priceWithUnits!.AiWorkflow, null, aiContentId, numOfUnits);
        result.Should().BeTrue();

        var myProfileService = provider.GetRequiredService<IMyProfileService>();
        var actualBalance = await myProfileService.GetMyBalance();
        actualBalance.HardCurrencyAmount.Should().Be(initialBalance - priceWithUnits.HardCurrencyPrice * numOfUnits);

        var transaction = await dataEnv.Db.AssetStoreTransactions.FirstOrDefaultAsync(t => t.EntityRefId == aiContentId);
        transaction.Should().NotBeNull();
        transaction.AiWorkflow.Should().Be(priceWithUnits.AiWorkflow);
        transaction.AiWorkflowBillingUnits.Should().Be(numOfUnits);
        transaction.TransactionType.Should().Be(AssetStoreTransactionType.AiWorkflowRun);
        transaction.GroupId.Should().Be(user.MainGroupId);
        transaction.HardCurrencyAmount.Should().Be(-priceWithUnits.HardCurrencyPrice * numOfUnits);

        var group = await dataEnv.Db.AssetStoreTransactions.Where(t => t.TransactionGroup == transaction.TransactionGroup).ToArrayAsync();
        group.Should().HaveCount(2);
        group.Sum(t => t.HardCurrencyAmount).Should().Be(0);
    }

    [Fact]
    public async Task TryPurchaseAiWorkflowRun_MustFailIfNotEnoughBalance()
    {
        var services = new ServiceCollection();
        services.AddClientIntegrationTests(testOut);

        await using var provider = services.BuildServiceProvider();

        var dataEnv = provider.GetRequiredService<DataEnvironment>();
        await dataEnv.WithSystemUserAndGroup();

        const int aiContentId = 1;

        var user = await dataEnv.WithUserAndGroup(new UserAndGroupCreateParams {CountryIso3 = "swe", LanguageIso3 = "swe"});
        provider.SetCurrentUser(user);

        await dataEnv.WithInitialBalance(user.MainGroupId, 2);
        await dataEnv.WithAiWorkflowPrices();

        var prices = await provider.GetRequiredService<IAiWorkflowMetadataService>().GetInternal();

        var service = provider.GetRequiredService<IAiBillingService>();

        await FluentActions.Invoking(async () => await service.TryPurchaseAiWorkflowRun(
                                                     prices[0].AiWorkflow,
                                                     null,
                                                     aiContentId,
                                                     prices[0].RequireBillingUnits ? 1 : null
                                                 )
                            )
                           .Should()
                           .ThrowAsync<Exception>();

        var myProfileService = provider.GetRequiredService<IMyProfileService>();
        var actualBalance = await myProfileService.GetMyBalance();
        actualBalance.HardCurrencyAmount.Should().Be(2);
    }

    [Fact]
    public async Task TryPurchaseAiWorkflowRun_MustNotChargeWorkflowWithoutPrice()
    {
        var services = new ServiceCollection();
        services.AddClientIntegrationTests(testOut);

        await using var provider = services.BuildServiceProvider();

        var dataEnv = provider.GetRequiredService<DataEnvironment>();
        await dataEnv.WithSystemUserAndGroup();

        const int aiContentId = 1;

        var user = await dataEnv.WithUserAndGroup(new UserAndGroupCreateParams {CountryIso3 = "swe", LanguageIso3 = "swe"});
        provider.SetCurrentUser(user);

        await dataEnv.WithInitialBalance(user.MainGroupId, 100);
        await dataEnv.WithAiWorkflowPrices();

        var service = provider.GetRequiredService<IAiBillingService>();

        var result = await service.TryPurchaseAiWorkflowRun(Guid.NewGuid().ToString("N"), null, aiContentId, null);

        result.Should().BeFalse();

        var myProfileService = provider.GetRequiredService<IMyProfileService>();
        var actualBalance = await myProfileService.GetMyBalance();
        actualBalance.HardCurrencyAmount.Should().Be(100);
    }
}