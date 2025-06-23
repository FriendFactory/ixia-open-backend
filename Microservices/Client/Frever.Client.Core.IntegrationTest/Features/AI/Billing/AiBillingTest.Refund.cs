using FluentAssertions;
using Frever.Client.Core.Features.Social.MyProfileInfo;
using Frever.Client.Core.IntegrationTest.Features.AI.Billing.Data;
using Frever.Client.Core.IntegrationTest.Utils;
using Frever.Client.Shared.AI.Billing;
using Frever.Client.Shared.AI.Metadata;
using Frever.Common.IntegrationTesting;
using Frever.Common.IntegrationTesting.Data;
using Frever.Common.Testing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Frever.Client.Core.IntegrationTest.Features.AI.Billing;

public partial class AiBillingTest
{
    [Fact]
    public async Task RefundAiWorkflowRun_MustWork()
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

        await service.TryPurchaseAiWorkflowRun(prices[0].AiWorkflow, null, aiContentId, prices[0].RequireBillingUnits ? 1 : null);

        var myProfileService = provider.GetRequiredService<IMyProfileService>();
        var actualBalance = await myProfileService.GetMyBalance();
        actualBalance.HardCurrencyAmount.Should().Be(initialBalance - prices[0].HardCurrencyPrice);

        await service.RefundAiWorkflowRun(aiContentId);
        var actualBalanceAfterRefund = await myProfileService.GetMyBalance();
        actualBalanceAfterRefund.HardCurrencyAmount.Should().Be(initialBalance);
    }

    [Fact]
    public async Task RefundAiWorkflowRun_MustIgnoreSecondRefundOrIfNothingToRefund()
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

        await service.TryPurchaseAiWorkflowRun(prices[0].AiWorkflow, null, aiContentId, prices[0].RequireBillingUnits ? 1 : null);

        var myProfileService = provider.GetRequiredService<IMyProfileService>();
        var actualBalance = await myProfileService.GetMyBalance();
        actualBalance.HardCurrencyAmount.Should().Be(initialBalance - prices[0].HardCurrencyPrice);

        await service.RefundAiWorkflowRun(aiContentId);
        actualBalance = await myProfileService.GetMyBalance();
        actualBalance.HardCurrencyAmount.Should().Be(initialBalance);

        await service.RefundAiWorkflowRun(aiContentId);
        actualBalance = await myProfileService.GetMyBalance();
        actualBalance.HardCurrencyAmount.Should().Be(initialBalance);
    }
}